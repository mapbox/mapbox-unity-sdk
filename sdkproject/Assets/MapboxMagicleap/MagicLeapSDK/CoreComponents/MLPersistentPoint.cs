// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
//
// Copyright (c) 2018 Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Creator Agreement, located
// here: https://id.magicleap.com/creator-terms
//
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.XR.MagicLeap
{
    /// <summary>
    /// This is a component you can use to make a specific game object a persistent
    /// anchor/point in space. This component would try to restore itself on Start and
    /// will notify the listener if it's restored correctly or not. If this is the first time
    /// it would automatically look for the right real world PCF to attach itself to. You can simply put
    /// your content you want to persist under the game object with this behavior attached to it.
    /// PLEASE NOTE: Once the PersistentPoint is found /restored it's transform is locked. You
    /// cannot move this persistent object at all.
    /// </summary>
    public class MLPersistentPoint : MonoBehaviour
    {
        #region public vars        
        /// <summary>
        /// Every persistent point in your project must have a unique Id
        /// </summary>

        [Tooltip("Unique id for this persistent point. If not provided the name of the GameObject would be used")]
        public string UniqueId;

        /// <summary>
        /// This event is raised when the persistent point is ready and available.
        /// </summary>
        public event System.Action OnAvailable;

        /// <summary>
        /// This event happens when there are errors.
        /// </summary>
        public event System.Action<MLResult> OnError;

        /// <summary>
        /// Gets the binding.
        /// </summary>
        /// <value>The binding.</value>
        public MLContentBinding Binding { get; private set; }

        /// <summary>
        /// The max real world PCFs to bind to. Tweak this number to control the
        /// number of neighboring PCfs to attach the persitent point to.
        /// Higher the number the more resilient it gets but you pay more cost in storage space
        /// </summary>
        [Tooltip("The max real world PCFs to bind to. Higher the number the more resilient it gets but you pay more cost in storage space")]
        public int MaxPCFsToBindTo = 3;

        #endregion
        #region private variables and types
        /// <summary>
        /// State.
        /// </summary>
        enum State
        {
            Unknown,
            RestoreBinding,
            BindToAllPCFs,
            BindingComplete,
            Locked
        }

        /// <summary>
        /// Represents the current state or restoration/binding
        /// </summary>
        private State _state = State.Unknown;

        /// <summary>
        /// locked transform
        /// </summary>
        private Transform _lockedTransform;

        private List<MLPCF> _allPCFs;

        /// <summary>
        /// Requests MLPrivilegeId.PwFoundObjRea privilege.
        /// Must be set before MLPersistentPoint.Awake.
        /// </summary>
        [Tooltip("Requests persistence privilege")]
        public PrivilegeRequester PrivilegeRequester;
        #endregion

        #region functions

        // Using Awake so that Privileges is set before PrivilegeRequester Start
        void Awake()
        {
            if (PrivilegeRequester == null)
            {
                Debug.LogError("PrivilegeRequester not assigned");
                enabled = false;
                return;
            }

            // Could have also been set via the editor.
            PrivilegeRequester.Privileges = new[] { MLRuntimeRequestPrivilegeId.PwFoundObjRead };

            PrivilegeRequester.OnPrivilegesDone += HandlePrivilegesDone;
        }

        /// <summary>
        /// Tries to restore the binding or find closest PCF. Note various errors
        /// can be shown during this step based on the state of the low level systems.
        /// </summary>
        void Start()
        {
            SetChildrenActive(false);
            _lockedTransform = gameObject.transform;

            if (string.IsNullOrEmpty(UniqueId))
            {
                Debug.LogWarning("Unique Id is empty will try to use game object's name. It's good to provide a unique id for virtual objects to avoid weird behavior.");
                if (string.IsNullOrEmpty(gameObject.name))
                {
                    SetError(new MLResult(MLResultCode.UnspecifiedFailure, "Either UniqueId or name should be non empty. Disabling component"));
                    enabled = false;
                    return;
                }
                UniqueId = gameObject.name;
            }
            else
            {
                gameObject.name = UniqueId;
            }
        }

        ///<summary>
        /// Starts the restoration process.
        /// </summary>
        void StartRestore()
        {
            MLResult result = MLPersistentStore.Start();
            if (!result.IsOk)
            {
                SetError(result);
                enabled = false;
                return;
            }

            result = MLPersistentCoordinateFrames.Start();
            if (!result.IsOk)
            {
                MLPersistentStore.Stop();
                SetError(result);
                enabled = false;
                return;
            }

            result = MLPersistentCoordinateFrames.GetAllPCFs(out _allPCFs, MaxPCFsToBindTo);
            if (!result.IsOk)
            {
                MLPersistentStore.Stop();
                MLPersistentCoordinateFrames.Stop();
                SetError(result);
                enabled = false;
                return;
            }

            StartCoroutine(TryRestoreBinding());
        }
        /// <summary>
        /// Sets the children active.
        /// </summary>
        /// <param name="active">If set to <c>true</c> active.</param>
        void SetChildrenActive(bool active)
        {
            for (int i = 0; i < transform.childCount; ++i)
            {
                transform.GetChild(i).gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// Utility function that shows the error and also raises the OnErrorEvent
        /// </summary>
        /// <param name="result">result to be shown.</param>
        void SetError(MLResult result)
        {
            Debug.LogError(result);
            if (OnError != null)
            {
                OnError(result);
            }
        }

        /// <summary>
        /// Tries the restore binding.
        /// </summary>
        /// <returns>The restore binding.</returns>
        IEnumerator TryRestoreBinding()
        {
            string suffix = "";
            int count = 0;
            string prefix = gameObject.name;
            for (int i = 0; i < MaxPCFsToBindTo; ++i)
            {
                gameObject.name = prefix + suffix;
                Debug.Log("Trying to look for persistent point attached to :" + gameObject.name);
                yield return StartCoroutine(RestoreBinding(gameObject.name));
                if (_state == State.BindingComplete)
                {
                    //in short binding wasn't found 
                    if (Binding == null || Binding.PCF == null || Binding.PCF.CurrentResult != MLResultCode.Ok)
                    {
                        suffix = "-" + count;
                        count++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (Binding != null && Binding.PCF != null && Binding.PCF.CurrentResult == MLResultCode.Ok)
            {
                SetAvailable();
            }
            else
            {
                SetError(new MLResult(MLResultCode.Pending, "Failed to find a suitable PCF"));
            }
        }

        /// <summary>
        /// Tries to restore the binding from persistent storage and PCF system
        /// </summary>
        IEnumerator RestoreBinding(string objId)
        {
            _state = State.RestoreBinding;

            if (MLPersistentStore.Contains(objId))
            {
                MLContentBinding binding;

                MLResult result = MLPersistentStore.Load(objId, out binding);
                if (!result.IsOk)
                {
                    SetError(result);
                    _state = State.BindingComplete;
                }
                else
                {
                    Binding = binding;
                    Debug.Log("binding result : " + Binding.PCF.CurrentResult);
                    Binding.GameObject = this.gameObject;
                    MLContentBinder.Restore(Binding, HandleBindingRestore);
                }
            }
            else
            {
                BindToAllPCFs();
            }

            while (_state != State.BindingComplete)
            {
                yield return null;
            }
            yield break;
        }

        /// <summary>
        /// Handler for binding restore 
        /// </summary>
        /// <param name="contentBinding">Content binding.</param>
        /// <param name="resultCode">Result code.</param>
        void HandleBindingRestore(MLContentBinding contentBinding, MLResult result)
        {
            _state = State.BindingComplete;
            Debug.Log("binding result : " + contentBinding.PCF.CurrentResult);
            if (!result.IsOk)
            {
                MLPersistentStore.DeleteBinding(contentBinding);
                Debug.LogFormat("Failed to restore : {0} - {1}. Result code:", gameObject.name, contentBinding.PCF.CFUID, result.Code);
            }
        }

        /// <summary>
        /// Finds the closest pcf for this persistent point.
        /// </summary>
        void BindToAllPCFs()
        {
            _state = State.BindToAllPCFs;
            string suffix = "";
            int count = 0;

            // In the loop below we try to associate the persitent point with not only
            // the closest but all pcfs in the surrounding. This will increase the probablilty
            // of restoration on reboots. It's costly in terms of disk space so we will limit it to 
            // a max
            foreach (MLPCF pcf in _allPCFs)
            {
                string objectName = gameObject.name + suffix;
                var returnResult = MLPersistentCoordinateFrames.GetPCFPosition(pcf, (result, returnPCF) =>
                {
                    if (result.IsOk && pcf.CurrentResult == MLResultCode.Ok)
                    {
                        Debug.Log("binding to PCF: " + pcf.CFUID);

                        Binding = MLContentBinder.BindToPCF(objectName, gameObject, pcf);
                        MLPersistentStore.Save(Binding);
                    }
                    else
                    {
                        Debug.LogWarningFormat("Failed to find the position for PCF {0}", returnPCF.CFUID);
                    }
                });
                if (!returnResult.IsOk)
                {
                    Debug.LogError("Failed to GetPCF");
                    break;
                }
                suffix = "-" + count;
                count++;
            }

            _state = State.BindingComplete;
        }

        /// <summary>
        /// Sets the available.
        /// </summary>
        void SetAvailable()
        {
            _state = State.Locked;
            _lockedTransform.transform.position = Binding.GameObject.transform.position;
            _lockedTransform.transform.rotation = Binding.GameObject.transform.rotation;

            Debug.Log("Transform locked for Persistent point : " + gameObject.name);
            if (OnAvailable != null)
            {
                OnAvailable();
            }
            SetChildrenActive(true);
        }

        /// <summary>
        /// Update this instance.
        /// </summary>
        void Update()
        {
            if (_state == State.Locked)
            {
                transform.position = _lockedTransform.position;
                transform.rotation = _lockedTransform.rotation;
            }
        }

        /// <summary>
        /// Shuts down the systems started in Start
        /// </summary>
        void OnDestroy()
        {
            if (MLPersistentCoordinateFrames.IsStarted)
            {
                MLPersistentCoordinateFrames.Stop();
            }

            if (MLPersistentStore.IsStarted)
            {
                MLPersistentStore.Stop();
            }

            PrivilegeRequester.OnPrivilegesDone -= HandlePrivilegesDone;
        }
        #endregion

        #region Private Functions
        /// <summary>
        /// Responds to privilege requester result.
        /// </summary>
        /// <param name="result"/>
        private void HandlePrivilegesDone(MLResult result)
        {
            if (!result.IsOk)
            {
                Debug.LogError("Failed to get requested privilege. MLResult: " + result);
                // TODO: Cleanup?
                enabled = false;
                return;
            }

            Debug.Log("Succeeded in requesting all privileges");
            StartRestore();
        }
        #endregion
    }
}
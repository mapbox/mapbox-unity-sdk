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

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;

namespace MagicLeap
{
    /// <summary>
    /// DynamicPersistence example. Demonstrates how to persist objects dynamically by
    /// interfacing with the MLPersistence api and other components in the ML persistence system.
    /// </summary>
    [RequireComponent(typeof(PrivilegeRequester))]
    public class DynamicPersistenceExample : MonoBehaviour
    {
        #region variables
        [Serializable]
        struct ObjIds
        {
            public string[] Ids;
        }

        class ExampleObject
        {
            public GameObject GO { get; set; }
            public MLContentBinding Binding { get; set; }
        }

        [SerializeField]
        Text _progressText;

        [SerializeField, Tooltip("Prefab you want to create.")]
        GameObject _goPrefab;

        bool CanPlaceObject
        {
            get
            {
                return _state == State.RestoreComplete || _state == State.Done;
            }
        }

        MLInputController _controller;
        const string _fileName = "test.json";
        string _filePath;
        Camera _mainCam;

        List<ExampleObject> _exampleObjects = new List<ExampleObject>();

        const string TEXT_RESTORING_OBJECTS = "Restoring objects please wait..";
        const string TEXT_FAILED_TO_START_PERSISTENT_STORE = "Failed to start persistent store. Retrying ..";
        const string TEXT_RESTORING_OBJECT = "Restoring Object : {0} {1}";
        const string RETORE_COMPLETE = "Restore complete";
        const string TEXT_RETRY_PCF = "Cannot Start PCF system due to error: {0}. Please make sure to scan the area around \n you and try again. Retrying in {0} seconds.";
        const string TEXT_SAVE_COMPLETE = "Saved {0} objects!";
        const string FAILED_TO_FIND_CLOSEST_PCF = "Failed to find closest PCF.";
        const string TEXT_ADD_OBJECT = "Ready to add objects (Press Bumper)";
        const string TEXT_FAILED_TO_START_INPUT = "Failed to connect to the control.";
        const int _retryIntervalInSeconds = 3;

        enum State
        {
            Idle,
            StartRestore,
            StartRestoreObject,
            RestoreInProgress,
            RestoreComplete,
            SaveRequired,
            CritialError,
            Done
        }
        State _state = State.Idle;

        PrivilegeRequester _privilegeRequester;
        #endregion
        #region functions
        void Awake()
        {
            _privilegeRequester = GetComponent<PrivilegeRequester>();
            if (_privilegeRequester == null)
            {
                Debug.LogError("Missing PrivilegeRequester component");
                enabled = false;
                return;
            }

            // Could have also been set via the editor.
            _privilegeRequester.Privileges = new[] { MLRuntimeRequestPrivilegeId.PwFoundObjRead };

            _privilegeRequester.OnPrivilegesDone += HandlePrivilegesDone;

            _filePath = Path.Combine(Application.persistentDataPath, _fileName);
            Debug.Log("File Path: " + _filePath);
            MLResult result = MLInput.Start();
            if(!result.IsOk)
            {
                SetProgress(TEXT_FAILED_TO_START_INPUT);
            }
            else
            {
                MLInput.OnControllerButtonDown += HandleButtonDown;
            }
        }

        /// <summary>
        /// Sets up the various Persistent systems and starts the object restoration
        /// </summary>
        void Start()
        {
            _mainCam = Camera.main;
        }

        /// <summary>
        /// Responds to privilege requester result.
        /// </summary>
        /// <param name="result"/>
        void HandlePrivilegesDone(MLResult result)
        {
            if (!result.IsOk)
            {
                _state = State.CritialError;
                string errorMsg = string.Format("Privilege Error: {0}", result);
                Debug.LogErrorFormat(errorMsg);
                SetProgress(errorMsg);
                return;
            }

            string message = "Privilege Granted";
            SetProgress(message);
            Debug.LogFormat(message);
            StartRestore();
        }

        /// <summary>
        /// Starts restoration.
        /// </summary>
        void StartRestore()
        {
            // TODO: check for errors
            StartCoroutine(StartPersistenceSystems());
        }
        /// <summary>
        /// Starts the persistence systems, MLPersistentStore and MLPersistentCoordinateFrames
        /// </summary>
        IEnumerator StartPersistenceSystems()
        {
            if (!MLPersistentStore.IsStarted)
            {
                MLResult result = MLPersistentStore.Start();
                if (!result.IsOk)
                {
                    SetProgress(TEXT_FAILED_TO_START_PERSISTENT_STORE);
                    _state = State.CritialError;
                }
                else
                {
                    while(true)
                    {
                        result = MLPersistentCoordinateFrames.Start();

                        if((MLPassableWorldResult)result.Code == MLPassableWorldResult.LowMapQuality 
                            || (MLPassableWorldResult)result.Code == MLPassableWorldResult.UnableToLocalize)
                        {
                            SetProgress(string.Format(TEXT_RETRY_PCF, result, _retryIntervalInSeconds));
                            yield return new WaitForSeconds(_retryIntervalInSeconds);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            OnStartPersistentSystemComplete();
        }
        /// <summary>
        /// Starts the restoration process after the basic systems are initialized.
        /// </summary>
        void OnStartPersistentSystemComplete()
        {
            SetProgress(TEXT_RESTORING_OBJECTS);

            _exampleObjects = new List<ExampleObject>();

            ReadAllStoredObjects();

            if (_exampleObjects.Count == 0)
            {
                _state = State.RestoreComplete;
            }
        }
        /// <summary>
        /// Reads all stored game object ids.
        /// </summary>
        void ReadAllStoredObjects()
        {
            if (File.Exists(_filePath))
            {
                Debug.LogFormat("Found file {0}", _filePath);
                using (var source = new StreamReader(_filePath))
                {
                    string fileContents = source.ReadToEnd();
                    if (!string.IsNullOrEmpty(fileContents))
                    {
                        ObjIds ids = JsonUtility.FromJson<ObjIds>(fileContents);
                        if (ids.Ids != null && ids.Ids.Length > 0)
                        {
                            foreach (string id in ids.Ids)
                            {
                                Debug.LogFormat("Found : {0} in file", id);
                                ExampleObject exampleObj = new ExampleObject();
                                exampleObj.GO = Instantiate(_goPrefab, Vector3.zero, Quaternion.identity);
                                exampleObj.GO.name = id;
                                exampleObj.GO.SetActive(false);
                                _exampleObjects.Add(exampleObj);
                            }
                        }
                    }
                    _state = State.StartRestore;
                }
            }
        }

        /// <summary>
        /// Helper function to log error message and update it on the progress
        /// indicator
        /// </summary>
        /// <param name="progressText">Progress text.</param>
        void SetProgress(string progressText)
        {
            Debug.Log("setting progress: " + progressText);
            _progressText.text = progressText;
        }

        /// <summary>
        /// updates the state and handles input
        /// </summary>
        void Update()
        {
            ProcessState();
        }

        /// <summary>
        /// Hands the various states and state transitions.
        /// </summary>
        void ProcessState()
        {
            switch (_state)
            {
                case State.StartRestore:
                    StartRestoration();
                    break;
                case State.RestoreInProgress:
                    foreach (ExampleObject obj in _exampleObjects)
                    {
                        if (obj.Binding == null || obj.Binding.PCF == null || obj.Binding.PCF.CurrentResult != MLResultCode.Ok)
                        {
                            return;
                        }
                    }
                    _state = State.RestoreComplete;
                    break;
                case State.RestoreComplete:
                    HandleRestoreComplete();
                    break;
                case State.SaveRequired:
                    TrySave();
                    break;
            }
        }

        /// <summary>
        /// Starts the restoration by loading game object bindings. Should
        /// only be called in the State StartRestore.
        /// </summary>
        void StartRestoration()
        {
            LoadBindings();
        }

        /// <summary>
        /// Loads the bindings from the json file
        /// </summary>
        void LoadBindings()
        {
            _state = State.RestoreInProgress;
            foreach (var exampleObject in _exampleObjects)
            {
                SetProgress(string.Format(TEXT_RESTORING_OBJECT, exampleObject.GO.name, "started"));

                if (MLPersistentStore.Contains(exampleObject.GO.name))
                {
                    MLContentBinding binding;

                    MLResult result = MLPersistentStore.Load(exampleObject.GO.name, out binding);
                    if (!result.IsOk)
                    {
                        Debug.LogError("Failed to load binding for game object " + exampleObject.GO.name);
                    }
                    else
                    {
                        binding.GameObject = exampleObject.GO;
                        exampleObject.Binding = binding;
                        Debug.LogFormat("Binding loaded from the store: " +
                                        "Id: {0} \n" +
                                        "PCFID: {1}\n",
                                        binding.ObjectId,
                                        binding.PCF.CFUID);
                        MLContentBinder.Restore(binding, HandleBindingRestore);
                    }
                }
                else
                {
                    SetProgress(string.Format(TEXT_RESTORING_OBJECT, exampleObject.GO.name, "failed"));
                }
            }
        }

        /// <summary>
        /// Handler for restore complete. After restore is complete we go into the Done state
        /// where you can start adding more objects.
        /// </summary>
        void HandleRestoreComplete()
        {
            SetProgress(TEXT_ADD_OBJECT);
            _state = State.Done;
        }

        /// <summary>
        /// Handler for restoring the bindings. This is called when the content binding
        /// is restored, and if this is successful, the object is rebound to the original
        /// location it was bound to when last saved.
        /// </summary>
        /// <param name="contentBinding">Content binding.</param>
        /// <param name="resultCode">Result code.</param>
        void HandleBindingRestore(MLContentBinding contentBinding, MLResult result)
        {
            if (result.IsOk)
            {
                SetProgress(string.Format(TEXT_RESTORING_OBJECT, contentBinding.GameObject.name, "succeeded"));
                contentBinding.GameObject.SetActive(true);
                Debug.LogFormat("object: {0} - {1} {2} {3} , {4} {5} {6} {7}", contentBinding.GameObject.name, 
                                contentBinding.GameObject.transform.position.x, 
                                contentBinding.GameObject.transform.position.y, 
                                contentBinding.GameObject.transform.position.z,
                                contentBinding.GameObject.transform.rotation.x,
                                contentBinding.GameObject.transform.rotation.y,
                                contentBinding.GameObject.transform.rotation.z,
                                contentBinding.GameObject.transform.rotation.w);
            }
            else
            {
                SetProgress(string.Format(TEXT_RESTORING_OBJECT, contentBinding.GameObject.name, "failed. Trying again"));
                //Note: Currently we try endlessly to restore. However, this can lead to a loop
                //when there's fundamental problem with the underlying system. This logic can be made smarter by
                //adding retry attempts however for sake of simplicitly we are not showing it here. It is advised
                //to do it however.
                MLContentBinder.Restore(contentBinding, HandleBindingRestore);
            }
        }

        /// <summary>
        /// Called only on device since it's registerd only on device
        /// </summary>
        /// <param name="index">Index.</param>
        /// <param name="button">Button.</param>
        void HandleButtonDown(byte controllerIndex, MLInputControllerButton button)
        {
            if(CanPlaceObject && button == MLInputControllerButton.Bumper)
            {
                CreateObject();
            }
        }

        /// <summary>
        /// Helper function to add new objects and binding them to closest PCFs.
        /// This function shows how youc an use the underlying systems to accomplish
        /// game object to a PCF binding
        /// </summary>
        void CreateObject()
        {
            Vector3 position = GetNewObjectPosition();

            ExampleObject newExampleObject = new ExampleObject();
            newExampleObject.GO = Instantiate(_goPrefab, position, UnityEngine.Random.rotation);
            newExampleObject.GO.name = Guid.NewGuid().ToString();
            _exampleObjects.Add(newExampleObject);

            var returnResult = MLPersistentCoordinateFrames.FindClosestPCF(position, (MLResult result, MLPCF pcf) =>
            {
                if (result.IsOk)
                {
                    Debug.LogFormat("Closest PCF found. Binding {0} to PCF {1}:", newExampleObject.GO.name, pcf.CFUID);
                    newExampleObject.Binding = MLContentBinder.BindToPCF(newExampleObject.GO.name, newExampleObject.GO, pcf);
                    _state = State.SaveRequired;
                    Debug.LogFormat("object: {0} - {1} {2} {3}, {4} {5} {6} {7}",
                                    newExampleObject.GO.name,
                                    newExampleObject.GO.transform.position.x,
                                    newExampleObject.GO.transform.position.y,
                                    newExampleObject.GO.transform.position.z,
                                    newExampleObject.GO.transform.rotation.x,
                                    newExampleObject.GO.transform.rotation.y,
                                    newExampleObject.GO.transform.rotation.z,
                                    newExampleObject.GO.transform.rotation.w);
                }
                else
                {
                    RemoveObject(newExampleObject);
                    SetProgress(FAILED_TO_FIND_CLOSEST_PCF + " Reason:" + result);
                }
            });

            if (!returnResult.IsOk)
            {
                RemoveObject(newExampleObject);
                SetProgress(FAILED_TO_FIND_CLOSEST_PCF + " Result Code:" + returnResult);
            }
        }
        Vector3 GetNewObjectPosition()
        {
            Vector3 position = new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), UnityEngine.Random.Range(-1.5f, 1.5f), UnityEngine.Random.Range(1.5f, 5.0f));
            if(_mainCam != null)
            {
                position = _mainCam.transform.forward.normalized * UnityEngine.Random.Range(0.5f, 6.0f);
            }
            return position;
        }
        void RemoveObject(ExampleObject exampleObject)
        {
            Destroy(exampleObject.GO);
            _exampleObjects.Remove(exampleObject);
        }

        /// <summary>
        /// Tries to save the existing game object ids and also calls the persistent
        /// system save call to ensure game object to the PCF bindings are saved.
        /// </summary>
        void TrySave()
        {
            if (_state == State.SaveRequired)
            {
                _state = State.Done;
                Debug.LogFormat("Saving Objects {0}", _exampleObjects.Count);
                if (!MLPersistentStore.IsStarted)
                {
                    Debug.LogError("MLPersistentStore is not started! can't save. ");
                    return;
                }
                if (_exampleObjects.Count > 0)
                {
                    ObjIds record = new ObjIds();
                    record.Ids = new string[_exampleObjects.Count];
                    int i = 0;
                    foreach (var someObj in _exampleObjects)
                    {
                        record.Ids[i++] = someObj.GO.name;
                        //Update the binding (re-store offsets) before saving
                        someObj.Binding.Update();
                        Debug.Log("saving binding for: " + someObj.Binding.GameObject.name);
                        MLPersistentStore.Save(someObj.Binding);
                    }

                    string jsonString = JsonUtility.ToJson(record);
                    File.WriteAllText(_filePath, jsonString);
                }
                SetProgress(string.Format(TEXT_SAVE_COMPLETE, _exampleObjects.Count));
            }
        }

        /// <summary>
        /// Shuts down the started systems.
        /// </summary>
        void OnDestroy()
        {
            if(MLInput.IsStarted)
            {
                MLInput.OnControllerButtonDown -= HandleButtonDown;
                MLInput.Stop();
            }
            if (MLPersistentCoordinateFrames.IsStarted)
            {
                MLPersistentCoordinateFrames.Stop();
            }
            if (MLPersistentStore.IsStarted)
            {
                MLPersistentStore.Stop();
            }
            if(_privilegeRequester != null)
            {
                _privilegeRequester.OnPrivilegesDone -= HandlePrivilegesDone;
            }
        }
        #endregion
    }

}

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
using UnityEngine.Video;

namespace UnityEngine.XR.MagicLeap
{
    /// <summary>
    ///  Shows all Persistent Points in the world around you.
    /// </summary>
    public class PersistentPointsVisualizer : MonoBehaviour
    {
        #region Variables
        [SerializeField, Tooltip("Prefab to represent a PCF visually")]
        private GameObject _representativePrefab;
        private List<GameObject> _pcfObjs = new List<GameObject>();
        #endregion

        #region functions
        /// <summary>
        /// Start this instance.
        /// </summary>
        void Start()
        {
            MLResult result = MLPersistentStore.Start();
            if (!result.IsOk)
            {
                SetError("Failed to start persistent store. Disabling component");
                enabled = false;
                return;
            }
            result = MLPersistentCoordinateFrames.Start();
            if (!result.IsOk)
            {
                MLPersistentStore.Stop();
                SetError("Failed to start coordinate frames system. disabling component");
                enabled = false;
                return;
            }

            if (_representativePrefab == null)
            {
                SetError("Error: _representativePrefab must be set");
                enabled = false;
                return;
            }

            List<MLPCF> pcfList;
            result = MLPersistentCoordinateFrames.GetAllPCFs(out pcfList, int.MaxValue);
            if (!result.IsOk)
            {
                MLPersistentStore.Stop();
                MLPersistentCoordinateFrames.Stop();
                SetError(result.ToString());
                enabled = false;
                return;
            }

            TryShowingAllPCFs(pcfList);
        }

        /// <summary>
        /// Sets the error.
        /// </summary>
        /// <param name="errorString">Error string.</param>
        void SetError(string errorString)
        {
            Debug.LogError(errorString);
        }

        /// <summary>
        /// Tries the showing all PCF.
        /// </summary>
        /// <param name="pcfList">Pcf list.</param>
        void TryShowingAllPCFs(List<MLPCF> pcfList)
        {
            foreach (MLPCF pcf in pcfList)
            {
                if (pcf.CurrentResult == MLResultCode.Pending)
                {
                    MLPersistentCoordinateFrames.GetPCFPosition(pcf, (r, p) =>
                    {
                        if (r.IsOk)
                        {
                            AddPCFObject(p);
                        }
                        else
                        {
                            SetError("failed to get position for pcf : " + p);
                        }
                    });
                }
                else
                {
                    AddPCFObject(pcf);
                }
            }
        }

        /// <summary>
        /// Creates the PCF game object.
        /// </summary>
        /// <param name="pcf">Pcf.</param>
        void AddPCFObject(MLPCF pcf)
        {
            if(!_pcfObjs.Contains(pcf.GameObj))
            {
                GameObject repObj = Instantiate(_representativePrefab, Vector3.zero, Quaternion.identity);
                repObj.name = pcf.GameObj.name;
                repObj.transform.parent = pcf.GameObj.transform;
                _pcfObjs.Add(pcf.GameObj);
            }
        }

        /// <summary>
        /// Clean up
        /// </summary>
        void OnDestroy()
        {
            if (MLPersistentStore.IsStarted)
            {
                MLPersistentStore.Stop();
            }
            if (MLPersistentCoordinateFrames.IsStarted)
            {
                MLPersistentCoordinateFrames.Stop();
            }

            foreach (GameObject go in _pcfObjs)
            {
                Destroy(go);
            }
        }
        #endregion
    }
}

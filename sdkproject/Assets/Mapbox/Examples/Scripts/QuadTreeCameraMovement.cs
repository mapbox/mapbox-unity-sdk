namespace Mapbox.Examples
{
	using Mapbox.Unity.Map;
	using Mapbox.Unity.Utilities;
	using Mapbox.Utils;
	using UnityEngine;
    using System;

	public class QuadTreeCameraMovement : MonoBehaviour
	{
		[SerializeField]
		[HideInInspector]
		public float _zoomSpeed = 50f;

		[SerializeField]
		//[HideInInspector]
		public Camera _referenceCamera;

		[SerializeField]
		QuadTreeTileProvider _quadTreeTileProvider;

		[SerializeField]
        BasicMap _dynamicZoomMap;

		private Vector3 _origin;
        Vector3 _delta;
        bool _shouldDrag;
        private Transform _originalCameraPosition; 

        /// <summary>min of y range camera is allowed to move in</summary>
        private int _cameraZoomingRangeMinY;
        public int CameraZoomingRangeMinY { get { return _cameraZoomingRangeMinY; } }
        /// <summary>max of y range camera is allowed to move in</summary>
        private int _cameraZoomingRangeMaxY;

        public int CameraZoomingRangeMaxY { get { return _cameraZoomingRangeMaxY; } }

		void Start()
		{
			if (null == _referenceCamera)
			{
				_referenceCamera = GetComponent<Camera>();
				if (null == _referenceCamera) { Debug.LogErrorFormat("{0}: reference camera not set", this.GetType().Name); }
			}

			//put camera into the middle of the allowed y movement range
			//Vector3 localPosition = _referenceCamera.transform.position;
			//localPosition.x = 0;
			//localPosition.y = (_dynamicZoomTileProvider.CameraZoomingRangeMaxY + _dynamicZoomTileProvider.CameraZoomingRangeMinY) / 2;
			//localPosition.z = 0;
			//_referenceCamera.transform.localPosition = localPosition;
			//_referenceCamera.transform.rotation = new Quaternion(0.7f, 0, 0, 0.7f);

			//link zoomspeed to tilesize
			_zoomSpeed = _dynamicZoomMap.UnityTileSize / 2f;

            _cameraZoomingRangeMaxY = (int)(_dynamicZoomMap.UnityTileSize * 2.5f);
            _cameraZoomingRangeMinY = (int)(_dynamicZoomMap.UnityTileSize * 1.25f);
		}


		private void LateUpdate()
		{
            if (null == _dynamicZoomMap) { return; }


            //development short cut: reset center to 0/0 via right click
            //if (Input.GetMouseButton(1))
            //{
            //	_dynamicZoomMap.SetCenterMercator(Vector2d.zero);
            //	return;
            //}


            // zoom
            var scrollDelta = Input.GetAxis("Mouse ScrollWheel");

            if(scrollDelta > 0f)
            {
                _dynamicZoomMap.SetZoomRange(Mathf.Min(_dynamicZoomMap.ZoomRange + 0.25f, 21.0f));
            }
            else if (scrollDelta < 0f)
            {
                _dynamicZoomMap.SetZoomRange(Mathf.Max(_dynamicZoomMap.ZoomRange - 0.25f, 0.0f));
            }


			//pan keyboard
			//float xMove = Input.GetAxis("Horizontal");
			//float zMove = Input.GetAxis("Vertical");
			//if (0 != xMove || 0 != zMove)
			//{
			//	float factor = Conversions.GetTileScaleInMeters((float)_dynamicZoomMap.CenterLatitudeLongitude.x, _dynamicZoomMap.Zoom) * 256 / _dynamicZoomMap.UnityTileSize;
			//	xMove *= factor;
			//	zMove *= factor;
			//	_dynamicZoomMap.SetCenterMercator(_dynamicZoomMap.CenterMercator + new Vector2d(xMove, zMove));
			//}

			//pan mouse
			if (Input.GetMouseButton(0))
			{
				var mousePosScreen = Input.mousePosition;
				//assign distance of camera to ground plane to z, otherwise ScreenToWorldPoint() will always return the position of the camera
				//http://answers.unity3d.com/answers/599100/view.html
				mousePosScreen.z = _referenceCamera.transform.localPosition.y;
                _delta = _referenceCamera.ScreenToWorldPoint(mousePosScreen) - _referenceCamera.transform.localPosition;
                _delta.y = 0f;
                if (_shouldDrag == false)
                {
                    _shouldDrag = true;
                    _origin = _referenceCamera.ScreenToWorldPoint(mousePosScreen);
                }
			}
            else
            {
                _shouldDrag = false;   
            }

            if(_shouldDrag == true)
            {
                var offset = _origin - _delta;
                offset.y = _referenceCamera.transform.localPosition.y;
                _referenceCamera.transform.localPosition = offset;
                float factor = 20f; // Conversions.GetTileScaleInMeters((float)_dynamicZoomMap.CenterLatitudeLongitude.x, _dynamicZoomMap.Zoom) * 256 / _dynamicZoomMap.UnityTileSize;
                //_dynamicZoomMap.SetCenterMercator(_dynamicZoomMap.CenterMercator + new Vector2d(offset.x * factor, offset.z * factor));
                UnityEngine.Debug.Log("Dragging");
            }
            else
            {
                var offset = _origin - _delta;
                if (null != _dynamicZoomMap)
                {
                    //float factor = 20f;//Conversions.GetTileScaleInMeters((float)_dynamicZoomMap.CenterLatitudeLongitude.x, _dynamicZoomMap.Zoom) * 256 / _dynamicZoomMap.UnityTileSize;
                    var centerOld = _dynamicZoomMap.CenterMercator;
                    //_dynamicZoomMap.SetCenterMercator(_dynamicZoomMap.CenterMercator + new Vector2d(offset.x * factor, offset.z * factor));
                    //Stransform.localPosition += transform.forward * y + (_originalRotation * new Vector3(x * _panSpeed, 0, z * _panSpeed));

                    //UnityEngine.Debug.Log("Not - Dragging");
                }
            }


    			//if (Input.GetMouseButtonUp(0))
    			//{
    			//	var mouseUpPosScreen = Input.mousePosition;
    			//	//assign distance of camera to ground plane to z, otherwise ScreenToWorldPoint() will always return the position of the camera
    			//	//http://answers.unity3d.com/answers/599100/view.html
    			//	mouseUpPosScreen.z = _referenceCamera.transform.localPosition.y;
    			//	var mouseUpPosWorld = _referenceCamera.ScreenToWorldPoint(mouseUpPosScreen);

    			//	//has position changed?
    			//	if (_origin != mouseUpPosWorld)
    			//	{
    			//		var offset = _origin - mouseUpPosWorld;
    			//		if (null != _dynamicZoomMap)
    			//		{
    			//			float factor = Conversions.GetTileScaleInMeters((float)_dynamicZoomMap.CenterLatitudeLongitude.x, _dynamicZoomMap.Zoom) * 256 / _dynamicZoomMap.UnityTileSize;
    			//			var centerOld = _dynamicZoomMap.CenterMercator;
    			//			_dynamicZoomMap.SetCenterMercator(_dynamicZoomMap.CenterMercator + new Vector2d(offset.x * factor, offset.z * factor));
    			//		}
    			//	}
    			//}
		}
	}
}
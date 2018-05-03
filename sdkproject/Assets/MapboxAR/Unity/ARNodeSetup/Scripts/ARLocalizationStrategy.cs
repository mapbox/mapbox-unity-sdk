namespace Mapbox.Unity.Ar
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using System;
	using UnityARInterface;
	using Mapbox.Unity.Location;
	using Mapbox.Unity.Map;

	public class ARLocalizationStrategy : ComputeARLocalizationStrategy
	{

		ARInterface.CustomTrackingState _trackingState;
		ARInterface _arInterface;
		bool _isTrackingGood;
		float _planePosOnY;

		public override event Action<Alignment> OnLocalizationComplete;

		private void Start()
		{
			_arInterface = ARInterface.GetInterface();
			_trackingState = new ARInterface.CustomTrackingState();
			ARInterface.planeAdded += GetPlaneCoords;
			ARInterface.planeRemoved += GetPlaneCoords;

			// TODO : Get always the current gps position.. and then return the mapmatching coords and after that..
			/// Choose which one to choose on relocalisation...
			/// Also create a custom new larp aligment strategy...
			/// 
		}

		public override void ComputeLocalization(CentralizedARLocator centralizedARLocator)
		{
			var currentLocation = LocationProviderFactory.Instance.DefaultLocationProvider.CurrentLocation;
			var aligment = new Alignment();
			// Checking if tracking is good, do nothing on location. Other than check if the newly calculated heading is better.
			if (CheckTracking())
			{
				if (currentLocation.IsLocationUpdated)
				{
					var mapPos = centralizedARLocator.CurrentMap.transform.position;
					var newPos = new Vector3(mapPos.x, _planePosOnY, mapPos.z);
					aligment.Position = newPos;
					aligment.Rotation = currentLocation.UserHeading;
					OnLocalizationComplete(aligment);
				}
				return;
			}

			var geoPos = centralizedARLocator.CurrentMap.GeoToWorldPosition(currentLocation.LatitudeLongitude);
			var geoAndPlanePos = new Vector3(geoPos.x, _planePosOnY, geoPos.z);
			aligment.Position = geoAndPlanePos;
			aligment.Rotation = currentLocation.IsLocationUpdated ? currentLocation.UserHeading : currentLocation.DeviceOrientation;
			OnLocalizationComplete(aligment);
		}

		void GetPlaneCoords(BoundedPlane plane)
		{
			_planePosOnY = plane.center.y;
		}

		bool CheckTracking()
		{
			if (_arInterface.GetTrackingState(ref _trackingState))
			{
				if (_trackingState == ARInterface.CustomTrackingState.Good)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return false;
			}
		}
	}

}


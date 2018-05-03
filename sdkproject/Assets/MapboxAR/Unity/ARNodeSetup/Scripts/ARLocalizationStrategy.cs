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

		public override event Action<Alignment> OnLocalizationComplete;

		private void Start()
		{
			_arInterface = ARInterface.GetInterface();
			_trackingState = new ARInterface.CustomTrackingState();

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
					aligment.Position = centralizedARLocator.CurrentMap.transform.position;
					aligment.Rotation = currentLocation.UserHeading;
					OnLocalizationComplete(aligment);
				}

				return;
			}

			aligment.Position = centralizedARLocator.CurrentMap.GeoToWorldPosition(currentLocation.LatitudeLongitude);
			aligment.Rotation = currentLocation.IsLocationUpdated ? currentLocation.UserHeading : currentLocation.DeviceOrientation;
			OnLocalizationComplete(aligment);
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


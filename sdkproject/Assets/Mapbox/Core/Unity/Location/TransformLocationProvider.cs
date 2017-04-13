using System;
using UnityEngine;
using Assets.Scripts.Helpers;

namespace Scripts.Location
{
	public class TransformLocationProvider : MonoBehaviour, ILocationProvider
	{
		[SerializeField]
		Transform _targetTransform;

		public Vector2 Location
		{
			get
			{
				return GetLocation();
			}
		}

		public Transform TargetTransform
		{
			set
			{
				_targetTransform = value;
			}
		}

		public event EventHandler<HeadingUpdatedEventArgs> OnHeadingUpdated;
		public event EventHandler<LocationUpdatedEventArgs> OnLocationUpdated;

		void Update()
		{
			if (OnHeadingUpdated != null)
			{
				OnHeadingUpdated(this, new HeadingUpdatedEventArgs() { Heading = _targetTransform.eulerAngles.y });
			}

			if (OnLocationUpdated != null)
			{
				// TODO: optimize this. Use Vector2 OR GeoCoordinate?
				OnLocationUpdated(this, new LocationUpdatedEventArgs() { Location = GetLocation() });
			}
		}

		Vector2 GetLocation()
		{
			var geo = _targetTransform.GetGeoPosition();
			var position = new Vector2((float)geo.Latitude, (float)geo.Longitude);
			return position;
		}
	}
}

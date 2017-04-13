namespace Mapbox.Unity.Location
{
    using System;
    using Mapbox.Unity.Utilities;
    using Mapbox.Utils;
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration;

    public class TransformLocationProvider : MonoBehaviour, ILocationProvider
	{
        [SerializeField]
		Transform _targetTransform;

		public Vector2d Location
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
				OnLocationUpdated(this, new LocationUpdatedEventArgs() { Location = GetLocation() });
			}
		}

		Vector2d GetLocation()
		{
            return _targetTransform.GetGeoPosition(MapController.ReferenceTileRect.Center, MapController.WorldScaleFactor);
		}
	}
}

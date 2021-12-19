using System;
using System.Globalization;
using Mapbox.Unity.DataContainers;
using Mapbox.Unity.Map;
using Mapbox.Unity.Map.Interfaces;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;

namespace Mapbox.Unity.QuadTree
{
	public class MapCore : MonoBehaviour, IMap
	{
		//not used in new system but reqiured because IMap
		public float WorldRelativeScale { get; }
		public float UnityTileSize => _options.scalingOptions.unityTileSize;

		protected Vector2d _centerMercator;
		protected Vector2d _centerLatitudeLongitude;
		protected bool _isDirty = false;

		public Vector2d CenterLatitudeLongitude => _centerLatitudeLongitude;
		public Vector2d CenterMercator => Conversions.LatLonToMeters(_centerLatitudeLongitude);

		[SerializeField] protected AbstractMapVisualizer _mapVisualizer;
		[SerializeField] private MapOptions _options = new MapOptions();
		public MapOptions Options
		{
			get => _options;
			set => _options = value;
		}

		public int InitialZoom { get; set; }
		public float Zoom => Options.locationOptions.zoom;
		public int AbsoluteZoom => (int)Math.Floor(Options.locationOptions.zoom);

		public Transform Root => transform;
		public Material TileMaterial => _options.tileMaterial;

		public virtual void Start()
		{
			Options.locationOptions.PropertyHasChanged += (object sender, System.EventArgs eventArgs) =>
			{
				UpdateMap(Conversions.StringToLatLon(_options.locationOptions.latitudeLongitude), _options.locationOptions.zoom);
			};

			_centerLatitudeLongitude = Conversions.StringToLatLon(_options.locationOptions.latitudeLongitude);
			InitialZoom = (int)_options.locationOptions.zoom;
			_mapVisualizer.Initialize(this);
		}

		#region SET STUFF
		public void SetZoom(float zoom)
		{
			Options.locationOptions.zoom = zoom;
		}
		public void SetWorldRelativeScale(float scale)
		{
			throw new NotImplementedException();
		}
		public virtual void SetCenterMercator(Vector2d centerMercator)
		{
			_centerMercator = centerMercator;
		}
		public virtual void SetCenterLatitudeLongitude(Vector2d centerLatitudeLongitude)
		{
			_options.locationOptions.latitudeLongitude = string.Format("{0}, {1}", centerLatitudeLongitude.x.ToString(CultureInfo.InvariantCulture), centerLatitudeLongitude.y.ToString(CultureInfo.InvariantCulture));
			_centerLatitudeLongitude = centerLatitudeLongitude;
		}
		#endregion

		public Vector2d WorldToGeoPosition(Vector3 realworldPoint)
		{
			throw new NotImplementedException();
		}
		public Vector3 GeoToWorldPosition(Vector2d latitudeLongitude, bool queryHeight = true)
		{
			throw new NotImplementedException();
		}

		public void UpdateMap(Vector2d latLon, float zoom)
		{
			SetCenterLatitudeLongitude(latLon);
			SetZoom(zoom);
			_isDirty = true;
		}

		public void ResetMap()
		{
			throw new NotImplementedException();
		}

		public bool IsAccessTokenValid
		{
			get
			{
				bool isAccessTokenValid = false;
				try
				{
					var accessTokenCheck = MapboxAccess.Instance;
					if (MapboxAccess.Instance.Configuration == null || string.IsNullOrEmpty(MapboxAccess.Instance.Configuration.AccessToken))
					{
						return false;
					}

					isAccessTokenValid = true;
				}
				catch (System.Exception)
				{
					isAccessTokenValid = false;
				}
				return isAccessTokenValid;
			}
		}

		public event Action OnInitialized;
		public event Action OnUpdated;

	}
}
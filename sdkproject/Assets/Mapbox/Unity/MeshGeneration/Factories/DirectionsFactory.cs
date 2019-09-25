using Mapbox.Examples;

namespace Mapbox.Unity.MeshGeneration.Factories
{
	using UnityEngine;
	using Mapbox.Directions;
	using System.Collections.Generic;
	using System.Linq;
	using Mapbox.Unity.Map;
	using Data;
	using Modifiers;
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;
	using System.Collections;

	public class DirectionsFactory : MonoBehaviour
	{
		[SerializeField] float RoadSizeMultiplier = 1;
		[SerializeField] private AnimationCurve RoadSizeCurve;
		[SerializeField] AbstractMap _map;

		//[SerializeField] MeshModifier[] MeshModifiers;
		[SerializeField] private LoftModifier _loftModifier;
		[SerializeField] Material _material;

		[SerializeField] Transform[] _waypoints;
		private List<Vector3> _cachedWaypoints;

		private Directions _directions;
		private int _counter;
		private bool _isDragging = false;

		GameObject _directionsGO;
		private bool _recalculateNext;

		protected virtual void Awake()
		{
			if (_map == null)
			{
				_map = FindObjectOfType<AbstractMap>();
			}

			_directions = MapboxAccess.Instance.Directions;
			_map.OnInitialized += Query;
			_map.OnUpdated += Query;

			foreach (var wp in GetComponentsInChildren<DragableDirectionWaypoint>())
			{
				wp.MouseDown += () =>
				{
					_directionsGO.SetActive(false);
					_isDragging = true;
				};

				wp.MouseDraging += () =>
				{


				};
				wp.MouseDrop += () =>
				{
					_isDragging = false;
					Query();
				};
			}
		}

		public void Start()
		{
			_cachedWaypoints = new List<Vector3>(_waypoints.Length);
			foreach (var item in _waypoints)
			{
				_cachedWaypoints.Add(item.position);
			}

			_recalculateNext = false;
			_loftModifier.Initialize();
		}

		protected virtual void OnDestroy()
		{
			_map.OnInitialized -= Query;
			_map.OnUpdated -= Query;
		}

		void Query()
		{
			var count = _waypoints.Length;
			var wp = new Vector2d[count];
			for (int i = 0; i < count; i++)
			{
				wp[i] = _waypoints[i].GetGeoPosition(_map.CenterMercator, _map.WorldRelativeScale);
			}

			var _directionResource = new DirectionResource(wp, RoutingProfile.Driving);
			_directionResource.Steps = true;
			_directions.Query(_directionResource, HandleDirectionsResponse);
		}

		void HandleDirectionsResponse(DirectionsResponse response)
		{

			if (response == null || null == response.Routes || response.Routes.Count < 1)
			{
				return;
			}

			var meshData = new MeshData();
			var unitySpacePositions = new List<Vector3>();
			foreach (var point in response.Routes[0].Geometry)
			{
				unitySpacePositions.Add(Conversions.GeoToWorldPosition(point.x, point.y, _map.CenterMercator, _map.WorldRelativeScale).ToVector3xz());
			}

			if (_waypoints.Length > 0 && unitySpacePositions.Count > 0)
			{
				_waypoints[0].transform.position = unitySpacePositions[0];
				_waypoints[_waypoints.Length - 1].transform.position = unitySpacePositions[unitySpacePositions.Count - 1];
			}

			var feat = new VectorFeatureUnity();
			feat.Points.Add(unitySpacePositions);

			_loftModifier.SliceScaleMultiplier = RoadSizeCurve.Evaluate(_map.Zoom) * RoadSizeMultiplier;
			_loftModifier.Run(feat, meshData, _map.WorldRelativeScale);

			CreateGameObject(meshData);
			_directionsGO.SetActive(true);
		}

		GameObject CreateGameObject(MeshData data)
		{
			if (_directionsGO != null)
			{
				_directionsGO.Destroy();
			}

			_directionsGO = new GameObject("direction waypoint " + " entity");
			var mesh = _directionsGO.AddComponent<MeshFilter>().mesh;
			mesh.subMeshCount = data.Triangles.Count;

			mesh.SetVertices(data.Vertices);
			_counter = data.Triangles.Count;
			for (int i = 0; i < _counter; i++)
			{
				var triangle = data.Triangles[i];
				mesh.SetTriangles(triangle, i);
			}

			_counter = data.UV.Count;
			for (int i = 0; i < _counter; i++)
			{
				var uv = data.UV[i];
				mesh.SetUVs(i, uv);
			}

			mesh.RecalculateNormals();
			_directionsGO.AddComponent<MeshRenderer>().material = _material;
			return _directionsGO;
		}
	}
}
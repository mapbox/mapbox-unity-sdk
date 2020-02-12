using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Mapbox.Examples;
using UnityEngine.UI;

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
		public Action ArrangingWaypointsStarted = () => { };
		public Action QuerySent = () => { };
		public Action<Vector3[]> ArrangingWaypoints = (positions) => { };
		public Action ArrangingWaypointsFinished = () => { };
		public Action<Vector3, float> RouteDrawn = (midPoint, TotalLength) => { };
		public RoutingProfile RoutingProfile = RoutingProfile.Driving;

		[SerializeField] float RoadSizeMultiplier = 1;
		[SerializeField] private AnimationCurve RoadSizeCurve;
		[SerializeField] AbstractMap _map;
		[SerializeField] private LineRenderer _lineRenderer;
		[SerializeField] private LoftModifier _loftModifier;
		[SerializeField] Material _material;
		[SerializeField] Transform _waypointsParent;
		//[SerializeField] private Dropdown RouteTypeDropdown;


		Transform[] _waypoints;

		private List<Vector3> _cachedWaypoints;
		private Directions _directions;
		private int _counter;
		private bool _isDragging = false;
		private Vector3[] _pointArray;
		private Vector3 _pointUpDelta = new Vector3(0, 3, 0);
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

			_waypoints = new Transform[_waypointsParent.childCount];
			for (int i = 0; i < _waypointsParent.childCount; i++)
			{
				_waypoints[i] = _waypointsParent.GetChild(i);
			}

			_pointArray = new Vector3[_waypoints.Length];

			foreach (var wp in GetComponentsInChildren<DragableDirectionWaypoint>())
			{
				wp.MouseDown += () =>
				{
					ArrangingWaypointsStarted();
					_lineRenderer.gameObject.SetActive(true);
					_directionsGO.SetActive(false);
					_isDragging = true;
				};

				wp.MouseDraging += () =>
				{
					_lineRenderer.positionCount = _waypoints.Length;
					for (int i = 0; i < _waypoints.Length; i++)
					{
						_pointArray[i] = _waypoints[i].position + _pointUpDelta;
					}

					_lineRenderer.SetPositions(_pointArray);
					ArrangingWaypoints(_pointArray);
				};
				wp.MouseDrop += () =>
				{
					ArrangingWaypointsFinished();
					_lineRenderer.gameObject.SetActive(false);
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

			var directionResource = new DirectionResource(wp, RoutingProfile);
			directionResource.Steps = true;
			_directions.Query(directionResource, HandleDirectionsResponse);
			QuerySent();
		}

		void HandleDirectionsResponse(DirectionsResponse response)
		{
			if (response == null || null == response.Routes || response.Routes.Count < 1)
			{
				return;
			}

			var meshData = new MeshData();
			var unitySpacePositions = new List<Vector3>();

			var totalLength = 0f;
			Vector3 prevPoint = Unity.Constants.Math.Vector3Zero;
			foreach (var point in response.Routes[0].Geometry)
			{
				var newPoint = Conversions.GeoToWorldPosition(point.x, point.y, _map.CenterMercator, _map.WorldRelativeScale).ToVector3xz();
				unitySpacePositions.Add(newPoint);

				if (prevPoint != Unity.Constants.Math.Vector3Zero)
				{
					totalLength += Vector3.Distance(prevPoint, newPoint);
				}

				prevPoint = newPoint;
			}

			var midLength = totalLength / 2;

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


			var midPoint = unitySpacePositions[0];
			for (int i = 1; i < unitySpacePositions.Count; i++)
			{
				var dist = (unitySpacePositions[i] - unitySpacePositions[i - 1]).magnitude;
				if (midLength > dist)
				{
					midLength -= dist;
				}
				else
				{
					midPoint = Vector3.Lerp(unitySpacePositions[i - 1], unitySpacePositions[i], (float) midLength / dist);
					break;
				}
			}

			RouteDrawn(midPoint, totalLength / _map.WorldRelativeScale);
		}

		GameObject CreateGameObject(MeshData data)
		{
			if (_directionsGO != null)
			{
				_directionsGO.Destroy();
			}

			_directionsGO = new GameObject("direction waypoint " + " entity");
			if (_map != null)
			{
				_directionsGO.transform.SetParent(_map.transform);
			}

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

		public void ChangeRoutingProfile(RoutingProfile profile, bool forceQuery = true)
		{
			RoutingProfile = profile;
			if (forceQuery)
			{
				Query();
			}
		}
	}
}
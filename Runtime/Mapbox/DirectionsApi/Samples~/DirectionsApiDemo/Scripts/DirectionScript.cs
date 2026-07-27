using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.BaseModule.Data;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.DirectionsApi;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Mapbox.DirectionsApi.Samples
{
	public class DirectionScript : MonoBehaviour
	{
		public MapBehaviourCore MapCore;
		private IFileSource _fileSource;
		private IMapInformation _mapInformation;
		private MapboxDirectionsApi _mapboxDirections;
		private Camera _camera;

		public GameObject Label;
		public string _labelFormat = "Distance : {0}";

		private Vector3 FirstPoint;
		private LatitudeLongitude FirstLatLng;
		private Vector3 SecondPoint;
		private LatitudeLongitude SecondLatLng;
		private bool IsFirstPointSet = false;
		private bool IsSecondPointSet = false;
		private GameObject startMarker;
		private GameObject finishMarker;
		private List<GameObject> _gos;
		private CongestionRouteRenderer _routeRenderer;

		[Tooltip("Routing profile for the request. Traffic congestion coloring is only available with DrivingTraffic; other profiles draw the route in the 'Unknown' color.")]
		public RoutingProfile.RoutingProfileOptions RoutingOption = RoutingProfile.RoutingProfileOptions.DrivingTraffic;

		[Tooltip("Width of the drawn route line, in the same units the demo used previously. Typical: 10.")]
		public float LineWidth = 10f;

		[Tooltip("Per-level colors for the traffic congestion overlay (green = clear, red = severe).")]
		public CongestionColors CongestionColors = new CongestionColors();

		public void Start()
		{
			startMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			startMarker.transform.parent = transform;
			startMarker.transform.localScale = Vector3.one * 0.2f;
			startMarker.SetActive(false);
			var mat1 = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
			mat1.color = new Color(134f / 255, 255f / 255, 121f / 255, 1);
			startMarker.GetComponent<MeshRenderer>().material = mat1;

			finishMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			finishMarker.transform.parent = transform;
			finishMarker.transform.localScale = Vector3.one * 0.2f;
			finishMarker.SetActive(false);
			var mat2 = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
			mat2.color = new Color(85f / 255, 217f / 255, 248f / 255, 1);
			finishMarker.GetComponent<MeshRenderer>().material = mat2;


			_camera = Camera.main;
			_gos = new List<GameObject>();

			MapCore.Initialized += map =>
			{
				if (_fileSource == null)
					_fileSource = map.MapService.FileSource;
				_mapboxDirections = new MapboxDirectionsApi(_fileSource);
				_mapInformation = map.MapInformation;
				_routeRenderer = new CongestionRouteRenderer(_mapInformation, CongestionColors, LineWidth);
			};
		}

		public void Update()
		{
			if (_mapboxDirections == null) return;

			if (Input.GetKey(KeyCode.LeftCommand) && Input.GetMouseButtonDown(0))
			{
				if (!IsFirstPointSet)
				{
					FirstPoint = GetPlaneIntersection(UnityEngine.Input.mousePosition);
					FirstLatLng = _mapInformation.ConvertPositionToLatLng(FirstPoint);
					startMarker.transform.position = FirstPoint;
					startMarker.SetActive(true);
					finishMarker.SetActive(false);
					if (Label != null) Label.SetActive(false);
					foreach (var go in _gos)
					{
						Destroy(go);
					}

					_gos.Clear();
					IsFirstPointSet = true;
				}
				else if (!IsSecondPointSet)
				{
					SecondPoint = GetPlaneIntersection(UnityEngine.Input.mousePosition);
					SecondLatLng = _mapInformation.ConvertPositionToLatLng(SecondPoint);
					finishMarker.transform.position = SecondPoint;
					finishMarker.SetActive(true);
					IsSecondPointSet = true;
				}

				if (IsFirstPointSet && IsSecondPointSet)
				{
					CreateRequest();
					IsFirstPointSet = false;
					IsSecondPointSet = false;
				}
			}
		}

		public void LateUpdate()
		{
			if (_mapboxDirections == null || Label == null) return;

			Label.transform.position = _camera.WorldToScreenPoint(FirstPoint);
		}

		private void CreateRequest()
		{
			var directionParameters = new DirectionResource(new[]
			{
				new LatitudeLongitude(FirstLatLng.Latitude, FirstLatLng.Longitude),
				new LatitudeLongitude(SecondLatLng.Latitude, SecondLatLng.Longitude)
			}, RoutingProfile.GetProfile(RoutingOption))
			{
				Alternatives = false,
				// Congestion coloring needs per-segment annotations, which require the full geometry.
				Annotations = AnnotationOptions.CongestionNumeric,
				Overview = Overview.Full
			};

			_mapboxDirections.Query(directionParameters, response =>
			{
				if (response == null)
					return;

				foreach (var route in response.Routes)
				{
					_gos.AddRange(_routeRenderer.Render(route));
				}

				if (Label != null)
				{
					Label.GetComponentInChildren<Text>().text = string.Format(_labelFormat, response.Routes[0].Distance);
					Label.SetActive(true);
				}
			});
		}

		private Vector3 GetPlaneIntersection(Vector3 screenPosition)
		{
			var ray = _camera.ScreenPointToRay(screenPosition);
			var dirNorm = ray.direction / ray.direction.y;
			var intersectionPos = ray.origin - dirNorm * ray.origin.y;
			return intersectionPos;
		}
	}
}

namespace Mapbox.Unity.MeshGeneration.Factories
{
	using UnityEngine;
	using Mapbox.Directions;
	using System.Collections.Generic;
	using System.Linq;
	using Mapbox.Unity.Map;
	using Data;
	using Modifiers;
	using Mapbox.Platform;
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;

	public class DirectionsFactory : MonoBehaviour
	{
		[SerializeField]
		AbstractMap _map;

		[SerializeField]
		MeshModifier[] MeshModifiers;

		[SerializeField]
		Transform[] _waypoints;

		[SerializeField]
		Material _material;

		Directions _directions;

		void Awake()
		{
			_directions = MapboxAccess.Instance.Directions;
			_map.OnInitialized += Query;
		}

		void OnDestroy()
		{
			_map.OnInitialized -= Query;
		}

		void Query()
		{
			_map.OnInitialized -= Query;
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
			if (null == response.Routes || response.Routes.Count < 1)
			{
				return;
			}

			var meshData = new MeshData();
			var dat = new List<Vector3>();
			foreach (var leg in response.Routes[0].Legs)
			{
				foreach (var point in response.Routes[0].Geometry)
				{
					dat.Add(Conversions.GeoToWorldPosition(point.x, point.y, _map.CenterMercator, _map.WorldRelativeScale).ToVector3xz());
				}
			}

			var feat = new VectorFeatureUnity();
			feat.Points.Add(dat);

			foreach (MeshModifier mod in MeshModifiers.Where(x => x.Active))
			{
				mod.Run(feat, meshData);
			}

			CreateGameObject(meshData);
		}

		GameObject CreateGameObject(MeshData data)
		{
			var go = new GameObject("direction waypoint " + " entity");
			var mesh = go.AddComponent<MeshFilter>().mesh;
			mesh.subMeshCount = data.Triangles.Count;

			mesh.SetVertices(data.Vertices);
			for (int i = 0; i < data.Triangles.Count; i++)
			{
				var triangle = data.Triangles[i];
				mesh.SetTriangles(triangle, i);
			}

			for (int i = 0; i < data.UV.Count; i++)
			{
				var uv = data.UV[i];
				mesh.SetUVs(i, uv);
			}

			mesh.RecalculateNormals();
			go.AddComponent<MeshRenderer>().material = _material;
			return go;
		}
	}

}
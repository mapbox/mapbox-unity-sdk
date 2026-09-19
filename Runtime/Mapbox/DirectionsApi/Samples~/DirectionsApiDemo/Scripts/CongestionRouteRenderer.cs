using System;
using System.Collections.Generic;
using System.Linq;
using Mapbox.BaseModule.Data;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.DirectionsApi.Response;
using UnityEngine;

namespace Mapbox.DirectionsApi.Samples
{
	/// <summary>
	/// Inspector-tweakable colors for each Directions API congestion level. Levels are bucketed from the
	/// numeric <c>congestion_numeric</c> annotation (0 = no congestion, 100 = maximum; null = unknown).
	/// </summary>
	[Serializable]
	public class CongestionColors
	{
		[Tooltip("Color for segments with no congestion data (numeric value null, or route not requested with the driving-traffic profile). Suggested: neutral gray.")]
		public Color Unknown = new Color(0.6f, 0.6f, 0.6f, 1f);

		[Tooltip("Color for 'low' congestion, numeric 0-39 (free-flowing traffic). Suggested: green.")]
		public Color Low = new Color(0.30f, 0.80f, 0.30f, 1f);

		[Tooltip("Color for 'moderate' congestion, numeric 40-59. Suggested: yellow.")]
		public Color Moderate = new Color(0.95f, 0.85f, 0.20f, 1f);

		[Tooltip("Color for 'heavy' congestion, numeric 60-79. Suggested: orange.")]
		public Color Heavy = new Color(0.95f, 0.50f, 0.15f, 1f);

		[Tooltip("Color for 'severe' congestion, numeric 80-100 (near-standstill). Suggested: red.")]
		public Color Severe = new Color(0.85f, 0.20f, 0.20f, 1f);

		/// <summary> Maps a congestion level to its configured color. </summary>
		public Color ColorFor(CongestionLevel level)
		{
			switch (level)
			{
				case CongestionLevel.Low: return Low;
				case CongestionLevel.Moderate: return Moderate;
				case CongestionLevel.Heavy: return Heavy;
				case CongestionLevel.Severe: return Severe;
				default: return Unknown;
			}
		}
	}

	/// <summary>
	/// Builds colored line meshes for a Directions route, one color per traffic
	/// congestion level. Reuses <see cref="LineMeshCore"/> for the extrusion.
	/// <para>Requires the route to have been requested with the driving-traffic profile and
	/// <c>annotations=congestion_numeric</c> + <c>overview=full</c>. When congestion data is absent or
	/// doesn't line up with the geometry, the whole route is drawn in the "unknown" color.</para>
	/// </summary>
	public class CongestionRouteRenderer
	{
		private readonly IMapInformation _mapInformation;
		private readonly CongestionColors _colors;
		private readonly float _width;

		// Reuse one material per distinct color so repeated levels share a material.
		private readonly Dictionary<Color, Material> _materials = new Dictionary<Color, Material>();

		public CongestionRouteRenderer(IMapInformation mapInformation, CongestionColors colors, float width)
		{
			_mapInformation = mapInformation;
			_colors = colors ?? new CongestionColors();
			_width = width;
		}

		/// <summary>
		/// Renders <paramref name="route"/> as one or more colored line-mesh GameObjects and returns them
		/// so the caller can track/destroy them. Returns an empty list if the route has no drawable geometry.
		/// </summary>
		public List<GameObject> Render(Route route)
		{
			var result = new List<GameObject>();
			if (route?.Geometry == null || route.Geometry.Count < 2)
				return result;

			var positions = route.Geometry
				.Select(geo => _mapInformation.ConvertLatLngToPosition(new LatitudeLongitude(geo.x, geo.y)))
				.ToList();

			// One sub-polyline list per congestion level, so each level becomes a single colored mesh.
			var pointsByLevel = new Dictionary<CongestionLevel, List<List<Vector3>>>();

			var congestion = CombineCongestionLevels(route, out var congestionFailureReason);
			if (congestion == null)
			{
				Debug.LogWarning($"CongestionRouteRenderer: {congestionFailureReason} Drawing the whole route in the 'Unknown' color.");
				AddRun(pointsByLevel, CongestionLevel.Unknown, positions, 0, positions.Count - 1);
			}
			else if (congestion.Count != positions.Count - 1)
			{
				Debug.LogWarning($"CongestionRouteRenderer: congestion segment count ({congestion.Count}) does not match geometry segment count ({positions.Count - 1}). Drawing the whole route in the 'Unknown' color.");
				AddRun(pointsByLevel, CongestionLevel.Unknown, positions, 0, positions.Count - 1);
			}
			else
			{
				// Group consecutive segments sharing the same congestion level into runs.
				var runStart = 0;
				for (var seg = 1; seg <= congestion.Count; seg++)
				{
					var boundary = seg == congestion.Count || congestion[seg] != congestion[seg - 1];
					if (!boundary)
						continue;

					// Run covers segments [runStart .. seg-1] -> geometry points [runStart .. seg].
					AddRun(pointsByLevel, congestion[runStart], positions, runStart, seg);
					runStart = seg;
				}
			}

			foreach (var kv in pointsByLevel)
			{
				var go = BuildMesh(kv.Value, _colors.ColorFor(kv.Key));
				if (go != null)
					result.Add(go);
			}

			return result;
		}

		/// <summary>
		/// Concatenates each leg's numeric congestion annotation in order and buckets it into congestion levels,
		/// or returns null (with a reason in <paramref name="failureReason"/>) if the data isn't usable.
		/// </summary>
		private static List<CongestionLevel> CombineCongestionLevels(Route route, out string failureReason)
		{
			if (route.Legs == null || route.Legs.Count == 0)
			{
				failureReason = "Route has no legs.";
				return null;
			}

			var combined = new List<CongestionLevel>();
			for (var i = 0; i < route.Legs.Count; i++)
			{
				var numeric = route.Legs[i]?.Annotation?.CongestionNumeric;
				if (numeric == null)
				{
					failureReason = $"Leg {i} has no congestion_numeric annotation (was the route requested with AnnotationOptions.CongestionNumeric and the driving-traffic profile?).";
					return null;
				}

				foreach (var value in numeric)
					combined.Add(CongestionLevelUtility.LevelForNumeric(value));
			}

			if (combined.Count == 0)
			{
				failureReason = "Congestion annotation arrays were all empty.";
				return null;
			}

			failureReason = null;
			return combined;
		}

		/// <summary> Adds the sub-polyline of geometry points [startPoint .. endPoint] to the given level's bucket. </summary>
		private static void AddRun(Dictionary<CongestionLevel, List<List<Vector3>>> pointsByLevel, CongestionLevel level,
			List<Vector3> positions, int startPoint, int endPoint)
		{
			if (!pointsByLevel.TryGetValue(level, out var runs))
			{
				runs = new List<List<Vector3>>();
				pointsByLevel[level] = runs;
			}

			runs.Add(positions.GetRange(startPoint, endPoint - startPoint + 1));
		}

		private GameObject BuildMesh(List<List<Vector3>> points, Color color)
		{
			var vectorEntity = new VectorEntity
			{
				Feature = new VectorFeatureUnity { Points = points }
			};

			var meshData = new MeshData();
			var lineMeshCore = new LineMeshCore(new LineMeshParameters { Width = _width }, _mapInformation);

			vectorEntity.GameObject = new GameObject("CongestionSegment");
			vectorEntity.MeshFilter = vectorEntity.GameObject.AddComponent<MeshFilter>();
			vectorEntity.MeshRenderer = vectorEntity.GameObject.AddComponent<MeshRenderer>();

			lineMeshCore.Run(vectorEntity.Feature, meshData);

			if (meshData.Vertices.Count == 0 || meshData.Triangles.Count == 0 || !AllFinite(meshData.Vertices))
			{
				// Skip degenerate/invalid runs rather than let Unity throw on an invalid mesh bounds.
				Debug.LogWarning("CongestionRouteRenderer: skipping a run because the generated mesh was empty or contained non-finite vertices.");
				UnityEngine.Object.Destroy(vectorEntity.GameObject);
				return null;
			}

			vectorEntity.Mesh = new Mesh
			{
				vertices = meshData.Vertices.ToArray(),
				triangles = meshData.Triangles[0].ToArray()
			};
			vectorEntity.MeshFilter.sharedMesh = vectorEntity.Mesh;
			vectorEntity.GameObject.transform.position += new Vector3(0.0f, 1 / _mapInformation.Scale, 0.0f);
			vectorEntity.MeshRenderer.sharedMaterial = GetMaterial(color);
			return vectorEntity.GameObject;
		}

		private static bool AllFinite(List<Vector3> vertices)
		{
			foreach (var v in vertices)
			{
				if (float.IsNaN(v.x) || float.IsInfinity(v.x) ||
				    float.IsNaN(v.y) || float.IsInfinity(v.y) ||
				    float.IsNaN(v.z) || float.IsInfinity(v.z))
				{
					return false;
				}
			}

			return true;
		}

		private Material GetMaterial(Color color)
		{
			if (_materials.TryGetValue(color, out var material))
				return material;

			material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = color };
			_materials[color] = material;
			return material;
		}
	}
}

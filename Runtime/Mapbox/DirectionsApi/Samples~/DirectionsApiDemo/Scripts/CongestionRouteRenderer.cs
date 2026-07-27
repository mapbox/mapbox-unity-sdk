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

		/// <summary> Maps a congestion level key to its configured color. </summary>
		public Color ColorFor(string level)
		{
			switch ((level ?? string.Empty).ToLowerInvariant())
			{
				case "low": return Low;
				case "moderate": return Moderate;
				case "heavy": return Heavy;
				case "severe": return Severe;
				default: return Unknown;
			}
		}

		/// <summary>
		/// Buckets a <c>congestion_numeric</c> value (0-100, or null for unknown) into a level key.
		/// Thresholds match the Mapbox Navigation SDK defaults: 0-39 low, 40-59 moderate, 60-79 heavy, 80-100 severe.
		/// </summary>
		public static string LevelForNumeric(int? value)
		{
			if (value == null) return "unknown";

			var v = value.Value;
			if (v < 40) return "low";
			if (v < 60) return "moderate";
			if (v < 80) return "heavy";
			return "severe";
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
			var pointsByLevel = new Dictionary<string, List<List<Vector3>>>();

			var congestion = CombineCongestionLevels(route);
			if (congestion == null || congestion.Count != positions.Count - 1)
			{
				// No/misaligned congestion data (e.g. non-traffic profile): draw the whole route in one color.
				AddRun(pointsByLevel, "unknown", positions, 0, positions.Count - 1);
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
		/// Concatenates each leg's numeric congestion annotation in order and buckets it into level keys,
		/// or returns null if any leg lacks the annotation.
		/// </summary>
		private static List<string> CombineCongestionLevels(Route route)
		{
			if (route.Legs == null || route.Legs.Count == 0)
				return null;

			var combined = new List<string>();
			foreach (var leg in route.Legs)
			{
				var numeric = leg?.Annotation?.CongestionNumeric;
				if (numeric == null)
					return null;

				foreach (var value in numeric)
					combined.Add(CongestionColors.LevelForNumeric(value));
			}

			return combined.Count > 0 ? combined : null;
		}

		/// <summary> Adds the sub-polyline of geometry points [startPoint .. endPoint] to the given level's bucket. </summary>
		private static void AddRun(Dictionary<string, List<List<Vector3>>> pointsByLevel, string level,
			List<Vector3> positions, int startPoint, int endPoint)
		{
			var key = (level ?? string.Empty).ToLowerInvariant();
			if (!pointsByLevel.TryGetValue(key, out var runs))
			{
				runs = new List<List<Vector3>>();
				pointsByLevel[key] = runs;
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

			vectorEntity.Mesh = new Mesh();
			vectorEntity.Mesh.SetMeshValues(meshData);
			vectorEntity.GameObject = new GameObject("CongestionSegment");
			vectorEntity.MeshFilter = vectorEntity.GameObject.AddComponent<MeshFilter>();
			vectorEntity.MeshFilter.mesh = vectorEntity.Mesh;
			vectorEntity.MeshRenderer = vectorEntity.GameObject.AddComponent<MeshRenderer>();

			lineMeshCore.Run(vectorEntity.Feature, meshData);

			if (meshData.Vertices.Count == 0 || meshData.Triangles.Count == 0 || !AllFinite(meshData.Vertices))
			{
				// Skip degenerate/invalid runs rather than let Unity throw on an invalid mesh bounds.
				UnityEngine.Object.Destroy(vectorEntity.GameObject);
				return null;
			}

			vectorEntity.MeshFilter.mesh = new Mesh
			{
				vertices = meshData.Vertices.ToArray(),
				triangles = meshData.Triangles[0].ToArray()
			};
			vectorEntity.GameObject.transform.position += new Vector3(0.0f, 1 / _mapInformation.Scale, 0.0f);
			vectorEntity.MeshRenderer.material = GetMaterial(color);
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

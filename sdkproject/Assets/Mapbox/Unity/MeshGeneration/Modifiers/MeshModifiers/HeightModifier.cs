namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;
	using Mapbox.Unity.Map;

	//public enum ExtrusionType
	//{
	//	Wall,
	//	FirstMidFloor,
	//	FirstMidTopFloor
	//}

	/// <summary>
	/// Height Modifier is responsible for the y axis placement of the feature. It pushes the original vertices upwards by "height" value and creates side walls around that new polygon down to "min_height" value.
	/// It also checkes for "ele" (elevation) value used for contour lines in Mapbox Terrain data. 
	/// Height Modifier also creates a continuous UV mapping for side walls.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Height Modifier")]
	public class HeightModifier : MeshModifier
	{
		//[SerializeField]
		//[Tooltip("Flatten top polygons to prevent unwanted slanted roofs because of the bumpy terrain")]
		//private bool _flatTops;

		//[SerializeField]
		//[Tooltip("Fix all features to certain height, suggested to be used for pushing roads above terrain level to prevent z-fighting.")]
		//private bool _forceHeight;

		//[SerializeField]
		//[Tooltip("Fixed height value for ForceHeight option")]
		//private float _height;
		private float _scale = 1;
		private bool _separateSubmesh = false;

		//[SerializeField]
		//[Tooltip("Create side walls from calculated height down to terrain level. Suggested for buildings, not suggested for roads.")]
		//private bool _createSideWalls = true;

		GeometryExtrusionOptions _options;

		public override ModifierType Type { get { return ModifierType.Preprocess; } }

		private int _counter = 0;

		class MinMaxPair
		{
			public float min, max;
		}

		MinMaxPair GetMinMaxHeight(List<Vector3> vertices)
		{
			int counter = vertices.Count;
			MinMaxPair returnValue = new MinMaxPair
			{
				max = float.MinValue,
				min = float.MaxValue
			};

			for (int i = 0; i < counter; i++)
			{
				if (vertices[i].y > returnValue.max)
					returnValue.max = vertices[i].y;
				else if (vertices[i].y < returnValue.min)
					returnValue.min = vertices[i].y;
			}

			return returnValue;
		}
		public override void SetProperties(ModifierProperties properties)
		{
			_options = (GeometryExtrusionOptions)properties;
		}
		public override void Run(VectorFeatureUnity feature, MeshData md, float scale)
		{
			_scale = scale;
			Run(feature, md);
		}


		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			if (md.Vertices.Count == 0 || feature == null || feature.Points.Count < 1)
				return;

			if (tile != null)
				_scale = tile.TileScale;

			var minHeight = 0f;
			float hf = 0.0f;

			switch (_options.extrusionType)
			{
				case ExtrusionType.None:
					break;
				case ExtrusionType.PropertyHeight:
				case ExtrusionType.MinHeight:
				case ExtrusionType.MaxHeight:
					if (feature.Properties.ContainsKey(_options.propertyName))
					{
						hf = Convert.ToSingle(feature.Properties[_options.propertyName]);
						if (feature.Properties.ContainsKey("min_height"))
						{
							minHeight = Convert.ToSingle(feature.Properties["min_height"]);
							hf -= minHeight;
						}
					}
					break;
				case ExtrusionType.RangeHeight:
					if (feature.Properties.ContainsKey(_options.propertyName))
					{
						var featureHeight = Convert.ToSingle(feature.Properties[_options.propertyName]);
						hf = Math.Min(Math.Max(_options.minimumHeight, featureHeight), _options.maximumHeight);
					}
					break;
				case ExtrusionType.AbsoluteHeight:
					hf = _options.maximumHeight;
					break;
				default:
					break;
			}

			hf = hf * _scale;
			//if (!_forceHeight)
			//{
			//	if (feature.Properties.ContainsKey("height"))
			//	{
			//		hf = Convert.ToSingle(feature.Properties["height"]);
			//		hf *= _scale;
			//		if (feature.Properties.ContainsKey("min_height"))
			//		{
			//			minHeight = Convert.ToSingle(feature.Properties["min_height"]) * _scale;
			//			hf -= minHeight;
			//		}
			//	}
			//	else if (feature.Properties.ContainsKey("ele"))
			//	{
			//		//"ele" is used in contour layer for elevation
			//		hf = Convert.ToSingle(feature.Properties["ele"]);
			//		hf *= _scale;
			//	}
			//}

			var max = md.Vertices[0].y;
			var min = md.Vertices[0].y;


			if (_options.extrusionGeometryType != ExtrusionGeometryType.SideOnly)
			{
				_counter = md.Vertices.Count;
				switch (_options.extrusionType)
				{
					case ExtrusionType.None:
						break;
					case ExtrusionType.PropertyHeight:
						for (int i = 0; i < _counter; i++)
						{
							md.Vertices[i] = new Vector3(md.Vertices[i].x, md.Vertices[i].y + minHeight + hf, md.Vertices[i].z);
						}
						break;
					case ExtrusionType.MinHeight:
						{
							var minmax = GetMinMaxHeight(md.Vertices);
							for (int i = 0; i < _counter; i++)
							{
								md.Vertices[i] = new Vector3(md.Vertices[i].x, minmax.min + minHeight + hf, md.Vertices[i].z);
							}
						}
						//hf += max - min;
						break;
					case ExtrusionType.MaxHeight:
						{
							var minmax = GetMinMaxHeight(md.Vertices);
							for (int i = 0; i < _counter; i++)
							{
								md.Vertices[i] = new Vector3(md.Vertices[i].x, max + minHeight + hf, md.Vertices[i].z);
							}
							hf += max - min;
						}
						break;
					case ExtrusionType.RangeHeight:
						break;
					case ExtrusionType.AbsoluteHeight:
						for (int i = 0; i < _counter; i++)
						{
							md.Vertices[i] = new Vector3(md.Vertices[i].x, minHeight + hf, md.Vertices[i].z);
						}
						break;
					default:
						break;
				}
			}

			//if (_flatTops)
			//{
			//	for (int i = 0; i < _counter; i++)
			//	{
			//		if (md.Vertices[i].y > max)
			//			max = md.Vertices[i].y;
			//		else if (md.Vertices[i].y < min)
			//			min = md.Vertices[i].y;
			//	}
			//	for (int i = 0; i < _counter; i++)
			//	{
			//		md.Vertices[i] = new Vector3(md.Vertices[i].x, max + minHeight + hf, md.Vertices[i].z);
			//	}
			//	hf += max - min;
			//}
			//else
			//{
			//	for (int i = 0; i < _counter; i++)
			//	{
			//		md.Vertices[i] = new Vector3(md.Vertices[i].x, md.Vertices[i].y + minHeight + hf, md.Vertices[i].z);
			//	}
			//}


			md.Vertices.Capacity = _counter + md.Edges.Count * 2;
			float d = 0f;
			Vector3 v1;
			Vector3 v2;
			int ind = 0;

			if (_options.extrusionGeometryType != ExtrusionGeometryType.RoofOnly)
			{
				_counter = md.Edges.Count;
				var wallTri = new List<int>(_counter * 3);
				var wallUv = new List<Vector2>(_counter * 2);
				Vector3 norm = Constants.Math.Vector3Zero;

				md.Vertices.Capacity = md.Vertices.Count + _counter * 2;
				md.Normals.Capacity = md.Normals.Count + _counter * 2;

				for (int i = 0; i < _counter; i += 2)
				{
					v1 = md.Vertices[md.Edges[i]];
					v2 = md.Vertices[md.Edges[i + 1]];
					ind = md.Vertices.Count;
					md.Vertices.Add(v1);
					md.Vertices.Add(v2);
					md.Vertices.Add(new Vector3(v1.x, v1.y - hf, v1.z));
					md.Vertices.Add(new Vector3(v2.x, v2.y - hf, v2.z));

					//d = (v2 - v1).magnitude;
					d = Mathf.Sqrt((v2.x - v1.x) + (v2.y - v1.y) + (v2.z - v1.z));
					norm = Vector3.Normalize(Vector3.Cross(v2 - v1, md.Vertices[ind + 2] - v1));
					md.Normals.Add(norm);
					md.Normals.Add(norm);
					md.Normals.Add(norm);
					md.Normals.Add(norm);

					wallUv.Add(new Vector2(0, 0));
					wallUv.Add(new Vector2(d, 0));
					wallUv.Add(new Vector2(0, -hf));
					wallUv.Add(new Vector2(d, -hf));

					wallTri.Add(ind);
					wallTri.Add(ind + 1);
					wallTri.Add(ind + 2);

					wallTri.Add(ind + 1);
					wallTri.Add(ind + 3);
					wallTri.Add(ind + 2);
				}

				// TODO: Do we really need this?
				if (_separateSubmesh)
				{
					md.Triangles.Add(wallTri);
				}
				else
				{
					md.Triangles.Capacity = md.Triangles.Count + wallTri.Count;
					md.Triangles[0].AddRange(wallTri);
				}
				md.UV[0].AddRange(wallUv);
			}
		}
	}
}

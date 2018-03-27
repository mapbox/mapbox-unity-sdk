namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Map;
	using System;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Textured Side Wall Modifier")]
	public class TextureSideWallModifier : MeshModifier
	{
		//[SerializeField]
		//private float _height;
		//[SerializeField]
		//private bool _forceHeight;

		private float _scaledFloorHeight = 0;
		private float _scaledFirstFloorHeight = 0;
		private float _scaledTopFloorHeight = 0;
		private int _maxEdgeSectionCount = 40;

		[SerializeField]
		private bool _centerSegments = true;
		[SerializeField]
		private bool _separateSubmesh = true;

		private List<Vector3> edgeList;
		float dist = 0;
		float step = 0;
		float dif = 0;
		Vector3 start = Constants.Math.Vector3Zero;
		Vector3 wallDirection = Constants.Math.Vector3Zero;
		Vector3 fs;
		Vector3 sc;
		float d;
		Vector3 v1;
		Vector3 v2;

		//public AtlasInfo AtlasInfo;
		private AtlasEntity _currentFacade;
		private Rect _currentTextureRect;

		private float firstHeight;
		private float topHeight;
		private float midHeight;
		private int floorCount;
		private float scaledFloorHeight;
		private int ind;
		private Vector3 wallNormal;
		private List<int> wallTriangles;
		private float columnScaleRatio;
		private float floorScaleRatio;
		private float rightOfEdgeUv;
		private float bottomOfTopUv;
		private float topOfBottomUv;
		private float currentY;
		private float bottomOfMidUv;
		private float topOfMidUv;

		GeometryExtrusionWithAtlasOptions _options;
		private int _counter = 0;
		float height = 0.0f;
		float _scale = 1f;

		public override void SetProperties(ModifierProperties properties)
		{
			_options = (GeometryExtrusionWithAtlasOptions)properties;
		}

		public override void Initialize()
		{
			base.Initialize();
			edgeList = new List<Vector3>();
		}

		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			if (md.Vertices.Count == 0 || feature == null || feature.Points.Count < 1)
				return;

			if (tile != null)
				_scale = tile.TileScale;

			_currentFacade = _options.atlasInfo.Textures[UnityEngine.Random.Range(0, _options.atlasInfo.Textures.Count)];
			//rect is a struct so we're caching this
			_currentTextureRect = _currentFacade.TextureRect;

			//this can be moved to initialize or in an if clause if you're sure all your tiles will be same level/scale
			_scaledFloorHeight = tile.TileScale * _currentFacade.FloorHeight;
			_scaledFirstFloorHeight = tile.TileScale * _currentFacade.FirstFloorHeight;
			_scaledTopFloorHeight = tile.TileScale * _currentFacade.TopFloorHeight;

			//read or force height
			float maxHeight = 1, minHeight = 0;
			//SetHeight(feature, md, tile, out maxHeight, out minHeight);
			//height = maxHeight - minHeight;
			//for (int i = 0; i < md.Vertices.Count; i++)
			//{
			//	md.Vertices[i] = new Vector3(md.Vertices[i].x, md.Vertices[i].y + maxHeight, md.Vertices[i].z);
			//}

			QueryHeight(feature, md, tile, out maxHeight, out minHeight);
			height = (maxHeight - minHeight) * _scale;
			maxHeight = maxHeight * _scale;
			minHeight = minHeight * _scale;

			GenerateRoofMesh(md, minHeight, maxHeight);

			if (_options.extrusionGeometryType != ExtrusionGeometryType.RoofOnly)
			{
				edgeList.Clear();
				//cuts long edges into smaller ones using PreferredEdgeSectionLength
				CalculateEdgeList(md, tile, _currentFacade.PreferredEdgeSectionLength);

				//limiting section heights, first floor gets priority, then we draw top floor, then mid if we still have space
				firstHeight = Mathf.Min(height, _scaledFirstFloorHeight);
				topHeight = Mathf.Min(height - firstHeight, _scaledTopFloorHeight);
				midHeight = Mathf.Max(0, height - (firstHeight + topHeight));

				//we're merging small mid sections to top and small top sections to first floor to avoid really short/compressed floors
				//I think we need this but I'm not sure about implementation. I feel like mid height should be shared by top&bottom for example.
				if (midHeight < _scaledFloorHeight / (_currentFacade.MidFloorCount * 2))
				{
					topHeight += midHeight;
					_scaledTopFloorHeight += midHeight;
					midHeight = 0;
				}
				if (topHeight < _scaledTopFloorHeight * 0.66f) //0.66 here is just a random number for acceptable stretching
				{
					firstHeight += topHeight;
					topHeight = 0;
				}

				floorCount = (int)(midHeight / _scaledFloorHeight) + 1;
				scaledFloorHeight = midHeight / floorCount;
				wallTriangles = new List<int>();

				//this first loop is for columns
				for (int i = 0; i < edgeList.Count - 1; i += 2)
				{
					v1 = edgeList[i];
					v2 = edgeList[i + 1];
					ind = md.Vertices.Count;
					wallDirection = (v2 - v1);
					d = wallDirection.magnitude;

					//this part minimizes stretching for narrow columns
					//if texture has 3 columns, 33% (of preferred edge length) wide walls will get 1 window.
					//0-33% gets 1 window, 33-66 gets 2, 66-100 gets all three
					//we're not wrapping/repeating texture as it won't work with atlases
					columnScaleRatio = Math.Min(1, d / (_currentFacade.PreferredEdgeSectionLength * tile.TileScale));
					rightOfEdgeUv = _currentTextureRect.xMin + _currentTextureRect.size.x * Math.Min(1, ((float)(Math.Floor(columnScaleRatio * _currentFacade.ColumnCount) + 1) / _currentFacade.ColumnCount));
					bottomOfTopUv = _currentTextureRect.yMax - (_currentTextureRect.size.y * _currentFacade.TopSectionRatio); //not doing that scaling thing for y axis and floors yet
					topOfBottomUv = _currentTextureRect.yMin + (_currentTextureRect.size.y * _currentFacade.BottomSectionRatio); // * (Mathf.Max(1, (float)Math.Floor(tby * textureSection.TopSectionFloorCount)) / textureSection.TopSectionFloorCount);

					wallNormal = new Vector3(-(v1.z - v2.z), 0, (v1.x - v2.x)).normalized;
					currentY = v1.y;

					floorScaleRatio = Math.Min(1, midHeight / _scaledFloorHeight);
					var midSecHeight = (_currentTextureRect.height * (1 - _currentFacade.TopSectionRatio - _currentFacade.BottomSectionRatio));
					var midFittedHeight = midSecHeight * Math.Min(1, ((float)(Math.Floor(floorScaleRatio * _currentFacade.MidFloorCount) + 1) / _currentFacade.MidFloorCount)); // midHeight < _scaledFloorHeight * 0.66 ? 0.5f : 0.125f;
					bottomOfMidUv = (_currentTextureRect.yMax - (_currentTextureRect.height * _currentFacade.TopSectionRatio)) - midFittedHeight;
					topOfMidUv = _currentTextureRect.yMax - (_currentTextureRect.height * _currentFacade.TopSectionRatio);

					TopFloor(md);
					MidFloors(md);
					FirstFloor(md, height);
				}

				if (_separateSubmesh)
				{
					md.Triangles.Add(wallTriangles);
				}
				else
				{
					md.Triangles.Capacity = md.Triangles.Count + wallTriangles.Count;
					md.Triangles[0].AddRange(wallTriangles);
				}
			}
		}

		private void MidFloors(MeshData md)
		{
			for (int f = 0; f < floorCount; f++)
			{
				currentY -= scaledFloorHeight;

				md.Vertices.Add(new Vector3(v1.x, currentY + scaledFloorHeight, v1.z));
				md.Vertices.Add(new Vector3(v2.x, currentY + scaledFloorHeight, v2.z));
				md.Vertices.Add(new Vector3(v1.x, currentY, v1.z));
				md.Vertices.Add(new Vector3(v2.x, currentY, v2.z));

				md.UV[0].Add(new Vector2(_currentTextureRect.xMin, topOfMidUv));
				md.UV[0].Add(new Vector2(rightOfEdgeUv, topOfMidUv));
				md.UV[0].Add(new Vector2(_currentTextureRect.xMin, bottomOfMidUv));
				md.UV[0].Add(new Vector2(rightOfEdgeUv, bottomOfMidUv));

				md.Normals.Add(wallNormal);
				md.Normals.Add(wallNormal);
				md.Normals.Add(wallNormal);
				md.Normals.Add(wallNormal);

				md.Tangents.Add(wallDirection);
				md.Tangents.Add(wallDirection);
				md.Tangents.Add(wallDirection);
				md.Tangents.Add(wallDirection);

				wallTriangles.Add(ind);
				wallTriangles.Add(ind + 1);
				wallTriangles.Add(ind + 2);

				wallTriangles.Add(ind + 1);
				wallTriangles.Add(ind + 3);
				wallTriangles.Add(ind + 2);

				ind += 4;
			}
		}

		private void TopFloor(MeshData md)
		{
			currentY -= topHeight;
			md.Vertices.Add(new Vector3(v1.x, v1.y, v1.z));
			md.Vertices.Add(new Vector3(v2.x, v2.y, v2.z));
			md.Vertices.Add(new Vector3(v1.x, v1.y - topHeight, v1.z));
			md.Vertices.Add(new Vector3(v2.x, v2.y - topHeight, v2.z));

			md.UV[0].Add(new Vector2(_currentTextureRect.xMin, _currentTextureRect.yMax));
			md.UV[0].Add(new Vector2(rightOfEdgeUv, _currentTextureRect.yMax));
			md.UV[0].Add(new Vector2(_currentTextureRect.xMin, bottomOfTopUv));
			md.UV[0].Add(new Vector2(rightOfEdgeUv, bottomOfTopUv));

			md.Normals.Add(wallNormal);
			md.Normals.Add(wallNormal);
			md.Normals.Add(wallNormal);
			md.Normals.Add(wallNormal);


			md.Tangents.Add(wallDirection);
			md.Tangents.Add(wallDirection);
			md.Tangents.Add(wallDirection);
			md.Tangents.Add(wallDirection);

			wallTriangles.Add(ind);
			wallTriangles.Add(ind + 1);
			wallTriangles.Add(ind + 2);

			wallTriangles.Add(ind + 1);
			wallTriangles.Add(ind + 3);
			wallTriangles.Add(ind + 2);

			ind += 4;
		}

		private void FirstFloor(MeshData md, float hf)
		{
			md.Vertices.Add(new Vector3(v1.x, v1.y - hf + firstHeight, v1.z));
			md.Vertices.Add(new Vector3(v2.x, v2.y - hf + firstHeight, v2.z));
			md.Vertices.Add(new Vector3(v1.x, v1.y - hf, v1.z));
			md.Vertices.Add(new Vector3(v2.x, v2.y - hf, v2.z));

			md.Normals.Add(wallNormal);
			md.Normals.Add(wallNormal);
			md.Normals.Add(wallNormal);
			md.Normals.Add(wallNormal);
			md.Tangents.Add(wallDirection);
			md.Tangents.Add(wallDirection);
			md.Tangents.Add(wallDirection);
			md.Tangents.Add(wallDirection);

			d = (v2 - v1).magnitude;
			md.UV[0].Add(new Vector2(_currentTextureRect.xMin, topOfBottomUv));
			md.UV[0].Add(new Vector2(rightOfEdgeUv, topOfBottomUv));
			md.UV[0].Add(new Vector2(_currentTextureRect.xMin, _currentTextureRect.yMin));
			md.UV[0].Add(new Vector2(rightOfEdgeUv, _currentTextureRect.yMin));


			wallTriangles.Add(ind);
			wallTriangles.Add(ind + 1);
			wallTriangles.Add(ind + 2);

			wallTriangles.Add(ind + 1);
			wallTriangles.Add(ind + 3);
			wallTriangles.Add(ind + 2);

			ind += 4;
		}

		private void CalculateEdgeList(MeshData md, UnityTile tile, float preferredEdgeSectionLength)
		{
			dist = 0;
			step = 0;
			dif = 0;
			start = Constants.Math.Vector3Zero;
			wallDirection = Constants.Math.Vector3Zero;
			for (int i = 0; i < md.Edges.Count; i += 2)
			{
				fs = md.Vertices[md.Edges[i]];
				sc = md.Vertices[md.Edges[i + 1]];

				dist = Vector3.Distance(fs, sc);
				step = Mathf.Min(_maxEdgeSectionCount, dist / (preferredEdgeSectionLength * tile.TileScale));

				start = fs;
				edgeList.Add(start);
				wallDirection = (sc - fs).normalized;
				if (_centerSegments && step > 1)
				{
					dif = dist - ((int)step * (preferredEdgeSectionLength * tile.TileScale));
					//prevent new point being to close to existing corner
					if (dif > 2 * tile.TileScale)
					{
						//first step, original point or another close point if sections are centered
						start = fs + (wallDirection * (dif / 2));
						//to compansate step-1 below, so if there's more than 2m to corner, go one more step
					}
					edgeList.Add(start);
					edgeList.Add(start);
				}
				if (step > 1)
				{
					for (int s = 1; s < step; s++)
					{
						var da = start + wallDirection * s * (preferredEdgeSectionLength * tile.TileScale);
						edgeList.Add(da);
						edgeList.Add(da);
					}
				}

				edgeList.Add(sc);
			}
		}

		private void GenerateRoofMesh(MeshData md, float minHeight, float maxHeight)
		{
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
							md.Vertices[i] = new Vector3(md.Vertices[i].x, md.Vertices[i].y + maxHeight, md.Vertices[i].z);
						}
						break;
					case ExtrusionType.MinHeight:
						{
							var minmax = MinMaxPair.GetMinMaxHeight(md.Vertices);
							for (int i = 0; i < _counter; i++)
							{
								md.Vertices[i] = new Vector3(md.Vertices[i].x, minmax.min + maxHeight, md.Vertices[i].z);
							}
						}
						//hf += max - min;
						break;
					case ExtrusionType.MaxHeight:
						{
							var minmax = MinMaxPair.GetMinMaxHeight(md.Vertices);
							for (int i = 0; i < _counter; i++)
							{
								md.Vertices[i] = new Vector3(md.Vertices[i].x, minmax.max + maxHeight, md.Vertices[i].z);
							}
							height += minmax.max - minmax.min;
						}
						break;
					case ExtrusionType.RangeHeight:
						break;
					case ExtrusionType.AbsoluteHeight:
						for (int i = 0; i < _counter; i++)
						{
							md.Vertices[i] = new Vector3(md.Vertices[i].x, maxHeight, md.Vertices[i].z);
						}
						break;
					default:
						break;
				}
			}
		}

		private void QueryHeight(VectorFeatureUnity feature, MeshData md, UnityTile tile, out float maxHeight, out float minHeight)
		{
			minHeight = 0.0f;
			maxHeight = 0.0f;

			switch (_options.extrusionType)
			{
				case ExtrusionType.None:
					break;
				case ExtrusionType.PropertyHeight:
				case ExtrusionType.MinHeight:
				case ExtrusionType.MaxHeight:
					if (feature.Properties.ContainsKey(_options.propertyName))
					{
						maxHeight = Convert.ToSingle(feature.Properties[_options.propertyName]);
						if (feature.Properties.ContainsKey("min_height"))
						{
							minHeight = Convert.ToSingle(feature.Properties["min_height"]);
							//hf -= minHeight;
						}
					}
					break;
				case ExtrusionType.RangeHeight:
					if (feature.Properties.ContainsKey(_options.propertyName))
					{
						var featureHeight = Convert.ToSingle(feature.Properties[_options.propertyName]);
						maxHeight = Math.Min(Math.Max(_options.minimumHeight, featureHeight), _options.maximumHeight);
					}
					break;
				case ExtrusionType.AbsoluteHeight:
					maxHeight = _options.maximumHeight;
					break;
				default:
					break;
			}
		}
	}
}

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
		#region ModifierOptions

		private float _scaledFirstFloorHeight = 0;

		private float _scaledTopFloorHeight = 0;

		//private int _maxEdgeSectionCount = 40;
		private float _scaledPreferredWallLength;
		[SerializeField] private bool _centerSegments = true;
		[SerializeField] private bool _separateSubmesh = true;

		#endregion

		float currentWallLength = 0;
		Vector3 start = Constants.Math.Vector3Zero;
		Vector3 wallDirection = Constants.Math.Vector3Zero;

		Vector3 wallSegmentFirstVertex;
		Vector3 wallSegmentSecondVertex;
		Vector3 wallSegmentDirection;
		float wallSegmentLength;

		//public AtlasInfo AtlasInfo;
		private AtlasEntity _currentFacade;
		private Rect _currentTextureRect;

		private float finalFirstHeight;
		private float finalTopHeight;
		private float finalMidHeight;
		private float finalLeftOverRowHeight;
		private float _scaledFloorHeight;
		private int triIndex;
		private Vector3 wallNormal;
		private List<int> wallTriangles;
		private float columnScaleRatio;
		private float rightOfEdgeUv;

		private float currentY1;
		private float currentY2;
		private float _wallSizeEpsilon = 0.99f;
		private float _narrowWallWidthDelta = 0.01f;
		private float _shortRowHeightDelta = 0.015f;

		GeometryExtrusionWithAtlasOptions _options;
		private int _counter = 0;
		private float height = 0.0f;
		private float _scale = 1f;
		private float _minWallLength;
		private float _singleFloorHeight;
		private float _currentMidHeight;
		private float _midUvInCurrentStep;
		private float _singleColumnLength;
		private float _leftOverColumnLength;

		public override void SetProperties(ModifierProperties properties)
		{
			if (properties is GeometryExtrusionWithAtlasOptions)
			{
				_options = (GeometryExtrusionWithAtlasOptions)properties;
			}
			else if (properties is GeometryExtrusionOptions)
			{
				_options = ((GeometryExtrusionOptions)properties).ToGeometryExtrusionWithAtlasOptions();
			}
			else if (properties is UVModifierOptions)
			{
				_options = ((UVModifierOptions)properties).ToGeometryExtrusionWithAtlasOptions();
			}
		}

		public override void UnbindProperties()
		{
			_options.PropertyHasChanged -= UpdateModifier;
		}

		public override void Initialize()
		{
			base.Initialize();
			foreach (var atlasEntity in _options.atlasInfo.Textures)
			{
				atlasEntity.CalculateParameters();
			}
		}

		public override void UpdateModifier(object sender, System.EventArgs layerArgs)
		{
			SetProperties((ModifierProperties)sender);
			NotifyUpdateModifier(new VectorLayerUpdateArgs { property = sender as MapboxDataProperty, modifier = this });
		}

		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{
			if (md.Vertices.Count == 0 || feature == null || feature.Points.Count < 1)
				return;

			if (tile != null)
				_scale = tile.TileScale;

			//facade texture to decorate this building
			_currentFacade =
				_options.atlasInfo.Textures[UnityEngine.Random.Range(0, _options.atlasInfo.Textures.Count)];
			//rect is a struct so we're caching this
			_currentTextureRect = _currentFacade.TextureRect;

			//this can be moved to initialize or in an if clause if you're sure all your tiles will be same level/scale
			_singleFloorHeight = (tile.TileScale * _currentFacade.FloorHeight) / _currentFacade.MidFloorCount;
			_scaledFirstFloorHeight = tile.TileScale * _currentFacade.FirstFloorHeight;
			_scaledTopFloorHeight = tile.TileScale * _currentFacade.TopFloorHeight;
			_scaledPreferredWallLength = tile.TileScale * _currentFacade.PreferredEdgeSectionLength;
			_scaledFloorHeight = _scaledPreferredWallLength * _currentFacade.WallToFloorRatio;
			_singleColumnLength = _scaledPreferredWallLength / _currentFacade.ColumnCount;

			//read or force height
			float maxHeight = 1, minHeight = 0;

			//query height and push polygon up to create roof
			//can we do this vice versa and create roof at last?
			QueryHeight(feature, md, tile, out maxHeight, out minHeight);
			maxHeight = maxHeight * _options.extrusionScaleFactor * _scale;
			minHeight = minHeight * _options.extrusionScaleFactor * _scale;
			height = (maxHeight - minHeight);
			GenerateRoofMesh(md, minHeight, maxHeight);

			if (_options.extrusionGeometryType != ExtrusionGeometryType.RoofOnly)
			{
				//limiting section heights, first floor gets priority, then we draw top floor, then mid if we still have space
				finalFirstHeight = Mathf.Min(height, _scaledFirstFloorHeight);
				finalTopHeight = (height - finalFirstHeight) < _scaledTopFloorHeight ? 0 : _scaledTopFloorHeight;
				finalMidHeight = Mathf.Max(0, height - (finalFirstHeight + finalTopHeight));
				//scaledFloorHeight = midHeight / floorCount;
				wallTriangles = new List<int>();

				//cuts long edges into smaller ones using PreferredEdgeSectionLength
				currentWallLength = 0;
				start = Constants.Math.Vector3Zero;
				wallSegmentDirection = Constants.Math.Vector3Zero;

				finalLeftOverRowHeight = 0f;
				if (finalMidHeight > 0)
				{
					finalLeftOverRowHeight = finalMidHeight;
					finalLeftOverRowHeight = finalLeftOverRowHeight % _singleFloorHeight;
					finalMidHeight -= finalLeftOverRowHeight;
				}
				else
				{
					finalLeftOverRowHeight = finalTopHeight;
				}

				for (int i = 0; i < md.Edges.Count; i += 2)
				{
					var v1 = md.Vertices[md.Edges[i]];
					var v2 = md.Vertices[md.Edges[i + 1]];

					wallDirection = v2 - v1;

					currentWallLength = Vector3.Distance(v1, v2);
					_leftOverColumnLength = currentWallLength % _singleColumnLength;
					start = v1;
					wallSegmentDirection = (v2 - v1).normalized;

					//half of leftover column (if _centerSegments ofc) at the begining
					if (_centerSegments && currentWallLength > _singleColumnLength)
					{
						//save left,right vertices and wall length
						wallSegmentFirstVertex = start;
						wallSegmentLength = (_leftOverColumnLength / 2);
						start += wallSegmentDirection * wallSegmentLength;
						wallSegmentSecondVertex = start;

						_leftOverColumnLength = _leftOverColumnLength / 2;
						CreateWall(md);
					}

					while (currentWallLength > _singleColumnLength)
					{
						wallSegmentFirstVertex = start;
						//columns fitting wall / max column we have in texture
						var stepRatio =
							(float)Math.Min(_currentFacade.ColumnCount,
								Math.Floor(currentWallLength / _singleColumnLength)) / _currentFacade.ColumnCount;
						wallSegmentLength = stepRatio * _scaledPreferredWallLength;
						start += wallSegmentDirection * wallSegmentLength;
						wallSegmentSecondVertex = start;

						currentWallLength -= (stepRatio * _scaledPreferredWallLength);
						CreateWall(md);
					}

					//left over column at the end
					if (_leftOverColumnLength > 0)
					{
						wallSegmentFirstVertex = start;
						wallSegmentSecondVertex = v2;
						wallSegmentLength = _leftOverColumnLength;
						CreateWall(md);
					}
				}

				//this first loop is for columns
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

		private void CreateWall(MeshData md)
		{
			//need to keep track of this for triangulation indices
			triIndex = md.Vertices.Count;

			//this part minimizes stretching for narrow columns
			//if texture has 3 columns, 33% (of preferred edge length) wide walls will get 1 window.
			//0-33% gets 1 window, 33-66 gets 2, 66-100 gets all three
			//we're not wrapping/repeating texture as it won't work with atlases
			columnScaleRatio = Math.Min(1, wallSegmentLength / _scaledPreferredWallLength);
			rightOfEdgeUv =
				_currentTextureRect.xMin +
				_currentTextureRect.size.x *
				columnScaleRatio; // Math.Min(1, ((float)(Math.Floor(columnScaleRatio * _currentFacade.ColumnCount) + 1) / _currentFacade.ColumnCount));

			_minWallLength = (_scaledPreferredWallLength / _currentFacade.ColumnCount) * _wallSizeEpsilon;
			//common for all top/mid/bottom segments
			wallNormal = new Vector3(-(wallSegmentFirstVertex.z - wallSegmentSecondVertex.z), 0,
				(wallSegmentFirstVertex.x - wallSegmentSecondVertex.x)).normalized;
			//height of the left/right edges
			currentY1 = wallSegmentFirstVertex.y;
			currentY2 = wallSegmentSecondVertex.y;

			//moving leftover row to top
			LeftOverRow(md, finalLeftOverRowHeight);

			FirstFloor(md, height);
			TopFloor(md, finalLeftOverRowHeight);
			MidFloors(md);
		}

		private void LeftOverRow(MeshData md, float leftOver)
		{
			//leftover. we're moving small leftover row to top of the building
			if (leftOver > 0)
			{
				md.Vertices.Add(new Vector3(wallSegmentFirstVertex.x, currentY1, wallSegmentFirstVertex.z));
				md.Vertices.Add(new Vector3(wallSegmentSecondVertex.x, currentY2, wallSegmentSecondVertex.z));
				//move offsets bottom
				currentY1 -= leftOver;
				currentY2 -= leftOver;
				//bottom two vertices
				md.Vertices.Add(new Vector3(wallSegmentFirstVertex.x, currentY1, wallSegmentFirstVertex.z));
				md.Vertices.Add(new Vector3(wallSegmentSecondVertex.x, currentY2, wallSegmentSecondVertex.z));

				if (wallSegmentLength >= _minWallLength)
				{
					md.UV[0].Add(new Vector2(_currentTextureRect.xMin, _currentTextureRect.yMax));
					md.UV[0].Add(new Vector2(rightOfEdgeUv, _currentTextureRect.yMax));
					md.UV[0].Add(new Vector2(_currentTextureRect.xMin,
						_currentTextureRect.yMax - _shortRowHeightDelta));
					md.UV[0].Add(new Vector2(rightOfEdgeUv, _currentTextureRect.yMax - _shortRowHeightDelta));
				}
				else
				{
					md.UV[0].Add(new Vector2(_currentTextureRect.xMin, _currentTextureRect.yMax));
					md.UV[0].Add(
						new Vector2(_currentTextureRect.xMin + _narrowWallWidthDelta, _currentTextureRect.yMax));
					md.UV[0].Add(new Vector2(_currentTextureRect.xMin,
						_currentTextureRect.yMax - _shortRowHeightDelta));
					md.UV[0].Add(new Vector2(_currentTextureRect.xMin + _narrowWallWidthDelta,
						_currentTextureRect.yMax - _shortRowHeightDelta));
				}

				md.Normals.Add(wallNormal);
				md.Normals.Add(wallNormal);
				md.Normals.Add(wallNormal);
				md.Normals.Add(wallNormal);

				md.Tangents.Add(wallDirection);
				md.Tangents.Add(wallDirection);
				md.Tangents.Add(wallDirection);
				md.Tangents.Add(wallDirection);

				wallTriangles.Add(triIndex);
				wallTriangles.Add(triIndex + 1);
				wallTriangles.Add(triIndex + 2);

				wallTriangles.Add(triIndex + 1);
				wallTriangles.Add(triIndex + 3);
				wallTriangles.Add(triIndex + 2);

				triIndex += 4;
			}
		}

		private void MidFloors(MeshData md)
		{
			_currentMidHeight = finalMidHeight;
			while (_currentMidHeight >= _singleFloorHeight - 0.01f)
			{
				//first part is the number of floors fitting current wall segment. You can fit max of "row count in mid". Or if wall
				//is smaller and it can only fit i.e. 3 floors instead of 5; we use 3/5 of the mid section texture as well.
				_midUvInCurrentStep =
					((float)Math.Min(_currentFacade.MidFloorCount,
						Math.Round(_currentMidHeight / _singleFloorHeight))) / _currentFacade.MidFloorCount;

				//top two vertices
				md.Vertices.Add(new Vector3(wallSegmentFirstVertex.x, currentY1, wallSegmentFirstVertex.z));
				md.Vertices.Add(new Vector3(wallSegmentSecondVertex.x, currentY2, wallSegmentSecondVertex.z));
				//move offsets bottom
				currentY1 -= (_scaledFloorHeight * _midUvInCurrentStep);
				currentY2 -= (_scaledFloorHeight * _midUvInCurrentStep);
				//bottom two vertices
				md.Vertices.Add(new Vector3(wallSegmentFirstVertex.x, currentY1, wallSegmentFirstVertex.z));
				md.Vertices.Add(new Vector3(wallSegmentSecondVertex.x, currentY2, wallSegmentSecondVertex.z));

				//we uv narrow walls different so they won't have condensed windows
				if (wallSegmentLength >= _minWallLength)
				{
					md.UV[0].Add(new Vector2(_currentTextureRect.xMin, _currentFacade.topOfMidUv));
					md.UV[0].Add(new Vector2(rightOfEdgeUv, _currentFacade.topOfMidUv));
					md.UV[0].Add(new Vector2(_currentTextureRect.xMin,
						_currentFacade.topOfMidUv - _currentFacade.midUvHeight * _midUvInCurrentStep));
					md.UV[0].Add(new Vector2(rightOfEdgeUv,
						_currentFacade.topOfMidUv - _currentFacade.midUvHeight * _midUvInCurrentStep));
				}
				else
				{
					md.UV[0].Add(new Vector2(_currentTextureRect.xMin, _currentFacade.topOfMidUv));
					md.UV[0].Add(new Vector2(_currentTextureRect.xMin + _narrowWallWidthDelta,
						_currentFacade.topOfMidUv));
					md.UV[0].Add(new Vector2(_currentTextureRect.xMin,
						_currentFacade.topOfMidUv - _currentFacade.midUvHeight * _midUvInCurrentStep));
					md.UV[0].Add(new Vector2(_currentTextureRect.xMin + _narrowWallWidthDelta,
						_currentFacade.topOfMidUv - _currentFacade.midUvHeight * _midUvInCurrentStep));
				}

				md.Normals.Add(wallNormal);
				md.Normals.Add(wallNormal);
				md.Normals.Add(wallNormal);
				md.Normals.Add(wallNormal);

				md.Tangents.Add(wallDirection);
				md.Tangents.Add(wallDirection);
				md.Tangents.Add(wallDirection);
				md.Tangents.Add(wallDirection);

				wallTriangles.Add(triIndex);
				wallTriangles.Add(triIndex + 1);
				wallTriangles.Add(triIndex + 2);

				wallTriangles.Add(triIndex + 1);
				wallTriangles.Add(triIndex + 3);
				wallTriangles.Add(triIndex + 2);

				triIndex += 4;
				_currentMidHeight -= Math.Max(0.1f, (_scaledFloorHeight * _midUvInCurrentStep));
			}
		}

		private void TopFloor(MeshData md, float leftOver)
		{
			//top floor start
			currentY1 -= finalTopHeight;
			currentY2 -= finalTopHeight;
			md.Vertices.Add(new Vector3(wallSegmentFirstVertex.x, wallSegmentFirstVertex.y - leftOver,
				wallSegmentFirstVertex.z));
			md.Vertices.Add(new Vector3(wallSegmentSecondVertex.x, wallSegmentSecondVertex.y - leftOver,
				wallSegmentSecondVertex.z));
			md.Vertices.Add(new Vector3(wallSegmentFirstVertex.x, wallSegmentFirstVertex.y - leftOver - finalTopHeight,
				wallSegmentFirstVertex.z));
			md.Vertices.Add(new Vector3(wallSegmentSecondVertex.x,
				wallSegmentSecondVertex.y - leftOver - finalTopHeight, wallSegmentSecondVertex.z));

			if (wallSegmentLength >= _minWallLength)
			{
				md.UV[0].Add(new Vector2(_currentTextureRect.xMin, _currentTextureRect.yMax));
				md.UV[0].Add(new Vector2(rightOfEdgeUv, _currentTextureRect.yMax));
				md.UV[0].Add(new Vector2(_currentTextureRect.xMin, _currentFacade.bottomOfTopUv));
				md.UV[0].Add(new Vector2(rightOfEdgeUv, _currentFacade.bottomOfTopUv));
			}
			else
			{
				md.UV[0].Add(new Vector2(_currentTextureRect.xMin, _currentTextureRect.yMax));
				md.UV[0].Add(new Vector2(_currentTextureRect.xMin + _narrowWallWidthDelta, _currentTextureRect.yMax));
				md.UV[0].Add(new Vector2(_currentTextureRect.xMin, _currentFacade.bottomOfTopUv));
				md.UV[0].Add(
					new Vector2(_currentTextureRect.xMin + _narrowWallWidthDelta, _currentFacade.bottomOfTopUv));
			}

			md.Normals.Add(wallNormal);
			md.Normals.Add(wallNormal);
			md.Normals.Add(wallNormal);
			md.Normals.Add(wallNormal);


			md.Tangents.Add(wallDirection);
			md.Tangents.Add(wallDirection);
			md.Tangents.Add(wallDirection);
			md.Tangents.Add(wallDirection);

			wallTriangles.Add(triIndex);
			wallTriangles.Add(triIndex + 1);
			wallTriangles.Add(triIndex + 2);

			wallTriangles.Add(triIndex + 1);
			wallTriangles.Add(triIndex + 3);
			wallTriangles.Add(triIndex + 2);

			triIndex += 4;
		}

		private void FirstFloor(MeshData md, float hf)
		{
			md.Vertices.Add(new Vector3(wallSegmentFirstVertex.x, wallSegmentFirstVertex.y - hf + finalFirstHeight,
				wallSegmentFirstVertex.z));
			md.Vertices.Add(new Vector3(wallSegmentSecondVertex.x, wallSegmentSecondVertex.y - hf + finalFirstHeight,
				wallSegmentSecondVertex.z));
			md.Vertices.Add(new Vector3(wallSegmentFirstVertex.x, wallSegmentFirstVertex.y - hf,
				wallSegmentFirstVertex.z));
			md.Vertices.Add(new Vector3(wallSegmentSecondVertex.x, wallSegmentSecondVertex.y - hf,
				wallSegmentSecondVertex.z));

			md.Normals.Add(wallNormal);
			md.Normals.Add(wallNormal);
			md.Normals.Add(wallNormal);
			md.Normals.Add(wallNormal);
			md.Tangents.Add(wallDirection);
			md.Tangents.Add(wallDirection);
			md.Tangents.Add(wallDirection);
			md.Tangents.Add(wallDirection);

			if (wallSegmentLength >= _minWallLength)
			{
				md.UV[0].Add(new Vector2(_currentTextureRect.xMin, _currentFacade.topOfBottomUv));
				md.UV[0].Add(new Vector2(rightOfEdgeUv, _currentFacade.topOfBottomUv));
				md.UV[0].Add(new Vector2(_currentTextureRect.xMin, _currentTextureRect.yMin));
				md.UV[0].Add(new Vector2(rightOfEdgeUv, _currentTextureRect.yMin));
			}
			else
			{
				md.UV[0].Add(new Vector2(_currentTextureRect.xMin, _currentFacade.topOfBottomUv));
				md.UV[0].Add(
					new Vector2(_currentTextureRect.xMin + _narrowWallWidthDelta, _currentFacade.topOfBottomUv));
				md.UV[0].Add(new Vector2(_currentTextureRect.xMin, _currentTextureRect.yMin));
				md.UV[0].Add(new Vector2(_currentTextureRect.xMin + _narrowWallWidthDelta, _currentTextureRect.yMin));
			}

			wallTriangles.Add(triIndex);
			wallTriangles.Add(triIndex + 1);
			wallTriangles.Add(triIndex + 2);

			wallTriangles.Add(triIndex + 1);
			wallTriangles.Add(triIndex + 3);
			wallTriangles.Add(triIndex + 2);

			triIndex += 4;
		}

		private void CalculateEdgeList(MeshData md, UnityTile tile, float preferredEdgeSectionLength)
		{
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
							md.Vertices[i] = new Vector3(md.Vertices[i].x, md.Vertices[i].y + maxHeight,
								md.Vertices[i].z);
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
						for (int i = 0; i < _counter; i++)
						{
							md.Vertices[i] = new Vector3(md.Vertices[i].x, md.Vertices[i].y + maxHeight,
								md.Vertices[i].z);
						}

						break;
					case ExtrusionType.AbsoluteHeight:
						for (int i = 0; i < _counter; i++)
						{
							md.Vertices[i] = new Vector3(md.Vertices[i].x, md.Vertices[i].y + maxHeight,
								md.Vertices[i].z);
						}

						break;
					default:
						break;
				}
			}
		}

		private void QueryHeight(VectorFeatureUnity feature, MeshData md, UnityTile tile, out float maxHeight,
			out float minHeight)
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
						if (_options.minimumHeight > _options.maximumHeight)
						{
							Debug.LogError("Maximum Height less than Minimum Height.Swapping values for extrusion.");
							var temp = _options.minimumHeight;
							_options.minimumHeight = _options.maximumHeight;
							_options.maximumHeight = temp;
						}

						var featureHeight = Convert.ToSingle(feature.Properties[_options.propertyName]);
						maxHeight = Math.Min(Math.Max(_options.minimumHeight, featureHeight), _options.maximumHeight);
						if (feature.Properties.ContainsKey("min_height"))
						{
							var featureMinHeight = Convert.ToSingle(feature.Properties["min_height"]);
							minHeight = Math.Min(featureMinHeight, _options.maximumHeight);
							//maxHeight -= minHeight;
						}
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
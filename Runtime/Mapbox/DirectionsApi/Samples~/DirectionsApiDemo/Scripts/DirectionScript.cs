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
		private Material _material;

		public RoutingProfile.RoutingProfileOptions RoutingOption;

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


			_material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
			_material.color = new Color(1, 132f / 255, 0, 1);
			_camera = Camera.main;
			_gos = new List<GameObject>();

			MapCore.Initialized += map =>
			{
				if (_fileSource == null)
					_fileSource = map.MapService.FileSource;
				_mapboxDirections = new MapboxDirectionsApi(_fileSource);
				_mapInformation = map.MapInformation;
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
				Alternatives = false
			};

			_mapboxDirections.Query(directionParameters, response =>
			{
				if (response == null)
					return;

				foreach (var route in response.Routes)
				{
					var vectorEntity = new VectorEntity
					{
						Feature = new VectorFeatureUnity
						{
							Points = new List<List<Vector3>>()
							{
								route.Geometry.Select(geo =>
										_mapInformation.ConvertLatLngToPosition(new LatitudeLongitude(geo.x, geo.y)))
									.ToList()
							}
						}
					};

					var meshData = new MeshData();
					var lineMeshCore = new LineMeshCore(new LineMeshParameters()
					{
						Width = 10
					}, _mapInformation);

					vectorEntity.Mesh = new Mesh();
					vectorEntity.Mesh.SetMeshValues(meshData);
					vectorEntity.GameObject = new GameObject();
					vectorEntity.MeshFilter = vectorEntity.GameObject.AddComponent<MeshFilter>();
					vectorEntity.MeshFilter.mesh = vectorEntity.Mesh;
					vectorEntity.MeshRenderer = vectorEntity.GameObject.AddComponent<MeshRenderer>();

					lineMeshCore.Run(vectorEntity.Feature, meshData);
					vectorEntity.MeshFilter.mesh = new Mesh()
					{
						vertices = meshData.Vertices.ToArray(),
						triangles = meshData.Triangles[0].ToArray()
					};
					vectorEntity.GameObject.transform.position += new Vector3(0.0f, 1 / _mapInformation.Scale, 0.0f);
					vectorEntity.MeshRenderer.material = _material;
					_gos.Add(vectorEntity.GameObject);
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


		private enum JoinType
		{
			Miter = 0,
			Round = 1,
			Bevel = 2,
			Butt,
			Square,
			Fakeround,
			Flipbevel
		}

		private class LineMeshParameters
		{
			public float MiterLimit = 0.2f;
			public float RoundLimit = 1.05f;
			public JoinType JoinType = JoinType.Round;
			public JoinType CapType = JoinType.Round;
			public float Width;
		}

		private class LineMeshCore
		{
			private readonly LineMeshParameters _lineMeshParameters;

			private float _scaledWidth;
			private readonly float _cosHalfSharpCorner = Mathf.Cos(75f / 2f * (Mathf.PI / 180f));
			private readonly float _sharpCornerOffset = 15f;
			private float _tileSize;
			private List<Vector3> _vertexList;
			private List<Vector3> _normalList;
			private List<int> _triangleList;
			private List<Vector2> _uvList;
			private List<Vector4> _tangentList;
			private int _index1 = -1;
			private int _index2 = -1;
			private int _index3 = -1;
			private float _cornerOffsetA;
			private float _cornerOffsetB;
			private bool _startOfLine = true;
			private Vector3 _prevVertex;
			private Vector3 _currentVertex;
			private Vector3 _nextVertex;
			private Vector3 _prevNormal;
			private Vector3 _nextNormal;
			private float _distance = 0f;

			public LineMeshCore(LineMeshParameters parameters, IMapInformation mapInformation)
			{
				_lineMeshParameters = parameters;
				_scaledWidth = _lineMeshParameters.Width / mapInformation.Scale;

				_vertexList = new List<Vector3>();
				_normalList = new List<Vector3>();
				_triangleList = new List<int>();
				_uvList = new List<Vector2>();
				_tangentList = new List<Vector4>();
			}

			public void Run(VectorFeatureUnity feature, MeshData md)
			{
				ExtrudeLine(feature, md);
			}

			private void ExtrudeLine(VectorFeatureUnity feature, MeshData md)
			{
				if (feature.Points.Count < 1)
				{
					return;
				}

				var allPoints = new List<List<Vector3>>();
				foreach (var segment in feature.Points)
				{
					var filteredRoadSegment = new List<Vector3>();
					var tolerance = 0.001f;
					for (int i = 0; i < segment.Count - 1; i++)
					{
						var p1 = segment[i];
						var p2 = segment[i + 1];
						if (!IsOnEdge(p1, p2, tolerance))
						{
							filteredRoadSegment.Add(p1);
							if (i == segment.Count - 2)
							{
								filteredRoadSegment.Add(p2);
							}
						}
						else
						{
							filteredRoadSegment.Add(p1);
							allPoints.Add(filteredRoadSegment);
							filteredRoadSegment = new List<Vector3>();
						}
					}

					allPoints.Add(filteredRoadSegment);
				}

				foreach (var roadSegment in allPoints)
				{
					if (roadSegment.Count < 2)
						continue;

					ResetFields();

					var roadSegmentCount = roadSegment.Count;
					for (int i = 0; i < roadSegmentCount; i++)
					{
						_nextVertex = i != (roadSegmentCount - 1) ? roadSegment[i + 1] : Constants.Math.Vector3Unused;

						if (_nextNormal != Constants.Math.Vector3Unused)
						{
							_prevNormal = _nextNormal;
						}

						if (_currentVertex != Constants.Math.Vector3Unused)
						{
							_prevVertex = _currentVertex;
						}

						_currentVertex = roadSegment[i];

						_nextNormal = (_nextVertex != Constants.Math.Vector3Unused)
							? (_nextVertex - _currentVertex).normalized.Perpendicular()
							: _prevNormal;

						if (_prevNormal == Constants.Math.Vector3Unused)
						{
							_prevNormal = _nextNormal;
						}

						var joinNormal = (_prevNormal + _nextNormal).normalized;

						/*  joinNormal     prevNormal
						 *             ↖      ↑
						 *                .________. prevVertex
						 *                |
						 * nextNormal  ←  |  currentVertex
						 *                |
						 *     nextVertex !
						 *
						 */

						var cosHalfAngle = joinNormal.x * _nextNormal.x + joinNormal.z * _nextNormal.z;
						var miterLength = cosHalfAngle != 0 ? 1 / cosHalfAngle : float.PositiveInfinity;
						var isSharpCorner = cosHalfAngle < _cosHalfSharpCorner &&
						                    _prevVertex != Constants.Math.Vector3Unused &&
						                    _nextVertex != Constants.Math.Vector3Unused;

						if (isSharpCorner && i > 0)
						{
							var prevSegmentLength = Vector3.Distance(_currentVertex, _prevVertex);
							if (prevSegmentLength > 2 * _sharpCornerOffset)
							{
								var dir = (_currentVertex - _prevVertex);
								var newPrevVertex = _currentVertex - (dir * (_sharpCornerOffset / prevSegmentLength));
								_distance += Vector3.Distance(newPrevVertex, _prevVertex);
								AddCurrentVertex(newPrevVertex, _distance, _prevNormal, md, _scaledWidth);
								_prevVertex = newPrevVertex;
							}
						}

						var middleVertex = _prevVertex != Constants.Math.Vector3Unused &&
						                   _nextVertex != Constants.Math.Vector3Unused;
						var currentJoin = middleVertex ? _lineMeshParameters.JoinType : _lineMeshParameters.CapType;

						if (middleVertex && currentJoin == JoinType.Round)
						{
							if (miterLength < _lineMeshParameters.RoundLimit)
							{
								currentJoin = JoinType.Miter;
							}
							else if (miterLength <= 2)
							{
								currentJoin = JoinType.Fakeround;
							}
						}

						if (currentJoin == JoinType.Miter && miterLength > _lineMeshParameters.MiterLimit)
						{
							currentJoin = JoinType.Bevel;
						}

						if (currentJoin == JoinType.Bevel)
						{
							// The maximum extrude length is 128 / 63 = 2 times the width of the line
							// so if miterLength >= 2 we need to draw a different type of bevel here.
							if (miterLength > 2)
							{
								currentJoin = JoinType.Flipbevel;
							}

							// If the miterLength is really small and the line bevel wouldn't be visible,
							// just draw a miter join to save a triangle.
							if (miterLength < _lineMeshParameters.MiterLimit)
							{
								currentJoin = JoinType.Miter;
							}
						}

						if (_prevVertex != Constants.Math.Vector3Unused)
						{
							_distance += Vector3.Distance(_currentVertex, _prevVertex);
						}

						if (currentJoin == JoinType.Miter)
						{
							joinNormal *= miterLength;
							AddCurrentVertex(_currentVertex, _distance, joinNormal, md, _scaledWidth);
						}
						else if (currentJoin == JoinType.Flipbevel)
						{
							// miter is too big, flip the direction to make a beveled join

							if (miterLength > 100)
							{
								// Almost parallel lines
								joinNormal = _nextNormal * -1;
							}
							else
							{
								var direction = (_prevNormal.x * _nextNormal.z - _prevNormal.z * _nextNormal.x) > 0
									? -1
									: 1;
								var bevelLength = miterLength * (_prevNormal + _nextNormal).magnitude /
								                  (_prevNormal - _nextNormal).magnitude;
								joinNormal = joinNormal.Perpendicular() * (bevelLength * direction);
							}

							AddCurrentVertex(_currentVertex, _distance, joinNormal, md, _scaledWidth, 0, 0);
							AddCurrentVertex(_currentVertex, _distance, joinNormal * -1, md, _scaledWidth, 0, 0);
						}
						else if (currentJoin == JoinType.Bevel || currentJoin == JoinType.Fakeround)
						{
							var lineTurnsLeft = (_prevNormal.x * _nextNormal.z - _prevNormal.z * _nextNormal.x) > 0;
							var offset = (float)-Math.Sqrt(miterLength * miterLength - 1);
							if (lineTurnsLeft)
							{
								_cornerOffsetB = 0;
								_cornerOffsetA = offset;
							}
							else
							{
								_cornerOffsetA = 0;
								_cornerOffsetB = offset;
							}

							// Close previous segment with a bevel
							if (!_startOfLine)
							{
								AddCurrentVertex(_currentVertex, _distance, _prevNormal, md, _scaledWidth,
									_cornerOffsetA,
									_cornerOffsetB);
							}

							if (currentJoin == JoinType.Fakeround)
							{
								// The join angle is sharp enough that a round join would be visible.
								// Bevel joins fill the gap between segments with a single pie slice triangle.
								// Create a round join by adding multiple pie slices. The join isn't actually round, but
								// it looks like it is at the sizes we render lines at.

								// Add more triangles for sharper angles.
								// This math is just a good enough approximation. It isn't "correct".
								var n = Mathf.Floor((0.5f - (cosHalfAngle - 0.5f)) * 8);
								Vector3 approxFractionalJoinNormal;
								for (var m = 0f; m < n; m++)
								{
									//approxFractionalJoinNormal isn't normal normal.
									//it's the vector showing out, we are using tangent field for that 
									//naming comes from native/js implementation so I'm keeping it
									//but it is not normal, we use UP as normal for all points
									approxFractionalJoinNormal =
										(_nextNormal * ((m + 1f) / (n + 1f)) + (_prevNormal)).normalized;
									AddPieSliceVertex(_currentVertex, _distance, approxFractionalJoinNormal,
										lineTurnsLeft,
										md, _scaledWidth);
								}

								AddPieSliceVertex(_currentVertex, _distance, joinNormal, lineTurnsLeft, md,
									_scaledWidth);

								//change it to go -1, not sure if it's a good idea but it adds the last vertex in the corner,
								//as duplicate of next road segment start
								for (var k = n - 1; k >= -1; k--)
								{
									approxFractionalJoinNormal =
										(_prevNormal * ((k + 1) / (n + 1)) + (_nextNormal)).normalized;
									AddPieSliceVertex(_currentVertex, _distance, approxFractionalJoinNormal,
										lineTurnsLeft,
										md, _scaledWidth);
								}

								//ending corner
								_index1 = -1;
								_index2 = -1;
							}

							if (_nextVertex != Constants.Math.Vector3Unused)
							{
								AddCurrentVertex(_currentVertex, _distance, _nextNormal, md, _scaledWidth,
									-_cornerOffsetA,
									-_cornerOffsetB);
							}
						}
						else if (currentJoin == JoinType.Butt)
						{
							if (!_startOfLine)
							{
								// Close previous segment with a butt
								AddCurrentVertex(_currentVertex, _distance, _prevNormal, md, _scaledWidth, 0, 0);
							}

							// Start next segment with a butt
							if (_nextVertex != Constants.Math.Vector3Unused)
							{
								AddCurrentVertex(_currentVertex, _distance, _nextNormal, md, _scaledWidth, 0, 0);
							}
						}
						else if (currentJoin == JoinType.Square)
						{
							if (!_startOfLine)
							{
								// Close previous segment with a square cap
								AddCurrentVertex(_currentVertex, _distance, _prevNormal, md, _scaledWidth, 1, 1);

								// The segment is done. Unset vertices to disconnect segments.
								_index1 = _index2 = -1;
							}

							// Start next segment
							if (_nextVertex != Constants.Math.Vector3Unused)
							{
								AddCurrentVertex(_currentVertex, _distance, _nextNormal, md, _scaledWidth, -1, -1);
							}
						}
						else if (currentJoin == JoinType.Round)
						{
							if (_startOfLine)
							{
								AddCurrentVertex(_currentVertex, _distance, _prevNormal * 0.33f, md, _scaledWidth, -2f,
									-2f);
								AddCurrentVertex(_currentVertex, _distance, _prevNormal * 0.66f, md, _scaledWidth, -.7f,
									-.7f);
								AddCurrentVertex(_currentVertex, _distance, _prevNormal, md, _scaledWidth, 0, 0);
							}
							else if (_nextVertex == Constants.Math.Vector3Unused)
							{
								AddCurrentVertex(_currentVertex, _distance, _prevNormal, md, _scaledWidth, 0, 0);
								AddCurrentVertex(_currentVertex, _distance, _prevNormal * 0.66f, md, _scaledWidth, .7f,
									.7f);
								AddCurrentVertex(_currentVertex, _distance, _prevNormal * 0.33f, md, _scaledWidth, 2f,
									2f);
								_index1 = -1;
								_index2 = -1;
							}
							else
							{
								AddCurrentVertex(_currentVertex, _distance, _prevNormal, md, _scaledWidth, 0, 0);
								AddCurrentVertex(_currentVertex, _distance, _nextNormal, md, _scaledWidth, 0, 0);
							}
						}

						if (isSharpCorner && i < roadSegmentCount - 1)
						{
							var nextSegmentLength = Vector3.Distance(_currentVertex, _nextVertex);
							if (nextSegmentLength > 2 * _sharpCornerOffset)
							{
								var newCurrentVertex = _currentVertex + ((_nextVertex - _currentVertex) *
								                                         (_sharpCornerOffset /
								                                          nextSegmentLength)); //._round()
								_distance += Vector3.Distance(newCurrentVertex, _currentVertex);
								AddCurrentVertex(newCurrentVertex, _distance, _nextNormal, md, _scaledWidth);
								_currentVertex = newCurrentVertex;
							}
						}

						_startOfLine = false;
					}

					md.Edges.Add(md.Vertices.Count);
					md.Edges.Add(md.Vertices.Count + 1);
					md.Edges.Add(md.Vertices.Count + _vertexList.Count - 1);
					md.Edges.Add(md.Vertices.Count + _vertexList.Count - 2);

					md.Vertices.AddRange(_vertexList);
					md.Normals.AddRange(_normalList);
					if (md.Triangles.Count == 0)
					{
						md.Triangles.Add(new List<int>());
					}

					md.Triangles[0].AddRange(_triangleList);
					md.Tangents.AddRange(_tangentList);
					md.UV[0].AddRange(_uvList);
				}
			}

			private bool IsOnEdge(Vector3 p1, Vector3 p2, float tolerance)
			{
				return ((Math.Abs(Math.Abs(p1.x) - (_tileSize / 2)) < tolerance &&
				         Math.Abs(Math.Abs(p2.x) - (_tileSize / 2)) < tolerance &&
				         Math.Sign(p1.x) == Math.Sign(p2.x)) ||
				        (Math.Abs(Math.Abs(p1.z) - (_tileSize / 2)) < tolerance &&
				         Math.Abs(Math.Abs(p2.z) - (_tileSize / 2)) < tolerance && Math.Sign(p1.z) == Math.Sign(p2.z)));
			}

			private void ResetFields()
			{
				_index1 = -1;
				_index2 = -1;
				_index3 = -1;
				_startOfLine = true;
				_cornerOffsetA = 0f;
				_cornerOffsetB = 0f;

				_vertexList.Clear();
				_normalList.Clear();
				_uvList.Clear();
				_tangentList.Clear();
				_triangleList.Clear();

				_prevVertex = Constants.Math.Vector3Unused;
				_currentVertex = Constants.Math.Vector3Unused;
				_nextVertex = Constants.Math.Vector3Unused;

				_prevNormal = Constants.Math.Vector3Unused;
				_nextNormal = Constants.Math.Vector3Unused;
				_distance = 0f;
			}

			private void AddPieSliceVertex(Vector3 vertexPosition, float dist, Vector3 normal, bool lineTurnsLeft,
				MeshData md, float width)
			{
				var triIndexStart = md.Vertices.Count;
				var extrude = normal * (lineTurnsLeft ? -1 : 1);
				_vertexList.Add(vertexPosition + extrude * width);
				_normalList.Add(Constants.Math.Vector3Up);
				_uvList.Add(new Vector2(1, dist));

				if (lineTurnsLeft)
				{
					_tangentList.Add(normal * -1);
				}
				else
				{
					_tangentList.Add(normal);
				}

				_index3 = triIndexStart + _vertexList.Count - 1;
				if (_index1 >= 0 && _index2 >= 0)
				{
					_triangleList.Add(_index1);
					_triangleList.Add(_index3);
					_triangleList.Add(_index2);
					if (!lineTurnsLeft)
					{
						md.Edges.Add(_index3);
						md.Edges.Add(_index1);
					}
					else
					{
						md.Edges.Add(_index2);
						md.Edges.Add(_index3);
					}
				}

				if (lineTurnsLeft)
				{
					_index2 = _index3;
				}
				else
				{
					_index1 = _index3;
				}
			}

			private void AddCurrentVertex(Vector3 vertexPosition, float dist, Vector3 normal, MeshData md, float width,
				float endLeft = 0, float endRight = 0)
			{
				var triIndexStart = md.Vertices.Count;
				var extrude = normal;
				if (endLeft != 0)
				{
					extrude -= (normal.Perpendicular() * endLeft);
				}

				var vert = vertexPosition + extrude * width;
				_vertexList.Add(vert);
				_normalList.Add(Constants.Math.Vector3Up);
				_uvList.Add(new Vector2(1, dist));
				_tangentList.Add(extrude);

				_index3 = triIndexStart + _vertexList.Count - 1;
				if (_index1 >= triIndexStart && _index2 >= triIndexStart)
				{
					_triangleList.Add(_index1);
					_triangleList.Add(_index3);
					_triangleList.Add(_index2);
					md.Edges.Add(triIndexStart + _vertexList.Count - 1);
					md.Edges.Add(triIndexStart + _vertexList.Count - 3);
				}

				_index1 = _index2;
				_index2 = _index3;


				extrude = normal * -1;
				if (endRight != 0)
				{
					extrude -= normal.Perpendicular() * endRight;
				}

				_vertexList.Add(vertexPosition + extrude * width);
				_normalList.Add(Constants.Math.Vector3Up);
				_uvList.Add(new Vector2(0, dist));
				_tangentList.Add(extrude);

				_index3 = triIndexStart + _vertexList.Count - 1;
				if (_index1 >= triIndexStart && _index2 >= triIndexStart)
				{
					_triangleList.Add(_index1);
					_triangleList.Add(_index2);
					_triangleList.Add(_index3);
					md.Edges.Add(triIndexStart + _vertexList.Count - 3);
					md.Edges.Add(triIndexStart + _vertexList.Count - 1);
				}

				_index1 = _index2;
				_index2 = _index3;
			}

		}
	}
}

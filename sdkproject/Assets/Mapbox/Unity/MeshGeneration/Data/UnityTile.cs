using System.Collections;
using JetBrains.Annotations;
using Mapbox.Unity.DataContainers;
using Mapbox.Unity.QuadTree;
using UnityEditor;
using UnityEngine.Rendering;

namespace Mapbox.Unity.MeshGeneration.Data
{
	using Mapbox.Unity.Map.Interfaces;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.Utilities;
	using Utils;
	using Mapbox.Map;
	using System;
	using Mapbox.Unity.Map;
	using System.Collections.Generic;

	public class UnityTile : MonoBehaviour
	{
		public Action<UnityTile> TileFinished = (t) => {};

		public TileTerrainType ElevationType;

		public RasterTile _parentRasterTile;
		private RasterTile _rasterTile;
		public RasterTile BaseRasterData => _rasterTile;

		public RasterTile _parentTerrainTile;
		private RawPngRasterTile _terrainTile;
		public RawPngRasterTile TerrainData => _terrainTile;
		private bool _terrainReady = false;
		public bool IsTerrainReady => _terrainReady;
		public float[] HeightData => _terrainTile?.HeightData;
		protected Vector4 _terrainTextureScaleOffset;

		private VectorTile _vectorTile;
		public VectorTile VectorData => _vectorTile;

		private Action<UnityTile, Action> _createMeshCallback;

		//private int _heightDataResolution = 100;
		//keeping track of tile objects to be able to cancel them safely if tile is destroyed before data fetching finishes
		public HashSet<Tile> Tiles = new HashSet<Tile>();
		public HashSet<Tile> _finishConditionTiles = new HashSet<Tile>();
		public bool IsRecycled = false;
		public bool IsStopped = false;

		#region CachedUnityComponents
		private Material _material => _meshRenderer.material;
		private MeshRenderer _meshRenderer;
		public MeshRenderer MeshRenderer
		{
			get
			{
				if (_meshRenderer == null)
				{
					_meshRenderer = GetComponent<MeshRenderer>();
					if (_meshRenderer == null)
					{
						_meshRenderer = gameObject.AddComponent<MeshRenderer>();
					}
				}
				return _meshRenderer;
			}
		}


		private MeshFilter _meshFilter;
		public MeshFilter MeshFilter
		{
			get
			{
				if (_meshFilter == null)
				{
					_meshFilter = GetComponent<MeshFilter>();
					if (_meshFilter == null)
					{
						_meshFilter = gameObject.AddComponent<MeshFilter>();
						_meshFilter.sharedMesh = new Mesh();
						ElevationType = TileTerrainType.None;
					}
				}
				return _meshFilter;
			}
		}

		private Collider _collider;
		public Collider Collider
		{
			get
			{
				if (_collider == null)
				{
					_collider = gameObject.GetComponent<MeshCollider>();
					if (_collider == null)
					{
						var meshCollider = gameObject.AddComponent<MeshCollider>();
						meshCollider.cookingOptions = MeshColliderCookingOptions.None;
						_collider = meshCollider;

					}
				}
				return _collider;
			}
		}
		#endregion

		#region Tile Positon/Scale Properties
		[SerializeField] private float _tileScale;
		public float TileScale
		{
			get { return _tileScale; }
			private set { _tileScale = value; }
		}

		public float TileSize;

		public List<string> Logs = new List<string>();

		public RectD Rect { get; private set; }
		public int CurrentZoom { get; private set; }

		public UnwrappedTileId UnwrappedTileId { get; private set; }
		public CanonicalTileId CanonicalTileId { get; private set; }

		private float _relativeScale;

		private string _elevationMultiplierFieldNameID = "_ElevationMultiplier";
		private string _shaderElevationTextureFieldNameID = "_HeightTexture";
		private string _textureChangeTimerFieldNameID = "_ElevationChangeTime";
		private string _previousMainTextureFieldNameID = "_PreviousMainTexture";
		private string _mainTextureChangeTimeFieldNameID = "_MainTextureChangeTime";
		private string _mainTexFieldNameID = "_MainTex";
		private string _mainTexStFieldNameID = "_MainTex_ST";
		private string _previousMainTextureScaleOffsetFieldNameID = "_PreviousMainTextureScaleOffset";
		private string _tileScaleFieldNameID = "_TileScale";
		private string _previousShaderElevationTextureFieldNameID = "_PreviousHeightTexture";
		private string _previousShaderElevationTextureScaleOffsetFieldNameID = "_PreviousHeightTexture_ST";
		private string _shaderElevationTextureScaleOffsetFieldNameID = "_HeightTexture_ST";

		// private static int _previousMainTextureFieldNameID = 0;
		// private static int _mainTextureChangeTimeFieldNameID = 0;
		// private static int _mainTexFieldNameID = 0;
		// private static int _mainTexStFieldNameID = 0;
		// private static int _previousMainTextureScaleOffsetFieldNameID = 0;
		// private static int _tileScaleFieldNameID;
		// private static int _previousShaderElevationTextureFieldNameID;
		// private static int _previousShaderElevationTextureScaleOffsetFieldNameID;
		// private static int _shaderElevationTextureScaleOffsetFieldNameID;
		// private static int _elevationMultiplierFieldNameID = 0;
		// private static int _shaderElevationTextureFieldNameID;
		// private static int _textureChangeTimerFieldNameID;

		#endregion

		internal void Initialize(IMapReadable map, UnwrappedTileId tileId, float scale, bool isElevationActive)
		{
			// {
			// 	if (_previousMainTextureFieldNameID == 0)
			// 	{
			// 		_previousMainTextureFieldNameID = Shader.PropertyToID(_previousMainTextureFieldName);
			// 		_mainTextureChangeTimeFieldNameID = Shader.PropertyToID(_mainTextureChangeTimeFieldName);
			// 		_mainTexFieldNameID = Shader.PropertyToID(_mainTexFieldName);
			// 		_mainTexStFieldNameID = Shader.PropertyToID(_mainTexStFieldName);
			// 		_previousMainTextureScaleOffsetFieldNameID = Shader.PropertyToID(_previousMainTextureScaleOffsetFieldName);
			// 		_tileScaleFieldNameID = Shader.PropertyToID(_tileScaleFieldName);
			// 		_previousShaderElevationTextureFieldNameID = Shader.PropertyToID(_previousShaderElevationTextureFieldName);
			// 		_previousShaderElevationTextureScaleOffsetFieldNameID = Shader.PropertyToID(_previousShaderElevationTextureScaleOffsetFieldName);
			// 		_elevationMultiplierFieldNameID = Shader.PropertyToID(_elevationMultiplierFieldName);
			// 		_shaderElevationTextureFieldNameID = Shader.PropertyToID(_shaderElevationTextureFieldName);
			// 		_textureChangeTimerFieldNameID = Shader.PropertyToID(_textureChangeTimerFieldName);
			// 	}
			// }

			IsStopped = false;
			gameObject.hideFlags = HideFlags.DontSave;
			TileSize = map.UnityTileSize;
			ElevationType = TileTerrainType.None;
			TileScale = scale;
			_relativeScale = 1 / Mathf.Cos(Mathf.Deg2Rad * (float)map.CenterLatitudeLongitude.x);
			Rect = Conversions.TileBounds(tileId);
			UnwrappedTileId = tileId;
			CanonicalTileId = tileId.Canonical;

			float scaleFactor = 1.0f;
			CurrentZoom = map.AbsoluteZoom;
			scaleFactor = Mathf.Pow(2, (map.InitialZoom - CurrentZoom));
			gameObject.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
			//gameObject.SetActive(true);

			IsRecycled = false;
			Logs.Add(string.Format("{0} - {1}", Time.frameCount, "initialized " + UnwrappedTileId));
			// Setup Loading as initial state - Unregistered
			// When tile registers with factories, it will set the appropriate state.
			// None, if Factory source is None, Loading otherwise.
		}

		internal void Recycle()
		{
			// MeshRenderer.GetPropertyBlock(_propertyBlock);
			// if (!_propertyBlock.isEmpty)
			// {
			// 	_propertyBlock.SetTexture("_MainTex", null);
			// 	MeshRenderer.SetPropertyBlock(_propertyBlock);
			// }

			_material.SetTexture(_previousShaderElevationTextureFieldNameID, null);
			_material.SetTexture(_previousMainTextureFieldNameID, null);

			_terrainReady = false;
			_createMeshCallback = null;
			IsStopped = false;
			gameObject.SetActive(false);
			IsRecycled = true;

			Cancel();

			_rasterTile = null;
			_terrainTile = null;
			_vectorTile = null;

			_finishConditionTiles.Clear();
			foreach (var tile in Tiles)
			{
				tile.Clear();
			}
			Tiles.Clear();

			if (_parentRasterTile != null)
			{
				_parentRasterTile.RemoveUser(CanonicalTileId);
				_parentRasterTile = null;
			}

			if (_parentTerrainTile != null)
			{
				_parentTerrainTile.RemoveUser(CanonicalTileId);
				_parentTerrainTile = null;
			}

			Logs.Add(string.Format("{0} - {1}", Time.frameCount, "recycled"));
		}

		public void SetHeightData(RasterTile terrainTile, float heightMultiplier = 1f, bool useRelative = false, bool addCollider = false, Action<UnityTile> callback = null)
		{
			if (terrainTile == null)
				return;

			_terrainTile = (RawPngRasterTile) terrainTile;
			_terrainTextureScaleOffset = CalculateScaleOffset(terrainTile.Id.Z);

			if (_material != null)
			{
				if (_material.GetTexture(_previousShaderElevationTextureFieldNameID) == null)
				{
					_material.SetTexture(_previousShaderElevationTextureFieldNameID, terrainTile.Texture2D);
					_material.SetVector(_previousShaderElevationTextureScaleOffsetFieldNameID, _terrainTextureScaleOffset);
				}

				_material.SetTexture(_shaderElevationTextureFieldNameID, terrainTile.Texture2D);
				_material.SetFloat(_textureChangeTimerFieldNameID, Time.time);
				_material.SetVector(_shaderElevationTextureScaleOffsetFieldNameID, _terrainTextureScaleOffset);
				_material.SetFloat(_tileScaleFieldNameID, TileScale);
				_material.SetFloat(_elevationMultiplierFieldNameID, heightMultiplier);
			}

			if (_parentTerrainTile != null)
			{
				Runnable.Run(DelayedAction(() =>
				{
					if (_parentTerrainTile != null)
					{
						_parentTerrainTile.RemoveUser(CanonicalTileId);
						//_parentTerrainTile = null;
					}
				}, 2));
			}

			//reset height data
			if (terrainTile == null || terrainTile.Texture2D == null)
			{
				//HeightData = new float[_heightDataResolution * _heightDataResolution];
				if (_createMeshCallback != null && _vectorTile != null)
				{
					CallCreateMeshCallback();
				}
				return;
			}

			CheckFinishedCondition(_terrainTile);

			//var tileId = terrainTile.Id;
			// if (SystemInfo.supportsAsyncGPUReadback)
			// {
			// 	AsyncGpuReadbackForElevation(terrainTile, scaleOffset, heightMultiplier, useRelative, callback, tileId);
			// }
			// else
			// {
			// 	SyncReadForElevation(terrainTile, scaleOffset, heightMultiplier, useRelative, callback);
			// }
		}

		// private void SyncReadForElevation(RasterTile terrainTile, Vector4 scaleOffset, float heightMultiplier, bool useRelative, Action<UnityTile> callback)
		// {
		// 	_terrainTile = terrainTile;
		// 	byte[] rgbData = _terrainTile.Texture2D.GetRawTextureData();
		// 	//var rgbData = _heightTexture.GetRawTextureData<Color32>();
		// 	var relativeScale = useRelative ? _relativeScale : 1f;
		// 	var width = _terrainTile.Texture2D.width;
		// 	var padding = _heightDataResolution * new Vector2(scaleOffset.z, scaleOffset.w);
		// 	for (float yy = 0; yy < _heightDataResolution * scaleOffset.y; yy++)
		// 	{
		// 		for (float xx = 0; xx < _heightDataResolution * scaleOffset.x; xx++)
		// 		{
		// 			var index = (((int) (((padding.y + yy) / _heightDataResolution) * width) * width) + (int) (((padding.x + xx) / _heightDataResolution) * width));
		//
		// 			float r = rgbData[index * 4 + 1];
		// 			float g = rgbData[index * 4 + 2];
		// 			float b = rgbData[index * 4 + 3];
		// 			//var color = rgbData[index];
		// 			// float r = color.g;
		// 			// float g = color.b;
		// 			// float b = color.a;
		// 			//the formula below is the same as Conversions.GetAbsoluteHeightFromColor but it's inlined for performance
		// 			HeightData[(int) (yy * _heightDataResolution + xx)] = relativeScale * heightMultiplier * (-10000f + ((r * 65536f + g * 256f + b) * 0.1f));
		// 			//678 ==> 012345678
		// 			//345
		// 			//012
		// 		}
		// 	}
		// 	_terrainReady = true;
		// 	if (callback != null)
		// 	{
		// 		callback(this);
		// 	}
		//
		// 	CheckFinishedCondition(_terrainTile);
		// 	if (_createMeshCallback != null && _vectorTile?.CurrentTileState == TileState.Loaded)
		// 	{
		// 		CallCreateMeshCallback();
		// 	}
		// }
		//
		// private void AsyncGpuReadbackForElevation(RasterTile terrainTile, Vector4 scaleoffset, float heightMultiplier, bool useRelative, Action<UnityTile> callback, CanonicalTileId tileId)
		// {
		// 	_terrainTile = terrainTile;
		// 	AsyncGPUReadback.Request(_terrainTile.Texture2D, 0, (t) =>
		// 	{
		// 		if (IsRecycled)
		// 		{
		// 			return;
		// 		}
		//
		// 		var width = t.width;
		// 		var data = t.GetData<Color32>().ToArray();
		//
		// 		if (HeightData == null || HeightData.Length != _heightDataResolution * _heightDataResolution)
		// 		{
		// 			HeightData = new float[_heightDataResolution * _heightDataResolution];
		// 		}
		//
		// 		var relativeScale = useRelative ? _relativeScale : 1f;
		// 		var padding = width * new Vector2(scaleoffset.z, scaleoffset.w);
		// 		//tt = new Texture2D(_heightDataResolution, _heightDataResolution, TextureFormat.RGBA32, false);
		// 		for (float yy = 0; yy < _heightDataResolution; yy++)
		// 		{
		// 			for (float xx = 0; xx < _heightDataResolution; xx++)
		// 			{
		// 				var xx2 = padding.x + (xx / _heightDataResolution) * (width * scaleoffset.x);
		// 				var yy2 = padding.y + (yy / _heightDataResolution) * (width * scaleoffset.y);
		// 				var index = (int) (((int) yy2 * width) + (int) xx2);
		// 				//var color = _heightTexture.GetPixel((int)xx2, (int)yy2);
		// 				//var index = (int)(((float)xx / _heightDataResolution) * 255 * 256 + (((float)yy / _heightDataResolution) * 255));
		//
		// 				float r = data[index].g;
		// 				float g = data[index].b;
		// 				float b = data[index].a;
		// 				//the formula below is the same as Conversions.GetAbsoluteHeightFromColor but it's inlined for performance
		// 				HeightData[(int) (yy * _heightDataResolution + xx)] = relativeScale * heightMultiplier * (-10000f + ((r * 65536f + g * 256f + b) * 0.1f));
		// 				//678 ==> 012345678
		// 				//345
		// 				//012
		//
		// 				//tt.SetPixel((int) xx, (int) yy, new Color(r/256, g/256, b/256));
		// 				//tt.SetPixel((int) xx, (int) yy, color); //new Color(rgbData[index * 4 + 1] / 256f, rgbData[index * 4 + 2] / 256f, rgbData[index * 4 + 3] / 256f, 1));
		// 			}
		// 		}
		//
		// 		_terrainReady = true;
		// 		//tt.Apply();
		// 		if (callback != null)
		// 		{
		// 			callback(this);
		// 		}
		//
		// 		CheckFinishedCondition(_terrainTile);
		// 		if (_createMeshCallback != null && _vectorTile?.CurrentTileState == TileState.Loaded)
		// 		{
		// 			CallCreateMeshCallback();
		// 		}
		// 	});
		// }

		public void SetRasterData(RasterTile rasterTile, bool useMipMap = false, bool useCompression = false)
		{
			_rasterTile = rasterTile;

			if (_rasterTile == null || (_rasterTile.Texture2D == null && _rasterTile.Data == null))
			{
				//MeshRenderer.material.mainTexture = null;
				return;
			}

			if (_rasterTile.Texture2D != null && useCompression && _rasterTile.Texture2D.isReadable)
			{
				_rasterTile.Texture2D.Compress(false);
			}
			else if (_rasterTile.Texture2D == null && _rasterTile.Data != null)
			{
				_rasterTile.SetTextureFromCache(new Texture2D(0, 0, TextureFormat.RGB24, useMipMap));
				_rasterTile.Texture2D.wrapMode = TextureWrapMode.Clamp;
				_rasterTile.Texture2D.LoadImage(_rasterTile.Data);
				if (useCompression)
				{
					// High quality = true seems to decrease image quality?
					_rasterTile.Texture2D.Compress(false);
				}
			}

			//MeshRenderer.GetPropertyBlock(_propertyBlock);
			if (_material.GetTexture(_previousMainTextureFieldNameID) == null)
			{
				_material.SetTexture(_previousMainTextureFieldNameID, rasterTile.Texture2D);
				_material.SetVector(_previousMainTextureScaleOffsetFieldNameID, new Vector4(1, 1, 0, 0));
			}

			if (_parentRasterTile != null)
			{
				Runnable.Run(DelayedAction(() =>
				{
					if (_parentRasterTile != null)
					{
						_parentRasterTile.AddLog("removed from parent ", CanonicalTileId);
						_parentRasterTile.RemoveUser(CanonicalTileId);
						//_parentRasterTile = null;
					}
				}, 2));
			}

			_material.SetFloat(_mainTextureChangeTimeFieldNameID, Time.time);
			_material.SetTexture(_mainTexFieldNameID, rasterTile.Texture2D);
			_material.SetVector(_mainTexStFieldNameID, new Vector4(1, 1, 0, 0));
			//MeshRenderer.SetPropertyBlock(_propertyBlock);

			CheckFinishedCondition(_rasterTile);
		}

		public void SetVectorData(VectorTile vectorTile, Action<UnityTile, Action> createMeshCallback = null)
		{
			Logs.Add("set vector data");
			_vectorTile = vectorTile;
			if (_vectorTile == null)
			{
				_createMeshCallback = null;
				return;
			}
			_createMeshCallback = createMeshCallback;

			if (_vectorTile != null)
			{
				if (vectorTile.Data != null)
				{
					if (_terrainTile == null || _terrainReady)
					{
						Logs.Add("CallCreateMeshCallback 1");
						CallCreateMeshCallback();
					}
				}
				else
				{
					if (_vectorTile.CurrentTileState == TileState.Processing)
					{
						_vectorTile.DataProcessingFinished += (success) =>
						{
							if (success)
							{
								if (_terrainTile == null || _terrainReady)
								{
									Logs.Add("CallCreateMeshCallback 2");
									CallCreateMeshCallback();
								}
							}
							else
							{
								Logs.Add("vector failed");
								CheckFinishedCondition(_vectorTile);
							}
						};
					}
					else
					{
						Logs.Add("vector was ready");
						CheckFinishedCondition(_vectorTile);
					}
				}
			}
			// _createMeshCallback = createMeshCallback;
			// if (!_isElevationActive && _createMeshCallback != null)
			// {
			// 	_createMeshCallback(this);
			// }
			//
			// if (_isElevationActive && _terrainTile != null && _createMeshCallback != null && _terrainTile.CurrentTileState == TileState.Loaded)
			// {
			// 	_createMeshCallback(this);
			// }


		}

		public void CallCreateMeshCallback()
		{
			if (_createMeshCallback != null)
			{
				_createMeshCallback(this, VectorGenerationCompleted);
			}
		}

		public void VectorGenerationCompleted()
		{
			CheckFinishedCondition(_vectorTile);
		}

		/// <summary>
		/// Method to query elevation data in any point in the tile using [0-1] range inputs.
		/// Input values are clamped for safety and QueryHeightDataNonclamped method should be used for
		/// higher performance usage.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public float QueryHeightData(float x, float y)
		{
			if (HeightData != null && HeightData.Length > 0)
			{
				var width = _terrainTile.ExtractedDataResolution;
				var sectionWidth = width * _terrainTextureScaleOffset.x;
				var padding = _terrainTile.ExtractedDataResolution * new Vector2(_terrainTextureScaleOffset.z, _terrainTextureScaleOffset.w);
				var xx = padding.x + (x * sectionWidth);
				var yy = padding.y + (y * sectionWidth);

				return HeightData[(int) yy * _terrainTile.ExtractedDataResolution
				                         + (int) xx] * _tileScale;
			}

			// if (HeightData != null && HeightData.Length > 0)
			// {
			// 	return HeightData[(int) (Mathf.Clamp01(y) * (_terrainTile.ExtractedDataResolution - 1)) * _terrainTile.ExtractedDataResolution
			// 	                  + (int) (Mathf.Clamp01(x) * (_terrainTile.ExtractedDataResolution - 1))] * _tileScale;
			// }
			return 0;
		}

		/// <summary>
		///  Method to query elevation data in any point in the tile using [0-1] range inputs.
		/// Input values aren't clamped for improved performance and assuring they are in [0-1] range
		/// is left to caller.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public float QueryHeightDataNonclamped(float x, float y)
		{
			if (HeightData != null && HeightData.Length > 0)
			{
				var width = _terrainTile.ExtractedDataResolution;
				var padding = _terrainTile.ExtractedDataResolution * new Vector2(_terrainTextureScaleOffset.z, _terrainTextureScaleOffset.w);
				var xx = padding.x + (x / width) * (width * _terrainTextureScaleOffset.x);
				var yy = padding.y + (y / width) * (width * _terrainTextureScaleOffset.y);

				return HeightData[(int) (y * (_terrainTile.ExtractedDataResolution - 1)
				                  + (int) (x * (_terrainTile.ExtractedDataResolution - 1)))] * _tileScale;
			}
			return 0;
		}

		public Texture2D GetRasterData()
		{
			return _rasterTile.Texture2D;
		}

		internal void AddTile(Tile tile)
		{
			Tiles.Add(tile);
		}

		public bool ContainsDataTile(Tile tile)
		{
			return Tiles.Contains(tile);
		}

		internal void RemoveTile(Tile tile)
		{
			Tiles.Remove(tile);
			if (_finishConditionTiles.Contains(tile))
			{
				_finishConditionTiles.Remove(tile);
			}
			CheckFinishedCondition();
		}

		public void ClearAssets()
		{
			if (Application.isEditor && !Application.isPlaying)
			{
				// DestroyImmediate(_heightTexture, true);
				// DestroyImmediate(_rasterData, true);
				DestroyImmediate(_meshFilter.sharedMesh);
			}
		}

		public void Cancel()
		{
			foreach (var tile in Tiles)
			{
				tile.Cancel();
			}
		}

		protected virtual void OnDestroy()
		{
			//Tiles doesn't get destroy frequently (or at all) as we recycle and reuse them
			Cancel();
		}

		public void SetParentTexture(UnwrappedTileId parent, RasterTile parentTile, int textureNameID = 0, int textureScaleOffsetNameID = 0)
		{
			_parentRasterTile = parentTile;
			_parentRasterTile.AddUser(CanonicalTileId);
			_parentRasterTile.AddLog("using as main texture parent ", CanonicalTileId);

			if (textureNameID == 0)
			{
				_material.SetTexture(_mainTexFieldNameID, _parentRasterTile.Texture2D);
				_material.SetTexture(_previousMainTextureFieldNameID, _parentRasterTile.Texture2D);
			}
			else
			{
				_material.SetTexture(textureNameID, _parentRasterTile.Texture2D);
			}

			var scaleOffset = CalculateScaleOffset(parent.Z);

			_material.SetVector(_previousMainTextureScaleOffsetFieldNameID, scaleOffset);
			_material.SetVector(_mainTexStFieldNameID, scaleOffset);
		}

		public void SetParentElevationTexture(UnwrappedTileId parent, RawPngRasterTile parentTile, bool isUsingShaderSolution)
		{
			_parentTerrainTile = parentTile;
			_parentTerrainTile.AddUser(CanonicalTileId);
			_parentTerrainTile.AddLog("using as elevation parent ", CanonicalTileId);

			_material.SetTexture(_shaderElevationTextureFieldNameID, _parentTerrainTile.Texture2D);

			var scaleOffset = CalculateScaleOffset(parent.Z);

			_material.SetTexture(_previousShaderElevationTextureFieldNameID, _parentTerrainTile.Texture2D);
			_material.SetVector(_previousShaderElevationTextureScaleOffsetFieldNameID, scaleOffset);
			_material.SetFloat(_tileScaleFieldNameID, TileScale);
			_material.SetVector(_shaderElevationTextureScaleOffsetFieldNameID, scaleOffset);
		}

		private Vector4 CalculateScaleOffset(int zoomDiff)
		{
			var tileZoom = this.UnwrappedTileId.Z;

			var scale = 1f;
			var offsetX = 0f;
			var offsetY = 0f;

			var current = UnwrappedTileId;
			var currentParent = current.Parent;

			for (int i = tileZoom - 1; i >= zoomDiff; i--)
			{
				scale /= 2;

				var bottomLeftChildX = currentParent.X * 2;
				var bottomLeftChildY = currentParent.Y * 2;

				//top left
				if (current.X == bottomLeftChildX && current.Y == bottomLeftChildY)
				{
					offsetY = 0.5f + (offsetY / 2);
					offsetX = offsetX / 2;
				}
				//top right
				else if (current.X == bottomLeftChildX + 1 && current.Y == bottomLeftChildY)
				{
					offsetX = 0.5f + (offsetX / 2);
					offsetY = 0.5f + (offsetY / 2);
				}
				//bottom left
				else if (current.X == bottomLeftChildX && current.Y == bottomLeftChildY + 1)
				{
					offsetX = offsetX / 2;
					offsetY = offsetY / 2;
				}
				//bottom right
				else if (current.X == bottomLeftChildX + 1 && current.Y == bottomLeftChildY + 1)
				{
					offsetX = 0.5f + (offsetX / 2);
					offsetY = offsetY / 2;
				}

				current = currentParent;
				currentParent = currentParent.Parent;
			}

			return new Vector4(scale, scale, offsetX, offsetY);
		}

		private void CheckFinishedCondition(Tile tile)
		{
			if (_finishConditionTiles.Contains(tile))
			{
				_finishConditionTiles.Remove(tile);
				CheckFinishedCondition();
			}
		}

		private void CheckFinishedCondition()
		{
			if (_finishConditionTiles.Count == 0)
			{
				TileFinished(this);
			}
		}

		public void SetFinishCondition()
		{
			_finishConditionTiles.Clear();
			foreach (var tile in Tiles)
			{
				if (tile.CurrentTileState == TileState.Loading || tile.CurrentTileState == TileState.New)
				{
					_finishConditionTiles.Add(tile);
				}
			}

			if (_finishConditionTiles.Count == 0)
			{
				TileFinished(this);
			}
		}

		public void SetRenderDepth(int depth)
		{
			//_meshRenderer.material.renderQueue = 2000 - (10 * depth);
		}

		private IEnumerator DelayedAction(Action act, int timer)
		{
			yield return new WaitForSeconds(timer);
			act();
		}

		public void ElevationDataParsingCompleted(RasterTile dataTile)
		{

		}
	}
}

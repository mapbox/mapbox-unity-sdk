using Mapbox.Unity.DataContainers;
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

		private RasterTile _rasterTile;
		public RasterTile BaseRasterData => _rasterTile;

		private RasterTile _terrainTile;
		public RasterTile TerrainData => _terrainTile;
		public float[] HeightData;

		private VectorTile _vectorTile;
		public VectorTile VectorData => _vectorTile;

		private Action<UnityTile> _createMeshCallback;
		private bool _isElevationActive;

		private int _heightDataResolution = 100;
		//keeping track of tile objects to be able to cancel them safely if tile is destroyed before data fetching finishes
		public HashSet<Tile> Tiles = new HashSet<Tile>();
		private HashSet<Tile> _finishConditionTiles = new HashSet<Tile>();
		public bool IsRecycled = false;
		public bool BackgroundImageInUse = false;
		public bool IsStopped = false;

		#region CachedUnityComponents
		MeshRenderer _meshRenderer;
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
						_collider = gameObject.AddComponent<MeshCollider>();
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
		#endregion

		internal void Initialize(IMapReadable map, UnwrappedTileId tileId, bool isElevationActive)
		{
			IsStopped = false;
			gameObject.hideFlags = HideFlags.DontSave;
			TileSize = map.UnityTileSize;
			_isElevationActive = isElevationActive;
			ElevationType = TileTerrainType.None;
			TileScale = map.WorldRelativeScale;
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

			// Setup Loading as initial state - Unregistered
			// When tile registers with factories, it will set the appropriate state.
			// None, if Factory source is None, Loading otherwise.
		}

		internal void Recycle()
		{
			if (MeshRenderer != null && MeshRenderer.sharedMaterial != null)
			{
				MeshRenderer.sharedMaterial.mainTexture = null;
			}

			IsStopped = false;
			gameObject.SetActive(false);
			IsRecycled = true;
			BackgroundImageInUse = false;

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
		}

		public void SetHeightData(RasterTile terrainTile, float heightMultiplier = 1f, bool useRelative = false, bool addCollider = false, Action<UnityTile> callback = null)
		{
			//reset height data
			if (terrainTile == null || terrainTile.Texture2D == null)
			{
				HeightData = new float[_heightDataResolution * _heightDataResolution];
				if (_createMeshCallback != null && _vectorTile != null)
				{
					_createMeshCallback(this);
				}
				return;
			}

			if (HeightData == null)
			{
				HeightData = new float[_heightDataResolution * _heightDataResolution];
			}

			_terrainTile = terrainTile;

			var tileId = terrainTile.Id;

			if (SystemInfo.supportsAsyncGPUReadback)
			{
				AsyncGpuReadbackForElevation(terrainTile, heightMultiplier, useRelative, callback, tileId);
			}
			else
			{
				SyncReadForElevation(terrainTile, heightMultiplier, useRelative, callback);
			}
		}

		private void SyncReadForElevation(RasterTile terrainTile, float heightMultiplier, bool useRelative, Action<UnityTile> callback)
		{
			_terrainTile = terrainTile;
			byte[] rgbData = _terrainTile.Texture2D.GetRawTextureData();
			//var rgbData = _heightTexture.GetRawTextureData<Color32>();
			var relativeScale = useRelative ? _relativeScale : 1f;
			var width = _terrainTile.Texture2D.width;
			for (float yy = 0; yy < _heightDataResolution; yy++)
			{
				for (float xx = 0; xx < _heightDataResolution; xx++)
				{
					var index = (((int) ((yy / _heightDataResolution) * width) * width) + (int) ((xx / _heightDataResolution) * width));

					float r = rgbData[index * 4 + 1];
					float g = rgbData[index * 4 + 2];
					float b = rgbData[index * 4 + 3];
					//var color = rgbData[index];
					// float r = color.g;
					// float g = color.b;
					// float b = color.a;
					//the formula below is the same as Conversions.GetAbsoluteHeightFromColor but it's inlined for performance
					HeightData[(int) (yy * _heightDataResolution + xx)] = relativeScale * heightMultiplier * (-10000f + ((r * 65536f + g * 256f + b) * 0.1f));
					//678 ==> 012345678
					//345
					//012
				}
			}

			if (callback != null)
			{
				callback(this);
			}

			CheckFinishedCondition(_terrainTile);
			if (_createMeshCallback != null && _vectorTile != null)
			{
				_createMeshCallback(this);
			}
		}

		private void AsyncGpuReadbackForElevation(RasterTile terrainTile, float heightMultiplier, bool useRelative, Action<UnityTile> callback, CanonicalTileId tileId)
		{
			_terrainTile = terrainTile;
			AsyncGPUReadback.Request(_terrainTile.Texture2D, 0, (t) =>
			{
				if (CanonicalTileId != tileId || IsRecycled)
				{
					return;
				}

				var width = t.width;
				var data = t.GetData<Color32>().ToArray();

				if (HeightData == null || HeightData.Length != _heightDataResolution * _heightDataResolution)
				{
					HeightData = new float[_heightDataResolution * _heightDataResolution];
				}

				var relativeScale = useRelative ? _relativeScale : 1f;
				//tt = new Texture2D(_heightDataResolution, _heightDataResolution, TextureFormat.RGBA32, false);
				for (float yy = 0; yy < _heightDataResolution; yy++)
				{
					for (float xx = 0; xx < _heightDataResolution; xx++)
					{
						var xx2 = (xx / _heightDataResolution) * width;
						var yy2 = (yy / _heightDataResolution) * width;
						var index = (((int) yy2 * width) + (int) xx2);
						//var color = _heightTexture.GetPixel((int)xx2, (int)yy2);
						//var index = (int)(((float)xx / _heightDataResolution) * 255 * 256 + (((float)yy / _heightDataResolution) * 255));

						float r = data[(int) index].g;
						float g = data[(int) index].b;
						float b = data[(int) index].a;
						//the formula below is the same as Conversions.GetAbsoluteHeightFromColor but it's inlined for performance
						HeightData[(int) (yy * _heightDataResolution + xx)] = relativeScale * heightMultiplier * (-10000f + ((r * 65536f + g * 256f + b) * 0.1f));
						//678 ==> 012345678
						//345
						//012

						//tt.SetPixel((int) xx, (int) yy, new Color(r/256, g/256, b/256));
						//tt.SetPixel((int) xx, (int) yy, color); //new Color(rgbData[index * 4 + 1] / 256f, rgbData[index * 4 + 2] / 256f, rgbData[index * 4 + 3] / 256f, 1));
					}
				}

				//tt.Apply();
				if (callback != null)
				{
					callback(this);
				}

				CheckFinishedCondition(_terrainTile);
				if (_createMeshCallback != null && _vectorTile != null)
				{
					_createMeshCallback(this);
				}
			});
		}

		public void SetRasterData(RasterTile rasterTile, bool useMipMap = false, bool useCompression = false)
		{
			_rasterTile = rasterTile;

			if (_rasterTile.Texture2D == null && _rasterTile.Data == null)
			{
				MeshRenderer.material.mainTexture = null;
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

			MeshRenderer.sharedMaterial.mainTexture = rasterTile.Texture2D;
			MeshRenderer.sharedMaterial.mainTextureScale = Unity.Constants.Math.Vector3One;
			MeshRenderer.sharedMaterial.mainTextureOffset = Unity.Constants.Math.Vector3Zero;
			BackgroundImageInUse = false;

			CheckFinishedCondition(_rasterTile);
		}

		public void SetVectorData(string tileset, VectorTile vectorTile, Action<UnityTile> createMeshCallback = null)
		{
			_vectorTile = vectorTile;
			_createMeshCallback = createMeshCallback;

			if (_vectorTile != null)
			{
				if (vectorTile.Data != null)
				{
					_createMeshCallback(this);
				}
				else
				{
					_vectorTile.DataProcessingFinished += (success) =>
					{
						if (success)
						{
							_createMeshCallback(this);
						}
					};
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
				return HeightData[(int) (Mathf.Clamp01(y) * (_heightDataResolution - 1)) * _heightDataResolution + (int) (Mathf.Clamp01(x) * (_heightDataResolution - 1))] * _tileScale;
			}
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
			if (HeightData != null)
			{
				return HeightData[(int)(y * 255) * 256 + (int)(x * 255)] * _tileScale;
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
		}

		public void ClearAssets()
		{
			if (Application.isEditor && !Application.isPlaying)
			{
				// DestroyImmediate(_heightTexture, true);
				// DestroyImmediate(_rasterData, true);
				DestroyImmediate(_meshFilter.sharedMesh);
				DestroyImmediate(_meshRenderer.sharedMaterial);
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

		public void SetParentTexture(UnwrappedTileId parent, Texture2D parentTexture, string textureName = "", string textureScaleOffsetName = "")
		{
			if (string.IsNullOrEmpty(textureName))
			{
				MeshRenderer.sharedMaterial.mainTexture = parentTexture;
			}
			else
			{
				MeshRenderer.sharedMaterial.SetTexture(textureName, parentTexture);
			}

			var tileZoom = this.UnwrappedTileId.Z;
			var parentZoom = parent.Z;

			var scale = 1f;
			var offsetX = 0f;
			var offsetY = 0f;

			var current = this.UnwrappedTileId;
			var currentParent = current.Parent;

			for (int i = tileZoom - 1; i >= parentZoom; i--)
			{
				scale /= 2;

				var bottomLeftChildX = currentParent.X * 2;
				var bottomLeftChildY = currentParent.Y * 2;

				//top left
				if (current.X == bottomLeftChildX && current.Y == bottomLeftChildY)
				{
					offsetY = 0.5f + (offsetY/2);
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

			if (string.IsNullOrEmpty(textureName))
			{
				MeshRenderer.sharedMaterial.mainTextureScale = new Vector2(scale, scale);
				MeshRenderer.sharedMaterial.mainTextureOffset = new Vector2(offsetX, offsetY);
			}
			else
			{
				MeshRenderer.sharedMaterial.SetVector(textureScaleOffsetName, new Vector4(scale, scale, offsetX, offsetY));
			}

			BackgroundImageInUse = (parentTexture != null);
		}

		private void CheckFinishedCondition(Tile tile)
		{
			if (_finishConditionTiles.Contains(tile))
			{
				_finishConditionTiles.Remove(tile);
				if (_finishConditionTiles.Count == 0)
				{
					TileFinished(this);
				}
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
	}
}

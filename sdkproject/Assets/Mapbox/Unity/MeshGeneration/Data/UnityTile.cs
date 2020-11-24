
using UnityEditor;

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
		public TileTerrainType ElevationType;
		[SerializeField] private Texture2D _rasterData;
		public VectorTile VectorData { get; private set; }
		[SerializeField] private Texture2D _heightTexture;
		public float[] HeightData;

		private Texture2D _loadingTexture;

		private int _heightDataResolution = 100;
		//keeping track of tile objects to be able to cancel them safely if tile is destroyed before data fetching finishes
		private HashSet<Tile> _tiles = new HashSet<Tile>();
		[SerializeField] private float _tileScale;

		public bool IsRecycled = false;

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
		public RectD Rect { get; private set; }
		public int InitialZoom { get; private set; }
		public int CurrentZoom { get; private set; }

		public float TileScale
		{
			get { return _tileScale; }
			private set { _tileScale = value; }
		}

		public UnwrappedTileId UnwrappedTileId { get; private set; }
		public CanonicalTileId CanonicalTileId { get; private set; }

		private float _relativeScale;
		#endregion

		[SerializeField]
		private TilePropertyState _rasterDataState;
		public TilePropertyState RasterDataState
		{
			get
			{
				return _rasterDataState;
			}
			internal set
			{
				if (_rasterDataState != value)
				{
					_rasterDataState = value;
					OnRasterDataChanged(this);
				}
			}
		}
		[SerializeField]
		private TilePropertyState _heightDataState;
		public TilePropertyState HeightDataState
		{
			get
			{
				return _heightDataState;
			}
			internal set
			{
				if (_heightDataState != value)
				{
					_heightDataState = value;
					OnHeightDataChanged(this);
				}
			}
		}
		[SerializeField]
		private TilePropertyState _vectorDataState;
		public TilePropertyState VectorDataState
		{
			get
			{
				return _vectorDataState;
			}
			internal set
			{
				if (_vectorDataState != value)
				{
					_vectorDataState = value;
					OnVectorDataChanged(this);
				}
			}
		}
		private TilePropertyState _tileState = TilePropertyState.None;
		public TilePropertyState TileState
		{
			get
			{
				return _tileState;
			}
			set
			{
				if (_tileState != value)
				{
					_tileState = value;
				}
			}
		}

		public event Action<UnityTile> OnHeightDataChanged = delegate { };
		public event Action<UnityTile> OnRasterDataChanged = delegate { };
		public event Action<UnityTile> OnVectorDataChanged = delegate { };

		private bool _isInitialized = false;


		internal void Initialize(IMapReadable map, UnwrappedTileId tileId, float scale, int zoom, Texture2D loadingTexture = null)
		{
			gameObject.hideFlags = HideFlags.DontSave;

			ElevationType = TileTerrainType.None;
			TileScale = scale;
			_relativeScale = 1 / Mathf.Cos(Mathf.Deg2Rad * (float)map.CenterLatitudeLongitude.x);
			Rect = Conversions.TileBounds(tileId);
			UnwrappedTileId = tileId;
			CanonicalTileId = tileId.Canonical;
			_loadingTexture = loadingTexture;

			float scaleFactor = 1.0f;
			if (_isInitialized == false)
			{
				_isInitialized = true;
				InitialZoom = zoom;
			}
			CurrentZoom = zoom;
			scaleFactor = Mathf.Pow(2, (map.InitialZoom - zoom));
			gameObject.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
			//gameObject.SetActive(true);

			IsRecycled = false;


			// Setup Loading as initial state - Unregistered
			// When tile registers with factories, it will set the appropriate state.
			// None, if Factory source is None, Loading otherwise.
		}

		internal void Recycle()
		{
			if (_loadingTexture && MeshRenderer != null && MeshRenderer.sharedMaterial != null)
			{
				MeshRenderer.sharedMaterial.mainTexture = _loadingTexture;
			}

			_rasterData = null;
			_heightTexture = null;
			gameObject.SetActive(false);
			IsRecycled = true;

			// Reset internal state.
			RasterDataState = TilePropertyState.Unregistered;
			HeightDataState = TilePropertyState.Unregistered;
			VectorDataState = TilePropertyState.Unregistered;
			TileState = TilePropertyState.Unregistered;

			OnHeightDataChanged = delegate { };
			OnRasterDataChanged = delegate { };
			OnVectorDataChanged = delegate { };

			Cancel();
			_tiles.Clear();
		}

		public void SetHeightData(byte[] data, float heightMultiplier = 1f, bool useRelative = false, bool addCollider = false)
		{
			if (HeightDataState != TilePropertyState.Unregistered)
			{
				//reset height data
				if (data == null)
				{
					HeightData = new float[256 * 256];
					HeightDataState = TilePropertyState.None;
					return;
				}

				// HACK: compute height values for terrain. We could probably do this without a texture2d.
				if (_heightTexture == null)
				{
					_heightTexture = new Texture2D(0, 0);
				}

				_heightTexture.LoadImage(data);
				byte[] rgbData = _heightTexture.GetRawTextureData();

				// Get rid of this temporary texture. We don't need to bloat memory.
				_heightTexture.LoadImage(null);

				if (HeightData == null)
				{
					HeightData = new float[256 * 256];
				}

				var relativeScale = useRelative ? _relativeScale : 1f;
				for (int xx = 0; xx < 256; ++xx)
				{
					for (int yy = 0; yy < 256; ++yy)
					{
						float r = rgbData[(xx * 256 + yy) * 4 + 1];
						float g = rgbData[(xx * 256 + yy) * 4 + 2];
						float b = rgbData[(xx * 256 + yy) * 4 + 3];
						//the formula below is the same as Conversions.GetAbsoluteHeightFromColor but it's inlined for performance
						HeightData[xx * 256 + yy] = relativeScale * heightMultiplier * (-10000f + ((r * 65536f + g * 256f + b) * 0.1f));
					}
				}
			}
		}

		public void SetElevationData(float[] data, float heightMultiplier = 1f, bool useRelative = false, bool addCollider = false)
		{
			if (HeightDataState != TilePropertyState.Unregistered)
			{
				//reset height data
				if (data == null)
				{
					HeightData = new float[256 * 256];
					HeightDataState = TilePropertyState.None;
					return;
				}

				HeightData = data;



				HeightDataState = TilePropertyState.Loaded;
			}
		}

		public void SetHeightTexture(Texture2D elevationTexture, float heightMultiplier = 1f, bool useRelative = false, bool addCollider = false, Action<UnityTile> callback = null)
		{
			if (HeightDataState != TilePropertyState.Unregistered)
			{
				//reset height data
				if (elevationTexture == null)
				{
					HeightData = new float[_heightDataResolution * _heightDataResolution];
					HeightDataState = TilePropertyState.None;
					return;
				}

				if (HeightData == null)
				{
					HeightData = new float[_heightDataResolution * _heightDataResolution];
				}

				_heightTexture = elevationTexture;
				byte[] rgbData = _heightTexture.GetRawTextureData();
				//var rgbData = _heightTexture.GetRawTextureData<Color32>();
				var relativeScale = useRelative ? _relativeScale : 1f;
				var width = _heightTexture.width;
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

				MeshRenderer.sharedMaterial.SetTexture("_HeightTexture", _heightTexture);
				MeshRenderer.sharedMaterial.SetFloat("_TileScale", _tileScale);
				HeightDataState = TilePropertyState.Loaded;

				// AsyncGPUReadback.Request(_heightTexture, 0, (t) =>
				// {
				// 	Debug.Log(UnwrappedTileId);
				// 	var data = t.GetData<Color32>();
				//
				// 	if (HeightData == null)
				// 	{
				// 		HeightData = new float[_heightDataResolution * _heightDataResolution];
				// 	}
				//
				// 	var relativeScale = useRelative ? _relativeScale : 1f;
				// 	//tt = new Texture2D(_heightDataResolution, _heightDataResolution, TextureFormat.RGBA32, false);
				// 	for (float yy = 0; yy < _heightDataResolution; yy++)
				// 	{
				// 		for (float xx = 0; xx < _heightDataResolution; xx++)
				// 		{
				// 			var xx2 = (xx / _heightDataResolution) * t.width;
				// 			var yy2 = (yy / _heightDataResolution) * t.width;
				// 			var index = (((int)yy2 * t.width) + (int)xx2);
				// 			//var color = _heightTexture.GetPixel((int)xx2, (int)yy2);
				// 			//var index = (int)(((float)xx / _heightDataResolution) * 255 * 256 + (((float)yy / _heightDataResolution) * 255));
				//
				// 			float r = data[(int) index].g;
				// 			float g = data[(int) index].b;
				// 			float b = data[(int) index].a;
				// 			//the formula below is the same as Conversions.GetAbsoluteHeightFromColor but it's inlined for performance
				// 			HeightData[(int) (yy * _heightDataResolution + xx)] = relativeScale * heightMultiplier * (-10000f + ((r * 65536f + g * 256f + b) * 0.1f));
				// 			//678 ==> 012345678
				// 			//345
				// 			//012
				//
				// 			//tt.SetPixel((int) xx, (int) yy, new Color(r/256, g/256, b/256));
				// 			//tt.SetPixel((int) xx, (int) yy, color); //new Color(rgbData[index * 4 + 1] / 256f, rgbData[index * 4 + 2] / 256f, rgbData[index * 4 + 3] / 256f, 1));
				// 		}
				// 	}
				// 	//tt.Apply();
				// 	if (callback != null)
				// 	{
				// 		callback(this);
				// 	}
				// 	HeightDataState = TilePropertyState.Loaded;
				//
				// });
				// Get rid of this temporary texture. We don't need to bloat memory.
				//_heightTexture.LoadImage(null);
			}
		}

		public void SetRasterData(byte[] data, bool useMipMap = true, bool useCompression = false)
		{
			// Don't leak the texture, just reuse it.
			if (RasterDataState != TilePropertyState.Unregistered)
			{
				//reset image on null data
				if (data == null)
				{
					MeshRenderer.material.mainTexture = null;
					return;
				}

				if (_rasterData == null)
				{
					_rasterData = new Texture2D(0, 0, TextureFormat.RGB24, useMipMap);
					_rasterData.wrapMode = TextureWrapMode.Clamp;
				}

				_rasterData.LoadImage(data);
				if (useCompression)
				{
					// High quality = true seems to decrease image quality?
					_rasterData.Compress(false);
				}

				MeshRenderer.sharedMaterial.mainTexture = _rasterData;

				RasterDataState = TilePropertyState.Loaded;
			}
		}

		public void SetRasterTexture(Texture2D rasterTileTexture2D, bool useMipMap = true, bool useCompression = false)
		{
			if (RasterDataState != TilePropertyState.Unregistered)
			{
				//reset image on null data
				if (rasterTileTexture2D == null)
				{
					MeshRenderer.material.mainTexture = null;
					return;
				}

				_rasterData = rasterTileTexture2D;
				_rasterData.wrapMode = TextureWrapMode.Clamp;
				if (useCompression)
				{
					// High quality = true seems to decrease image quality?
					_rasterData.Compress(false);
				}

				MeshRenderer.sharedMaterial.mainTextureScale = Unity.Constants.Math.Vector3One;
				MeshRenderer.sharedMaterial.mainTextureOffset = Unity.Constants.Math.Vector3Zero;

				//MeshRenderer.sharedMaterial.mainTexture = _rasterData;
				MeshRenderer.sharedMaterial.mainTextureScale = Unity.Constants.Math.Vector3One;
                MeshRenderer.sharedMaterial.mainTextureOffset = Unity.Constants.Math.Vector3Zero;

                MeshRenderer.sharedMaterial.SetTexture("_MainTex", _rasterData);

				RasterDataState = TilePropertyState.Loaded;
			}
		}

		public void SetVectorData(VectorTile vectorTile)
		{
			if (VectorDataState != TilePropertyState.Unregistered)
			{
				VectorData = vectorTile;
			}
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
			if (HeightData != null)
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

		public void SetLoadingTexture(Texture2D texture)
		{
			MeshRenderer.material.mainTexture = texture;
		}

		public Texture2D GetRasterData()
		{
			return _rasterData;
		}

		internal void AddTile(Tile tile)
		{
			_tiles.Add(tile);
		}

		internal void RemoveTile(Tile tile)
		{
			_tiles.Remove(tile);
		}

		public void ClearAssets()
		{
			if (Application.isEditor && !Application.isPlaying)
			{
				DestroyImmediate(_heightTexture, true);
				DestroyImmediate(_rasterData, true);
				DestroyImmediate(_meshFilter.sharedMesh);
				DestroyImmediate(_meshRenderer.sharedMaterial);
			}
		}

		public void Cancel()
		{
			foreach (var tile in _tiles)
			{
				tile.Cancel();
			}
		}

		protected virtual void OnDestroy()
		{
			//Tiles doesn't get destroy frequently (or at all) as we recycle and reuse them
			Cancel();
			if (_heightTexture != null)
			{
				_heightTexture.Destroy();
			}
			if (_rasterData != null)
			{
				_rasterData.Destroy();
			}
		}

		public void SetParentTexture(UnwrappedTileId parent, Texture2D parentTexture)
		{
			MeshRenderer.sharedMaterial.mainTexture = parentTexture;

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


			MeshRenderer.sharedMaterial.mainTextureScale = new Vector2(scale, scale);
			MeshRenderer.sharedMaterial.mainTextureOffset = new Vector2(offsetX, offsetY);
		}
	}
}

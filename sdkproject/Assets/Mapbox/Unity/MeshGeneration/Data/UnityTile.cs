namespace Mapbox.Unity.MeshGeneration.Data
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.Utilities;
	using Utils;
	using System;

	public class UnityTile : MonoBehaviour
	{
		float[] _heightData;

		private MeshRenderer _meshRenderer;
		public MeshRenderer MeshRenderer
		{
			get
			{
				if (_meshRenderer == null)
				{
					_meshRenderer = GetComponent<MeshRenderer>();
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
				}
				return _meshFilter;
			}
		}

		private MeshCollider _collider;
		public MeshCollider Collider
		{
			get
			{
				if (_collider == null)
				{
					_collider = GetComponent<MeshCollider>();
				}
				return _collider;
			}
		}

		string _vectorData;
		public string VectorData
		{
			get { return _vectorData; }
			set
			{
				_vectorData = value;
				OnVectorDataChanged(this);
			}
		}

		public MeshData MeshData { get; set; }

		public TilePropertyState RasterDataState { get; set; }
		public TilePropertyState HeightDataState { get; set; }
		public TilePropertyState VectorDataState { get; set; }

		// TODO: pass in unwrapped tile or map to initialize?
		public Vector2 TileCoordinate;
		public int Zoom;
		public RectD Rect;
		public float RelativeScale;

		public event Action<UnityTile> OnHeightDataChanged = delegate { };
		public event Action<UnityTile> OnRasterDataChanged = delegate { };
		public event Action<UnityTile> OnVectorDataChanged = delegate { };

		internal void Enable()
		{
			gameObject.SetActive(true);
		}

		internal void Disable()
		{
			gameObject.SetActive(false);

			// Reset internal state.
			RasterDataState = TilePropertyState.None;
			HeightDataState = TilePropertyState.None;
			VectorDataState = TilePropertyState.None;

			// HACK: this is for vector layer features and such.
			// It's slow and wasteful, but a better solution will be difficult.
			var childCount = transform.childCount;
			if (childCount > 0)
			{
				for (int i = 0; i < childCount; i++)
				{
					Destroy(transform.GetChild(i).gameObject);
				}
			}
		}

		internal void SetHeightData(byte[] data, float heightMultiplier = 1f)
		{
			// HACK: compute height values for terrain. We could probably do this without a texture2d.
			var heightTexture = new Texture2D(0, 0);
			heightTexture.LoadImage(data);
			var colors = heightTexture.GetPixels32();

			// Get rid of this temporary texture. We don't need it, and we don't want to leak it.
			Destroy(heightTexture);

			var count = colors.Length;

			if (_heightData == null)
			{
				_heightData = new float[count];
			}

			for (int i = 0; i < count; i++)
			{
				var height = Conversions.GetAbsoluteHeightFromColor32(colors[i]) * RelativeScale * heightMultiplier;
				_heightData[i] = height;
			}

			HeightDataState = TilePropertyState.Loaded;
			OnHeightDataChanged(this);
		}

		public float QueryHeightData(float x, float y)
		{
			if (_heightData != null)
			{
				var intX = (int)Mathf.Clamp(x * 256, 0, 255);
				var intY = (int)Mathf.Clamp(y * 256, 0, 255);
				return _heightData[intY * 256 + intX];
			}

			return 0;
		}

		Texture2D _rasterData;
		public void SetRasterData(byte[] data, bool useMipMap, bool useCompression)
		{
			// Don't leak the texture, just reuse it.
			if (_rasterData == null)
			{
				_rasterData = new Texture2D(0, 0, TextureFormat.RGB24, useMipMap);
				_rasterData.wrapMode = TextureWrapMode.Clamp;
				MeshRenderer.material.mainTexture = _rasterData;
			}

			_rasterData.LoadImage(data);
			if (useCompression)
			{
				// High quality = true seems to decrease image quality?
				_rasterData.Compress(false);
			}

			RasterDataState = TilePropertyState.Loaded;
			OnRasterDataChanged(this);
		}

		public Texture2D GetRasterData()
		{
			return _rasterData;
		}
	}
}
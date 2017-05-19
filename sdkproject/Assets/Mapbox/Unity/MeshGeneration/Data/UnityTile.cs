namespace Mapbox.Unity.MeshGeneration.Data
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.Utilities;
	using Utils;
	using Mapbox.Map;
	using Mapbox.Platform;
	using System;
	using Mapbox.Unity.Map;

	public class UnityTile : MonoBehaviour, IAsyncRequest
	{
		float[] _heightData;
		Texture2D _rasterData;
		float _relativeScale;

		MeshRenderer _meshRenderer;
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

		// TODO: should this be a string???
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
		RectD _rect;
		public RectD Rect
		{
			get
			{
				return _rect;
			}
		}

		CanonicalTileId _canonicalTileId;
		public CanonicalTileId CanonicalTileId
		{
			get
			{
				return _canonicalTileId;
			}
		}

		IAsyncRequest _asyncRequest;
		public IAsyncRequest AsyncRequest
		{
			set
			{
				_asyncRequest = value;
			}
		}

		public bool IsCompleted
		{
			get
			{
				return _asyncRequest.IsCompleted;
			}
		}

		public TilePropertyState RasterDataState { get; set; }
		public TilePropertyState HeightDataState { get; set; }
		public TilePropertyState VectorDataState { get; set; }

		public event Action<UnityTile> OnHeightDataChanged = delegate { };
		public event Action<UnityTile> OnRasterDataChanged = delegate { };
		public event Action<UnityTile> OnVectorDataChanged = delegate { };

		internal void Initialize(IMap map, UnwrappedTileId tileId)
		{
			_relativeScale = 1 / Mathf.Cos(Mathf.Deg2Rad * (float)map.CenterLatitudeLongitude.x);
			_rect = Conversions.TileBounds(tileId);
			_canonicalTileId = tileId.Canonical;
			var position = new Vector3((float)(Rect.Center.x - map.CenterMercator.x), 0, (float)(Rect.Center.y - map.CenterMercator.y));

#if !UNITY_EDITOR
			position *= map.WorldRelativeScale;
#else
			gameObject.name = tileId.ToString();
#endif
			transform.localPosition = position;
			gameObject.SetActive(true);
		}

		internal void Recycle()
		{
			// TODO: to hide potential visual artifacts, use placeholder mesh / texture?

			gameObject.SetActive(false);

			// Reset internal state.
			RasterDataState = TilePropertyState.None;
			HeightDataState = TilePropertyState.None;
			VectorDataState = TilePropertyState.None;
			_asyncRequest = null;

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
				var height = Conversions.GetAbsoluteHeightFromColor32(colors[i]) * _relativeScale * heightMultiplier;
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

		public void Cancel()
		{
			if (_asyncRequest != null)
			{
				_asyncRequest.Cancel();
			}
		}
	}
}
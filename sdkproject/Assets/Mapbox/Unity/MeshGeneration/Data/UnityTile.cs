namespace Mapbox.Unity.MeshGeneration.Data
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.Utilities;
	using Utils;
	using System;

	[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
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

		Texture2D _rasterData;
		public Texture2D RasterData
		{
			get { return _rasterData; }
			set
			{
				_rasterData = value;
				OnRasterDataChanged(this);
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

			// TODO: reset states and such.
			var childCount = transform.childCount;
			if (childCount > 0)
			{
				for (int i = 0; i < childCount; i++)
				{
					Destroy(transform.GetChild(i).gameObject);
				}
			}
		}

		internal void SetHeightData(Color32[] colors)
		{
			var count = colors.Length;

			if (_heightData == null)
			{
				_heightData = new float[count];
			}

			for (int i = 0; i < count; i++)
			{
				var height = Conversions.GetAbsoluteHeightFromColor32(colors[i]) * RelativeScale;
				_heightData[i] = height;
			}

			OnHeightDataChanged(this);
		}

		public float QueryHeightData(float x, float y)
		{
			if (_heightData != null)
			{
				return _heightData[(int)(y * 256 + x)];
			}

			return 0;
		}
	}
}
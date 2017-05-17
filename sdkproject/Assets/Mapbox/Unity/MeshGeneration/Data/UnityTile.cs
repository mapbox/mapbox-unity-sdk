namespace Mapbox.Unity.MeshGeneration.Data
{
	using System.ComponentModel;
	using JetBrains.Annotations;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.Utilities;
	using Utils;
	using System;

	[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
	public class UnityTile : MonoBehaviour
	{
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

		private Collider _collider;
		public Collider Collider
		{
			get
			{
				if (_collider == null)
				{
					_collider = GetComponent<Collider>();
				}
				return _collider;
			}
		}

		public MeshData MeshData { get; set; }

		[SerializeField]
		private Texture2D _heightData;
		[SerializeField]
		private Texture2D _imageData;
		[SerializeField]
		private string _vectorData;

		public Texture2D ImageData
		{
			get { return _imageData; }
			set
			{
				_imageData = value;
				OnRasterDataChanged(this);
			}
		}
		public Texture2D HeightData
		{
			get { return _heightData; }
			set
			{
				_heightData = value;
				OnHeightDataChanged(this);
			}
		}
		public string VectorData
		{
			get { return _vectorData; }
			set
			{
				_vectorData = value;
				OnVectorDataChanged(this);
			}
		}

		public TilePropertyState RasterDataState { get; set; }
		public TilePropertyState HeightDataState { get; set; }
		public TilePropertyState VectorDataState { get; set; }

		public Vector2 TileCoordinate;
		public int Zoom;
		public RectD Rect;
		public float RelativeScale;

		public event Action<UnityTile> OnHeightDataChanged = delegate { };
		public event Action<UnityTile> OnRasterDataChanged = delegate { };
		public event Action<UnityTile> OnVectorDataChanged = delegate { };
		
		public float QueryHeightData(float x, float y)
		{
			if (HeightData != null)
			{
				return Conversions.GetRelativeHeightFromColor(HeightData.GetPixel(
						(int)Mathf.Clamp((x * 256), 0, 255),
						(int)Mathf.Clamp((y * 256), 0, 255)), RelativeScale);
			}

			return 0;
		}

		internal void Enable()
		{
			gameObject.SetActive(true);
		}

		internal void Disable()
		{
			gameObject.SetActive(false);
		}
	}
}

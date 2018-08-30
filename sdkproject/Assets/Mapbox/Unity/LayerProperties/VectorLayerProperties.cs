namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections.Generic;
	using Mapbox.Platform.TilesetTileJSON;
	using Mapbox.Unity.Utilities;
	using UnityEngine;

	[Serializable]
	public class VectorLayerProperties : LayerProperties
	{

		public event Action OnPropertyUpdated = delegate { };

		public event Action OnPropertyUpdatedComplete = delegate { };
		public event Action OnPropertyUpdatedCollider = delegate { };
		public event Action OnPropertyUpdatedMaterial = delegate { };

		public void UpdateProperty(VectorUpdateType updateType)
		{
			//probably a better way to do this...we are doing conditional checks here, can we just call OnPropertyUpdated and pass updateType to it?
			switch (updateType)
			{
				case VectorUpdateType.Complete:
					if (OnPropertyUpdatedComplete != null)
					{
						OnPropertyUpdatedComplete();
					}
					break;
				case VectorUpdateType.Collider:
					if (OnPropertyUpdatedCollider != null)
					{
						OnPropertyUpdatedCollider();
					}
					break;
				case VectorUpdateType.Material:
					if (OnPropertyUpdatedMaterial != null)
					{
						OnPropertyUpdatedMaterial();
					}
					break;
				default:
					break;
			}

			if (OnPropertyUpdated != null)
			{
				OnPropertyUpdated();
			}
		}

		/// <summary>
		/// Raw tileJSON response received from the requested source tileset id(s)
		/// </summary>
		public TileJsonData tileJsonData = new TileJsonData();
		[SerializeField]
		protected VectorSourceType _sourceType = VectorSourceType.MapboxStreets;
		public VectorSourceType sourceType
		{
			get
			{
				return _sourceType;
			}
			set
			{
				if (value != VectorSourceType.Custom)
				{
					sourceOptions.Id = MapboxDefaultVector.GetParameters(value).Id;
				}

				if (value == VectorSourceType.None)
				{
					sourceOptions.isActive = false;
				}
				else
				{
					sourceOptions.isActive = true;
				}

				_sourceType = value;
			}
		}

		public LayerSourceOptions sourceOptions = new LayerSourceOptions()
		{
			isActive = true,
			layerSource = MapboxDefaultVector.GetParameters(VectorSourceType.MapboxStreets)
		};
		[Tooltip("Use Mapbox style-optimized tilesets, remove any layers or features in the tile that are not represented by a Mapbox style. Style-optimized vector tiles are smaller, served over-the-wire, and a great way to reduce the size of offline caches.")]
		public bool useOptimizedStyle = false;
		[StyleSearch]
		public Style optimizedStyle;
		public LayerPerformanceOptions performanceOptions;
		[NodeEditorElementAttribute("Feature Sublayers")]
		public List<VectorSubLayerProperties> vectorSubLayers = new List<VectorSubLayerProperties>();
		[NodeEditorElementAttribute("POI Sublayers")]
		public List<PrefabItemOptions> locationPrefabList = new List<PrefabItemOptions>();
	}
}

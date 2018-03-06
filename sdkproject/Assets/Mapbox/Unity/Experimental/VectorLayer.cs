namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Factories;
	[Serializable]
	public class VectorLayer : IVectorDataLayer
	{
		public MapLayerType LayerType
		{
			get
			{
				return MapLayerType.Vector;
			}
		}

		public bool IsLayerActive
		{
			get;
			set;
		}

		public string LayerSource
		{
			get;
			set;
		}

		public LayerProperties LayerProperty
		{
			get;
			set;
		}

		public VectorPrimitiveType PrimitiveType
		{
			get;
			set;
		}

		public void Initialize(LayerProperties properties)
		{
			var vectorLayerProperties = (VectorLayerProperties)properties;
			_vectorTileFactory = ScriptableObject.CreateInstance<VectorTileFactory>();
			_vectorTileFactory.SetOptions(vectorLayerProperties);
		}

		public void Remove()
		{
			throw new System.NotImplementedException();
		}

		public void Update(LayerProperties properties)
		{
			throw new System.NotImplementedException();
		}

		public VectorTileFactory VectorFactory
		{
			get
			{
				return _vectorTileFactory;
			}
		}
		private VectorTileFactory _vectorTileFactory;
	}
}

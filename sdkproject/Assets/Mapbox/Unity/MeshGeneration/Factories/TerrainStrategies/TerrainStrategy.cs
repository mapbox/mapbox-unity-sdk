using UnityEngine;
using Mapbox.Unity.MeshGeneration.Data;
using System;
using Mapbox.Unity.Map;
using System.Collections.ObjectModel;
using Mapbox.Map;

namespace Mapbox.Unity.MeshGeneration.Factories.TerrainStrategies
{
	public class TerrainStrategy
	{
		[SerializeField]
		protected ElevationLayerProperties _elevationOptions = new ElevationLayerProperties();

		public virtual int RequiredVertexCount
		{
			get { return 0; }
		}

		public virtual void Initialize(ElevationLayerProperties elOptions)
		{
			_elevationOptions = elOptions;
		}

		public virtual void RegisterTile(UnityTile tile)
		{

		}

		public virtual void PostProcessTile(UnityTile tile)
		{

		}

		public virtual void UnregisterTile(UnityTile tile)
		{

		}

		public virtual void DataErrorOccurred(UnityTile tile, TileErrorEventArgs e)
		{

		}
	}
}

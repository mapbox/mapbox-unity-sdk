using UnityEngine;
using Mapbox.Unity.MeshGeneration.Data;
using System;
using Mapbox.Unity.Map;
using System.Collections.ObjectModel;

namespace Mapbox.Unity.MeshGeneration.Factories.TerrainStrategies
{
	public class TerrainStrategy
	{
		[SerializeField]
		protected ElevationLayerProperties _elevationOptions = new ElevationLayerProperties();

		public virtual void OnInitialized(ElevationLayerProperties elOptions)
		{
			_elevationOptions = elOptions;
		}

		public virtual void OnRegistered(UnityTile tile)
		{

		}

		public virtual void OnUnregistered(UnityTile tile)
		{

		}

		internal void OnFetchingError(ReadOnlyCollection<Exception> exceptions)
		{

		}
	}
}
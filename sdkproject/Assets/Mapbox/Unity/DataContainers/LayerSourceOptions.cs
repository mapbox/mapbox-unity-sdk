namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Unity.MeshGeneration.Factories;
	[Serializable]
	public class LayerSourceOptions
	{
		public bool isActive;
		public Style layerSource;

		public string Id
		{
			get
			{
				return layerSource.Id;
			}
			set
			{
				layerSource.Id = value;
			}
		}
	}
}

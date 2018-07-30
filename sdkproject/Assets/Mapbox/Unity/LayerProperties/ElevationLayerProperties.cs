namespace Mapbox.Unity.Map
{
	using System;
	using System.ComponentModel;
	using Mapbox.Unity.MeshGeneration.Factories;

	[Serializable]
	public class ElevationLayerProperties : LayerProperties, INotifyPropertyChanged
	{
		public ElevationSourceType sourceType = ElevationSourceType.MapboxTerrain;

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(String propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public LayerSourceOptions sourceOptions = new LayerSourceOptions()
		{
			layerSource = new Style()
			{
				Id = "mapbox.terrain-rgb"
			},
			isActive = true
		};
		public ElevationLayerType elevationLayerType = ElevationLayerType.FlatTerrain;
		public ElevationRequiredOptions requiredOptions = new ElevationRequiredOptions();
		public ElevationModificationOptions modificationOptions = new ElevationModificationOptions();
		public UnityLayerOptions unityLayerOptions = new UnityLayerOptions();
		public TerrainSideWallOptions sideWallOptions = new TerrainSideWallOptions();

		public ElevationLayerType ElevationLayerType
		{
			get
			{
				return elevationLayerType;
			}
			set
			{

				if (value != this.elevationLayerType)
				{
					this.elevationLayerType = value;
					NotifyPropertyChanged("ElevationLayerType");
				}
			}
		}
	}
}

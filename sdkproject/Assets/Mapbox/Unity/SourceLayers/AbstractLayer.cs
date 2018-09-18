namespace Mapbox.Unity.Map
{
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.MeshGeneration.Interfaces;

	public class LayerUpdateArgs : System.EventArgs
	{
		public AbstractTileFactory factory;
		public MapboxDataProperty property;
		public bool effectsVectorLayer;
	}

	public class VectorLayerUpdateArgs : LayerUpdateArgs
	{
		public LayerVisualizerBase visualizer;
	}

	public class AbstractLayer
	{
		public event System.EventHandler UpdateLayer;
		protected virtual void NotifyUpdateLayer(LayerUpdateArgs layerUpdateArgs)
		{
			System.EventHandler handler = UpdateLayer;
			if (handler != null)
			{
				handler(this, layerUpdateArgs);
			}
		}
		protected virtual void NotifyUpdateLayer(AbstractTileFactory factory, MapboxDataProperty prop, bool effectsVectorLayer = false)
		{
			System.EventHandler handler = UpdateLayer;
			if (handler != null)
			{
				LayerUpdateArgs layerUpdateArgs = new LayerUpdateArgs()
				{
					factory = factory,
					effectsVectorLayer = effectsVectorLayer,
					property = prop
				};
				handler(this, layerUpdateArgs);
			}
		}
	}
}

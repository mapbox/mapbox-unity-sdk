namespace Mapbox.Unity.Map
{
	using Mapbox.Unity.MeshGeneration.Factories;

	public class LayerUpdateArgs : System.EventArgs
	{
		public AbstractTileFactory factory;
		public MapboxDataProperty property;
		public bool effectsVectorLayer;
	}

	public class AbstractLayer
	{
		public event System.EventHandler UpdateLayer;
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

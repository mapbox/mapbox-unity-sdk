namespace Mapbox.Unity.Map
{
	using Mapbox.Unity.MeshGeneration.Factories;

	public class LayerUpdateArgs : System.EventArgs
	{
		public AbstractTileFactory factory;
		public bool effectsVectorLayer;
	}

	public class AbstractLayer
	{
		public event System.EventHandler UpdateLayer;
		protected virtual void NotifyUpdateLayer(AbstractTileFactory factory, bool effectsVectorLayer = false)
		{
			System.EventHandler handler = UpdateLayer;
			if (handler != null)
			{
				LayerUpdateArgs layerUpdateArgs = new LayerUpdateArgs()
				{
					factory = factory,
					effectsVectorLayer = effectsVectorLayer
				};
				handler(this, layerUpdateArgs);
			}
		}
	}
}

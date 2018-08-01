namespace Mapbox.Unity.Map
{
	using Mapbox.Unity.MeshGeneration.Factories;

	public class AbstractLayer
	{
		public void NotifyUpdateLayer(AbstractTileFactory factory, bool effectsVectorLayer = false)
		{
			UpdateLayer(factory, effectsVectorLayer);
		}

		public event UpdateLayerHandler UpdateLayer;
		public delegate void UpdateLayerHandler(AbstractTileFactory factory, bool effectsVectorLayer);
	}
}

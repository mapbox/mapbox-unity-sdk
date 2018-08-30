namespace Mapbox.Unity.Map
{
	using Mapbox.Unity.MeshGeneration.Factories;

	public class AbstractLayer
	{
		public void NotifyUpdateLayer(AbstractTileFactory factory, VectorUpdateType updateType = VectorUpdateType.None)
		{
			if(UpdateLayer != null)
			{
				UpdateLayer(factory, updateType);
			}
		}

		public event UpdateLayerHandler UpdateLayer;
		public delegate void UpdateLayerHandler(AbstractTileFactory factory, VectorUpdateType updateType);
	}
}

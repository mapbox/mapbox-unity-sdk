namespace Mapbox.Unity.Map
{
	//public class Terrain
	// Layer Interfaces
	public interface ILayer
	{
		MapLayerType LayerType { get; }
		bool IsLayerActive { get; set; }
		string LayerSource { get; set; }

		LayerProperties LayerProperty { get; set; }

		//TODO : These methods should return a status. 
		void Initialize(LayerProperties properties);
		void Update(LayerProperties properties);
		void Remove();

	}

	public interface ITerrainLayer : ILayer
	{

	}

	public interface IImageryLayer : ILayer
	{

	}

	public interface IVectorDataLayer : ILayer
	{
		VectorPrimitiveType PrimitiveType { get; set; }
	}
}

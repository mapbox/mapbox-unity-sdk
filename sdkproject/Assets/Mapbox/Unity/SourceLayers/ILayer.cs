using System.Linq;
using Mapbox.Unity.SourceLayers;

namespace Mapbox.Unity.Map
{
	//public class Terrain
	// Layer Interfaces
	public interface ILayer
	{
		MapLayerType LayerType { get; }
		bool IsLayerActive { get; }
		string LayerSourceId { get; }

		//LayerProperties LayerProperty { get; set; }

		//TODO : These methods should return a status.
		void SetLayerSource(string source);
		void Initialize();
		void Initialize(LayerProperties properties);
		void Update(LayerProperties properties);
		void Remove();

	}

	public interface IVectorDataLayer : ILayer
	{

	}

	// TODO: Move interfaces into individual files.

	public interface ISubLayerPolygonGeometryOptions
	{

	}

	public interface ISubLayerFiltering
	{

	}

	public interface IVectorSubLayer
	{
		ISubLayerFiltering Filtering { get; }
		ISubLayerModeling Modeling { get; }
		ISubLayerTexturing Texturing { get; }
		ISubLayerBehaviorModifiers BehaviorModifiers { get; }

		// Add methods that we need at sublayer level
	}
}

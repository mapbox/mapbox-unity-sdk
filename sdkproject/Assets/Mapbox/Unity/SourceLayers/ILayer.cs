namespace Mapbox.Unity.Map
{
	//public class Terrain
	// Layer Interfaces
	public interface ILayer
	{
		MapLayerType LayerType { get; }
		bool IsLayerActive { get; }
		string LayerSource { get; }

		//LayerProperties LayerProperty { get; set; }

		//TODO : These methods should return a status. 
		void SetLayerSource(string source);
		void Initialize();
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

	}


	// TODO: Move interfaces into individual files. 
	public interface ISubLayerCoreOptions
	{
	}
	public interface ISubLayerExtrusionOptions
	{
	}

	public interface ISubLayerLineGeometryOptions
	{

	}

	public interface ISubLayerPolygonGeometryOptions
	{

	}

	public interface ISubLayerFiltering
	{

	}

	public interface ISubLayerModeling :
	ISubLayerCoreOptions,
	ISubLayerExtrusionOptions,
	ISubLayerLineGeometryOptions,
	ISubLayerPolygonGeometryOptions
	{

	}

	public interface ISubLayerTexturing
	{

	}

	public interface ISubLayerBehaviorModifiers
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


	// TODO Move classes into individual files. 
	public class SubLayerModeling : ISubLayerModeling
	{
		VectorSubLayerProperties _subLayerProperties;
		public SubLayerModeling(VectorSubLayerProperties subLayerProperties)
		{
			_subLayerProperties = subLayerProperties;
		}
	}

	public class SubLayerBehaviorModifiers : ISubLayerBehaviorModifiers
	{
		// TODO: Remove if not required. 
		VectorSubLayerProperties _subLayerProperties;
		public SubLayerBehaviorModifiers(VectorSubLayerProperties subLayerProperties)
		{
			_subLayerProperties = subLayerProperties;
		}
	}

}



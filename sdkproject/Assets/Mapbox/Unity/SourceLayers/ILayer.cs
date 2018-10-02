namespace Mapbox.Unity.Map
{
	//public class Terrain
	// Layer Interfaces
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Filters;

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
		LayerFilter AddStringFilterContains(string key, string property);
		LayerFilter AddNumericFilterEquals(string key, float value);
		LayerFilter AddNumericFilterLessThan(string key, float value);
		LayerFilter AddNumericFilterGreaterThan(string key, float value);
		LayerFilter AddNumericFilterInRange(string key, float min, float max);

		LayerFilter GetFilter(int index);
		void DeleteFilter(int index);

		IEnumerable<LayerFilter> GetAllFilters();
		IEnumerable<LayerFilter> GetFiltersByQuery(System.Func<LayerFilter, bool> query);

		LayerFilterCombinerOperationType GetFilterCombinerType();

		void SetFilterCombinerType(LayerFilterCombinerOperationType layerFilterCombinerOperationType);
	}

	public interface ISubLayerFilteringOptions
	{
		bool FilterKeyContains(string key);
		bool FilterKeyMatchesExact(string key);
		bool FilterUsesOperationType(LayerFilterOperationType layerFilterOperationType);
		bool FilterPropertyContains(string property);
		bool FilterPropertyMatchesExact(string property);
		bool FilterNumberValueEquals(float value);
		bool FilterNumberValueIsGreaterThan(float value);
		bool FilterNumberValueIsLessThan(float value);
		bool FilterIsInRangeValueContains(float value);

		void SetStringContains(string key, string property);
		void SetNumberIsEqual(string key, float value);
		void SetNumberIsLessThan(string key, float value);
		void SetNumberIsGreaterThan(string key, float value);
		void SetNumberIsInRange(string key, float min, float max);

	}

	public interface ISubLayerModeling :
	ISubLayerCoreOptions,
	ISubLayerExtrusionOptions,
	ISubLayerLineGeometryOptions,
	ISubLayerPolygonGeometryOptions
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



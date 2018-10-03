using Mapbox.Unity.SourceLayers;

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
		ILayerFilter AddStringFilterContains(string key, string property);
		ILayerFilter AddNumericFilterEquals(string key, float value);
		ILayerFilter AddNumericFilterLessThan(string key, float value);
		ILayerFilter AddNumericFilterGreaterThan(string key, float value);
		ILayerFilter AddNumericFilterInRange(string key, float min, float max);

		ILayerFilter GetFilter(int index);

		void RemoveFilter(int index);
		void RemoveFilter(LayerFilter filter);
		void RemoveFilter(ILayerFilter filter);
		void RemoveAllFilters();

		IEnumerable<ILayerFilter> GetAllFilters();
		IEnumerable<ILayerFilter> GetFiltersByQuery(System.Func<ILayerFilter, bool> query);

		LayerFilterCombinerOperationType GetFilterCombinerType();

		void SetFilterCombinerType(LayerFilterCombinerOperationType layerFilterCombinerOperationType);
	}

	public interface ILayerFilter
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

		string GetKey { get; }
		LayerFilterOperationType GetFilterOperationType { get; }

		string GetPropertyValue { get; }
		float GetNumberValue { get; }

		float GetMinValue { get; }
		float GetMaxValue { get; }

		void SetStringContains(string key, string property);
		void SetNumberIsEqual(string key, float value);
		void SetNumberIsLessThan(string key, float value);
		void SetNumberIsGreaterThan(string key, float value);
		void SetNumberIsInRange(string key, float min, float max);

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



using Mapbox.Unity.SourceLayers;

namespace Mapbox.Unity.Map
{
	//public class Terrain
	// Layer Interfaces
	public interface ILayer
	{
		/// <summary>
		/// Gets the type of feature from the `FEATURES` section.
		/// </summary>
		MapLayerType LayerType { get; }
		/// <summary>
		/// Boolean for setting the feature layer active or inactive.
		/// </summary>
		bool IsLayerActive { get; }
		/// <summary>
		/// Gets the source ID for the feature layer.
		/// </summary>
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

	public interface ISubLayerBehaviorModifiers
	{

	}

	public interface IVectorSubLayer
	{
		/// <summary>
		/// Gets `Filters` data from the feature.
		/// </summary>
		ISubLayerFiltering Filtering { get; }
		/// <summary>
		/// Gets `Modeling` data from the feature.
		/// </summary>
		ISubLayerModeling Modeling { get; }
		/// <summary>
		/// Gets `Texturing` data from the feature.
		/// </summary>
		ISubLayerTexturing Texturing { get; }
		/// <summary>
		/// Gets `Behavior Modifiers` data from the feature.
		/// </summary>
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

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

	public interface ISubLayerTexturing
	{
		void SetDefaultStyleType(StyleTypes style);
	}

	public interface IVectorSubLayer
	{
		ISubLayerTexturing Texturing { get; }
		ISubLayerModeling Modeling { get; }
	}
	public class SubLayerTexturing : ISubLayerTexturing
	{
		public void SetDefaultStyleType(StyleTypes style)
		{
			throw new System.NotImplementedException();
		}
	}

	public interface ISubLayerCoreOptions
	{
		void SetGeometryPrimitiveType(VectorPrimitiveType primitiveType);
	}
	public interface ISubLayerExtrusionOptions
	{
		void SetExtrusion(ExtrusionType extrusionType, ExtrusionGeometryType geometryType = ExtrusionGeometryType.RoofAndSide);
	}

	public interface ISubLayerModeling : ISubLayerCoreOptions, ISubLayerExtrusionOptions
	{
		void SetBlah();
	}

	public class SubLayerModeling : ISubLayerModeling
	{
		VectorSubLayerProperties _subLayerProperties;
		public SubLayerModeling(VectorSubLayerProperties subLayerProperties)
		{
			_subLayerProperties = subLayerProperties;
		}
		public void SetBlah()
		{
			throw new System.NotImplementedException();
		}

		public void SetExtrusion(ExtrusionType extrusionType, ExtrusionGeometryType geometryType = ExtrusionGeometryType.RoofAndSide)
		{
			_subLayerProperties.extrusionOptions.extrusionType = extrusionType;
		}

		public void SetGeometryPrimitiveType(VectorPrimitiveType primitiveType)
		{
			throw new System.NotImplementedException();
		}
	}
}



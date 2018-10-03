namespace Mapbox.Unity.Map
{
	public interface ISubLayerCoreOptions
	{
		/// <summary>
		/// Change the primtive type of the feature which will be used to decide
		/// what type of mesh operations features will require.
		/// In example, roads are generally visualized as lines and buildings are
		/// generally visualized as polygons.
		/// </summary>
		/// <param name="type">Primitive type of the featues in the layer.</param>
		void SetPrimitiveType(VectorPrimitiveType type);
	}
}
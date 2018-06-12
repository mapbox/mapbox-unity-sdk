namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using Mapbox.Unity.MeshGeneration.Data;

	public interface IReplacementCriteria
	{
		bool ShouldReplaceFeature(VectorFeatureUnity feature);
	}
}
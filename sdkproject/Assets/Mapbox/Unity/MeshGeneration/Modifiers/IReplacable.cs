using System.Collections.Generic;

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	public interface IReplacable
	{
		List<IReplacementCriteria> Criteria { get; set; }
	}
}

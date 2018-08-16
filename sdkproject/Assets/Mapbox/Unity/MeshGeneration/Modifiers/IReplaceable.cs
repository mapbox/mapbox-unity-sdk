using System.Collections.Generic;

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	public interface IReplaceable
	{
		HashSet<IReplacementCriteria> Criteria { get; set; }

	}
}

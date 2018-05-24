using System.Collections.Generic;

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	public interface IReplaceable
	{
		List<IReplacementCriteria> Criteria { get; set; }

	}
}

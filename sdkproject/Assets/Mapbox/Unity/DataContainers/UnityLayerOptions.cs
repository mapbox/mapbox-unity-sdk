using Mapbox.Unity.MeshGeneration.Data;

namespace Mapbox.Unity.Map
{
	using System;
	using UnityEngine;

	[Serializable]
	public class UnityLayerOptions : MapboxDataProperty
	{
		[Tooltip("Add terrain tiles to Unity Layer")]
		public bool addToLayer = false;
		[Tooltip("Unity Layer id to which terrain tiles will get added.")]
		public int layerId = 0;

		public override void UpdateProperty(UnityTile tile)
		{
			tile.gameObject.layer = layerId;
		}
	}
}

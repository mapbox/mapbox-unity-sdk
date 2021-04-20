﻿using System;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEngine;

namespace Mapbox.Unity.DataContainers
{
	[Serializable]
	public class TerrainColliderOptions : MapboxDataProperty
	{
		[Tooltip("Add Unity Physics collider to terrain tiles, used for detecting collisions etc.")]
		public bool addCollider = false;

		public override void UpdateProperty(UnityTile tile)
		{
			var existingCollider = tile.Collider;
			if (addCollider)
			{
				if (existingCollider == null)
				{
					tile.gameObject.AddComponent<MeshCollider>();
				}
			}
			else
			{
				tile.Collider.Destroy();
			}
		}

		public override bool NeedsForceUpdate()
		{
			return true;
		}
	}
}

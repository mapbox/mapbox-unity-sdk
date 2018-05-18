namespace Mapbox.Unity.Map
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using System;
	using Mapbox.Unity.Map;

	[Serializable]
	public class SpawnPrefabOptions : ModifierProperties
	{
		public override Type ModifierType
		{
			get
			{
				return typeof(PrefabModifier);
			}
		}

		public GameObject prefab;
		public bool scaleDownWithWorld = true;
		[NonSerialized]
		public Action<List<GameObject>> AllPrefabsInstatiated = delegate { };
	}
}
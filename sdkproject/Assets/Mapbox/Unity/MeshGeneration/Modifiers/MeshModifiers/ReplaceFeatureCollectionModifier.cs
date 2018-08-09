namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.Map;
	using System;

	[System.Serializable]
	public class FeatureBundle
	{
		//Name parameter shows up in inspector display instead of Element 0 , 1, etc...
		[HideInInspector]
		public string Name;
		public bool active;

		public SpawnPrefabOptions spawnPrefabOptions;
		public bool alwaysSpawnPrefab;

		[Geocode]
		public List<string> _prefabLocations;

		public List<string> _explicitlyBlockedFeatureIds;
	}

	/// <summary>
	/// ReplaceFeatureCollectionModifier aggregates multiple ReplaceFeatureModifier objects into one modifier.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Replace Feature Collection Modifier")]
	public class ReplaceFeatureCollectionModifier : GameObjectModifier, IReplacementCriteria
	{
		public List<FeatureBundle> features = new List<FeatureBundle>();

		private List<ReplaceFeatureModifier> _replaceFeatureModifiers;

		//Set the Name parameter to be either the prefab name or Feature; helps make the UI look slightly better...
		private void OnValidate()
		{
			foreach(FeatureBundle feature in features)
			{
				feature.Name = (feature.spawnPrefabOptions.prefab != null) ? feature.spawnPrefabOptions.prefab.name : "Feature";
			}
		}

		public override void Initialize()
		{
			base.Initialize();
			_replaceFeatureModifiers = new List<ReplaceFeatureModifier>();
			foreach (FeatureBundle feature in features)
			{
				ReplaceFeatureModifier replaceFeatureModifier = ScriptableObject.CreateInstance<ReplaceFeatureModifier>();

				replaceFeatureModifier.Active = feature.active;
				replaceFeatureModifier.SpawnPrefabOptions = feature.spawnPrefabOptions;
				replaceFeatureModifier.alwaysSpawnPrefab = feature.alwaysSpawnPrefab;
				replaceFeatureModifier.PrefabLocations = new List<string>(feature._prefabLocations);
				replaceFeatureModifier.BlockedIds = new List<string>(feature._explicitlyBlockedFeatureIds);
				replaceFeatureModifier.Initialize();

				_replaceFeatureModifiers.Add(replaceFeatureModifier);
			}
		}

		public override void FeaturePreProcess(VectorFeatureUnity feature)
		{
			foreach (ReplaceFeatureModifier modifier in _replaceFeatureModifiers)
			{
				if (modifier == null)
				{
					continue;
				}
				modifier.FeaturePreProcess(feature);
			}
		}

		public override void SetProperties(ModifierProperties properties)
		{
			foreach (ReplaceFeatureModifier modifier in _replaceFeatureModifiers)
			{
				if (modifier == null)
				{
					continue;
				}
				modifier.SetProperties(properties);
			}
		}

		public bool ShouldReplaceFeature(VectorFeatureUnity feature)
		{
			foreach (ReplaceFeatureModifier modifier in _replaceFeatureModifiers)
			{
				if (modifier == null)
				{
					continue;
				}
				if(modifier.ShouldReplaceFeature(feature))
				{
					return true;
				}
			}
			return false;
		}

		public override void Run(VectorEntity ve, UnityTile tile)
		{
			foreach (ReplaceFeatureModifier modifier in _replaceFeatureModifiers)
			{
				modifier.Run(ve, tile);
			}
		}
	}
}

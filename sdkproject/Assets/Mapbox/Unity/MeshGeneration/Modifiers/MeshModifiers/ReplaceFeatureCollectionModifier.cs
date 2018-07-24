namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Data;

	/// <summary>
	/// ReplaceFeatureCollectionModifier aggregates multiple ReplaceFeatureModifier objects into one modifier.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Replace Feature Collection Modifier")]
	public class ReplaceFeatureCollectionModifier : GameObjectModifier, IReplacementCriteria
	{
		public List<ReplaceFeatureModifier> _replaceFeatureModifiers = new List<ReplaceFeatureModifier>();

		public override void Initialize()
		{
			base.Initialize();
			foreach (ReplaceFeatureModifier modifier in _replaceFeatureModifiers)
			{
				if(modifier == null)
				{
					continue;
				}
				modifier.Initialize();
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

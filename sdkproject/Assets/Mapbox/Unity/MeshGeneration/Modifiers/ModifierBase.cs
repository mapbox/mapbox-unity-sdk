namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using System;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Map;

	[Serializable]
	public abstract class ModifierProperties : MapboxDataProperty
	{
		public abstract Type ModifierType
		{
			get;
		}
	}

	public class ModifierBase : ScriptableObject
	{
		[SerializeField]
		public bool Active = true;
		public virtual void SetProperties(ModifierProperties properties)
		{

		}
		public virtual void Initialize()
		{

		}

		public virtual void FeaturePreProcess(VectorFeatureUnity feature)
		{

		}
	}
}

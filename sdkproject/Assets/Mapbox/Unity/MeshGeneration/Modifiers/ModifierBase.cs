namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using System;

	[Serializable]
	public abstract class ModifierProperties
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
	}
}

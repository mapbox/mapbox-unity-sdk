namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using System;
	using Mapbox.Unity.Map;

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
		protected IMapReadable _map;

		public virtual void SetProperties(ModifierProperties properties)
		{

		}
		public virtual void Initialize( IMapReadable map )
		{
			_map = map;
		}
	}
}

namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using System;
	using Mapbox.Unity.MeshGeneration.Data;


	public abstract class MapboxDataProperty
	{
		public event System.EventHandler PropertyHasChanged;
		protected virtual void OnPropertyHasChanged(System.EventArgs e)
		{
			System.EventHandler handler = PropertyHasChanged;
			if (handler != null)
			{
				handler(this, e);
			}
		}
		bool _hasChanged;

		public bool HasChanged
		{
			set
			{
				if (value == true)
				{
					OnPropertyHasChanged(null /*Pass args here */);
					// reset HasChanged 
					_hasChanged = false;
				}
			}
		}
	}

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

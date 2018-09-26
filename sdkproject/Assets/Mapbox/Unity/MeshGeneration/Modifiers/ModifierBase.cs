namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using System;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.MeshGeneration.Interfaces;

	[Serializable]
	public abstract class ModifierProperties : MapboxDataProperty
	{
		public abstract Type ModifierType
		{
			get;
		}
		public virtual void UpdateProperty(LayerVisualizerBase layerVisualizer)
		{

		}
		public override bool HasChanged
		{
			set
			{
				if (value == true)
				{
					OnPropertyHasChanged(new VectorLayerUpdateArgs { property = this });
				}
			}
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

		public virtual void UnbindProperties()
		{

		}
		public virtual void UpdateModifier(object sender, System.EventArgs layerArgs)
		{
			NotifyUpdateModifier(new VectorLayerUpdateArgs { property = sender as MapboxDataProperty, modifier = this });
		}

		public event System.EventHandler ModifierHasChanged;
		protected virtual void NotifyUpdateModifier(VectorLayerUpdateArgs layerUpdateArgs)
		{
			System.EventHandler handler = ModifierHasChanged;
			if (handler != null)
			{
				handler(this, layerUpdateArgs);
			}
		}
	}
}

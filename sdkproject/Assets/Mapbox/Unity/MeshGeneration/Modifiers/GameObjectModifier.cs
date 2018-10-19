namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Components;
	using System;

	/// <summary>
	/// GameObject Modifiers
	/// Game object modifiers ran after the mesh modifiers and game object creation.Their main purpose is to work on 
	/// game object and decorate/improve them in their own ways.They ran for each game object individually. 
	/// It's possible to do lots of different things with GameObject Modifiers.A simple example would be MaterialModifier, 
	/// which simply sets random materials to gameobject and submeshes.A more complicated example would be 
	/// SpawnInside Modifier which instantiates prefabs in a polygon, like trees in a park.
	/// Any operation, you want to perform on generated entity, that would require a game object is a good candidate 
	/// for game object modifiers. For example, things like adding a collider or animation would require a gameobject 
	/// hence cannot be done in mesh modifier.
	/// Game object modifiers is the suggested way of customizing generated game object and we expect developers to 
	/// fully utilize this by creating their own custom game object modifiers.
	/// </summary>
	public class GameObjectModifier : ModifierBase
	{
		public virtual void Run(VectorEntity ve, UnityTile tile)
		{

		}

		public virtual void OnPoolItem(VectorEntity vectorEntity)
		{
			
		}

		public virtual void ClearCaches()
		{

		}
	}
}

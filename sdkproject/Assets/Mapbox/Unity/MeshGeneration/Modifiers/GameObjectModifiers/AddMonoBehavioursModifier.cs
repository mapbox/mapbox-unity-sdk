namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Components;
	using UnityEngine;
	using System.Collections.Generic;
	using System;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Add Monobehaviours Modifier")]
	public class AddMonoBehavioursModifier : GameObjectModifier
	{
		[SerializeField]
		AddMonoBehavioursModifierType[] _types;
		private HashSet<string> _scripts;
		private string _tempId;

		public override void Initialize()
		{
			if (_scripts == null)
			{
				_scripts = new HashSet<string>();
				_tempId = string.Empty;
			}
		}

		public override void Run(VectorEntity ve, UnityTile tile)
		{
			foreach (var t in _types)
			{
				_tempId = ve.GameObject.GetInstanceID() + t.Type.FullName;
				if (!_scripts.Contains(_tempId))
				{
					ve.GameObject.AddComponent(t.Type);
					_scripts.Add(_tempId);
				}
			}
		}
	}
}
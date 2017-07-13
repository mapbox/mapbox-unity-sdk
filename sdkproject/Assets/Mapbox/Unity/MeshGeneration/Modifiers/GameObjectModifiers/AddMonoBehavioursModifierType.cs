namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System;
	using UnityEngine;

#if UNITY_EDITOR
	using UnityEditor;
#endif

	[Serializable]
	public class AddMonoBehavioursModifierType
	{
		[SerializeField]
		string _type;

#if UNITY_EDITOR
		// HACK: what is the implication of keeping this around? Memory?
		[SerializeField]
		MonoScript _script;
#endif

		public Type Type
		{
			get
			{
				return Type.GetType(_type);
			}
		}
	}
}
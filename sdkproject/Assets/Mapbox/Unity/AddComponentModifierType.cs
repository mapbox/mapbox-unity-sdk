using System;
using UnityEngine;

namespace ModuleMachine
{
	[Serializable]
	public class AddComponentModifierType
	{
		[SerializeField]
		string _type;

		public Type Type
		{
			get
			{
				return Type.GetType(_type);
			}
		}
	}
}
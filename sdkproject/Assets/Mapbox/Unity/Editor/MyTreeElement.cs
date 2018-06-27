namespace Mapbox.Editor
{	
	using System;
	using UnityEngine;
	using Random = UnityEngine.Random;
	[Serializable]
	public class MyTreeElement : TreeElement
	{
		public string Name;
		public string Type;
		public bool isActive;

		public MyTreeElement(string name, int depth, int id) : base(name, depth, id)
		{
			isActive = true;
		}
	}
}

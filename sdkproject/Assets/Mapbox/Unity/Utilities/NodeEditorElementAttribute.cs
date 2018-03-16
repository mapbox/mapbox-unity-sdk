namespace Mapbox.Unity.Utilities
{
	using UnityEngine;
	using System.Collections;
	using System;

	public class NodeEditorElementAttribute : Attribute
	{
		public string Name;

		public NodeEditorElementAttribute(string s)
		{
			Name = s;
		}
	}
}
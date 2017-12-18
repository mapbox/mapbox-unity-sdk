namespace Mapbox.Examples
{
	using Mapbox.Unity.MeshGeneration.Interfaces;
	using System.Collections.Generic;
	using UnityEngine;

	public class LabelTextSetter : MonoBehaviour, IFeaturePropertySettable
	{
		[SerializeField]
		TextMesh _textMesh; 

		public void Set(Dictionary<string, object> props)
		{
			_textMesh.text = "";

			if (props.ContainsKey("name"))
			{
				_textMesh.text = props["name"].ToString();
			}
			else if (props.ContainsKey("house_num"))
			{
				_textMesh.text = props["house_num"].ToString();
			}
			else if (props.ContainsKey("type"))
			{
				_textMesh.text = props["type"].ToString();
			}
		}
	}
}
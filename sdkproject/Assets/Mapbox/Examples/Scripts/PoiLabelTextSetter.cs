 namespace Mapbox.Examples
{
	using Mapbox.Unity.MeshGeneration.Interfaces;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.UI;

	public class PoiLabelTextSetter : MonoBehaviour, IFeaturePropertySettable
	{
		//[SerializeField]
		//TextMesh _textMesh;
		[SerializeField]
		Text _text;
		[SerializeField]
		Image _background;

		public void Set(Dictionary<string, object> props)
		{
			//_textMesh.text = "";
			_text.text = "";

			if (props.ContainsKey("name"))
			{
				//_textMesh.text = props["name"].ToString();
				_text.text = props["name"].ToString();
			}
			else if (props.ContainsKey("house_num"))
			{
				//_textMesh.text = props["house_num"].ToString();
				_text.text = props["name"].ToString();
			}
			else if (props.ContainsKey("type"))
			{
				//_textMesh.text = props["type"].ToString();
				_text.text = props["name"].ToString();
			}
			RefreshBackground();
		}

		public void RefreshBackground()
		{
			RectTransform backgroundRect = _background.GetComponent<RectTransform>();
			LayoutRebuilder.ForceRebuildLayoutImmediate(backgroundRect);
		}
	}
}
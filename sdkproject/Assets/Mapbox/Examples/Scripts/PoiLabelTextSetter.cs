 namespace Mapbox.Examples
{
	using Mapbox.Unity.MeshGeneration.Interfaces;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.UI;

	public class PoiLabelTextSetter : MonoBehaviour, IFeaturePropertySettable
	{
		[SerializeField]
		TextMesh _textMesh;

		[SerializeField]
		Image _background;

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

			SetBackgroundWidth();
		}

		public void Start()
		{
			SetBackgroundWidth();
		}

		public void SetBackgroundWidth()
		{
			float textWidth = _textMesh.GetComponent<Renderer>().bounds.size.x;
			float textHeight = _textMesh.GetComponent<Renderer>().bounds.size.y;
			RectTransform backgroundRect = _background.GetComponent<RectTransform>();
			backgroundRect.sizeDelta = new Vector2(textWidth + 8f, textHeight + 1f);
		}
	}
}
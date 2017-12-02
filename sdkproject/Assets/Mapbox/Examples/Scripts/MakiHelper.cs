namespace Mapbox.Examples
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.UI;
	using Mapbox.Unity.MeshGeneration.Interfaces;

	public class MakiHelper : MonoBehaviour, IFeaturePropertySettable
	{
		public static RectTransform Parent;
		public static GameObject UiPrefab;

		private GameObject _uiObject;

		public void Set(Dictionary<string, object> props)
		{
			if (Parent == null)
			{
				var canv = GameObject.Find("PoiCanvas");
				var ob = new GameObject("PoiContainer");
				ob.transform.SetParent(canv.transform);
				Parent = ob.AddComponent<RectTransform>();
				UiPrefab = Resources.Load<GameObject>("MakiUiPrefab");
			}

			if (props.ContainsKey("maki"))
			{
				_uiObject = Instantiate(UiPrefab);
				_uiObject.transform.SetParent(Parent);
				_uiObject.transform.Find("Image").GetComponent<Image>().sprite = Resources.Load<Sprite>("maki/" + props["maki"].ToString() + "-15");
				if (props.ContainsKey("name"))
				{
					_uiObject.GetComponentInChildren<Text>().text = props["name"].ToString();
				}
			}
		}

		public void LateUpdate()
		{
			if (_uiObject)
				_uiObject.transform.position = Camera.main.WorldToScreenPoint(transform.position);
		}
	}
}
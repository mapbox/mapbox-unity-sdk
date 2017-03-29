using System.Collections;
using System.Collections.Generic;
using Mapbox.VectorTile;
using UnityEngine;
using UnityEngine.UI;
using Mapbox.Unity.MeshGeneration.Interfaces;

public class MakiHelper : MonoBehaviour, ILabelVisualizationHelper
{
    public static RectTransform Parent;
    public static GameObject UiPrefab;

    private GameObject _uiObject;

    public void Initialize(Dictionary<string, object> props)
    {
        if (Parent == null)
        {
            Parent = GameObject.Find("Canvas/PoiContainer").GetComponent<RectTransform>();
            UiPrefab = Resources.Load<GameObject>("MakiUiPrefab");
        }
        
        if (props.ContainsKey("maki"))
        {
            _uiObject = Instantiate(UiPrefab);
            _uiObject.transform.SetParent(Parent);
            _uiObject.transform.Find("Image").GetComponent<Image>().sprite = Resources.Load<Sprite>("maki/" + props["maki"].ToString() + "-15");
            _uiObject.GetComponentInChildren<Text>().text = props["name"].ToString();
        }
    }

    public void Update()
    {
        if(_uiObject)
            _uiObject.transform.position = Camera.main.WorldToScreenPoint(transform.position);
    }
}

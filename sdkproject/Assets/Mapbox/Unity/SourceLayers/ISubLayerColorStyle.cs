namespace Mapbox.Unity.Map
{
	using UnityEngine;

	public interface ISubLayerColorStyle : ISubLayerStyle
	{
		Color FeatureColor { get; set; }
		void SetAsStyle(Color featureColor);
	}

}



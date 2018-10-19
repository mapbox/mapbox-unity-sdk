namespace Mapbox.Unity.Map
{
	using UnityEngine;

	public class SubLayerColorStyle : ISubLayerColorStyle
	{
		private GeometryMaterialOptions _materialOptions;
		public SubLayerColorStyle(GeometryMaterialOptions materialOptions)
		{
			_materialOptions = materialOptions;
		}

		public Color FeatureColor
		{
			get
			{
				return _materialOptions.colorStyleColor;
			}

			set
			{
				if (_materialOptions.colorStyleColor != value)
				{
					_materialOptions.colorStyleColor = value;
					_materialOptions.HasChanged = true;
				}
			}
		}

		public void SetAsStyle()
		{
			SetAsStyle(Color.white);
		}

		public void SetAsStyle(Color featureColor)
		{
			_materialOptions.style = StyleTypes.Color;
			_materialOptions.colorStyleColor = featureColor;
			_materialOptions.HasChanged = true;
		}
	}

}



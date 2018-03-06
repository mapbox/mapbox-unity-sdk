namespace Mapbox.Unity.Map
{
	using System;

	[Serializable]
	public class Style
	{
		public string Name;
		public string Id;
		public string Modified;
		public string UserName;
	}

	[Serializable]
	public class LayerSourceOptions
	{
		public bool isActive;
		public Style layerSource;

		public string Id
		{
			get
			{
				return layerSource.Id;
			}
			set
			{
				layerSource.Id = value;
			}
		}
	}
}

namespace Mapbox.Unity.Map
{
	using System;

	[Serializable]
	public class UnifiedMapOptions
	{
		public MapPresetType mapPreset = MapPresetType.LocationBasedMap;
		public MapOptions mapOptions = new MapOptions();
		[NodeEditorElementAttribute("Image Layer")]
		public ImageryLayerProperties imageryLayerProperties = new ImageryLayerProperties();
		[NodeEditorElementAttribute("Terrain Layer")]
		public ElevationLayerProperties elevationLayerProperties = new ElevationLayerProperties();
		[NodeEditorElementAttribute("Vector Layer")]
		public VectorLayerProperties vectorLayerProperties = new VectorLayerProperties();
	}
}

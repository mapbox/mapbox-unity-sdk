using Mapbox.Utils;

namespace Mapbox.Unity.Map.Interfaces
{
	public interface IUnifiedMap
	{
		void UpdateMap(Vector2d latLon, float zoom);
		void ResetMap();
	}
}

namespace Mapbox.Unity.Map
{
	using System.IO;

	public class StreetsV7TileStats
	{
		private static StreetsV7TileStats  _instance;
		private string _filePath = "../Editor/data.json";
		public static StreetsV7TileStats Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new StreetsV7TileStats();
				}
				return _instance;
			}
		}

		public string GetJson()
		{
			StreamReader reader = new StreamReader(_filePath);
			reader.Close();
			return reader.ReadToEnd();
		}
	}
}

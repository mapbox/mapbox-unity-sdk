namespace Mapbox.Unity.Utilities.DebugTools
{
	using UnityEngine;
    using System.Collections.Generic;

	public class ScenesList : ScriptableObject
	{
        public SceneData[] SceneList;
	}

    public class SceneData : ScriptableObject
    {
        public string Name;
        public string ScenePath;
        public Texture2D Image;
        public TextAsset Text;
    }


}
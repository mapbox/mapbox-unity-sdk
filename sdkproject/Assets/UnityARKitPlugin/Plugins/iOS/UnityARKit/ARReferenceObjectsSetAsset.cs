using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;
using System.IO;

[CreateAssetMenu(fileName = "ARReferenceObjectsSetAsset" , menuName = "UnityARKitPlugin/ARReferenceObjectsSetAsset", order = 4)]
public class ARReferenceObjectsSetAsset : ScriptableObject {

	public string resourceGroupName;
	public ARReferenceObjectAsset [] referenceObjectAssets;

	public List<ARReferenceObject> LoadReferenceObjectsInSet()
	{
		List<ARReferenceObject> listRefObjects = new List<ARReferenceObject> ();

		if (UnityARSessionNativeInterface.IsARKit_2_0_Supported() == false)
		{
			return listRefObjects;
		}

		string folderPath = Application.streamingAssetsPath + "/ARReferenceObjects/" + resourceGroupName + ".arresourcegroup";
		string contentsJsonPath = Path.Combine(folderPath, "Contents.json");

		ARResourceGroupContents resGroupContents = JsonUtility.FromJson<ARResourceGroupContents>(File.ReadAllText (contentsJsonPath));

		foreach (ARResourceGroupResource arrgr in resGroupContents.resources) 
		{
			string objectFolderPath = Path.Combine(folderPath, arrgr.filename);
			string objJsonPath = Path.Combine (objectFolderPath, "Contents.json");
			ARReferenceObjectResourceContents resourceContents = JsonUtility.FromJson<ARReferenceObjectResourceContents> (File.ReadAllText (objJsonPath));
			string fileToLoad = Path.Combine (objectFolderPath, resourceContents.objects [0].filename);
			ARReferenceObject arro =  ARReferenceObject.Load(fileToLoad);
			arro.name = resourceContents.referenceObjectName;
			listRefObjects.Add (arro);
		}

		return listRefObjects;
	}

}
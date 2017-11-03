namespace Mapbox.Unity.MeshGeneration.Components
{
	using UnityEngine;
	using System.Linq;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;

	public class FeatureBehaviour : MonoBehaviour
	{
		public VectorEntity VectorEntity;
		public Transform Transform;
		public VectorFeatureUnity Data;

		public void ShowDebugData()
		{
			DataString = string.Join("\r\n", Data.Properties.Select(x => x.Key + " - " + x.Value.ToString()).ToArray());
		}

		public void ShowDataPoints()
		{
			foreach (var item in VectorEntity.Feature.Points)
			{
				for (int i = 0; i < item.Count; i++)
				{
					var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
					go.name = i.ToString();
					go.transform.SetParent(transform, false);
					go.transform.localPosition = item[i];
				}
			}
		}

		internal void Initialize(VectorEntity ve)
		{
			VectorEntity = ve;
			Transform = transform;
			Data = ve.Feature;
		}

		[Multiline(5)]
		public string DataString;

		public void Init(VectorFeatureUnity feature)
		{
			Transform = transform;
			Data = feature;
		}
	}
}
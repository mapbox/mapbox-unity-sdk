namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Components;
	using Mapbox.Unity.MeshGeneration.Interfaces;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Prefab Modifier")]
	public class PrefabModifier : GameObjectModifier
	{
		[SerializeField]
		private GameObject _prefab;

		[SerializeField]
		private bool _scaleDownWithWorld = false;

		public override void Run(VectorEntity ve, UnityTile tile)
		{
			int selpos = ve.Feature.Points[0].Count / 2;
			var met = ve.Feature.Points[0][selpos];

			IFeaturePropertySettable settable = null;
			GameObject go;
			if (ve.GameObject.transform.childCount > 0)
			{
				settable = ve.GameObject.GetComponentInChildren<IFeaturePropertySettable>();
				if (settable != null)
				{
					go = (settable as MonoBehaviour).gameObject;
					go.name = ve.Feature.Data.Id.ToString();
					go.transform.localPosition = met;
					go.transform.localScale = Constants.Math.Vector3One;
					go.GetComponent<FeatureBehaviour>().Init(ve.Feature);
					settable.Set(ve.Feature.Properties);
					if (!_scaleDownWithWorld)
					{
						go.transform.localScale = Vector3.one / tile.TileScale;
					}
					return;
				}
			}

			go = Instantiate(_prefab);
			go.name = ve.Feature.Data.Id.ToString();
			go.transform.position = met;
			go.transform.SetParent(ve.GameObject.transform, false);
			go.transform.localScale = Constants.Math.Vector3One;

			var bd = go.AddComponent<FeatureBehaviour>();
			bd.Init(ve.Feature);

			settable = go.GetComponent<IFeaturePropertySettable>();
			if (settable != null)
			{
				settable.Set(ve.Feature.Properties);
			}

			if (!_scaleDownWithWorld)
			{
				go.transform.localScale = Vector3.one / tile.TileScale;
			}

		}
	}
}

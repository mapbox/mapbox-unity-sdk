namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Components;
	using Mapbox.Unity.MeshGeneration.Interfaces;
	using System.Collections.Generic;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Prefab Modifier")]
	public class PrefabModifier : GameObjectModifier
	{
		[SerializeField]
		private GameObject _prefab;

		[SerializeField]
		private bool _scaleDownWithWorld = false;

		private Dictionary<GameObject, GameObject> _objects;

		public override void Initialize()
		{
			if (_objects == null)
			{
				_objects = new Dictionary<GameObject, GameObject>();
			}
		}

		public override void Run(VectorEntity ve, UnityTile tile)
		{
			int selpos = ve.Feature.Points[0].Count / 2;
			var met = ve.Feature.Points[0][selpos];

			IFeaturePropertySettable settable = null;
			GameObject go;

			if (_objects.ContainsKey(ve.GameObject))
			{
				go = _objects[ve.GameObject];
				settable = go.GetComponent<IFeaturePropertySettable>();
				if (settable != null)
				{
					go = (settable as MonoBehaviour).gameObject;
					settable.Set(ve.Feature.Properties);
				}
				// set gameObject transform
				go.name = ve.Feature.Data.Id.ToString();
				go.transform.localPosition = met;
				go.transform.localScale = Constants.Math.Vector3One;

				if (!_scaleDownWithWorld)
				{
					go.transform.localScale = Vector3.one / tile.TileScale;
				}
				return;
			}
			else
			{
				go = Instantiate(_prefab);
				_objects.Add(ve.GameObject, go);
			}

			go.name = ve.Feature.Data.Id.ToString();
			go.transform.position = met;
			go.transform.SetParent(ve.GameObject.transform, false);
			go.transform.localScale = Constants.Math.Vector3One;

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

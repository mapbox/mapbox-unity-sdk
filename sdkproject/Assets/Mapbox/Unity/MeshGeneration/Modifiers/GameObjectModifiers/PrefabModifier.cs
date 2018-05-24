namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Components;
	using Mapbox.Unity.MeshGeneration.Interfaces;
	using System.Collections.Generic;
	using Mapbox.Unity.Map;
	using System;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Prefab Modifier")]
	public class PrefabModifier : GameObjectModifier
	{
		private Dictionary<GameObject, GameObject> _objects;
		[SerializeField]
		private SpawnPrefabOptions _options;
		private List<GameObject> _prefabList = new List<GameObject>();

		public override void Initialize()
		{
			if (_objects == null)
			{
				_objects = new Dictionary<GameObject, GameObject>();
			}
		}

		public override void SetProperties(ModifierProperties properties)
		{
			_options = (SpawnPrefabOptions)properties;
		}

		public override void Run(VectorEntity ve, UnityTile tile)
		{
			int selpos = ve.Feature.Points[0].Count / 2;
			var met = ve.Feature.Points[0][selpos];
			RectTransform goRectTransform;
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
				goRectTransform = go.GetComponent<RectTransform>();
				if (goRectTransform == null)
				{
					go.transform.localPosition = met;
				}
				else
				{
					goRectTransform.anchoredPosition3D = met;
				}
				//go.transform.localScale = Constants.Math.Vector3One;

				if (_options.scaleDownWithWorld)
				{
					go.transform.localScale = (go.transform.localScale * (tile.TileScale));
				}
				return;
			}
			else
			{
				go = Instantiate(_options.prefab);
				_prefabList.Add(go);
				_objects.Add(ve.GameObject, go);
			}

			go.name = ve.Feature.Data.Id.ToString();

			goRectTransform = go.GetComponent<RectTransform>();
			if (goRectTransform == null)
			{
				go.transform.localPosition = met;
			}
			else
			{
				goRectTransform.anchoredPosition3D = met;
			}
			//go.transform.localPosition = met;
			go.transform.SetParent(ve.GameObject.transform, false);
			//go.transform.localScale = Constants.Math.Vector3One;

			settable = go.GetComponent<IFeaturePropertySettable>();
			if (settable != null)
			{
				settable.Set(ve.Feature.Properties);
			}

			if (_options.scaleDownWithWorld)
			{
				go.transform.localScale = (go.transform.localScale * (tile.TileScale));
			}

			if (_options.AllPrefabsInstatiated != null)
			{
				_options.AllPrefabsInstatiated(_prefabList);
			}
		}

		public List<GameObject> returnInstanceList()
		{
			return _prefabList;
		}
	}
}
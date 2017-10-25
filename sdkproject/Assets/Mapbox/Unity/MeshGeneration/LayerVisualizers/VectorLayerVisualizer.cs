namespace Mapbox.Unity.MeshGeneration.Interfaces
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Mapbox.VectorTile;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Filters;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.Unity.Utilities;
	using System.Collections;

	[Serializable]
	public class TypeVisualizerTuple
	{
		public string Type;
		[SerializeField]
		public ModifierStackBase Stack;
	}


	/// <summary>
	/// VectorLayerVisualizer is a specialized layer visualizer working on polygon and line based vector data (i.e. building, road, landuse) using modifier stacks.
	/// Each feature is preprocessed and passed down to a modifier stack, which will create and return a game object for that given feature.
	/// Key is the name of the layer to be processed.
	/// Classification Key is the property name to be used for stack selection.
	/// It also supports filters; objects that goes over features and decides if it'll be visualized or not.
	/// Default Stack is the stack that'll be used for any feature that passes the filters but isn't matched to any special stack.
	/// 
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Layer Visualizer/Vector Layer Visualizer")]
	public class VectorLayerVisualizer : LayerVisualizerBase
	{
		[SerializeField]
		private string _classificationKey;
		[SerializeField]
		private string _key;
		public override string Key
		{
			get { return _key; }
			set { _key = value; }
		}

		[SerializeField]
		private List<FilterBase> Filters;

		[SerializeField]
		[NodeEditorElementAttribute("Default Stack")]
		public ModifierStackBase _defaultStack;
		[SerializeField]
		[NodeEditorElementAttribute("Custom Stacks")]
		public List<TypeVisualizerTuple> Stacks;

		[NonSerialized]
		private Dictionary<UnityTile, List<int>> _activeCoroutines;

		[NonSerialized]
		private int _entityPerCoroutine = 20;
		[NonSerialized]
		private int _entityInCurrentCoroutine = 0;

		public override void Initialize()
		{
			base.Initialize();
			_entityInCurrentCoroutine = 0;
			_activeCoroutines = new Dictionary<UnityTile, List<int>>();

			foreach (var filter in Filters)
			{
				if (filter != null)
				{
					filter.Initialize();
				}
			}
			foreach (var item in Stacks)
			{
				if (item != null && item.Stack != null)
				{
					item.Stack.Initialize();
				}
			}
			if (_defaultStack != null)
			{
				_defaultStack.Initialize();
			}

			foreach (var item in Stacks)
			{
				if (item != null && item.Stack != null)
				{
					item.Stack.Initialize();
				}
			}
		}

		public override void Create(VectorTileLayer layer, UnityTile tile, Action callback)
		{
			if (!_activeCoroutines.ContainsKey(tile))
				_activeCoroutines.Add(tile, new List<int>());
			_activeCoroutines[tile].Add(Runnable.Run(ProcessLayer(layer, tile, callback)));
		}

		private IEnumerator ProcessLayer(VectorTileLayer layer, UnityTile tile, Action callback = null)
		{
			
			//testing each feature with filters
			var fc = layer.FeatureCount();
			
			//HACK to prevent request finishing on same frame which breaks modules started/finished events 
			if(fc <= _entityPerCoroutine)
				yield return null;

			var filterOut = false;
			for (int i = 0; i < fc; i++)
			{
				filterOut = false;
				var feature = new VectorFeatureUnity(layer.GetFeature(i, 0), tile, layer.Extent);
				foreach (var filter in Filters)
				{
					if (!string.IsNullOrEmpty(filter.Key) && !feature.Properties.ContainsKey(filter.Key))
						continue;

					if (!filter.Try(feature))
					{
						filterOut = true;
						break;
					}
				}

				if (!filterOut)
				{
					if (tile.VectorDataState != Enums.TilePropertyState.Cancelled)
						Build(feature, tile, tile.gameObject);
				}

				_entityInCurrentCoroutine++;

				if (_entityInCurrentCoroutine >= _entityPerCoroutine)
				{
					_entityInCurrentCoroutine = 0;
					yield return null;
				}
			}

			var mergedStack = _defaultStack as MergedModifierStack;
			if (mergedStack != null)
			{
				mergedStack.End(tile, tile.gameObject, layer.Name);
			}

			foreach (var item in Stacks)
			{
				mergedStack = item.Stack as MergedModifierStack;
				if (mergedStack != null)
				{
					mergedStack.End(tile, tile.gameObject, layer.Name);
				}
			}

			if (callback != null)
				callback();
		}

		/// <summary>
		/// Preprocess features, finds the relevant modifier stack and passes the feature to that stack
		/// </summary>
		/// <param name="feature"></param>
		/// <param name="tile"></param>
		/// <param name="parent"></param>
		private bool IsFeatureValid(VectorFeatureUnity feature)
		{
			if (feature.Properties.ContainsKey("extrude") && !bool.Parse(feature.Properties["extrude"].ToString()))
				return false;

			if (feature.Points.Count < 1)
				return false;

			return true;
		}

		private void Build(VectorFeatureUnity feature, UnityTile tile, GameObject parent)
		{
			if (!IsFeatureValid(feature))
				return;

			//this will be improved in next version and will probably be replaced by filters
			var styleSelectorKey = FindSelectorKey(feature);

			var meshData = new MeshData();
			meshData.TileRect = tile.Rect;

			//and finally, running the modifier stack on the feature
			var mod = Stacks.FirstOrDefault(x => x.Type.Contains(styleSelectorKey));
			if (mod != null)
			{
				mod.Stack.Execute(tile, feature, meshData, parent, mod.Type);
			}
			else
			{
				if (_defaultStack != null)
				{
					_defaultStack.Execute(tile, feature, meshData, parent, _key);
				}
			}
		}

		private string FindSelectorKey(VectorFeatureUnity feature)
		{
			if (string.IsNullOrEmpty(_classificationKey))
			{
				if (feature.Properties.ContainsKey("type"))
				{
					return feature.Properties["type"].ToString().ToLowerInvariant();
				}
				else if (feature.Properties.ContainsKey("class"))
				{
					return feature.Properties["class"].ToString().ToLowerInvariant();
				}
			}
			else if (feature.Properties.ContainsKey(_classificationKey))
			{
				if (feature.Properties.ContainsKey(_classificationKey))
				{
					return feature.Properties[_classificationKey].ToString().ToLowerInvariant();
				}
			}

			return "";
		}

		/// <summary>
		/// Handle tile destruction event and propagate it to modifier stacks
		/// </summary>
		/// <param name="tile">Destroyed tile object</param>
		public override void OnUnregisterTile(UnityTile tile)
		{
			base.OnUnregisterTile(tile);
			tile.VectorDataState = Enums.TilePropertyState.Cancelled;
			if (_activeCoroutines.ContainsKey(tile))
			{
				foreach (var cor in _activeCoroutines[tile])
				{
					Runnable.Stop(cor);
				}
			}

			if (_defaultStack != null)
				_defaultStack.UnregisterTile(tile);
			foreach (var val in Stacks)
			{
				if (val != null && val.Stack != null)
					val.Stack.UnregisterTile(tile);
			}
		}
	}
}

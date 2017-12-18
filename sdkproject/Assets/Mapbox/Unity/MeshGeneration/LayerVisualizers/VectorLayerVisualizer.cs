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
		public string[] Types;
		[SerializeField]
		public ModifierStackBase Stack;
	}


	/// <summary>
	/// Layer Visualizer / Vector Layer Visualizer
	/// Layer visualizers corresponds to 'vector layer' in vector data.So it receives layer data, which contains bunch of 
	/// features inside. But layer visualizer doesn't do the actual visualization itself either, and uses objects called 
	/// ModifierStacks to do it.Reason for this is that it's very likely that you'll need some sort of customization and/or 
	/// categorization at this point, like coloring buildings different by their type or showing only major road segments.
	/// To achieve this categorization, we have two properties; a default stack and a stack dictionary.
	/// Default Stack is like a fallback stack.If there isn't any custom stack found for a particular feature, it's passed 
	/// to default stack.If default stack is null, it isn't visualized at all.
	/// Stack dictionary is a dictionary of string key and modifier stack value. String key is the value of a property 
	/// inside the feature and this 'property' is the 'type' by default, so i.e. using key 'school' means features with 
	/// 'type == school', or 'park' means features with 'type==park'. But school is a type of building and park is a type of landuse, at this point, we assume you know the properties and possible values of them in the vector data.You can easily check them here; https://www.mapbox.com/vector-tiles/mapbox-streets-v7/#overview
	/// As mentioned, default property for this is 'type' but that doesn't work for all layers/features.For example, 
	/// road features use class property more than type, or traffic data has congestion property which you probably will want to use.
	/// For these cases, Vector Layer Visualizer also has a field called 'classification key'. It looks like an empty field 
	/// in the inspector for now(we'll improve the ui on that) but if you set a value in there, like 'class' or 'congestion', 
	/// it'll override the default property name 'type' and will be used for finding the correct modifier stack.
	/// You can check Drive demo and traffic layer to see how this works.
	/// So to wrap it up, for each feature in the layer, we first check if there's a custom modifier stack defined for that 
	/// type of feature in the stack dictionary.If we find one, we pass feature to that stack. If not, we check the default
	/// dictionary, if it's set, we pass feature to that, if not we skip the feature and not visualize it.
	/// We also have filters in vector layer visualizer.Filters are simple classes which decides if a particular feature 
	/// will be visualized or not and returns a boolean. It runs before stacks so features decided not to be visualized is skipped right away.Using filters, you can do things like; 'not visualize schools' or 'not (or only) visualize buildings taller than 50meters'.
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

		private Dictionary<UnityTile, List<int>> _activeCoroutines;
		[SerializeField]
		private bool _enableCoroutines = false;
		[SerializeField]
		private int _entityPerCoroutine = 20;
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

			if (_defaultStack != null)
			{
				_defaultStack.Initialize();
			}

			foreach (var item in Stacks)
			{
				if (item != null && item.Stack != null)
				{
					item.Types = item.Type.Split(',');
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
			//HACK to prevent request finishing on same frame which breaks modules started/finished events 
			yield return null;

			if (tile == null)
			{
				yield break;
			}

			//testing each feature with filters
			var fc = layer.FeatureCount();
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
					if (tile != null && tile.gameObject != null && tile.VectorDataState != Enums.TilePropertyState.Cancelled)
						Build(feature, tile, tile.gameObject);
				}

				_entityInCurrentCoroutine++;

				if (_enableCoroutines && _entityInCurrentCoroutine >= _entityPerCoroutine)
				{
					_entityInCurrentCoroutine = 0;
					yield return null;
				}
			}

			var mergedStack = _defaultStack as MergedModifierStack;
			if (mergedStack != null && tile != null)
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
			if (feature.Properties.ContainsKey("extrude") && !Convert.ToBoolean(feature.Properties["extrude"]))
				return;

			if (feature.Points.Count < 1)
				return;

			//this will be improved in next version and will probably be replaced by filters
			var styleSelectorKey = FindSelectorKey(feature);

			var meshData = new MeshData();
			meshData.TileRect = tile.Rect;

			//and finally, running the modifier stack on the feature
			var processed = false;
			for (int i = 0; i < Stacks.Count; i++)
			{
				foreach (var key in Stacks[i].Types)
				{
					if (key == styleSelectorKey)
					{
						processed = true;
						Stacks[i].Stack.Execute(tile, feature, meshData, parent, styleSelectorKey);
						break;
					}
				}

				if (processed)
					break;
			}
			if (!processed)
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

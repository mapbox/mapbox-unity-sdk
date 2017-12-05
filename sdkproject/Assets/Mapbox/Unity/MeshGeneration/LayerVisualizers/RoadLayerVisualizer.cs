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
	
	public class Junction
	{
		public Vector3 Point;
		public List<Road> Roads;

		public Junction()
		{
			Roads = new List<Road>();
		}
	}

	public class Road
	{
		public List<Junction> Junctions;

		public Road()
		{
			Junctions = new List<Junction>();
		}
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
	[CreateAssetMenu(menuName = "Mapbox/Layer Visualizer/Road Layer Visualizer")]
	public class RoadLayerVisualizer : LayerVisualizerBase
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

		public List<Junction> Junctions;
		public List<Road> Roads;

		public override void Initialize()
		{
			base.Initialize();
			Junctions = new List<Junction>();
			Roads = new List<Road>();
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

				foreach (var item in feature.Points)
				{
					for (int j = 0; j < item.Count - 1; j++)
					{
						var j1 = GetJunction(item[j]);
						var j2 = GetJunction(item[j + 1]);
						var r = new Road();
						j1.Roads.Add(r);
						j2.Roads.Add(r);

						r.Junctions.Add(j1);
						r.Junctions.Add(j2);
					}
				}
				
			}

			foreach (var item in Junctions.Where(x => x.Roads.Count > 2))
			{
				var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				go.transform.SetParent(tile.transform, false);
				go.transform.position = tile.transform.position + item.Point;
			}

			if (callback != null)
				callback();
		}

		public Junction GetJunction(Vector3 pos)
		{
			foreach (var item in Junctions)
			{
				if (Vector3.Distance(pos, item.Point) < .1f)
					return item;
			}

			var j = new Junction() { Point = pos };
			Junctions.Add(j);
			return j;
		}

		/// <summary>
		/// Preprocess features, finds the relevant modifier stack and passes the feature to that stack
		/// </summary>
		/// <param name="feature"></param>
		/// <param name="tile"></param>
		/// <param name="parent"></param>
		
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

namespace Mapbox.Unity.Ar
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.Location;
	using System;
	using Mapbox.MapMatching;
	using Mapbox.Utils;
	//using System.Threading.Tasks;
	using UnityARInterface;
	using Mapbox.Unity.Map;

	public class CentralizedARLocator : MonoBehaviour, ISynchronizationContext
	{
		// TODO : Snap should happening here for things to happen...
		// Lol. Snap Snap Snap... after yeach new better GPS val...
		[SerializeField]
		AbstractMap _map;
		public AbstractMap CurrentMap
		{
			get
			{
				return _map;
			}
		}

		ARInterface _arInterface;

		[SerializeField]
		ARMapMatching _mapMathching;

		[SerializeField]
		bool _useSnapping;

		[SerializeField]
		float _desiredStartingAccuracy = 5f;

		[SerializeField]
		int _amountOfNodesToCheck;

		[SerializeField]
		int _desiredAccuracy;

		[SerializeField]
		NodeSyncBase[] _syncNodes;
		public NodeSyncBase[] SyncNodes
		{
			get
			{
				return _syncNodes;
			}
		}

		[SerializeField]
		Transform _player;

		[SerializeField]
		ComputeARLocalizationStrategy _initialLocalizationStrategy;

		[SerializeField]
		ComputeARLocalizationStrategy _generalLocalizationStrategy;

		[SerializeField]
		AbstractAlignmentStrategy _alignmentStrategy;

		Location _highestLocation;


		public static Action<Location> OnNewHighestAccuracyGPS;

		//ARInterface.CustomTrackingState _trackingState;

		public event Action<Alignment> OnAlignmentAvailable;

		void Awake()
		{
			_alignmentStrategy.Register(this);
			_map = LocationProviderFactory.Instance.mapManager;

			// Initialize all sync-nodes.Make them ready to recieve node data.
			// Map needs to be generated before init. Otherwise bunch of errors.

			InitializeSyncNodes();

			_map.OnInitialized += Map_OnInitialized;
		}

		void Map_OnInitialized()
		{
			_map.OnInitialized -= Map_OnInitialized;

			// We don't want location updates until we have a map, otherwise our conversion will fail.
			ComputeFirstLocalization();
		}

		/// <summary>
		/// Computes the first localization.
		/// </summary>
		protected void ComputeFirstLocalization()
		{
			_initialLocalizationStrategy.OnLocalizationComplete += OnFirstLocalizationComplete;
			_initialLocalizationStrategy.ComputeLocalization(this);
		}
		/// <summary>
		/// Delegate that gets triggered when first localization computation is complete
		/// </summary>
		/// <param name="alignment">Alignment from the first localization.</param>
		void OnFirstLocalizationComplete(Alignment alignment)
		{
			_initialLocalizationStrategy.OnLocalizationComplete -= OnFirstLocalizationComplete;
			// Localization is complete. Now call AlignmentStrategy to align the map
			OnAlignmentAvailable(alignment);

			//We want Syncronize to be called when location is updated. This could extend to any other polling methods in the future.
			LocationProviderFactory.Instance.DefaultLocationProvider.OnLocationUpdated += SyncronizeNodesToFindAlignment;

			//Save new nodes for each type of sync node.
			SaveNodes();
		}

		/// <summary>
		/// Computes the general localization. Localization strategy to be used after first localization.
		/// </summary>
		void ComputeGeneralLocalization()
		{
			_generalLocalizationStrategy.OnLocalizationComplete += OnGeneralLocalizationComplete;
			_generalLocalizationStrategy.ComputeLocalization(this);
		}
		/// <summary>
		/// Delegate that gets triggered when the general localization is complete.
		/// </summary>
		/// <param name="alignment">Alignment.</param>
		void OnGeneralLocalizationComplete(Alignment alignment)
		{
			_initialLocalizationStrategy.OnLocalizationComplete -= OnGeneralLocalizationComplete;
			// Localization is complete. Now call AlignmentStrategy to align the map
			OnAlignmentAvailable(alignment);
		}

		/// <summary>
		/// Syncronizes the nodes to find alignment.
		/// </summary>
		/// <param name="location">Location.</param>
		protected void SyncronizeNodesToFindAlignment(Location location)
		{
			// Our location provider just got a new update.
			// We now ask all our nodes to update and save the most recent node.
			// Sync Nodes should also update a "Quality/Accuracy" metric.
			// Quality/Accuracy metric will be used to determine whether the node will be considered for the alignment computation or not.
			Debug.Log("SyncronizeNodesToFindAlignment");

			foreach (var node in _syncNodes)
			{
				node.SaveNode();
			}
			// Compute Alignment
			ComputeGeneralLocalization();
		}

		/// <summary>
		/// Initializes the sync nodes.
		/// </summary>
		protected void InitializeSyncNodes()
		{
			for (int i = 0; i < _syncNodes.Length; i++)
			{
				_syncNodes[i].InitializeNodeBase();
			}
		}

		/// <summary>
		/// Saves the nodes.
		/// </summary>
		protected void SaveNodes()
		{
			foreach (var node in _syncNodes)
			{
				node.SaveNode();
			}
		}



		//void CheckTracking()
		//{
		//	var tracking = new ARInterface.CustomTrackingState();
		//	if (_arInterface.GetTrackingState(ref tracking))
		//	{
		//		if (tracking == ARInterface.CustomTrackingState.Good)
		//		{
		//			// Blah blah..
		//		}
		//	}
		//}

		void SaveHighestAccuracy(Location location)
		{
			if (location.Accuracy <= _desiredStartingAccuracy)
			{
				_highestLocation = location;
				_desiredStartingAccuracy = location.Accuracy;

				if (OnNewHighestAccuracyGPS != null)
				{
					OnNewHighestAccuracyGPS(location);
				}

				// TODO:
				// And snap player to there...
			}
		}

		//async void FindBestNodes()
		//{

		//	while (true)
		//	{
		//		foreach (var nodeSync in _nodeSyncs)
		//		{
		//			if (nodeSync.ReturnNodes().Length >= _amountOfNodesToCheck)
		//			{
		//				var average = CheckAverageAccuracy(nodeSync, _amountOfNodesToCheck);

		//				if (average <= _desiredAccuracy)
		//				{
		//					_mapMathching.MapMatchQuery(nodeSync.ReturnNodes());
		//				}
		//			}
		//		}

		//		await Task.Delay(TimeSpan.FromSeconds(10));
		//	}
		//}

		float CheckAverageAccuracy(NodeSyncBase syncBase, int howManyNodes)
		{
			var nodes = syncBase.ReturnNodes();
			float accuracy = 0;

			for (int i = 1; i < howManyNodes; i++)
			{
				accuracy += nodes[nodes.Length - i].Accuracy;
			}

			var average = accuracy / howManyNodes;
			return average;
		}

		void GetMapMatchingCoords(Node[] nodes)
		{
			foreach (var node in nodes)
			{
				Debug.Log("Check lat lon coords: " + node.LatLon);
			}
		}

		//private void Update()
		//{
		//	if (Input.GetKeyDown(KeyCode.Space))
		//	{
		//		_mapMathching.MapMatchQuery(_syncNodes[0].ReturnNodes());
		//	}
		//}

		void SnapMapToNode(Node node)
		{

		}

		// TODO: Check trackingQuality in AR
		// and snap to GPS nodes if tracking goes bad..
	}
}

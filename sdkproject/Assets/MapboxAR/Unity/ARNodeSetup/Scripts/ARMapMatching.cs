namespace Mapbox.Unity.Ar
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.MapMatching;
	using Mapbox.Utils;
	using Mapbox.Platform;
	using Mapbox.Unity.Map;
	using System.Linq;

	public class ARMapMatching : NodeSyncBase
	{
		[SerializeField]
		MapMatching.Profile _profile;

		[SerializeField]
		AbstractMap _map;

		public Action<Node[]> ReturnMapMatchCoords;
		private List<Node> _savedNodes;
		IEnumerator _mapMatching, _waitForRequest;
		WaitForSeconds _waitFor;

		private List<Node> _nodesForMapMatchingQuery = new List<Node>();

		public void MapMatchQuery(Node[] nodes)
		{
			_nodesForMapMatchingQuery.Clear();
			_nodesForMapMatchingQuery.AddRange(nodes);

		}

		void SimpleQuery(Vector2d[] coords)
		{
			MapMatchingResource resource = new MapMatchingResource();
			resource.Coordinates = coords;
			resource.Profile = _profile;
			MapMatcher matcher = MapboxAccess.Instance.MapMatcher;

			matcher.Match(
				resource,
				(MapMatchingResponse responce) =>
			 {
				 SendResponseCoords(responce);
			 }
			);
		}

		void SendResponseCoords(MapMatchingResponse response)
		{
			var coordinates = response.Matchings[0].Geometry;
			var quality = response.Matchings[0].Confidence;
			var nodes = new List<Node>();

			if (ReturnMapMatchCoords != null)
			{
				for (int i = 0; i < coordinates.Count; i++)
				{
					nodes.Add(new Node
					{
						Confidence = quality,
						LatLon = coordinates[i]
					});
				}

				ReturnMapMatchCoords(nodes.ToArray());
				_savedNodes = nodes;

				if (NodeAdded != null)
				{
					NodeAdded();
				}
			}

			if (NodeAdded != null)
			{
				NodeAdded();
			}
		}

		public override Node[] ReturnNodes()
		{
			return _savedNodes.ToArray();
		}

		public override Node ReturnLatestNode()
		{
			return _savedNodes[_savedNodes.Count - 1];
		}

		public override void InitializeNodeBase()
		{
			_savedNodes = new List<Node>();
			_nodesForMapMatchingQuery = new List<Node>();
		}

		public override void SaveNode()
		{
			var coordinates = _nodesForMapMatchingQuery.Select(t => t.LatLon);
			SimpleQuery(coordinates.ToArray());

		}
	}
}
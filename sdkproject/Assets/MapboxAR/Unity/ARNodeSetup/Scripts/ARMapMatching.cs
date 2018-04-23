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

	public class ARMapMatching : NodeSyncBase
	{
		[SerializeField]
		MapMatching.Profile _profile;

		[SerializeField]
		AbstractMap _map;

		public Action<Node[]> ReturnMapMatchCoords;
		Node[] _savedNodes;
		IEnumerator _mapMatching, _waitForRequest;
		WaitForSeconds _waitFor;

		public void MapMatchQuery(Node[] nodes)
		{

			Vector2d[] coordinates = new Vector2d[nodes.Length];

			for (int i = 0; i < nodes.Length; i++)
			{
				coordinates[i] = nodes[i].LatLon;
			}

			SimpleQuery(coordinates);
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
			var nodes = new Node[coordinates.Count];

			if (ReturnMapMatchCoords != null)
			{
				for (int i = 0; i < coordinates.Count; i++)
				{
					nodes[i].Confidence = quality;
					nodes[i].LatLon = coordinates[i];
				}

				ReturnMapMatchCoords(nodes);
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
			return _savedNodes;
		}

		public override Node ReturnLatestNode()
		{
			return _savedNodes[_savedNodes.Length - 1];
		}
	}
}
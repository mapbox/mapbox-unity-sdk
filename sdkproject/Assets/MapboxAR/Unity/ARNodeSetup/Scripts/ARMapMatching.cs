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
		NodeSyncBase _inputNodes;

		[SerializeField]
		float _updateInterval = 10f;

		float _elapsedTime;

		public Action<Node[]> ReturnMapMatchCoords;
		private List<Node> _savedNodes;

		private List<Node> _nodesForMapMatchingQuery = new List<Node>();

		protected void Update()
		{
			_elapsedTime += Time.deltaTime;
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
			if (response == null)
			{
				Debug.Log("Empty Response");
				return;
			}
			if (response.HasMatchingError)
			{
				Debug.Log("MapMatching error : " + response.Message);
				return;
			}

			Debug.Log("Before if");
			if (response.Matchings != null && response.Matchings.Length > 0)
			{
				Debug.Log("Here");
				var coordinates = response.Matchings[0].Geometry;
				var quality = response.Matchings[0].Confidence;
				var nodes = new List<Node>();


				for (int i = 0; i < coordinates.Count; i++)
				{
					nodes.Add(new Node
					{
						Confidence = quality,
						LatLon = coordinates[i]
					});
				}
				_savedNodes = nodes;

				if (ReturnMapMatchCoords != null)
				{
					ReturnMapMatchCoords(nodes.ToArray());
				}
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

		public override void InitializeNodeBase(AbstractMap map)
		{
			_savedNodes = new List<Node>();
			_nodesForMapMatchingQuery = new List<Node>();
			IsNodeBaseInitialized = true;
		}

		public override void SaveNode()
		{
			_nodesForMapMatchingQuery = _inputNodes.ReturnNodes().ToList();
			if (_nodesForMapMatchingQuery.Count > 2 && _elapsedTime > _updateInterval)
			{
				_elapsedTime = 0;
				var coordinates = _nodesForMapMatchingQuery.Select(t => t.LatLon);
				SimpleQuery(coordinates.ToArray());
			}
		}
	}
}
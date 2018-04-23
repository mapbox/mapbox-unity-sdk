namespace Mapbox.Unity.Ar
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Utils;

	public abstract class NodeSyncBase : MonoBehaviour
	{
		/// <summary>
		/// Returns the nodes that the sync base has collected.
		/// </summary>
		/// <returns>The nodes.</returns>
		public abstract Node[] ReturnNodes();
		/// <summary>
		/// Returns the latest node added to sync base.
		/// </summary>
		/// <returns>The latest node.</returns>
		public abstract Node ReturnLatestNode();
		/// <summary>
		/// An event that is called when a node is added.
		/// </summary>
		public Action NodeAdded;
	}

	public struct Node
	{
		/// <summary>
		/// Represents the saved Latitude Longitude value of the Node.
		/// </summary>
		public Vector2d LatLon;
		/// <summary>
		/// Accuracy of the Node. ARNodes accuracy is determined by the latest and most accurate GPS point.
		/// </summary>
		public int Accuracy;
		/// <summary>
		/// Represents the Confidence of a Map Matching node. Not valid on ARNode or GPSNode.
		/// </summary>
		public float Confidence;
	}


}

using System;
using UnityEditor;
using UnityEngine;

namespace Mapbox.Editor.NodeEditor
{
	public class Connection
	{
		public ConnectionPoint inPoint;
		public ConnectionPoint outPoint;
		public Action<Connection> OnClickRemoveConnection;

		public Connection(ConnectionPoint inPoint, ConnectionPoint outPoint)
		{
			this.inPoint = inPoint;
			this.outPoint = outPoint;
		}

		public void Draw()
		{
			Handles.DrawBezier(
				inPoint.left,
				outPoint.rect.center,
				inPoint.left + Vector2.left * 50f,
				outPoint.rect.center - Vector2.left * 50f,
				Color.white,
				null,
				2f
			);

			//if (Handles.Button((inPoint.rect.center + outPoint.rect.center) * 0.5f, Quaternion.identity, 4, 8, Handles.RectangleCap))
			//{

			//}
		}
	}
}
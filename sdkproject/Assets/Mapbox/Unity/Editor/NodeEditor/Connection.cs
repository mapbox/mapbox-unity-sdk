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
		private Vector2 _outVector;

		public Connection(ConnectionPoint inPoint, ConnectionPoint outPoint)
		{
			this.inPoint = inPoint;
			this.outPoint = outPoint;			
		}

		public void Draw()
		{
			_outVector = new Vector2(outPoint.node.rect.xMax, outPoint.rect.center.y);

			Handles.DrawBezier(
				inPoint.left,
				_outVector,
				inPoint.left + Vector2.left * 50f,
				outPoint.rect.center - Vector2.left * 50f,
				outPoint.isActive ? Color.white : Color.gray,
				null,
				2f
			);

			//if (Handles.Button((inPoint.rect.center + outPoint.rect.center) * 0.5f, Quaternion.identity, 4, 8, Handles.RectangleCap))
			//{

			//}
		}
	}
}
using System.Collections.Generic;
using Mapbox.Map;
using UnityEngine;

namespace Mapbox.Unity.QuadTree
{
	public class QuadTreeGenerator : MonoBehaviour
	{
		private Camera _camera;
		private QuadTreeMap _map;
		private float WorldScale;

		public int MinZoom;
		public int MaxZoom
		{
			get { return _map.AbsoluteZoom; }
		}

		public float LevelDecisionMultiplier = 1;

		private QuadTreeView _view;

		public void Initialize(float worldScale, Camera cam, QuadTreeMap map)
		{
			WorldScale = worldScale;
			_camera = cam;
			_map = map;

			_view = new QuadTreeView();
		}

		public QuadTreeView UpdateQuadTree()
		{
			_view.Clear();
			var planes = GeometryUtility.CalculateFrustumPlanes(_camera);
			var worldBaseBounds = new UnityRectD(new UnwrappedTileId(0, 0, 0), -_map.CenterMercator, WorldScale);
			var stack = new Stack<UnityRectD>();
			stack.Push(worldBaseBounds);
			while (stack.Count > 0)
			{
				var tile = stack.Pop();

				if ((!GeometryUtility.TestPlanesAABB(planes, tile.UnityBounds)))
				{
					continue;
				}

				if (tile.Id.Z == MaxZoom || !ShouldSplit(tile))
				{
					_view.Tiles.Add(tile.Id, tile);

					if (tile.UnityBounds.Contains(Vector3.zero))
					{
						_view.CenterRect = tile;
					}
					continue;
				}

				for (var i = 0; i < 4; i++)
				{
					var child  = tile.Quadrant(i);
					stack.Push(child);
				}
			}

			return _view;
		}

		private bool ShouldSplit(UnityRectD tile)
		{
			if (tile.Id.Z < MinZoom)
			{
				return true;
			}
			else if (tile.Id.Z == MaxZoom)
			{
				return false;
			}

			var dist = tile.ShortestDistanceTo(Vector3.zero);
			var camDistance = _camera.transform.position.magnitude;
			var levelDiff = Mathf.Floor(dist * LevelDecisionMultiplier / camDistance);
			return tile.Id.Z < MaxZoom - levelDiff;
		}
	}
}
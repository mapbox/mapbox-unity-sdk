namespace Mapbox.Examples
{
	using KDTree;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration;
	using Mapbox.Unity.MeshGeneration.Data;

	public class HighlightBuildings : MonoBehaviour
	{
		public KdTreeCollection Collection;
		public int MaxCount = 100;
		public float Range = 10;
		Ray ray;
		Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
		Vector3 pos;
		float rayDistance;
		private NearestNeighbour<VectorEntity> pIter;

		void Update()
		{
			if (Input.GetMouseButton(0))
			{
				ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				if (groundPlane.Raycast(ray, out rayDistance))
				{
					pos = ray.GetPoint(rayDistance);
					pIter = Collection.NearestNeighbors(new double[] { pos.x, pos.z }, MaxCount, Range);
					while (pIter.MoveNext())
					{
						pIter.Current.Transform.localScale = Vector3.zero;
					}
				}
			}
		}
	}
}

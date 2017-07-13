namespace Mapbox.Unity.MeshGeneration.Components
{
	using UnityEngine;

	public class VertexDebugger : MonoBehaviour
	{
		[Multiline(10)]
		public string Triangles;

		void Start()
		{
			var mf = GetComponent<MeshFilter>();
			if (mf)
			{
				var mesh = mf.mesh;
				var verts = mesh.vertices;
				for (int i = 0; i < verts.Length; i++)
				{
					var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
					go.name = i.ToString();
					go.transform.SetParent(transform, false);
					go.transform.localPosition = verts[i];
				}
				var tris = mesh.triangles;
				Triangles = "";
				for (int i = 0; i < tris.Length; i += 3)
				{
					Triangles += tris[i] + "," + tris[i + 1] + "," + tris[i + 2] + "\r\n";
				}
			}
		}
	}
}
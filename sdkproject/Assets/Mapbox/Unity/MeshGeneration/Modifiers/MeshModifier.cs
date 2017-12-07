namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using Mapbox.Unity.MeshGeneration.Data;

	public enum ModifierType
	{
		Preprocess,
		Postprocess
	}

	/// <summary>
	/// Mesh Data class and Mesh Modifier
	/// MeshData class is used to keep data required for a mesh.Mesh modifiers recieve raw feature data and a mesh modifier, 
	/// generate their data and keep it inside MeshData. i.e.PolygonMeshModifier is responsible for triangulating the polygon, 
	/// so it calculates the triangle indices using the vertices and save it into MeshData.Triangles list.There's no control 
	/// over which field a mesh modifier fills or overrides but some of the basic mesh modifier goes like this;
	/// Polygon mesh modifier - Vertices and triangles
	/// Line Mesh modifier - Vertices, triangles and uvs
	/// UV modifier - uvs(only used with polygon mesh modifier)
	/// height modifier - vertices(adds new points), triangles(for side walls), uvs(for side walls)
	/// After running all mesh modifiers, mesh data is expected to have at least vertices and triangles set as 
	/// they are the bare minimum to create a unity mesh object.
	/// So the main purpose of the mesh modifiers is to fill up the mesh data class but they aren't limited to that either.
	/// You can always create gameobjects and debug lines/spheres inside them for debugging purposes whenever you have a problem.
	/// MeshData class also has some extra fields inside for data transfer purposes(between modifiers). i.e.Edges list 
	/// inside mesh data isn't used for mesh itself, but it's calculated by PolygonMeshModifier and used by HeightModifier 
	/// so to avoid calculating it twice, we're keeping it in the mesh data object. You can also extend mesh data like this 
	/// if you ever need to save data or share information between mesh modifiers.
	/// We fully expect developers to create their own mesh modifiers to customize the look of their world.It would probably 
	/// take a little experience with mesh generation to be able to do this but it's the suggested way to create 
	/// custom world objects, like blobly toonish buildings, or wobbly roads.
	/// </summary>
	public class MeshModifier : ModifierBase
	{
		public virtual ModifierType Type { get { return ModifierType.Preprocess; } }

		public virtual void Run(VectorFeatureUnity feature, MeshData md, float scale)
		{
			Run(feature, md);
		}

		public virtual void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{

		}
	}
}
using UnityEngine;

namespace Area730.TextureBlock
{

    [RequireComponent(typeof(BezierCurve), typeof(MeshFilter), typeof(MeshRenderer))]
    public class TextureBlock : MonoBehaviour
    {

        public float        height              = 2.0f;
        public int          widthSegments       = 15;
        
        public Material     blockMaterial;
        public bool         flipImageX          = false;
        public bool         twoSided            = false;
        public int          heightSegments      = 1;


        public const int    VERTS_IN_TRIANGLE   = 3;
        public const int    TRIS_PER_QUAD       = 2;

    }

}
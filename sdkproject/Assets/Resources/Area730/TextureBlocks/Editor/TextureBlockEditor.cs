using UnityEngine;
using UnityEditor;

using Area730.TextureBlock;
using Area730;

[CustomEditor(typeof(TextureBlock))]
public class TextureBlockEditor : Editor
{

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        TextureBlock    block           = (TextureBlock)target;
        MeshFilter      meshFilter      = block.GetComponent<MeshFilter>();
        MeshRenderer    meshRenderer    = block.GetComponent<MeshRenderer>();
        BezierCurve     curve           = block.GetComponent<BezierCurve>();

        Mesh mesh       = new Mesh();
        mesh.name       = "Mesh";

        int vertsInRow  = block.widthSegments + 1;
        int vertsInCol  = block.heightSegments + 1;
        int numIndices  = block.widthSegments * block.heightSegments * TextureBlock.VERTS_IN_TRIANGLE * TextureBlock.TRIS_PER_QUAD;

        if (block.twoSided)
        {
            numIndices *= 2;
        }

        int         numVertices = vertsInRow * vertsInCol;
        Vector3[]   vertices    = new Vector3[numVertices];
        Vector2[]   uvs         = new Vector2[numVertices];
        int[]       indices     = new int[numIndices];
        Vector4[]   tangents    = new Vector4[numVertices];
        Vector4     tangent     = new Vector4(1f, 0f, 0f, -1f);

        int     index           = 0;
        float   uvFactorX       = 1.0f / block.widthSegments;
        float   uvFactorY       = 1.0f / block.heightSegments;
        float   segmentHeight   = block.height / block.heightSegments;

        //generate vertices
        for (int y = 0; y < vertsInCol; y++)
        {
            float uvX = 0;

            for (int x = 0; x < vertsInRow; x++)
            {
                Vector3 pt = curve.GetLocalPoint((float)x / (float)block.widthSegments);

                vertices[index] = new Vector3(pt.x, pt.y + y * segmentHeight, pt.z);
                tangents[index] = tangent;

                if (x > 0)
                {
                    uvX += uvFactorX /*Vector3.Distance(vertices[index], vertices[index - 1]) */   /*/ block.blockMaterial.textureLength*/;
                }

                float newUvX = block.flipImageX ? (1 - uvX) : uvX;
                uvs[index++] = new Vector2(newUvX, y * uvFactorY);
            }

        }

        

        index = 0;
        //generate triangle indices
        for (int y = 0; y < block.heightSegments; y++)
        {
            for (int x = 0; x < block.widthSegments; x++)
            {
                indices[index] = (y * vertsInRow) + x;
                indices[index + 1] = ((y + 1) * vertsInRow) + x;
                indices[index + 2] = (y * vertsInRow) + x + 1;

                indices[index + 3] = ((y + 1) * vertsInRow) + x;
                indices[index + 4] = ((y + 1) * vertsInRow) + x + 1;
                indices[index + 5] = (y * vertsInRow) + x + 1;

                index += TextureBlock.VERTS_IN_TRIANGLE * TextureBlock.TRIS_PER_QUAD;
            }

            if (block.twoSided)
            {
                for (int x = 0; x < block.widthSegments; x++)
                {
                    indices[index] = (y * vertsInRow) + x;
                    indices[index + 1] = (y * vertsInRow) + x + 1;
                    indices[index + 2] = ((y + 1) * vertsInRow) + x;

                    indices[index + 3] = ((y + 1) * vertsInRow) + x;
                    indices[index + 4] = (y * vertsInRow) + x + 1;
                    indices[index + 5] = ((y + 1) * vertsInRow) + x + 1;

                    index += TextureBlock.VERTS_IN_TRIANGLE * TextureBlock.TRIS_PER_QUAD;
                }
            }

        }

        mesh.vertices   = vertices;
        mesh.uv         = uvs;
        mesh.triangles  = indices;
        mesh.tangents   = tangents;
        mesh.RecalculateNormals();

        meshFilter.mesh         = mesh;
        meshRenderer.material   = block.blockMaterial;

        EditorUtility.SetDirty(block);
    }


}
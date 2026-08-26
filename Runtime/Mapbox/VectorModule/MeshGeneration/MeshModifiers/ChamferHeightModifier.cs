using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.VectorModule.MeshGeneration.MeshModifiers
{
    public class ChamferHeightModifier : MeshModifier
    {
        private ChamferModifierSettings _chamferOptions;
	    
        public ChamferHeightModifier(ChamferModifierSettings chamferSettings = null)
        {
            _chamferOptions = chamferSettings ?? new ChamferModifierSettings();
        }

        public override void Run(VectorFeatureUnity feature, MeshData md, IMapInformation mapInfo)
        {
            new ChamferHeightCore(_chamferOptions).Run(feature, md, mapInfo);
        }
    }
    
     [Serializable]
    public class ChamferModifierSettings
    {
        [NonSerialized] public float ScaledOffset;

        [Tooltip("Suggested starting value is 1")]
        public float OffsetInMeters = 1f;

        [Tooltip("Flatten top polygons to prevent unwanted slanted roofs because of the bumpy terrain")]
        public bool FlatTops = true;
        
        public ChamferModifierSettings(float offsetInMetersInMeters = 1f)
        {
            OffsetInMeters = offsetInMetersInMeters;
        }
    }
    
    public sealed class ChamferHeightCore
    {
        private ChamferModifierSettings _settings;

        private float _scale = 1;

        public ChamferHeightCore(ChamferModifierSettings settings)
        {
            _settings = settings;
        }

        public void Run(VectorFeatureUnity feature, MeshData md, IMapInformation mapInformation, UnityMapTile tile = null)
        {
            if (md.Vertices.Count == 0 || feature == null || feature.Points.Count < 1)
                return;

            var tileSize = Conversions.TileEdgeSizeInMercator(feature.TileId);
            _scale = mapInformation.Scale;
            _settings.ScaledOffset = (float) (_settings.OffsetInMeters / tileSize);
            
            QueryHeight(feature, md, out var maxHeight, out var minHeight);

            maxHeight = (maxHeight / tileSize) / mapInformation.GetLatitudeCompensationForLocation;
            minHeight = (minHeight / tileSize) / mapInformation.GetLatitudeCompensationForLocation;
            var height = (maxHeight - minHeight);

            var max = md.Vertices[0].y;
            var min = md.Vertices[0].y;
            if (_settings.FlatTops)
            {
                FlattenTops(md, minHeight, ref height, ref maxHeight, ref minHeight);
            }
            else
            {
                for (int i = 0; i < md.Vertices.Count; i++)
                {
                    md.Vertices[i] = new Vector3(md.Vertices[i].x, md.Vertices[i].y + minHeight + height, md.Vertices[i].z);
                }
            }

            var originalVertexCount = md.Vertices.Count;
            Chamfer(feature, md);

            Sides(feature, md, height, originalVertexCount);
        }

        private void Sides(VectorFeatureUnity feature, MeshData meshData, float hf, int originalVertexCount)
        {


            float d = 0f;
            Vector3 v1;
            Vector3 v2 = Constants.Math.Vector3Zero;
            int ind = 0;

            var wallTri = new List<int>();
            var wallUv = new List<Vector2>();
            meshData.Vertices.Add(new Vector3(meshData.Vertices[originalVertexCount - 1].x, meshData.Vertices[originalVertexCount - 1].y - hf, meshData.Vertices[originalVertexCount - 1].z));
            meshData.Tangents.Add(meshData.Tangents[originalVertexCount - 1]);
            wallUv.Add(new Vector2(0, -hf));
            meshData.Normals.Add(meshData.Normals[originalVertexCount - 1]);

            for (int i = 0; i < meshData.Edges.Count; i += 2)
            {
                v1 = meshData.Vertices[meshData.Edges[i]];
                v2 = meshData.Vertices[meshData.Edges[i + 1]];
                ind = meshData.Vertices.Count;
                meshData.Vertices.Add(v1);
                meshData.Vertices.Add(v2);
                meshData.Vertices.Add(new Vector3(v1.x, v1.y - hf, v1.z));
                meshData.Vertices.Add(new Vector3(v2.x, v2.y - hf, v2.z));

                meshData.Normals.Add(meshData.Normals[meshData.Edges[i]]);
                meshData.Normals.Add(meshData.Normals[meshData.Edges[i + 1]]);
                meshData.Normals.Add(meshData.Normals[meshData.Edges[i]]);
                meshData.Normals.Add(meshData.Normals[meshData.Edges[i + 1]]);

                meshData.Tangents.Add(v2 - v1.normalized);
                meshData.Tangents.Add(v2 - v1.normalized);
                meshData.Tangents.Add(v2 - v1.normalized);
                meshData.Tangents.Add(v2 - v1.normalized);

                d = (v2 - v1).magnitude;

                wallUv.Add(new Vector2(0, 0));
                wallUv.Add(new Vector2(d, 0));
                wallUv.Add(new Vector2(0, -hf));
                wallUv.Add(new Vector2(d, -hf));

                wallTri.Add(ind);
                wallTri.Add(ind + 1);
                wallTri.Add(ind + 2);

                wallTri.Add(ind + 1);
                wallTri.Add(ind + 3);
                wallTri.Add(ind + 2);
            }

            meshData.Triangles[0].AddRange(wallTri);
            meshData.UV[0].AddRange(wallUv);
        }

        private static void FlattenTops(MeshData meshData, float minHeight, ref float hf, ref float max, ref float min)
        {
            for (int i = 0; i < meshData.Vertices.Count; i++)
            {
                if (meshData.Vertices[i].y > max)
                    max = meshData.Vertices[i].y;
                else if (meshData.Vertices[i].y < min)
                    min = meshData.Vertices[i].y;
            }
            for (int i = 0; i < meshData.Vertices.Count; i++)
            {
                meshData.Vertices[i] = new Vector3(meshData.Vertices[i].x, max + minHeight + hf, meshData.Vertices[i].z);
            }
            hf += max - min;
        }

        private static void GetHeightData(VectorFeatureUnity feature, float scale, ref float minHeight, ref float hf)
        {
            if (feature.Properties.ContainsKey("height"))
            {
                hf = Convert.ToSingle(feature.Properties["height"]);
                hf *= scale;
                if (feature.Properties.ContainsKey("min_height"))
                {
                    minHeight = Convert.ToSingle(feature.Properties["min_height"]) * scale;
                    hf -= minHeight;
                }

            }
            if (feature.Properties.ContainsKey("ele"))
            {
                hf = Convert.ToSingle(feature.Properties["ele"]);
                hf *= scale;
            }
        }

        public void QueryHeight(VectorFeatureUnity feature, MeshData md, out float maxHeight, out float minHeight)
        {
            minHeight = 0.0f;
            maxHeight = 0.0f;

            if (feature.Properties.ContainsKey("height"))
            {
                maxHeight = Convert.ToSingle(feature.Properties["height"]);
            }
            else
            {
                maxHeight = 0;
            }

            if (feature.Properties.ContainsKey("min_height"))
            {
                minHeight = Convert.ToSingle(feature.Properties["min_height"]);
            }
            else
            {
                minHeight = 0;
            }
                    
        }
        public void Chamfer(VectorFeatureUnity feature, MeshData md)
        {
            if (md.Vertices.Count == 0 || feature.Points.Count < 1)
                return;

            List<Vector3> newVertices = new List<Vector3>();
            List<Vector2> newUV = new List<Vector2>();
            md.Normals.Clear();
            md.Edges.Clear();
            md.Tangents.Clear();

            for (int t = 0; t < md.Triangles[0].Count; t++)
            {
                md.Triangles[0][t] *= 3;
            }

            var next = 0; var current = 0; var prev = 0;
            Vector3 v1, v2, n1, n2, pij1, pij2, pjk1, pjk2;
            Vector3 poi, close1, close2;

            var start = 0;
            for (int i = 0; i < feature.Points.Count; i++)
            {
                var count = feature.Points[i].Count;
                var cst = newVertices.Count;
                for (int j = 0; j < count; j++)
                {
                    if (j == count - 1)
                    {
                        newVertices.Add(newVertices[cst]);
                        newVertices.Add(newVertices[cst + 1]);
                        newVertices.Add(newVertices[cst + 2]);
                        newUV.Add(newUV[cst]);
                        newUV.Add(newUV[cst + 1]);
                        newUV.Add(newUV[cst + 2]);
                        md.Normals.Add(md.Normals[cst]);
                        md.Normals.Add(md.Normals[cst + 1]);
                        md.Normals.Add(md.Normals[cst + 2]);

                        md.Tangents.Add(md.Tangents[cst]);
                        md.Tangents.Add(md.Tangents[cst + 1]);
                        md.Tangents.Add(md.Tangents[cst + 2]);

                        continue;
                    }

                    current = start + j;
                    if (j > 0)
                        next = start + j - 1;
                    else
                        next = start + j - 1 + count - 1; //another -1  as last item equals first
                    prev = start + j + 1;


                    v1 = new Vector3(
                        md.Vertices[current].x - md.Vertices[next].x, 0,
                        md.Vertices[current].z - md.Vertices[next].z);
                    v1.Normalize();
                    v1 *= -_settings.ScaledOffset;
                    n1 = new Vector3(-v1.z, 0, v1.x);

                    pij1 = new Vector3(
                        (float)(md.Vertices[next].x + n1.x), 0,
                        (float)(md.Vertices[next].z + n1.z));
                    pij2 = new Vector3(
                        (float)(md.Vertices[current].x + n1.x), 0,
                        (float)(md.Vertices[current].z + n1.z));

                    v2 = new Vector3(
                        md.Vertices[prev].x - md.Vertices[current].x, 0,
                        md.Vertices[prev].z - md.Vertices[current].z);

                    v2.Normalize();
                    v2 *= -_settings.ScaledOffset;
                    n2 = new Vector3(-v2.z, 0, v2.x);
                    pjk1 = new Vector3(
                        (float)(md.Vertices[current].x + n2.x), 0,
                        (float)(md.Vertices[current].z + n2.z));
                    pjk2 = new Vector3(
                        (float)(md.Vertices[prev].x + n2.x), 0,
                        (float)(md.Vertices[prev].z + n2.z));

                    // See where the shifted lines ij and jk intersect.
                    bool lines_intersect, segments_intersect;

                    FindIntersection(pij1, pij2, pjk1, pjk2,
                        out lines_intersect, out segments_intersect,
                        out poi, out close1, out close2);

                    var d = Vector3.Distance(poi, pij2);
                    // if (d > 10 * _offset)
                    // {
                    // 	poi = new Vector3((md.Vertices[current].x + (poi - (-v1 - v2)).normalized.x), 0,
                    // 		(md.Vertices[current].z + (poi - (-v1 - v2)).normalized.z));
                    // }

                    newVertices.Add(new Vector3(poi.x, poi.y + _settings.ScaledOffset + md.Vertices[current].y, poi.z));
                    newVertices.Add(md.Vertices[current] + v1);
                    newVertices.Add(md.Vertices[current] - v2);

                    md.Normals.Add(Constants.Math.Vector3Up);
                    md.Normals.Add((-n1).normalized);
                    md.Normals.Add((-n2).normalized);

                    md.Tangents.Add(v1 - v2);
                    md.Tangents.Add(v1 - v2);
                    md.Tangents.Add(v1 - v2);

                    newUV.Add(md.UV[0][current]);
                    newUV.Add(md.UV[0][current]);
                    newUV.Add(md.UV[0][current]);

                    md.Triangles[0].Add(3 * current);
                    md.Triangles[0].Add(3 * current + 1);
                    md.Triangles[0].Add(3 * current + 2);

                    md.Edges.Add(3 * current + 2);
                    md.Edges.Add(3 * current + 1);

                    md.Triangles[0].Add(3 * prev);
                    md.Triangles[0].Add(3 * current + 2);
                    md.Triangles[0].Add(3 * prev + 1);

                    md.Triangles[0].Add(3 * current);
                    md.Triangles[0].Add(3 * current + 2);
                    md.Triangles[0].Add(3 * prev);

                    md.Edges.Add(3 * prev + 1);
                    md.Edges.Add(3 * current + 2);
                }
                start += count;
            }

            md.Vertices = newVertices;
            md.UV[0] = newUV;
        }

        private List<Vector3> GetEnlargedPolygon(List<Vector3> old_points, float offset)
        {
            List<Vector3> enlarged_points = new List<Vector3>();
            int num_points = old_points.Count;
            for (int j = 0; j < num_points; j++)
            {
                // Find the new location for point j.
                // Find the points before and after j.
                int i = (j - 1);
                if (i < 0) i += num_points;
                int k = (j + 1) % num_points;

                // Move the points by the offset.
                Vector3 v1 = new Vector3(
                    old_points[j].x - old_points[i].x, 0,
                    old_points[j].z - old_points[i].z);
                v1.Normalize();
                v1 *= offset;
                Vector3 n1 = new Vector3(-v1.z, 0, v1.x);

                Vector3 pij1 = new Vector3(
                    (float)(old_points[i].x + n1.x), 0,
                    (float)(old_points[i].z + n1.z));
                Vector3 pij2 = new Vector3(
                    (float)(old_points[j].x + n1.x), 0,
                    (float)(old_points[j].z + n1.z));

                Vector3 v2 = new Vector3(
                    old_points[k].x - old_points[j].x, 0,
                    old_points[k].z - old_points[j].z);
                v2.Normalize();
                v2 *= offset;
                Vector3 n2 = new Vector3(-v2.z, 0, v2.x);

                Vector3 pjk1 = new Vector3(
                    (float)(old_points[j].x + n2.x), 0,
                    (float)(old_points[j].z + n2.z));
                Vector3 pjk2 = new Vector3(
                    (float)(old_points[k].x + n2.x), 0,
                    (float)(old_points[k].z + n2.z));

                // See where the shifted lines ij and jk intersect.
                bool lines_intersect, segments_intersect;
                Vector3 poi, close1, close2;
                FindIntersection(pij1, pij2, pjk1, pjk2,
                    out lines_intersect, out segments_intersect,
                    out poi, out close1, out close2);
                Debug.Assert(lines_intersect,
                    "Edges " + i + "-->" + j + " and " +
                    j + "-->" + k + " are parallel");

                enlarged_points.Add(poi);
            }

            return enlarged_points;
        }

        private void FindIntersection(
            Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4,
            out bool lines_intersect, out bool segments_intersect,
            out Vector3 intersection,
            out Vector3 close_p1, out Vector3 close_p2)
        {
            // Get the segments' parameters.
            float dx12 = p2.x - p1.x;
            float dy12 = p2.z - p1.z;
            float dx34 = p4.x - p3.x;
            float dy34 = p4.z - p3.z;

            // Solve for t1 and t2
            float denominator = (dy12 * dx34 - dx12 * dy34);

            // Tested on the denominator rather than on float.IsInfinity(t1). A zero denominator
            // with a zero numerator divides to NaN, not infinity, so the fully degenerate corner -
            // a duplicated footprint point - slipped past the old check entirely.
            if (Mathf.Abs(denominator) < 1e-8f)
            {
                // Parallel, which is simply what a collinear corner looks like and not an error;
                // redundant points in a footprint produce them constantly. There is no single
                // crossing point, and the corner's own offset (p2) is the correct chamfer vertex.
                //
                // Returning that rather than the NaN triple this used to emit: neither caller acts
                // on lines_intersect - one ignores it, the other only Debug.Asserts, which is
                // stripped from a release build - so the NaN went straight into the vertex buffer.
                // A building then carried NaN in X and Z with a finite Y, and Unity reported it
                // later as "abnormal mesh bounds" on an anonymous mesh, nowhere near here.
                lines_intersect = false;
                segments_intersect = false;
                intersection = p2;
                close_p1 = p2;
                close_p2 = p2;
                return;
            }

            float t1 =
                ((p1.x - p3.x) * dy34 + (p3.z - p1.z) * dx34)
                / denominator;
            lines_intersect = true;

            float t2 =
                ((p3.x - p1.x) * dy12 + (p1.z - p3.z) * dx12)
                / -denominator;

            // Find the point of intersection.
            intersection = new Vector3(p1.x + dx12 * t1, 0, p1.z + dy12 * t1);

            // The segments intersect if t1 and t2 are between 0 and 1.
            segments_intersect =
                ((t1 >= 0) && (t1 <= 1) &&
                 (t2 >= 0) && (t2 <= 1));

            // Find the closest points on the segments.
            if (t1 < 0)
            {
                t1 = 0;
            }
            else if (t1 > 1)
            {
                t1 = 1;
            }

            if (t2 < 0)
            {
                t2 = 0;
            }
            else if (t2 > 1)
            {
                t2 = 1;
            }

            close_p1 = new Vector3(p1.x + dx12 * t1, 0, p1.z + dy12 * t1);
            close_p2 = new Vector3(p3.x + dx34 * t2, 0, p3.z + dy34 * t2);
        }
    }
}
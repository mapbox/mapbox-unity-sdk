namespace Mapbox.Unity.MeshGeneration.Factories
{
    using System.Collections.Generic;
    using UnityEngine;
    using Mapbox.Map;
    using Mapbox.Unity.MeshGeneration.Enums;
    using Mapbox.Unity.MeshGeneration.Data;
    using Mapbox.Platform;
    using Mapbox.Unity.Utilities;
    using Utils;

    [CreateAssetMenu(menuName = "Mapbox/Factories/Terrain Factory")]
    public class TerrainFactory : Factory
    {
        [SerializeField]
        private bool _createFlatBase = false;
        [SerializeField]
        private Material _flatTileMaterial;
        [SerializeField]
        private bool _createRealTerrain = true;
        [SerializeField]
        private string _mapId;

        private Dictionary<Vector2, UnityTile> _tiles;
        [SerializeField]
        private int sampleCount = 40;

        public override void Initialize(MonoBehaviour mb, IFileSource fs)
        {
            base.Initialize(mb, fs);
            _tiles = new Dictionary<Vector2, UnityTile>();
        }

        public override void Register(UnityTile tile)
        {
            base.Register(tile);
            if (_createRealTerrain)
            {
                _tiles.Add(tile.TileCoordinate, tile);
                Run(tile);
            }
            else
            {
                if (_createFlatBase)
                {
                    CreateTileBase(tile);
                }
            }
        }

        private void Run(UnityTile tile)
        {
            var parameters = new Tile.Parameters
            {
                Fs = this.FileSource,
                Id = new CanonicalTileId(tile.Zoom, (int)tile.TileCoordinate.x, (int)tile.TileCoordinate.y),
                MapId = _mapId
            };

            tile.HeightDataState = TilePropertyState.Loading;
            var pngRasterTile = new RawPngRasterTile();

            pngRasterTile.Initialize(parameters, () =>
            {
                if (pngRasterTile.Error != null)
                {
                    tile.HeightDataState = TilePropertyState.Error;
                    return;
                }

                var texture = new Texture2D(256, 256);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.LoadImage(pngRasterTile.Data);
                CreateTerrain(tile, texture);

                tile.HeightData = texture;
                tile.HeightDataState = TilePropertyState.Loaded;

                FixStitches(tile);
            });
        }

        private void CreateTerrain(UnityTile tile, Texture2D texture)
        {
            var go = tile.gameObject;
            var mesh = new Mesh();
            var verts = new List<Vector3>(sampleCount* sampleCount);

            for (float x = 0; x < sampleCount; x++)
            {
                for (float y = 0; y < sampleCount; y++)
                {
                    var xx = Mathd.Lerp(tile.Rect.Min.x, tile.Rect.Max.x,
                        x / (sampleCount - 1));
                    var yy = Mathd.Lerp(tile.Rect.Min.y, tile.Rect.Max.y,
                        y / (sampleCount - 1));

                    verts.Add(new Vector3(
                        (float)(xx - tile.Rect.Center.x),
                        Conversions.GetRelativeHeightFromColor(texture.GetPixel((int)Mathf.Clamp((x / (sampleCount - 1) * 256), 0, 255),
                                                                                (int)Mathf.Clamp((256 - (y / (sampleCount - 1) * 256)), 0, 255)), tile.RelativeScale),
                        (float)(yy - tile.Rect.Center.y)));
                }
            }

            mesh.SetVertices(verts);

            //we can read these from a hardcoded dictionary as well
            //no need to calculate this every single time unless we need a really high range for sampleCount
            var trilist = new List<int>(sampleCount * sampleCount * 3);
            for (int y = 0; y < sampleCount - 1; y++)
            {
                for (int x = 0; x < sampleCount - 1; x++)
                {
                    trilist.Add((y * sampleCount) + x);
                    trilist.Add((y * sampleCount) + x + sampleCount);
                    trilist.Add((y * sampleCount) + x + sampleCount + 1);

                    trilist.Add((y * sampleCount) + x);
                    trilist.Add((y * sampleCount) + x + sampleCount + 1);
                    trilist.Add((y * sampleCount) + x + 1);
                }
            }
            mesh.SetTriangles(trilist, 0);

            var uvlist = new List<Vector2>(sampleCount * sampleCount);
            var step = 1f / (sampleCount - 1);
            for (int i = 0; i < sampleCount; i++)
            {
                for (int j = 0; j < sampleCount; j++)
                {
                    uvlist.Add(new Vector2(i * step, 1 - (j * step)));
                }
            }
            mesh.SetUVs(0, uvlist);
            mesh.RecalculateNormals();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            
            //go.AddComponent<MeshCollider>();
            //go.layer = LayerMask.NameToLayer("terrain");
        }

        //BRNKHY there has to be a better way to do this
        private void FixStitches(UnityTile tile)
        {
            var tmesh = tile.GetComponent<MeshFilter>().mesh;
            var left = new Vector2(tile.TileCoordinate.x - 1, tile.TileCoordinate.y);
            if (_tiles.ContainsKey(left) && _tiles[left].HeightData != null)
            {
                var t2mesh = _tiles[left].GetComponent<MeshFilter>().mesh;

                var verts = tmesh.vertices;
                for (int i = 0; i < sampleCount; i++)
                {
                    verts[i].Set(verts[i].x, t2mesh.vertices[verts.Length - sampleCount + i].y, verts[i].z);
                }
                tmesh.vertices = verts;
            }

            var right = new Vector2(tile.TileCoordinate.x + 1, tile.TileCoordinate.y);
            if (_tiles.ContainsKey(right) && _tiles[right].HeightData != null)
            {
                var t2mesh = _tiles[right].GetComponent<MeshFilter>().mesh;

                var verts = tmesh.vertices;
                for (int i = 0; i < sampleCount; i++)
                {
                    verts[verts.Length - sampleCount + i].Set(verts[verts.Length - sampleCount + i].x, t2mesh.vertices[i].y,
                        verts[verts.Length - sampleCount + i].z);
                }
                tmesh.vertices = verts;
            }

            var up = new Vector2(tile.TileCoordinate.x, tile.TileCoordinate.y - 1);
            if (_tiles.ContainsKey(up) && _tiles[up].HeightData != null)
            {
                var t2mesh = _tiles[up].GetComponent<MeshFilter>().mesh;
                var verts = tmesh.vertices;
                for (int i = 0; i < sampleCount; i++)
                {
                    verts[i * sampleCount].Set(verts[i * sampleCount].x, t2mesh.vertices[i * sampleCount + sampleCount - 1].y,
                        verts[i * sampleCount].z);
                }
                tmesh.vertices = verts;
            }

            var down = new Vector2(tile.TileCoordinate.x, tile.TileCoordinate.y + 1);
            if (_tiles.ContainsKey(down) && _tiles[down].HeightData != null)
            {
                var t2mesh = _tiles[down].GetComponent<MeshFilter>().mesh;

                var verts = tmesh.vertices;
                for (int i = 0; i < sampleCount; i++)
                {
                    verts[i * sampleCount + sampleCount - 1].Set(verts[i * sampleCount + sampleCount - 1].x, t2mesh.vertices[i * sampleCount].y,
                        verts[i * sampleCount + sampleCount - 1].z);
                }
                tmesh.vertices = verts;
            }

            tmesh.RecalculateNormals();
        }

        private void CreateTileBase(UnityTile tile)
        {
            var mesh = new Mesh();
            var verts = new List<Vector3>();

            verts.Add((tile.Rect.Min - tile.Rect.Center).ToVector3xz());
            verts.Add(
                new Vector3(
                    (float)(tile.Rect.Max.x - tile.Rect.Center.x),
                    0,
                    (float)(tile.Rect.Min.y - tile.Rect.Center.y)));
            verts.Add(
                new Vector3(
                    (float)(tile.Rect.Min.x - tile.Rect.Center.x),
                    0,
                    (float)(tile.Rect.Max.y - tile.Rect.Center.y)));
            verts.Add((tile.Rect.Max - tile.Rect.Center).ToVector3xz());

            mesh.SetVertices(verts);
            var trilist = new List<int>() { 0, 1, 2, 1, 3, 2 };
            mesh.SetTriangles(trilist, 0);
            var uvlist = new List<Vector2>()
            {
                new Vector2(0,1),
                new Vector2(1,1),
                new Vector2(0,0),
                new Vector2(1,0)
            };
            mesh.SetUVs(0, uvlist);
            mesh.RecalculateNormals();
            tile.GetComponent<MeshFilter>().sharedMesh = mesh;
            tile.GetComponent<MeshRenderer>().material = _flatTileMaterial;
            tile.gameObject.AddComponent<MeshCollider>();
        }

        private float GetHeightFromColor(Color c)
        {
            //additional *256 to switch from 0-1 to 0-256
            return (float)(-10000 + ((c.r * 256 * 256 * 256 + c.g * 256 * 256 + c.b * 256) * 0.1));
        }
    }
}

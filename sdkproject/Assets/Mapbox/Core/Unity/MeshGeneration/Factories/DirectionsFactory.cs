namespace Mapbox.Unity.MeshGeneration.Factories
{
    using UnityEngine;
    using Mapbox.Directions;
    using System.Collections.Generic;
    using System.Linq;
    using Data;
    using Modifiers;
    using Mapbox.Platform;
    using Mapbox.Utils;
    using Mapbox.Unity.Utilities;

    [CreateAssetMenu(menuName = "Mapbox/Factories/Direction Factory")]
    public class DirectionsFactory : Factory
    {
        [SerializeField]
        private Material _material;
        private Directions _directions;
        public List<MeshModifier> MeshModifiers;

        public override void Initialize(IFileSource fileSource, WorldParameters parameters)
        {
            base.Initialize(fileSource, parameters);
            _directions = MapboxAccess.Instance.Directions;
        }

        public void Query(List<Transform> waypoints)
        {
            var wp = new Vector2d[waypoints.Count];
            for (int i = 0; i < waypoints.Count; i++)
            {
                wp[i] = waypoints[i].GetGeoPosition(Parameters.ReferenceTileRect.Center, Parameters.WorldScaleFactor);
            }
            var _directionResource = new DirectionResource(wp, RoutingProfile.Driving);
            _directionResource.Steps = true;
            _directions.Query(_directionResource, HandleDirectionsResponse);
        }

        void HandleDirectionsResponse(DirectionsResponse response)
        {
            var meshData = new MeshData();
            var dat = new List<Vector3>();
            foreach (var leg in response.Routes[0].Legs)
            {
                foreach (var point in response.Routes[0].Geometry)
                {
                    dat.Add(Conversions.GeoToWorldPosition(point.x, point.y, Parameters.ReferenceTileRect.Center, Parameters.WorldScaleFactor).ToVector3xz());
                }
            }

            var feat = new VectorFeatureUnity();
            feat.Points.Add(dat);

            foreach (MeshModifier mod in MeshModifiers.Where(x => x.Active))
            {
                mod.Run(feat, meshData);
            }

            CreateGameObject(meshData);
        }

        private GameObject CreateGameObject(MeshData data)
        {
            var go = new GameObject("direction waypoint " + " entity");
            var mesh = go.AddComponent<MeshFilter>().mesh;
            mesh.subMeshCount = data.Triangles.Count;

            mesh.SetVertices(data.Vertices);
            for (int i = 0; i < data.Triangles.Count; i++)
            {
                var triangle = data.Triangles[i];
                mesh.SetTriangles(triangle, i);
            }

            for (int i = 0; i < data.UV.Count; i++)
            {
                var uv = data.UV[i];
                mesh.SetUVs(i, uv);
            }

            mesh.RecalculateNormals();
            go.AddComponent<MeshRenderer>().material = _material;
            return go;
        }
    }

}
namespace Mapbox.Unity.Map
{
    using UnityEngine;
    using Mapbox.Map;
    using Mapbox.Unity.Utilities;
    using Mapbox.Utils;
    using System.Collections.Generic;
    using System.Linq;
    using System;

    public class QuadTreeTileProvider : AbstractTileProvider
    {
        [SerializeField]
        Camera _camera;


        [SerializeField]
        float _updateInterval;
        Plane _groundPlane; 
        float _elapsedTime;
        bool _shouldUpdate;
        int _previousZoomLevel;

        public override void OnInitialized()
        {
            _groundPlane = new Plane(Vector3.up, 0);
            _shouldUpdate = true;
            _previousZoomLevel = _map.Zoom;
        }

        public void UpdateMapProperties(float diffZoom)
        {            
            // Update the center based on current zoom level.
            var referenceTileRect = Conversions.TileBounds(TileCover.CoordinateToTileId(_map.CenterLatitudeLongitude, _map.Zoom));
            _map.SetCenterMercator(referenceTileRect.Center);

            //Scale the map accordingly.
            _map.Root.localScale = Vector3.one * Mathf.Pow(2, diffZoom);
                
        }
                           
        void Update()
        {
            //Camera Debugging
            Vector3[] frustumCorners = new Vector3[4];
            _camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _camera.transform.position.y, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);

            for (int i = 0; i < 4; i++)
            {
                var worldSpaceCorner = _camera.transform.TransformVector(frustumCorners[i]);
                Debug.DrawRay(_camera.transform.position, worldSpaceCorner, Color.blue);
            }

            if (!_shouldUpdate)
            {
                return;
            }

            _elapsedTime += Time.deltaTime;

            if (_elapsedTime >= _updateInterval)
            {
                _elapsedTime = 0f;

                //if(_previousZoomLevel != _map.Zoom)
                //{
                //    var diffZoom = _map.Zoom - _map.InitialZoom;
                //    _map.SetZoom(_map.Zoom);
                //    _previousZoomLevel = _map.Zoom;
                //    UpdateMapProperties(diffZoom);
                //}

                if (Math.Abs(_map.ZoomRange - _map.Zoom) > 0.0f)
                {                    
                    var diffZoom = _map.ZoomRange - _map.InitialZoom;
                    if (Math.Abs(diffZoom) >= 1.0f)
                    {
                        _map.SetZoom((int) Math.Ceiling(_map.ZoomRange));
                        _previousZoomLevel = _map.Zoom;
                    }
                    UpdateMapProperties(diffZoom);
                }

                if(_map.MapPanned)
                {
                    Vector2d panRange = _map.PanRange;
                    double xDelta = _map.CenterLatitudeLongitude.x + panRange.y;
                    double zDelta = _map.CenterLatitudeLongitude.y + panRange.x;

                    xDelta = xDelta > 0 ? Mathd.Min(xDelta, Mapbox.Utils.Constants.WebMercMax) : Mathd.Max(xDelta, -Mapbox.Utils.Constants.WebMercMax);
                    zDelta = zDelta > 0 ? Mathd.Min(zDelta, Mapbox.Utils.Constants.WebMercMax) : Mathd.Max(zDelta, -Mapbox.Utils.Constants.WebMercMax);

                    _map.SetCenterLatitudeLongitude(new Vector2d(xDelta, zDelta));
                    UpdateMapProperties(0);
                }

                //update viewport in case it was changed by switching zoom level
                Vector2dBounds _viewPortWebMercBounds = getcurrentViewPortWebMerc();

                var tilesToRequest = TileCover.GetWithWebMerc(_viewPortWebMercBounds, _map.Zoom);

                var activeTiles = _activeTiles.Keys.ToList();
                List<UnwrappedTileId> toRemove = activeTiles.Except(tilesToRequest).ToList();
                foreach (var t2r in toRemove) { RemoveTile(t2r); }
                var finalTilesNeeded = tilesToRequest.Except(activeTiles);

                if(_map.MapPanned)
                {
                    foreach (var tile in activeTiles)
                    {
                        // Reposition tiles in case we panned.
                        RepositionTile(tile);
                    }
                    _map.SetPanRange(Vector2d.zero,true);
                }

                foreach (var tile in finalTilesNeeded)
                {
                    AddTile(tile);
                }
            }
        }

        private Vector2dBounds getcurrentViewPortWebMerc(bool useGroundPlane = true)
        {
            Vector3 hitPntLL;
            Vector3 hitPntUR;

            if (useGroundPlane)
            {
                // rays from camera to groundplane: lower left and upper right
                Ray rayLL = _camera.ViewportPointToRay(new Vector3(0, 0));
                Ray rayUR = _camera.ViewportPointToRay(new Vector3(1, 1));
                hitPntLL = getGroundPlaneHitPoint(rayLL);
                hitPntUR = getGroundPlaneHitPoint(rayUR);
            }
            else
            {
                hitPntLL = _camera.ViewportToWorldPoint(new Vector3(0, 0, _camera.transform.localPosition.y));
                hitPntUR = _camera.ViewportToWorldPoint(new Vector3(1, 1, _camera.transform.localPosition.y));
            }

            //get tile scale at equator, otherwise calucations don't work at higher latitudes
            double factor = Conversions.GetTileScaleInMeters(0, _map.Zoom) * 256 / _map.UnityTileSize;
            //convert Unity units to WebMercator and LatLng to get real world bounding box
            double llx = _map.CenterMercator.x + hitPntLL.x * factor;
            double lly = _map.CenterMercator.y + hitPntLL.z * factor;
            double urx = _map.CenterMercator.x + hitPntUR.x * factor;
            double ury = _map.CenterMercator.y + hitPntUR.z * factor;
            llx = llx > 0 ? Mathd.Min(llx, Mapbox.Utils.Constants.WebMercMax) : Mathd.Max(llx, -Mapbox.Utils.Constants.WebMercMax);
            lly = lly > 0 ? Mathd.Min(lly, Mapbox.Utils.Constants.WebMercMax) : Mathd.Max(lly, -Mapbox.Utils.Constants.WebMercMax);
            urx = urx > 0 ? Mathd.Min(urx, Mapbox.Utils.Constants.WebMercMax) : Mathd.Max(urx, -Mapbox.Utils.Constants.WebMercMax);
            ury = ury > 0 ? Mathd.Min(ury, Mapbox.Utils.Constants.WebMercMax) : Mathd.Max(ury, -Mapbox.Utils.Constants.WebMercMax);
            Vector2d llWebMerc = new Vector2d(llx, lly);
            Vector2d urWebMerc = new Vector2d(urx, ury);


            return new Vector2dBounds(
                llWebMerc
                , urWebMerc
            );
        }


        private Vector3 getGroundPlaneHitPoint(Ray ray)
        {
            float distance;
            if (!_groundPlane.Raycast(ray, out distance)) { return Vector3.zero; }
            return ray.GetPoint(distance);
        }
    }
}
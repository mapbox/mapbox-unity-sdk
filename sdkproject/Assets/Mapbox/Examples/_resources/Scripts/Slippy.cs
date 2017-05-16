namespace Mapbox.Examples
{
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration;
    using Mapbox.Utils;
    using Mapbox.Unity.Utilities;

    public class Slippy : MonoBehaviour
    {
        private Transform _root;
        private MapController _mapController;
        private Camera _camera;
        [SerializeField]
        private int _range = 2;

        Ray _ray;
        float _hitDistance;
        Plane _yPlane;
        Vector3 _cameraTarget;
        Vector2 _cachedTile;
        Vector2 _currentTile;

        void Start()
        {
            _mapController = GetComponent<MapController>();
            _camera = Camera.main;
            _yPlane = new Plane(Vector3.up, Vector3.zero);
            _root = _mapController.Root.transform;
            _mapController.WorldCreated += (s, e) => { _root = e.transform; };
        }

        void Update()
        {
            if (_root == null || _mapController == null)
                return;

            _ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (_yPlane.Raycast(_ray, out _hitDistance))
            {
                _cameraTarget = _ray.GetPoint(_hitDistance) / _root.localScale.x;
                _currentTile = Conversions.MetersToTile(new Vector2d(_mapController.WorldParameters.ReferenceTileRect.Center.x + _cameraTarget.x, _mapController.WorldParameters.ReferenceTileRect.Center.y + _cameraTarget.z), _mapController.Zoom);
                if (_currentTile != _cachedTile)
                {
                    for (int i = -_range; i <= _range; i++)
                    {
                        for (int j = -_range; j <= _range; j++)
                        {
                            _mapController.Request(new Vector2(_currentTile.x + i, _currentTile.y + j), _mapController.Zoom);
                        }
                    }
                    _cachedTile = _currentTile;
                }
            }
        }
    }
}
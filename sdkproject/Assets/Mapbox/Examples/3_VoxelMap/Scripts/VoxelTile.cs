namespace Mapbox.Examples.Voxels
{
	using Mapbox.Geocoding;
	using Mapbox.Map;
	using Mapbox.Unity;
	using System.Collections.Generic;
	using UnityEngine;
	using System.Collections;
	using System.Linq;
	using System;
	using Mapbox.Utils;
	using Mapbox.Platform;
	using Mapbox.Unity.Utilities;
	class VoxelTile : MonoBehaviour, Mapbox.Utils.IObserver<RasterTile>, Mapbox.Utils.IObserver<RawPngRasterTile>
	{
		[SerializeField]
		ForwardGeocodeUserInput _geocodeInput;

		[SerializeField]
		int _zoom = 17;

		[SerializeField]
		float _elevationMultiplier = 1f;

		[SerializeField]
		int _voxelDepthPadding = 1;

		[SerializeField]
		int _tileWidthInVoxels;

		[SerializeField]
		VoxelFetcher _voxelFetcher;

		[SerializeField]
		GameObject _camera;

		[SerializeField]
		int _voxelBatchCount = 100;

		[SerializeField]
		string _styleUrl;

		Map<RasterTile> _raster;
		Map<RawPngRasterTile> _elevation;

		Texture2D _rasterTexture;
		Texture2D _elevationTexture;

		IFileSource _fileSource;

		List<VoxelData> _voxels = new List<VoxelData>();

		List<GameObject> _instantiatedVoxels = new List<GameObject>();

		float _tileScale;

		void Awake()
		{
			_geocodeInput.OnGeocoderResponse += GeocodeInput_OnGeocoderResponse;
		}

		void OnDestroy()
		{
			if (_geocodeInput)
			{
				_geocodeInput.OnGeocoderResponse -= GeocodeInput_OnGeocoderResponse;
			}
		}

		void Start()
		{
			_fileSource = MapboxAccess.Instance;

			_raster = new Map<RasterTile>(_fileSource);
			_elevation = new Map<RawPngRasterTile>(_fileSource);

			if (!string.IsNullOrEmpty(_styleUrl))
			{
				_raster.MapId = _styleUrl;
			}
			_elevation.MapId = "mapbox.terrain-rgb";

			_elevation.Subscribe(this);
			_raster.Subscribe(this);

			// Torres Del Paine
			FetchWorldData(new Vector2d(-50.98306, -72.96639));
		}

		void GeocodeInput_OnGeocoderResponse(ForwardGeocodeResponse response)
		{
			Cleanup();
			FetchWorldData(_geocodeInput.Coordinate);
		}

		void Cleanup()
		{
			StopAllCoroutines();
			_rasterTexture = null;
			_elevationTexture = null;
			_voxels.Clear();
			foreach (var voxel in _instantiatedVoxels)
			{
				Destroy(voxel);
			}
		}

		void FetchWorldData(Vector2d coordinates)
		{
			_tileScale = (_tileWidthInVoxels / 256f) / Conversions.GetTileScaleInMeters((float)coordinates.x, _zoom);
			var bounds = new Vector2dBounds();
			bounds.Center = coordinates;
			_raster.SetVector2dBoundsZoom(bounds, _zoom);
			_elevation.SetVector2dBoundsZoom(bounds, _zoom);
			_raster.Update();
			_elevation.Update();
		}

		public void OnNext(RasterTile tile)
		{
			if (tile.CurrentState == Tile.State.Loaded && !tile.HasError)
			{
				_rasterTexture = new Texture2D(2, 2);
				_rasterTexture.LoadImage(tile.Data);
				TextureScale.Point(_rasterTexture, _tileWidthInVoxels, _tileWidthInVoxels);

				if (ShouldBuildWorld())
				{
					BuildVoxelWorld();
				}
			}
		}

		public void OnNext(RawPngRasterTile tile)
		{
			if (tile.CurrentState == Tile.State.Loaded && !tile.HasError)
			{
				_elevationTexture = new Texture2D(2, 2);
				_elevationTexture.LoadImage(tile.Data);
				TextureScale.Point(_elevationTexture, _tileWidthInVoxels, _tileWidthInVoxels);

				if (ShouldBuildWorld())
				{
					BuildVoxelWorld();
				}
			}
		}

		bool ShouldBuildWorld()
		{
			return _rasterTexture != null && _elevationTexture != null;
		}

		void BuildVoxelWorld()
		{
			var baseHeight = (int)Conversions.GetRelativeHeightFromColor((_elevationTexture.GetPixel(_elevationTexture.width / 2, _elevationTexture.height / 2)),
																		 _elevationMultiplier * _tileScale);
			for (int x = 0; x < _rasterTexture.width; x++)
			{
				for (int z = 0; z < _rasterTexture.height; z++)
				{
					var height = (int)Conversions.GetRelativeHeightFromColor(_elevationTexture.GetPixel(x, z),
																			 _elevationMultiplier * _tileScale) - baseHeight;

					var startHeight = height - _voxelDepthPadding - 1;
					var color = _rasterTexture.GetPixel(x, z);

					for (int y = startHeight; y < height; y++)
					{
						_voxels.Add(new VoxelData() { Position = new Vector3(x, y, z), Prefab = _voxelFetcher.GetVoxelFromColor(color) });
					}
				}
			}

			if (_camera != null)
			{
				_camera.transform.position = new Vector3(_tileWidthInVoxels * .5f, 2f, _tileWidthInVoxels * .5f);
			}

			if (this != null)
			{
				StartCoroutine(BuildRoutine());
			}
		}

		IEnumerator BuildRoutine()
		{
			var distanceOrderedVoxels = _voxels.OrderBy(x => (_camera.transform.position - x.Position).magnitude).ToList();

			for (int i = 0; i < distanceOrderedVoxels.Count; i += _voxelBatchCount)
			{
				for (int j = 0; j < _voxelBatchCount; j++)
				{
					var index = i + j;
					if (index < distanceOrderedVoxels.Count)
					{
						var voxel = distanceOrderedVoxels[index];
						_instantiatedVoxels.Add(Instantiate(voxel.Prefab, voxel.Position, Quaternion.identity, transform) as GameObject);
					}
				}
				yield return null;
			}
		}
	}
}
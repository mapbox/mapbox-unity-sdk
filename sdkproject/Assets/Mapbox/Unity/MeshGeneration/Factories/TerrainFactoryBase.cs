using Mapbox.Unity.MeshGeneration.Factories;
using System.Collections;
using UnityEngine;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.Map;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Enums;
using Mapbox.Unity.MeshGeneration.Factories.TerrainStrategies;

namespace Mapbox.Unity.MeshGeneration.Factories
{
	public class TerrainFactoryBase : AbstractTileFactory
	{
		public TerrainStrategy Strategy;
		[SerializeField]
		protected ElevationLayerProperties _elevationOptions = new ElevationLayerProperties();

		public override void SetOptions(LayerProperties options)
		{
			_elevationOptions = (ElevationLayerProperties)options;
		}

		internal override void OnInitialized()
		{
			Strategy.OnInitialized(_elevationOptions);
		}

		internal override void OnRegistered(UnityTile tile)
		{
			Progress++;
			if (Strategy is IElevationBasedTerrainStrategy)
			{
				tile.HeightDataState = TilePropertyState.Loading;
				var pngRasterTile = new RawPngRasterTile();

				tile.AddTile(pngRasterTile);


				pngRasterTile.Initialize(_fileSource, tile.CanonicalTileId, _elevationOptions.sourceOptions.Id, () =>
				{
					if (tile == null)
					{
						Progress--;
						return;
					}

					if (pngRasterTile.HasError)
					{
						OnErrorOccurred(new TileErrorEventArgs(tile.CanonicalTileId, pngRasterTile.GetType(), tile, pngRasterTile.Exceptions));
						tile.HeightDataState = TilePropertyState.Error;

						// Handle missing elevation from server (404)!
						// TODO: optimize this search!
						if (pngRasterTile.ExceptionsAsString.Contains("404"))
						{
							Strategy.OnFetchingError(pngRasterTile.Exceptions);
						}
						Progress--;
						return;
					}

					tile.SetHeightData(pngRasterTile.Data, _elevationOptions.requiredOptions.exaggerationFactor, _elevationOptions.modificationOptions.useRelativeHeight);
					Strategy.OnRegistered(tile);

				});
			}
			else
			{
				Strategy.OnRegistered(tile);
			}
			Progress--;
		}

		internal override void OnUnregistered(UnityTile tile)
		{
			Strategy.OnUnregistered(tile);
		}
	}
}
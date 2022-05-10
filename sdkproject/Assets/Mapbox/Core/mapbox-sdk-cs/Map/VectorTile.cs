//-----------------------------------------------------------------------
// <copyright file="VectorTile.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mapbox.Platform;
using Mapbox.Unity;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEngine;

namespace Mapbox.Map
{
	using System.Collections.ObjectModel;
	using Mapbox.Utils;
	using Mapbox.VectorTile;
	using Mapbox.VectorTile.ExtensionMethods;
	using System;

	/// <summary>
	///    A decoded vector tile, as specified by the
	///    <see href="https://www.mapbox.com/vector-tiles/specification/">
	///    Mapbox Vector Tile specification</see>.
	///    See available layers and features <see href="https://www.mapbox.com/vector-tiles/mapbox-streets-v7/">here</see>.
	///    The tile might be incomplete if the network request and parsing are still pending.
	/// </summary>
	///  <example>
	/// Making a VectorTile request:
	/// <code>
	/// var parameters = new Tile.Parameters();
	/// parameters.Fs = MapboxAccess.Instance;
	/// parameters.Id = new CanonicalTileId(_zoom, _tileCoorindateX, _tileCoordinateY);
	/// parameters.TilesetId = "mapbox.mapbox-streets-v7";
	/// var vectorTile = new VectorTile();
	///
	/// // Make the request.
	/// vectorTile.Initialize(parameters, (Action)(() =>
	/// {
	/// 	if (!string.IsNullOrEmpty(vectorTile.Error))
	/// 	{
	///			// Handle the error.
	///		}
	///
	/// 	// Consume the <see cref="Data"/>.
	///	}));
	/// </code>
	/// </example>
	public sealed class VectorTile : Tile
	{
		public Action<bool> DataProcessingFinished = (success) => { };
		// FIXME: Namespace here is very confusing and conflicts (sematically)
		// with his class. Something has to be renamed here.
		private Mapbox.VectorTile.VectorTile data;

		bool _isStyleOptimized = false;

		string _optimizedStyleId;

		string _modifiedDate;

		private bool isDisposed = false;

		private byte[] byteData;
		private TaskWrapper _task;

		public byte[] ByteData
		{
			get { return this.byteData; }
		}

		/// <summary> Gets the vector decoded using Mapbox.VectorTile library. </summary>
		/// <value> The GeoJson data. </value>
		public Mapbox.VectorTile.VectorTile Data
		{
			get
			{
				return this.data;
			}
		}

		public VectorTile()
		{
			_isStyleOptimized = false;
		}

		public VectorTile(CanonicalTileId tileId, string tilesetId) : base(tileId, tilesetId)
		{

		}

		public VectorTile(CanonicalTileId tileId, string tilesetId, string styleId, string modifiedDate) : base(tileId, tilesetId)
		{
			if (string.IsNullOrEmpty(styleId) || string.IsNullOrEmpty(modifiedDate))
			{
				UnityEngine.Debug.LogWarning("Style Id or Modified Time cannot be empty for style optimized tilesets. Switching to regular tilesets!");
				_isStyleOptimized = false;
			}
			else
			{
				_isStyleOptimized = true;
				_optimizedStyleId = styleId;
				_modifiedDate = modifiedDate;
			}
		}

		internal override void Initialize(IFileSource fileSource, CanonicalTileId canonicalTileId, string tilesetId, Action p)
		{
			Cancel();

			TileState = TileState.Loading;
			//Id = canonicalTileId;
			_callback = p;
			TilesetId = tilesetId;

			_unityRequest = fileSource.MapboxImageRequest(MakeTileResource(tilesetId).GetUrl(), HandleTileResponse, 2);
		}

		public override void Cancel()
		{
			base.Cancel();
			if (_task != null)
			{
				MapboxAccess.Instance.TaskManager.CancelTask(_task);
			}
		}

		private void HandleTileResponse(TextureResponse response)
		{
			_unityRequest = null;
			//unlike rastertile, we are able to null this field at the beginning of this method
			//as the data we need is stored in the response anyway and we are pretty much done with
			//request object.
			//to understand why we need to null the request object,
			//please check RasterTile.cs HandleTileResponse


			//callback has to be called here
			//otherwise requests are never complete (success or failure) and pipeline gets blocked
			if (response.HasError)
			{
				TileState = TileState.Canceled;
				foreach (var exception in response.Exceptions)
				{
					AddException(exception);
				}

				if (_callback != null)
				{
					_callback();
				}
			}
			else
			{
				StatusCode = response.StatusCode;
				ETag = response.ETag;
				ExpirationDate = response.ExpirationDate;
				// only try to parse if request was successful
				byteData = response.Data;


				_task = new TaskWrapper(Id.GenerateKey(TilesetId, "VectorTile"))
				{
					OwnerTileId = Id,
					TileId = Id,
					Action = () =>
					{
						ParseTileData(byteData);
						TileState = TileState.Loaded;
					},
					ContinueWith = (t) =>
					{
						AddLog("processing finished");
						// Cancelled is not the same as loaded!
						if (TileState != TileState.Canceled)
						{
							TileState = TileState.Loaded;
							DataProcessingFinished(true);
						}
						else
						{
							DataProcessingFinished(false);
						}

						_task = null;
					},
					OnCancelled = () =>
					{
						TileState = TileState.Canceled;
						DataProcessingFinished(false);
						_task = null;
					},
#if UNITY_EDITOR
					Info = "VectorTile.HandleTileResponse"
#endif
				};

				TileState = TileState.Processing;
				MapboxAccess.Instance.TaskManager.AddTask(_task, 0);
				//first add the parsing task,
				//THEN call the callback which will lead into caching etc.
				_callback();
			}
		}

		public void SetByteData(byte[] newData)
		{
			byteData = newData;
			ParseTileData(byteData);
			TileState = TileState.Loaded;
		}

		internal override bool ParseTileData(byte[] newData)
		{
			var decompressed = Compression.Decompress(newData);
			data = new Mapbox.VectorTile.VectorTile(decompressed);
			return true;
		}

		//TODO: change signature if 'VectorTile' class changes from 'sealed'
		//protected override void Dispose(bool disposeManagedResources)
		public void Dispose(bool disposeManagedResources)
		{
			if (!isDisposed)
			{
				if (disposeManagedResources)
				{
					//TODO implement IDisposable with Mapbox.VectorTile.VectorTile
					if (null != data)
					{
						data = null;
					}
				}
			}
		}

		/// <summary>
		/// <para>Gets the vector in a GeoJson format.</para>
		/// <para>
		/// This method should be avoided as it fully decodes the whole tile and might pose performance and memory bottle necks.
		/// </para>
		/// </summary>
		/// <value> The GeoJson data. </value>
		/// <example>
		/// Inspect the GeoJson.
		/// <code>
		/// var json = VectorTile.GeoJson;
		/// Console.Write("GeoJson: " + json);
		/// </code>
		/// </example>
		public string GeoJson
		{
			get
			{
				return this.data.ToGeoJson((ulong)Id.Z, (ulong)Id.X, (ulong)Id.Y, 0);
			}
		}

		/// <summary>
		/// Gets all availble layer names.
		/// See available layers and features <see href="https://www.mapbox.com/vector-tiles/mapbox-streets-v7/">here</see>.
		/// </summary>
		/// <returns>Collection of availble layers.</returns>
		/// <example>
		/// Inspect the LayerNames.
		/// <code>
		/// var layerNames = vectorTile.LayerNames();
		/// foreach (var layer in layerNames)
		/// {
		/// 	Console.Write("Layer: " + layer);
		/// }
		/// </code>
		/// </example>
		public ReadOnlyCollection<string> LayerNames()
		{
			return this.data.LayerNames();
		}

		// FIXME: Why don't these work?
		/// <summary>
		/// Decodes the requested layer.
		/// </summary>
		/// <param name="layerName">Name of the layer to decode.</param>
		/// <returns>Decoded VectorTileLayer or 'null' if an invalid layer name was specified.</returns>
		/// <example>
		/// Inspect a layer of the vector tile.
		/// <code>
		/// var countryLabelLayer = vectorTile.GetLayer("country_label");
		/// var count = countryLabelLayer.Keys.Count;
		/// for (int i = 0; i &lt; count; i++)
		/// {
		/// 	Console.Write(string.Format("{0}:{1}", countryLabelLayer.Keys[i], countryLabelLayer.Values[i]));
		/// }
		/// </code>
		/// </example>
		public VectorTileLayer GetLayer(string layerName)
		{
			return this.data.GetLayer(layerName);
		}


		internal override TileResource MakeTileResource(string tilesetId)
		{

			return (_isStyleOptimized) ?
				TileResource.MakeStyleOptimizedVector(Id, tilesetId, _optimizedStyleId, _modifiedDate)
			  : TileResource.MakeVector(Id, tilesetId);
		}

		public void SetVectorFromCache(VectorTile vectorTile)
		{
			data = vectorTile.Data;
			TileState = TileState.Loaded;
		}
	}
}

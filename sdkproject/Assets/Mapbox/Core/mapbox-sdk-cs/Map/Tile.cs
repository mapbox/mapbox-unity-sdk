//-----------------------------------------------------------------------
// <copyright file="Tile.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Assets.Mapbox.Unity.MeshGeneration.Modifiers.MeshModifiers;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;

namespace Mapbox.Map
{
	using System;
	using Mapbox.Platform;
	using System.Linq;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using Mapbox.Unity.Utilities;

	public enum CacheType
	{
		MemoryCache,
		FileCache,
		SqliteCache,
		NoCache
	}

	public enum TileState
	{
		/// <summary> New tile, not yet initialized. </summary>
		New,

		/// <summary> Loading data. </summary>
		Loading,

		/// <summary> Data loaded and parsed. </summary>
		Loaded,

		/// <summary> Data loading cancelled. </summary>
		Canceled,

		/// <summary> Data has been loaded before and got updated. </summary>
		Updated,

		Processing,
		Destroyed
	}

	/// <summary>
	///    A Map tile, a square with vector or raster data representing a geographic
	///    bounding box. More info <see href="https://en.wikipedia.org/wiki/Tiled_web_map">
	///    here </see>.
	/// </summary>
	[Serializable]
	public abstract class Tile : IAsyncRequest
	{
#if UNITY_EDITOR
		public bool IsMapboxTile = false;
		public CacheType FromCache = CacheType.NoCache;
#endif

		public Action Cancelled = () => { };

		public string TilesetId;
		public CanonicalTileId Id;
		private int _key = 0;

		public int Key
		{
			get
			{
				if (_key == 0)
				{
					_key = Id.GenerateKey(TilesetId);
				}

				return _key;
			}
		}

		public long StatusCode;
		public DateTime ExpirationDate;
		public string ETag;

		protected HashSet<CanonicalTileId> _userTiles = new HashSet<CanonicalTileId>();
		protected List<Exception> _exceptions;
		protected TileState TileState = TileState.New;
		protected IAsyncRequest _request;

		protected UnityWebRequest _unityRequest;
		protected Action _callback;

		protected List<string> _logs;

		protected Tile()
		{

		}

		protected Tile(CanonicalTileId tileId, string tilesetId)
		{
			TilesetId = tilesetId;
			Id = tileId;
#if DEBUG
			_logs = new List<string>();
#endif
		}

		/// <summary> Gets the <see cref="T:Mapbox.Map.CanonicalTileId"/> identifier. </summary>
		/// <value> The canonical tile identifier. </value>

		/// <summary>Flag to indicate if the request was successful</summary>
		public bool HasError
		{
			get { return _exceptions == null ? false : _exceptions.Count > 0; }
		}


		/// <summary> Exceptions that might have occured during creation of the tile. </summary>
		public ReadOnlyCollection<Exception> Exceptions
		{
			get { return null == _exceptions ? null : _exceptions.AsReadOnly(); }
		}


		/// <summary> Messages of exceptions otherwise empty string. </summary>
		public string ExceptionsAsString
		{
			get
			{
				if (null == _exceptions || _exceptions.Count == 0)
				{
					return string.Empty;
				}

				return string.Join(Environment.NewLine, _exceptions.Select(e => e.Message).ToArray());
			}
		}


		/// <summary>
		/// Sets the error message.
		/// </summary>
		/// <param name="errorMessage"></param>
		internal void AddException(Exception ex)
		{
			if (null == _exceptions)
			{
				_exceptions = new List<Exception>();
			}

			_exceptions.Add(ex);
		}


		/// <summary>
		///     Gets the current state. When fully loaded, you must
		///     check if the data actually arrived and if the tile
		///     is accusing any error.
		/// </summary>
		/// <value> The tile state. </value>
		public TileState CurrentTileState
		{
			get { return TileState; }
		}


		public HttpRequestType RequestType
		{
			get { return _request.RequestType; }
		}


		public bool IsCompleted
		{
			get { return TileState == TileState.Loaded; }
		}

		/// <summary>
		///     Initializes the <see cref="T:Mapbox.Map.Tile"/> object. It will
		///     start a network request and fire the callback when completed.
		/// </summary>
		/// <param name="param"> Initialization parameters. </param>
		/// <param name="callback"> The completion callback. </param>
		public void Initialize(Parameters param, Action callback)
		{
			Initialize(param.Fs, param.Id, param.TilesetId, callback);
		}

		internal virtual void Initialize(IFileSource fileSource, CanonicalTileId canonicalTileId, string tilesetId, Action p)
		{
			Cancel();

			TileState = TileState.Loading;
			//Id = canonicalTileId;
			_callback = p;
			TilesetId = tilesetId;

			_request = fileSource.Request(MakeTileResource(tilesetId).GetUrl(), HandleTileResponse);
		}

		/// <summary>
		///     Returns a <see cref="T:System.String"/> that represents the current
		///     <see cref="T:Mapbox.Map.Tile"/>.
		/// </summary>
		/// <returns>
		///     A <see cref="T:System.String"/> that represents the current
		///     <see cref="T:Mapbox.Map.Tile"/>.
		/// </returns>
		public override string ToString()
		{
			return Id.ToString();
		}


		/// <summary>
		///     Cancels the request for the <see cref="T:Mapbox.Map.Tile"/> object.
		///     It will stop a network request and set the tile's state to Canceled.
		/// </summary>
		/// <example>
		/// <code>
		/// // Do not request tiles that we are already requesting
		///	// but at the same time exclude the ones we don't need
		///	// anymore, cancelling the network request.
		///	tiles.RemoveWhere((T tile) =>
		///	{
		///		if (cover.Remove(tile.Id))
		///		{
		///			return false;
		///		}
		///		else
		///		{
		///			tile.Cancel();
		///			NotifyNext(tile);
		///			return true;
		/// 	}
		///	});
		/// </code>
		/// </example>
		public virtual void Cancel()
		{
			if (_request != null)
			{
				_request.Cancel();
				_request = null;
			}

			if (_unityRequest != null)
			{
				_unityRequest.Abort();
				_unityRequest = null;
			}

			TileState = TileState.Canceled;
			Cancelled();
		}

		// Get the tile resource (raster/vector/etc).
		internal abstract TileResource MakeTileResource(string tilesetId);


		// Decode the tile.
		internal abstract bool ParseTileData(byte[] newData);


		// TODO: Currently the tile decoding is done on the main thread. We must implement
		// a Worker class to abstract this, so on platforms that support threads (like Unity
		// on the desktop, Android, etc) we can use worker threads and when building for
		// the browser, we keep it single-threaded.

		private void HandleTileResponse(Response response)
		{
			if (response.HasError)
			{
				response.Exceptions.ToList().ForEach(e => AddException(e));
			}
			else
			{
				// only try to parse if request was successful

				// current implementation doesn't need to check if parsing is successful:
				// * Mapbox.Map.VectorTile.ParseTileData() already adds any exception to the list
				// * Mapbox.Map.RasterTile.ParseTileData() doesn't do any parsing

				ParseTileData(response.Data);
			}

			// Cancelled is not the same as loaded!
			if (TileState != TileState.Canceled)
			{
				if (response.IsUpdate)
				{
					TileState = TileState.Updated;
				}
				else
				{
					TileState = TileState.Loaded;
				}
			}

			_callback();
		}

		public virtual void Clear()
		{
			if (_request != null)
			{
				_request.Cancel();
				_request = null;
			}

			if (_unityRequest != null)
			{
				_unityRequest.Abort();
				_unityRequest = null;
			}
		}

		/// <summary>
		///    Parameters for initializing a Tile object.
		/// </summary>
		/// <example>
		/// <code>
		/// var parameters = new Tile.Parameters();
		/// parameters.Fs = MapboxAccess.Instance;
		/// parameters.Id = new CanonicalTileId(_zoom, _tileCoorindateX, _tileCoordinateY);
		/// parameters.TilesetId = "mapbox.mapbox-streets-v7";
		/// </code>
		/// </example>
		public struct Parameters
		{
			/// <summary> The tile id. </summary>
			public CanonicalTileId Id;

			/// <summary>
			///     The tileset ID, usually in the format "user.mapid". Exceptionally,
			///     <see cref="T:Mapbox.Map.RasterTile"/> will take the full style URL
			///     from where the tile is composited from, like mapbox://styles/mapbox/streets-v9.
			/// </summary>
			public string TilesetId;

			/// <summary> The data source abstraction. </summary>
			public IFileSource Fs;
		}

		public void Prune()
		{
			TileState = TileState.Destroyed;
		}

#region UserTracking
		public void AddUser(CanonicalTileId tileId)
		{
			if (!_userTiles.Contains(tileId))
			{
				_userTiles.Add(tileId);
			}
		}
		public void RemoveUser(CanonicalTileId tileId)
		{
			if (_userTiles.Contains(tileId))
			{
				_userTiles.Remove(tileId);
			}
		}
		public bool UsedByTile(CanonicalTileId tileId)
		{
			return _userTiles.Contains(tileId);
		}
		public bool IsInUse()
		{
			return _userTiles.Count > 0;
		}
		public HashSet<CanonicalTileId> GetUsers()
		{
			return _userTiles;
		}
		public string UsersCSV()
		{
			return string.Join(" | ", _userTiles);
		}
#endregion


#region Logs
		public List<string> GetLogs => _logs;
		public void AddLog(string text)
		{
	#if DEBUG
			_logs.Add(text);
	#endif
		}

		public void AddLog(string text, CanonicalTileId relatedTileId)
		{
	#if DEBUG
			_logs.Add(string.Format("{0} - {1}", text, relatedTileId));
	#endif
		}
#endregion

	}
}
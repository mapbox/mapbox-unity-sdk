using System;
using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Unity.DataContainers;
using Mapbox.Unity.Map.Interfaces;
using Mapbox.Utils;
using UnityEngine;

namespace Mapbox.Unity.Map.TileProviders
{
	public enum ZoomState
	{
		ZoomIn,
		NoChange,
		ZoomOut

	}
	public class ExtentArgs : EventArgs
	{
		//TODO: Override GetHashCode for UnwrappedTileId
		public HashSet<UnwrappedTileId> ActiveTiles;

		public Dictionary<UnwrappedTileId, UnwrappedTileId> ZoomOutTileRelationships;
		public Dictionary<UnwrappedTileId, UnwrappedTileId> ZoomInTileRelationships;
		public ZoomState ZoomState = ZoomState.NoChange;
		public Vector2dBounds Bounds;
	}

	public abstract class AbstractTileProvider : MonoBehaviour, ITileProvider
	{
		public event EventHandler<ExtentArgs> ExtentChanged;

		protected IMap _map;
		protected ExtentArgs _currentExtent = new ExtentArgs();

		protected ITileProviderOptions _options;
		public ITileProviderOptions Options
		{
			get
			{
				return _options;
			}
		}

		public virtual void Initialize(IMap map)
		{
			_map = map;
			OnInitialized();
		}

		public virtual void OnExtentChanged()
		{
			if (ExtentChanged != null)
			{
				ExtentChanged(this, _currentExtent);
			}
		}

		public abstract void OnInitialized();
		public abstract void UpdateTileExtent();

		public abstract bool Cleanup(UnwrappedTileId tile);

		public virtual void SetOptions(ITileProviderOptions options)
		{
			_options = options;
		}

		public virtual void UpdateTileProvider()
		{

		}
	}
}

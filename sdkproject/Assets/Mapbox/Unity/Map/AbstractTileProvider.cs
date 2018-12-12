namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Map;
	using System.Linq;

	public class ExtentArgs : EventArgs
	{
		//TODO: Override GetHashCode for UnwrappedTileId
		public HashSet<UnwrappedTileId> activeTiles;
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
			ExtentChanged(this, _currentExtent);
		}

		public abstract void OnInitialized();
		public abstract void UpdateTileExtent();

		public virtual void SetOptions(ITileProviderOptions options)
		{
			_options = options;
		}
	}
}
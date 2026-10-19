using System;
using System.ComponentModel;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.VectorModule.Filters
{
	public abstract class FilterBaseObject : ScriptableObject, IFilterObject
	{
		[SerializeField, HideInInspector] private bool m_Active = true;

		/// <summary>
		/// Whether this filter takes part in its stack. The filter-stack Inspector draws a toggle
		/// for it; nothing read it at runtime, so unticking a filter left it running. Read when the
		/// stack is built rather than per feature, so an inactive filter costs nothing.
		/// </summary>
		public bool Active => m_Active;

		public abstract ILayerFeatureFilterComparer Filter { get; }
	}
	
	public abstract class FilterBase : ILayerFeatureFilterComparer
	{
		

		public virtual void Initialize()
		{
			
		}
		
		public virtual bool Try(VectorFeatureUnity feature)
		{
			return true;
		}
	}

	public interface IFilterObject
	{
		ILayerFeatureFilterComparer Filter { get; }
	}
}
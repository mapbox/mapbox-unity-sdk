using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace Mapbox.Platform
{
	public class TextureResponse
	{
		public Texture2D Texture2D;

		private List<Exception> _exceptions;
		/// <summary> Exceptions that might have occured during the request. </summary>
		public ReadOnlyCollection<Exception> Exceptions
		{
			get { return null == _exceptions ? null : _exceptions.AsReadOnly(); }
		}

		/// <summary> Messages of exceptions otherwise empty string. </summary>
		public string ExceptionsAsString
		{
			get
			{
				if (null == _exceptions || _exceptions.Count == 0) { return string.Empty; }
				return string.Join(Environment.NewLine, _exceptions.Select(e => e.Message).ToArray());
			}
		}

		public bool LoadedFromCache = false;
		public long StatusCode;

		public bool RateLimitHit
		{
			get { return StatusCode == 429; }
		}

		public bool HasError
		{
			get { return _exceptions == null ? false : _exceptions.Count > 0; }
		}

		public void AddException(Exception exception)
		{
			if (null == _exceptions) { _exceptions = new List<Exception>(); }
			_exceptions.Add(exception);
		}
	}
}
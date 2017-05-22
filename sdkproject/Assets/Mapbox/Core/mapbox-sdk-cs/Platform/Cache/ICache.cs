using System.Collections;
using System.Collections.Generic;


namespace Mapbox.Platform.Cache
{


	public interface ICache
	{

		void Add(string key, byte[] data);

		byte[] Get(string key);

		void Clear();

	}
}
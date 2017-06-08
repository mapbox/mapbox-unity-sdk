//-----------------------------------------------------------------------
// <copyright file="Compression.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Utils
{

	using System.IO;
	using Mapbox.IO.Compression;


	/// <summary> Collection of constants used across the project. </summary>
	public static class Compression
	{
		/// <summary>
		///     Decompress the specified buffer previously compressed using GZip.
		/// </summary>
		/// <param name="buffer">
		///     The GZip'ed buffer.
		/// </param>
		/// <returns>
		///     Returns the uncompressed buffer or the buffer in case decompression
		///     is not possible.
		/// </returns>
		public static byte[] Decompress(byte[] buffer)
		{
			// Test for magic bits.
			if (buffer.Length < 2 || buffer[0] != 0x1f || buffer[1] != 0x8b)
			{
				return buffer;
			}

			using (GZipStream stream = new GZipStream(new MemoryStream(buffer), CompressionMode.Decompress))
			{
				const int Size = 4096; // Pagesize.
				byte[] buf = new byte[Size];

				using (MemoryStream memory = new MemoryStream())
				{
					int count = 0;

					do
					{
						try
						{
							count = stream.Read(buf, 0, Size);
						}
						catch
						{
							// For now we return the uncompressed buffer
							// on error. Assumes the magic check passed
							// by luck.
							return buffer;
						}

						if (count > 0)
						{
							memory.Write(buf, 0, count);
						}
					}
					while (count > 0);

					buffer = memory.ToArray();
				}
			}

			return buffer;
		}


		public static byte[] Compress(byte[] raw)
		{
			using (MemoryStream memory = new MemoryStream())
			{
				using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
				{
					gzip.Write(raw, 0, raw.Length);
				}
				return memory.ToArray();
			}
		}

		public static byte[] Compress2(byte[] buffer)
		{
			if (buffer == null)
			{
				throw new System.ArgumentNullException("buffer is null");
			}

			int BUFFER_SIZE = 64 * 1024; //64kB

			using (var ms = new MemoryStream())
			{
				using (var gzs = new BufferedStream(new GZipStream(ms, CompressionMode.Compress), BUFFER_SIZE))
				{
					gzs.Write(buffer, 0, buffer.Length);
				}
				return ms.ToArray();
			}
		}




	}
}
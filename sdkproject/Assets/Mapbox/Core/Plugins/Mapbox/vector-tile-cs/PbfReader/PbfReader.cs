using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Mapbox.VectorTile.Contants;


namespace Mapbox.VectorTile
{


	//TODO: implement DataView using the same byte array instead of copying byte arrays
	//public struct DataView {
	//	public ulong start;
	//	public ulong end;
	//}


	/// <summary>
	/// Low level protobuf (PBF) decoder https://developers.google.com/protocol-buffers/docs/overview
	/// </summary>
	public class PbfReader
	{


		/// <summary>Tag at current position</summary>
		public int Tag { get; private set; }
		/// <summary>Value at current position</summary>
		public ulong Value { get; private set; }
		//public ulong Pos { get; private set; }
		/// <summary>Wire type at current position</summary>
		public WireTypes WireType { get; private set; }


		private byte[] _buffer;
		private ulong _length;
		private ulong _pos;


		/// <summary>
		/// PbfReader constructor
		/// </summary>
		/// <param name="tileBuffer">Byte array containing the raw (already unzipped) tile data</param>
		public PbfReader(byte[] tileBuffer)
		{
			_buffer = tileBuffer;
			_length = (ulong)_buffer.Length;
			WireType = WireTypes.UNDEFINED;
		}


		/// <summary>
		/// <para>Gets Varint at current position, moves to position after Varint.</para>
		/// <para>Throws exception if Varint cannot be decoded</para>
		/// </summary>
		/// <returns>Decoded Varint</returns>
		public long Varint()
		{
			// convert to base 128 varint
			// https://developers.google.com/protocol-buffers/docs/encoding
			int shift = 0;
			long result = 0;
			while (shift < 64)
			{
				byte b = _buffer[_pos];
				result |= (long)(b & 0x7F) << shift;
				_pos++;
				if ((b & 0x80) == 0)
				{
					return result;
				}
				shift += 7;
			}
			throw new System.ArgumentException("Invalid varint");

		}


		/// <summary>
		/// <para>Get a view into the buffer.</para>
		/// <para>TODO: refactor to return a DataView instead of a byte array</para>
		/// </summary>
		/// <returns>Byte array containing the view</returns>
		public byte[] View()
		{
			// return layer/feature subsections of the main stream
			if (Tag == 0)
			{
				throw new System.Exception("call next() before accessing field value");
			};
			if (WireType != WireTypes.BYTES)
			{
				throw new System.Exception("not of type string, bytes or message");
			}

			ulong skipBytes = (ulong)Varint();
			SkipBytes(skipBytes);

			byte[] buf = new byte[skipBytes];
			System.Array.Copy(_buffer, (int)_pos - (int)skipBytes, buf, 0, (int)skipBytes);

			return buf;
		}


		/// <summary>
		/// Get repeated `uint`s a current position, move position
		/// </summary>
		/// <returns>List of decoded `uint`s</returns>
		public List<uint> GetPackedUnit32()
		{
			List<uint> values = new List<uint>(200);
			ulong sizeInByte = (ulong)Varint();
			ulong end = _pos + sizeInByte;
			while (_pos < end)
			{
				values.Add((uint)Varint());
			}
			return values;
		}


		public List<int> GetPackedSInt32()
		{
			List<int> values = new List<int>(200);
			ulong sizeInByte = (ulong)Varint();
			ulong end = _pos + sizeInByte;
			while (_pos < end)
			{
				values.Add(decodeZigZag32((int)Varint()));
			}
			return values;
		}


		public List<long> GetPackedSInt64()
		{
			List<long> values = new List<long>(200);
			ulong sizeInByte = (ulong)Varint();
			ulong end = _pos + sizeInByte;
			while (_pos < end)
			{
				values.Add(decodeZigZag64((long)Varint()));
			}
			return values;
		}


		private int decodeZigZag32(int value)
		{
			return (value >> 1) ^ -(value & 1);
		}


		private long decodeZigZag64(long value)
		{
			return (value >> 1) ^ -(value & 1);
		}


		/// <summary>
		/// Get double at current position, move to next position
		/// </summary>
		/// <returns>Decoded double</returns>
		public double GetDouble()
		{
			byte[] buf = new byte[8];
			System.Array.Copy(_buffer, (int)_pos, buf, 0, 8);
			_pos += 8;
			double dblVal = System.BitConverter.ToDouble(buf, 0);
			return dblVal;
		}


		/// <summary>
		/// Get float a current position, move to next position
		/// </summary>
		/// <returns>Decoded float</returns>
		public float GetFloat()
		{
			byte[] buf = new byte[4];
			System.Array.Copy(_buffer, (int)_pos, buf, 0, 4);
			_pos += 4;
			float snglVal = System.BitConverter.ToSingle(buf, 0);
			return snglVal;
		}


		/// <summary>
		/// Get bytes as string
		/// </summary>
		/// <param name="length">Number of bytes to read</param>
		/// <returns>Decoded string</returns>
		public string GetString(ulong length)
		{
			byte[] buf = new byte[length];
			System.Array.Copy(_buffer, (int)_pos, buf, 0, (int)length);
			_pos += length;
			return Encoding.UTF8.GetString(buf, 0, buf.Length);
		}


		/// <summary>
		/// Move to next byte and set wire type. Throws exeception if tag is out of range
		/// </summary>
		/// <returns>Returns false if at end of buffer</returns>
		public bool NextByte()
		{
			if (_pos >= _length)
			{
				return false;
			}
			// get and process the next byte in the buffer
			// return true until end of stream
			Value = (ulong)Varint();
			Tag = (int)Value >> 3;
			if (
				(Tag == 0 || Tag >= 19000)
				&& (Tag > 19999 || Tag <= ((1 << 29) - 1))
			)
			{
				throw new System.Exception("tag out of range");
			}
			WireType = (WireTypes)(Value & 0x07);
			return true;
		}


		/// <summary>
		/// Skip over a Varint
		/// </summary>
		public void SkipVarint()
		{
			Varint();
			//while (0 == (_buffer[Pos] & 0x80))
			//{
			//    Pos++;
			//    if (Pos >= _length)
			//    {
			//        throw new Exception("Truncated message.");
			//    }
			//}

			//if (Pos > _length)
			//{
			//    throw new Exception("Truncated message.");
			//}
		}


		/// <summary>
		/// Skip bytes
		/// </summary>
		/// <param name="skip">Number of bytes to skip</param>
		public void SkipBytes(ulong skip)
		{
			if (_pos + skip > _length)
			{
				string msg = string.Format(NumberFormatInfo.InvariantInfo, "[SkipBytes()] skip:{0} pos:{1} len:{2}", skip, _pos, _length);
				throw new System.Exception(msg);
			}
			_pos += skip;
		}


		/// <summary>
		/// Automatically skip bytes based on wire type
		/// </summary>
		/// <returns>New position within the byte array</returns>
		public ulong Skip()
		{
			if (Tag == 0)
			{
				throw new System.Exception("call next() before calling skip()");
			}

			switch (WireType)
			{
				case WireTypes.VARINT:
					SkipVarint();
					break;
				case WireTypes.BYTES:
					SkipBytes((ulong)Varint());
					break;
				case WireTypes.FIXED32:
					SkipBytes(4);
					break;
				case WireTypes.FIXED64:
					SkipBytes(8);
					break;
				case WireTypes.UNDEFINED:
					throw new System.Exception("undefined wire type");
				default:
					throw new System.Exception("unknown wire type");
			}

			return _pos;
		}



	}
}

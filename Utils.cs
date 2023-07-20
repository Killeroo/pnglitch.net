using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace pnglitch
{
    public static class Utils
    {
		/// <summary>
		/// Gets the byte value of a uint in big endian 
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] ToBigEndianBytes(uint value)
		{
			return new byte[]{
				(byte)((value>>24)&255),
				(byte)((value>>16)&255),
				(byte)((value>>8)&255),
				(byte)((value>>0)&255)
			};
		}

		/// <summary>
		/// Converts a number in big endian bytes to little endian
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static uint FromBigEndianBytes(byte[] data)
        {
			// TODO: Im not actually sure this is little endian just check later
			// im lazy and this manual method works
			data = data.Reverse().ToArray();
			return BitConverter.ToUInt32(data, 0);
		}
	}
}

﻿/*
 *
 * This file is in the public domain.
 *
 */
using System;
using System.IO;
using System.IO.Compression;

namespace PeterO
{
	/// <summary>
	/// A simple class for encoding PNG image files.
	/// https://stackoverflow.com/questions/24082305/how-is-png-crc-calculated-exactly
	/// https://web.archive.org/web/20150825201508/http://upokecenter.dreamhosters.com/articles/png-image-encoder-in-c/
	/// </summary>
	public class Png
	{
		private byte[] Adler32(byte[] stream, int offset,
							   int length)
		{
			var adler = 1;
			var len = length;
			var NMAX = 3854;
			var BASE = 65521;
			var s1 = adler & 0xffff;
			var s2 = ((adler & 0xffff0000) >> 16) & 0xFFFF;
			var k = 0;
			var bpos = offset;
			while (len > 0)
			{
				k = len < NMAX ? len : NMAX;
				len -= k;
				while (k > 0)
				{
					s1 = unchecked((int)s1 + stream[bpos]);
					s2 = unchecked((int)s2 + s1);
					bpos += 1;
					k -= 1;
				}
				s1 = s1 % BASE;
				s2 = s2 % BASE;
			}
			return new byte[]{(byte)(s2>>8),
				(byte)(s2&255),
				(byte)(s1>>8),
				(byte)(s1&255)
			};
		}

		byte[] subdata1;
		uint[] crcTable;

		private uint Crc32(byte[] stream, int offset, int length, uint crc)
		{
			uint c;
			if (crcTable == null)
			{
				crcTable = new uint[256];
				for (uint n = 0; n <= 255; n++)
				{
					c = n;
					for (var k = 0; k <= 7; k++)
					{
						if ((c & 1) == 1)
							c = 0xEDB88320 ^ ((c >> 1) & 0x7FFFFFFF);
						else
							c = ((c >> 1) & 0x7FFFFFFF);
					}
					crcTable[n] = c;
				}
			}


			c = crc ^ 0xffffffff;
			var endOffset = offset + length;
			for (var i = offset; i < endOffset; i++)
			{
				c = crcTable[(c ^ stream[i]) & 255] ^ ((c >> 8) & 0xFFFFFF);
			}
			return c ^ 0xffffffff;
		}

		/// Sets the PNG filter for the current row.
		public void SetFilter(int y, byte filter)
		{
			this.data[y * this.realRowSize] = filter;
		}

		/// Gets the PNG filter for the current row.
		public byte GetFilter(int y)
		{
			return this.data[y * this.realRowSize];
		}

		///
		///  Sets the pixel located at the X and Y coordinates of the
		///  image.  The pixel byte array is either 3 or 4 elements
		///  long and contains the red, green, blue, and optionally
		///  alpha components in that order.  If the byte array is 3
		///  elements long, sets alpha to 255.
		///  Because the row may use a PNG filter, those components may
		///  not actually represent the intensity of each color returned.
		///
		public void SetPixel(int x, int y, byte[] pixel)
		{
			if (pixel == null)
				throw new ArgumentNullException("pixel");
			if (pixel.Length >= 4)
			{
				SetPixel(x, y, pixel[0], pixel[1], pixel[2], pixel[3]);
			}
			else if (pixel.Length == 3)
			{
				SetPixel(x, y, pixel[0], pixel[1], pixel[2], (byte)255);
			}
			else
			{
				throw new ArgumentException("'pixel' has an improper length");
			}
		}

		private static int BytesPerPixel = 4;

		///
		///  Sets the pixel located at the X and Y coordinates of the
		///  image.  r, g, b are the red, green, and blue components.
		///  Sets alpha to 255.  This function should only be used if the
		///  PNG filter for the row is 0 (or not set), since this class
		///  currently does not apply PNG filters.
		///
		public void SetPixel(int x, int y, byte r, byte g, byte b)
		{
			SetPixel(x, y, r, g, b, (byte)255);
		}
		///
		///  Sets the pixel located at the X and Y coordinates of the
		///  image.  r, g, b are the red, green, and blue components.
		///  a is the alpha component.
		///  Because the row may use a PNG filter, those components may
		///  not actually represent the intensity of each color returned.
		///
		public void SetPixel(int x, int y, byte r, byte g, byte b, byte a)
		{
			if (x < 0 || x >= width) throw new ArgumentOutOfRangeException("x");
			if (y < 0 || y >= height) throw new ArgumentOutOfRangeException("y");
			int offset = (y * this.realRowSize) + (x * BytesPerPixel) + 1;
			this.data[offset] = r;
			this.data[offset + 1] = g;
			this.data[offset + 2] = b;
			if (BytesPerPixel >= 4) this.data[offset + 3] = a;
		}

		///
		///  Gets the pixel located at the X and Y coordinates of the
		///  image.  Returns a bit array containing four elements for
		///  the red, green, blue, and alpha components, in that order.
		///  Because the row may use a PNG filter, the returned data may
		///  not actually represent the intensity of each color returned.
		///
		public byte[] GetPixel(int x, int y)
		{
			if (x < 0 || x >= width) throw new ArgumentOutOfRangeException("x");
			if (y < 0 || y >= height) throw new ArgumentOutOfRangeException("y");
			int offset = (y * this.realRowSize) + (x * BytesPerPixel) + 1;
			return new byte[]{
				this.data[offset],
				this.data[offset+1],
				this.data[offset+2],
				(byte)((BytesPerPixel>=4) ? this.data[offset+3] : 255)
			};
		}

		private byte[] GetBE(uint crc)
		{
			return new byte[]{
				(byte)((crc>>24)&255),
				(byte)((crc>>16)&255),
				(byte)((crc>>8)&255),
				(byte)((crc>>0)&255)
			};
		}

		/// Saves the image to a file.
		public void Save(string filename)
		{
			using (FileStream fs = new FileStream(filename, FileMode.Create))
			{


				// Compress image data
				byte[] deflated = null;
				using (MemoryStream ms = new MemoryStream())
				{
					// PNG compression uses a ZLIB stream not a DEFLATE stream

					// Write Deflate signature
					ms.WriteByte(0x78);
					ms.WriteByte(0x9c);

					// Write compressed data
					using (DeflateStream ds = new DeflateStream(ms,
															 CompressionMode.Compress, true))
					{
						ds.Write(this.data, 0, this.data.Length);
					}
					// zlib checksum of the uncompressed data (used to confirm decompression)
					ms.Write(Adler32(this.data, 0, this.data.Length), 0, 4);
					deflated = ms.ToArray();
				}
				// Big edian length
				byte[] defLength = new byte[]{
					(byte)((deflated.Length>>24)&255),
					(byte)((deflated.Length>>16)&255),
					(byte)((deflated.Length>>8)&255),
					(byte)((deflated.Length>>0)&255)
				};

				// ----- Write File signature + header chunk 
				fs.Write(this.subdata1, 0, this.subdata1.Length);
				uint crc32 = Crc32(subdata1, 12, 17, 0);
				fs.Write(GetBE(crc32), 0, 4);


				// ---- Write data chunk

				// Length
				fs.Write(defLength, 0, defLength.Length);

				// Type
				fs.Write(new byte[]{
							 0x49,0x44,0x41,0x54
						 }, 0, 4);


				// Data
				fs.Write(deflated, 0, deflated.Length);
				uint crc = Crc32(deflated, 0, deflated.Length, this.idatCrc);
				byte[] subdcrc = GetBE(crc);

				// CRC
				fs.Write(subdcrc, 0, subdcrc.Length);


				// ---- Write end chunk
				fs.Write(subdata2, 0, subdata2.Length);
			}
		}

		int width, height, realRowSize, blockSize, rowSize;

		/// Gets the height of the image.
		public int Height {
			get { return height; }
		}

		/// Gets the width of the image.
		public int Width {
			get { return width; }
		}
		byte[] imageData, data, subdata2;
		uint idatCrc;

		/// Creates a new PNG image with the given
		/// width and height.
		public Png(int width, int height)
		{
			if (width > 65535 || width <= 0)
				throw new ArgumentOutOfRangeException("width");
			if (height > 65535 || height <= 0)
				throw new ArgumentOutOfRangeException("height");


			// Hardcoded PNG file start
			// Signature + Header chunk
			subdata1 = new byte[]{ 

				// ------- File signature
				0x89, // - (High bit)
				0x50, // P
				0x4e, // N
				0x47, // G
				0x0d, // CR (Carrige return DOS)
				0x0a, // LF (Carrige return DOS)
				0x1a, // - (do not display bit
				0x0a, // LF (Line ending Linus 


				// ------- 1st chunk (header)
				
				// == Length (13)
				// (4 bytes - big edian)
				0,
				0,
				0,
				0xd,
				
				// == Type (iHDR)
				// (4 bytes, case sensitive ascii)
				0x49,
				0x48,
				0x44,
				0x52,

				// == Chunk data (Header)
				// (13 bytes)

				// Width
				0,
				0,
				(byte)(width>>8),
				(byte)(width&255),

				// Height
				0,
				0,
				(byte)(height>>8),
				(byte)(height&255),


				8,
				(byte)((BytesPerPixel==4) ? 6 : 2),
				0,
				0,
				0
			};


			this.width = width;
			this.height = height;
			this.realRowSize = (this.width * BytesPerPixel) + 1;
			this.blockSize = this.realRowSize;
			this.rowSize = this.realRowSize;
			this.imageData = new byte[this.rowSize * this.height];
			this.data = this.imageData;
			idatCrc = Crc32(
				new byte[]{
					0x49,0x44,0x41,0x54
				}, 0, 4, 0);


			// Hardcoded end chunk
			subdata2 = new byte[]{
				
				// Length
				0,
				0,
				0,
				0,

				// Type (IEND)
				0x49,
				0x45,
				0x4e,
				0x44,
				
				// Crc
				0xae,
				0x42,
				0x60,
				0x82
			};
		}
	}
}
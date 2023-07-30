using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace pnglitch
{

    internal class PngHeader
    {
        public uint Width;
        public uint Height;
        public byte BitDepth;
        public byte ColorType;
        public byte CompressionMethod;
        public byte FilterMethod;
        public byte InterlaceMethod;

        public void FromBytes(byte[] headerData)
        {
            using (MemoryStream stream = new MemoryStream(headerData))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                // TODO: Add a reader extension to convert the number to little endian
                Width = Utils.FromBigEndianBytes(reader.ReadBytes(4));
                Height = Utils.FromBigEndianBytes(reader.ReadBytes(4));
                BitDepth = reader.ReadByte();
                ColorType = reader.ReadByte();
                CompressionMethod = reader.ReadByte();
                FilterMethod = reader.ReadByte();
                InterlaceMethod = reader.ReadByte();
            }

        }

        public byte[] ToBytes()
        {
            // Write header to chunk data
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(Utils.ToBigEndianBytes(Width));
                writer.Write(Utils.ToBigEndianBytes(Height));
                writer.Write(BitDepth);
                writer.Write(ColorType);
                writer.Write(CompressionMethod);
                writer.Write(FilterMethod);
                writer.Write(InterlaceMethod);

                return stream.ToArray();
            }
        }
    }


    internal enum FilterType : byte
    {
        None,
        Sub,
        Up,
        Average,
        Paeth
    }

    internal enum ColorType : byte
    {
        Indexed,
        Grayscale,
        GrascaleAndAlpha,
        Truecolor,
        TruecolorAndAlpha
    }

    internal class ImageData
    {
        public byte[] FilteredData;
        public byte[] CompressedData;
        public Scanline[] Scanlines;
        public FilterType[] ScanlineFilters;
        public uint SampleSize;
        public ColorType ColorType;

        private bool _loaded = false;
        private bool _dirty = false;

        public void Decompress(byte[] data)
        {

        }

        public byte[] Compress()
        {
            return new byte[10];
        }

        internal void Unpack(List<byte> compressedImageData, PngHeader imageInfo)
        {
            switch (imageInfo.ColorType)
            {
                case 0: SampleSize = 1; break;
                case 2: SampleSize = 3; break;
                case 3: SampleSize = 1; break;
                case 4: SampleSize = 2; break;
                case 6: SampleSize = 4; break;
            }

            using (MemoryStream destination = new MemoryStream())
            using (MemoryStream ms = new MemoryStream(compressedImageData.ToArray()))
            {
                // Read past the first two bytes of the zlib header
                // System.IO.Compression doesn't recognise the first 2 bytes of the data
                //https://stackoverflow.com/a/21544269
                ms.Seek(2, SeekOrigin.Begin);

                using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                {
                    ds.CopyTo(destination);
                    FilteredData = destination.ToArray();
                }
            }

            uint expectedScanlineSize = (1 + imageInfo.Width * SampleSize);
            uint expectedSize = expectedScanlineSize * imageInfo.Height;
            Debug.Assert(FilteredData.Length == expectedSize);

            // Arrange into scanlines
            byte[][] rawScanlineData = new byte[imageInfo.Height][];
            int pos = 0, line = 0;
            while (pos != expectedSize)
            {
                byte[] scanline = new byte[expectedScanlineSize];
                Buffer.BlockCopy(FilteredData, pos, scanline, 0, (int) expectedScanlineSize);
                rawScanlineData[line] = scanline;

                pos += (int) expectedScanlineSize;
                line++;

                Console.WriteLine("line={0} {1}/{2}", line, pos, expectedSize);
            }

            // Get filters for each scanline
            byte[] filterTypes = new byte[imageInfo.Height];
            for (int i = 0; i < (int)imageInfo.Height; i++)
            {
                filterTypes[i] = rawScanlineData[i][0];
                Console.WriteLine(rawScanlineData[i][0]);
            }

        }

        //internal byte[] Pack()
        //{
        //    byte[] recompressedData;
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        // Write deflate signature
        //        ms.WriteByte(0x78);
        //        ms.WriteByte(0x9c);

        //        // Compress the data using deflate
        //        using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress, true))
        //        {
        //            ds.Write(decompressedData, 0, decompressedData.Length);
        //        }

        //        // Write the checksum of the uncompressed data for zlib to verify decomp
        //        ms.Write(Adler32_(decompressedData, 0, decompressedData.Length), 0, 4);

        //        // Save it
        //        recompressedData = ms.ToArray();
        //    }
        //}
    }

    internal class Scanline
    {
        public FilterType FilterType;
        public byte[] FilteredData;
        public byte[] RawData;
        //public byte[] RawData;


        private bool _requiresFiltering = false;

        //PixelData[] Pixels;
    }



    internal struct PixelData
    {

    }




    internal class FileSignature
    {
        public FileSignature(byte[] data)
        {
            // TODO: Check length
            RawData = data;
        }

        public byte[] RawData = new byte[8];
        public bool Valid = false;
    }


    class Png
    {
        public FileSignature Signature { private set; get; }
        public PngHeader Header { private set; get; }
        public List<Chunk> Chunks { private set; get; } = new List<Chunk>();
        public ImageData ImageData { private set; get; }  = new ImageData();
        

        public void Load(string path)
        {
            // TODO: Exceptions and checks etc etc


            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                stream.Seek(0, SeekOrigin.Begin);
                Chunks.Clear();
                
                // Load signature
                Signature = new FileSignature(reader.ReadBytes(8));

                // TODO: Check signature before parsing further

                // The rest of the file are chunks of data
                while (stream.Position != stream.Length)
                {
                    Chunk currentChunk = new Chunk(reader);

                    // Parse the header
                    if (currentChunk.Type == Chunk.HEADER_TYPE)
                    {
                        Header = new PngHeader();
                        Header.FromBytes(currentChunk.Data);
                    }

                    // Add to file chunks
                    Chunks.Add(currentChunk);
                    currentChunk.Dump();

                    // TODO: Quit on IEND chunk
                }

                // TODO: If no header - error

                // Collect compressed image data
                List<byte> compressedImageData = new List<byte>();
                Chunk[] dataChunks = Chunks.Where(x => x.Type == Chunk.DATA_TYPE).ToArray();
                foreach (Chunk chunk in dataChunks)
                {
                    compressedImageData.AddRange(chunk.Data);
                }

                ImageData.Unpack(compressedImageData, Header);
            }
        }

        public void Save(string path)
        {

        }
    }
}

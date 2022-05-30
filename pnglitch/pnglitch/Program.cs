using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;

namespace pnglitch
{
    class PNGHeader
    {
        public uint Width;
        public uint Height;
        public byte BitDepth;
        public byte ColorType;
        public byte CompressionMethod;
        public byte FilterMethod;
        public byte InterlaceMethod;

        public void Parse(byte[] data)
        {
            //Debug.Assert(data.Length != 13);

            // TODO: Hack hack hack hack hack hack, hack hack hack hack hack hack
            byte[] widthData = new byte[4];
            Buffer.BlockCopy(data, 0, widthData, 0, 4);
            Width = LittleEndianConverter.FromBytes(widthData);
            byte[] heightData = new byte[4];
            Buffer.BlockCopy(data, 4, heightData, 0, 4);
            Height = LittleEndianConverter.FromBytes(heightData);
            BitDepth = data[8];
            ColorType = data[9];
            CompressionMethod = data[10];
            FilterMethod = data[11];
            InterlaceMethod = data[12];
        }

        public void Dump()
        {
            Console.WriteLine("Width: {0}, Height: {1}, BitDepth: {2}, " +
                "ColorType: {3}, CompressionMethod: {4}, FilterMethod: {5}, " +
                "InterlaceMethod: {6}",
                Width,
                Height,
                BitDepth,
                ColorType,
                CompressionMethod,
                FilterMethod,
                InterlaceMethod);
        }
    }

    class Chunk
    {
        public string Type;
        public UInt32 Length;
        public byte[] Data;
        public UInt32 Crc;

        public byte[] DecompressedData;
        public bool Dirty = false;

        public void Parse()
        {

        }

        public void Dump()
        {
            Console.WriteLine("Type: {0}, Len: {1}, crc: {2}", Type, Length, Crc);
            string val = "";
            if (Data == null)
                return;
            //foreach (var b in Data)
            //{
            //    Console.Write(b.ToString("X") + " ");
            //}
            //Console.WriteLine();

        }
    }

    class PNG
    {
        public byte[] HeaderData = new byte[8];
        public List<Chunk> Chunks = new List<Chunk>();
    }

    public static class LittleEndianConverter
    {
        public static uint FromBytes(byte[] data)
        {
            // TODO: Im not actually sure this is little endian just check later
            // im lazy and this manual method works
            data = data.Reverse().ToArray();
            return BitConverter.ToUInt32(data, 0);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string path = "image.png";
            PNG image = new PNG();
            PNGHeader header = new PNGHeader();

            List<byte> compressedData = new List<byte>();
            List<byte> uncompressedData = new List<byte>();

            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                stream.Seek(0, SeekOrigin.Begin);
                image.HeaderData = reader.ReadBytes(8);

                while (stream.Position != stream.Length)
                {
                    Chunk currentChunk = new Chunk();
                    
                    // It's in little endian (the bastard), I think? Check I mean actually check please
                    currentChunk.Length = LittleEndianConverter.FromBytes(reader.ReadBytes(4));
                    currentChunk.Type = Encoding.ASCII.GetString(reader.ReadBytes(4));

                    if (currentChunk.Type == "IHDR")
                    {
                        byte[] headerData = reader.ReadBytes((int)currentChunk.Length);
                        header.Parse(headerData);
                        header.Dump();
                    }
                    else
                    {
                        currentChunk.Data = reader.ReadBytes((int)currentChunk.Length);
                        compressedData.AddRange(currentChunk.Data);
                        //currentChunk.Data = currentChunk.Data.Reverse().ToArray();
                        
                        //using (MemoryStream destination = new MemoryStream())
                        //using (MemoryStream ms = new MemoryStream(currentChunk.Data))
                        //using (DeflateStream ds = new DeflateStream(destination, CompressionLevel.Optimal))
                        //{
                        //    ds.Write(currentChunk.Data, 0, (int)currentChunk.Length);
                        //    //ds.CopyTo(destination);
                        //    currentChunk.DecompressedData = destination.ToArray();
                        //}
                        //using (MemoryStream destination = new MemoryStream())
                        //using (MemoryStream data = new MemoryStream(currentChunk.Data))
                        //using (DeflateStream ds = new DeflateStream(data, CompressionMode.Decompress))
                        //{
                        //    ds.CopyTo(destination);
                        //    currentChunk.DecompressedData = destination.ToArray();
                        //}
                    }
                    currentChunk.Crc = reader.ReadUInt32();
                    currentChunk.Dump();

                    image.Chunks.Add(currentChunk);

                    //Console.WriteLine("{0}/{1}", stream.Position, stream.Length);
                }

                using (MemoryStream destination = new MemoryStream())
                using (MemoryStream ms = new MemoryStream(compressedData.ToArray()))
                {
                    // Read past the first two bytes of the zlib header
                    // System.IO.Compression doesn't recognise the first 2 bytes of the data
                    //https://stackoverflow.com/a/21544269
                    ms.Seek(2, SeekOrigin.Begin);

                    using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                    {
                        ds.CopyTo(destination);
                        uncompressedData.AddRange(destination.ToArray());
                    }
                }


                Console.ReadLine();
            }
        }
    }
}

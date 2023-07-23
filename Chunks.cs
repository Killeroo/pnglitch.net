using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

namespace pnglitch
{

    public class Chunk
    {
        public const string HEADER_TYPE = "IHDR";
        public const string DATA_TYPE = "IDAT";
        public const string END_TYPE = "IEND";

        public uint Length { get; private set; }
        public string Type { get; private set; }
        public byte[] Data { get; private set; }
        public uint Crc { get; private set; }

        public Chunk(BinaryReader data)
        {
            FromBytes(data);
        }

        public string GetType(byte[] chunkData)
        {
            return Encoding.ASCII.GetString(chunkData, 4, 4);
        }

        public virtual void FromBytes(BinaryReader data)
        {
            Length = Utils.FromBigEndianBytes(data.ReadBytes(4));
            Type = Encoding.ASCII.GetString(data.ReadBytes(4));
            Data = data.ReadBytes((int) Length);
            Crc = Utils.FromBigEndianBytes(data.ReadBytes(4));
        }

        public virtual byte[] ToBytes()
        {
            Length = (uint) Data.Length;

            // Calculate checksum using chunk type and data
            List<byte> checksumData = new List<byte>();
            checksumData.AddRange(Encoding.ASCII.GetBytes(Type));
            checksumData.AddRange(Data);
            Crc = Checksums.Crc32(checksumData.ToArray(), 0, Data.Length);

            // Write chunk data to array
            using (MemoryStream outputStream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(outputStream))
            {
                writer.Write(Utils.ToBigEndianBytes(Length));
                writer.Write(checksumData.ToArray());
                writer.Write(Crc);
            }

            return Data; 
        }

        public virtual void Dump()
        {
            Console.WriteLine("Chunk: Type: {0}, Len: {1}, crc: {2}", Type, Length, Crc.ToString("X"));
        }

    }

    //public class DataChunk : Chunk
    //{
    //    public byte[] CompressedData;
    //    public byte[] FilteredData;

    //    public DataChunk(BinaryReader stream) : base(stream) { }

    //    public override void FromBytes(BinaryReader data)
    //    {
    //        base.FromBytes(data);
    //    }

    //    public override byte[] ToBytes()
    //    {
    //        return base.ToBytes();
    //    }
    //}

    public class HeaderChunk : Chunk
    {
        public uint Width;
        public uint Height;
        public byte BitDepth;
        public byte ColorType;
        public byte CompressionMethod;
        public byte FilterMethod;
        public byte InterlaceMethod;

        public HeaderChunk(BinaryReader stream) : base(stream) { }

        public override void FromBytes(BinaryReader data)
        {
            // Parse the chunk first
            base.FromBytes(data);

            // Parse contents of chunk data
            using (MemoryStream stream = new MemoryStream(this.Data))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                Debug.Assert(Data.Length == 13);

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

        public override byte[] ToBytes()
        {
            // Write header to chunk data
            using (MemoryStream stream = new MemoryStream(this.Data))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(Utils.ToBigEndianBytes(Width));
                writer.Write(Utils.ToBigEndianBytes(Height));
                writer.Write(BitDepth);
                writer.Write(ColorType);
                writer.Write(CompressionMethod);
                writer.Write(FilterMethod);
                writer.Write(InterlaceMethod);
            }

            return base.ToBytes();
        }

        public override void Dump()
        {
            base.Dump();

            Console.WriteLine("Header: Width: {0}, Height: {1}, BitDepth: {2}, " +
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

    //public class EndChunk : Chunk
    //{
    //    public EndChunk(BinaryReader stream) : base(stream) { }
    //}
}

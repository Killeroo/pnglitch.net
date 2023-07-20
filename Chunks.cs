using System;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace pnglitch
{

    public class Chunk
    {
        public uint Length { get; private set; }
        public string Type { get; private set; }
        public byte[] Data { get; private set; }
        public uint Crc { get; private set; }

        public Chunk(BinaryReader data)
        {
            FromBytes(data);
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
            Crc = Checksums.Crc32(Data, 0, Data.Length);

            using (MemoryStream outputStream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(outputStream))
            {
                writer.Write(Utils.ToBigEndianBytes(Length));
                writer.Write(Encoding.ASCII.GetBytes(Type));
                writer.Write(Data);
                writer.Write(Crc);
            }

            return Data; 
        }

        public virtual void Dump()
        {
            Console.WriteLine("BaseChunk: Type: {0}, Len: {1}, crc: {2}", Type, Length, Crc.ToString("X"));
        }

    }

    public class DataChunk : Chunk
    {
        public byte[] CompressedData;
        public byte[] FilteredData;

        public DataChunk(BinaryReader stream) : base(stream) { }

        public override void FromBytes(BinaryReader data)
        {
            base.FromBytes(data);


        }
    }

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
            // TODO: I was here doing things
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
}

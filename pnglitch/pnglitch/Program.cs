using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pnglitch
{
    class Chunk
    {
        public string Type;
        public UInt32 Length;
        public byte[] Data;
        public UInt32 Crc;

        public void Parse()
        {

        }

        public void Dump()
        {
            Console.WriteLine("Type: {0}, Len: {1}, crc: {2}", Type, Length, Crc);
        }
    }

    class PNG
    {
        public byte[] HeaderData = new byte[8];
        public List<Chunk> Chunks = new List<Chunk>();
    }

    class Program
    {
        static void Main(string[] args)
        {
            string path = "image.png";
            PNG image = new PNG();

            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                stream.Seek(0, SeekOrigin.Begin);
                image.HeaderData = reader.ReadBytes(8);

                while (stream.Position != stream.Length)
                {
                    Chunk currentChunk = new Chunk();
                    //current.Length = reader.ReadUInt32();
                    byte[] lenData = reader.ReadBytes(4); // It's in little endian (the bastard)
                    lenData = lenData.Reverse().ToArray();
                    currentChunk.Length = BitConverter.ToUInt32(lenData, 0);
                    currentChunk.Type = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    currentChunk.Data = reader.ReadBytes((int)currentChunk.Length);
                    currentChunk.Crc = reader.ReadUInt32();
                    currentChunk.Dump();

                    image.Chunks.Add(currentChunk);

                    //Console.WriteLine("{0}/{1}", stream.Position, stream.Length);
                }
                Console.ReadLine();
            }
        }
    }
}

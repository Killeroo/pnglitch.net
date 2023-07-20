using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;

using PeterO;

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

        // TODO: Now technically part of header
        public byte SampleSize;

        // TEMP
        public byte[] RawBytes;

        public void Parse(byte[] data) // TODO: Add an offset and stuff 
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

            // TODO: Switch to fancy array
            switch (ColorType)
            {
                case 0: SampleSize = 1; break;
                case 2: SampleSize = 3; break;
                case 3: SampleSize = 1; break;
                case 4: SampleSize = 2; break;
                case 6: SampleSize = 4; break;
            }

            RawBytes = new byte[13];
            Array.Copy(data, 0, RawBytes, 0, 13);
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

        public void DumpRaw()
        {
            foreach (byte data in RawBytes)
            {
                Console.Write("{0} ", data.ToString("X2"));
            }
            Console.WriteLine();
        }
    }

    class Chunk_
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
            Console.WriteLine("Type: {0}, Len: {1}, crc: {2}", Type, Length, Crc.ToString("X"));
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
        public byte[] FileSignature = new byte[8];
        public List<Chunk_> Chunks = new List<Chunk_>();
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



            PeterO.Png png = new PeterO.Png(1000, 1000);

            png.Save("Test.png");
            //Console.ReadLine();



            string path = "image.png";
            PNG image = new PNG();
            PNGHeader header = new PNGHeader();

            List<byte> compressedData = new List<byte>();
            List<byte> uncompressedData = new List<byte>();

            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                stream.Seek(0, SeekOrigin.Begin);
                image.FileSignature = reader.ReadBytes(8);

                while (stream.Position != stream.Length)
                {
                    Chunk_ currentChunk = new Chunk_();

                    List<byte> crc_data = new List<byte>();

                    // It's in little endian (the bastard), I think? Check I mean actually check please
                    currentChunk.Length = LittleEndianConverter.FromBytes(reader.ReadBytes(4));

                    byte[] type = reader.ReadBytes(4);
                    currentChunk.Type = Encoding.ASCII.GetString(type);
                    crc_data.AddRange(type);

                    if (currentChunk.Type == "IHDR")
                    {
                        byte[] headerData = reader.ReadBytes((int)currentChunk.Length);
                        header.Parse(headerData);
                        header.Dump();
                        header.DumpRaw();

                    }
                    else
                    {
                        currentChunk.Data = reader.ReadBytes((int)currentChunk.Length);
                        compressedData.AddRange(currentChunk.Data);

                        crc_data.AddRange(currentChunk.Data);
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

                    currentChunk.Crc = LittleEndianConverter.FromBytes(reader.ReadBytes(4));// reader.ReadUInt32();
                    currentChunk.Dump();
                    
                    if (currentChunk.Type != "IHDR")
                    {
                        List<byte> data = new List<byte>();

                        var ourCrc = Crc32(crc_data.ToArray(), 0, (int)currentChunk.Length + 4);
                        Console.WriteLine("Ours={0} Theirs={1}", ourCrc.ToString("X8"), currentChunk.Crc.ToString("X8"));
                    }


                    image.Chunks.Add(currentChunk);

                    //Console.WriteLine("{0}/{1}", stream.Position, stream.Length);
                }

                uint expectedSize = (1 + header.Width * header.SampleSize) * header.Height;

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
                Debug.Assert(uncompressedData.Count == expectedSize);
                byte[] decompressedData = uncompressedData.ToArray();

                // Great we can apparently decompress, lets try compressing again...

                byte[] recompressedData;
                using (MemoryStream ms = new MemoryStream())
                {
                    // Write deflate signature
                    ms.WriteByte(0x78);
                    ms.WriteByte(0x9c);

                    // Compress the data using deflate
                    using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress, true))
                    {
                        ds.Write(decompressedData, 0, decompressedData.Length);
                    }

                    // Write the checksum of the uncompressed data for zlib to verify decomp
                    ms.Write(Adler32_(decompressedData, 0, decompressedData.Length), 0, 4);

                    // Save it
                    recompressedData = ms.ToArray();
                }
                
                for (int i = 0; i < 20; i++)
                {
                    Console.Write("{0}-{1} ", compressedData[i].ToString("X2"), recompressedData[i].ToString("X2"));
                }


                // Save the file again I guess
                using (FileStream outputFile = new FileStream("Output.png", FileMode.Create))
                {
                    // Write out signature again
                    outputFile.Write(image.FileSignature, 0, image.FileSignature.Length);

                    // Write out header chunk 
                    byte[] headerData = new byte[] {
                        0,
                        0,
                        0,
                        0xd,
                        0x49,
                        0x48,
                        0x44,
                        0x52};
                    outputFile.Write(headerData, 0, headerData.Length);
                    outputFile.Write(header.RawBytes, 0, 13);
                    {
                        uint crc = Crc32(header.RawBytes, 0, header.RawBytes.Length);
                        byte[] beCrc = new byte[]{
                    (byte)((crc>>24)&255),
                    (byte)((crc>>16)&255),
                    (byte)((crc>>8)&255),
                    (byte)((crc>>0)&255)};
                        outputFile.Write(beCrc, 0, 4);
                    }


                    byte[] defLength = new byte[]{
                    (byte)((recompressedData.Length>>24)&255),
                    (byte)((recompressedData.Length>>16)&255),
                    (byte)((recompressedData.Length>>8)&255),
                    (byte)((recompressedData.Length>>0)&255)};

                    // Length
                    outputFile.Write(defLength, 0, defLength.Length);

                    // Type
                    outputFile.Write(new byte[]{
                             0x49,0x44,0x41,0x54
                         }, 0, 4);

                    // Data
                    outputFile.Write(recompressedData, 0, recompressedData.Length);

                    // Crc
                    {
                        uint crc = Crc32(recompressedData, 0, recompressedData.Length);
                        byte[] beCrc = new byte[]{
                    (byte)((crc>>24)&255),
                    (byte)((crc>>16)&255),
                    (byte)((crc>>8)&255),
                    (byte)((crc>>0)&255)};
                        outputFile.Write(beCrc, 0, 4);
                    }



                    // Write end chunk

                    byte[] subdata2 = new byte[]{
				
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


                    // ---- Write end chunk
                    outputFile.Write(subdata2, 0, subdata2.Length);

                }

                
                //Crc32 crc = new Crc32();
                //using (MemoryStream destination = new MemoryStream())
                //using (MemoryStream d = new MemoryStream())
                ////using (MemoryStream ms = new MemoryStream(uncompressedData.ToArray()))
                //using (DeflateStream ds = new DeflateStream(d, CompressionMode.Compress, true))
                ////using (DeflateStream ds = new DeflateStream(d, CompressionMode.Compress, true))
                //{
                //    ds.Write(uncompressedData.ToArray(), 0, uncompressedData.Count);
                //    //ds.CopyTo(destination);
                //    //ds.CopyTo(d);

                //    ds.Close();

                //    //////////////////////////////////////////////////////////////////////////////////////
                //    // TODO: Now we havbe CRC and deflate maybe working do this next
                //    //////////////////////////////////////////////////////////////////////////////////////

                //    // 1. Put compressed data into an IDAT chunk (remember to decorate the deflate data) CRC the chunk
                //    // 2. Add a header and chunk end
                //    // 3. see if it opens

                //    //https://github.dev/codebude/QRCoder/blob/master/QRCoder/PngByteQRCode.cs
                //    // https://github.dev/EliotJones/BigGustave

                //    Console.WriteLine(compressedData.Count);
                //    Console.WriteLine(d.Length);

                //    var newCompressedData = d.ToArray();
                //    int count = 0;
                //    //for (int i = newCompressedData.Length; i > newCompressedData.Length - 10; i--)
                //    //{
                //    //    Console.Write("{0} ", newCompressedData[i-1].ToString("X"));
                //    //}
                //    //Console.WriteLine();
                //    foreach (byte data in newCompressedData)
                //    {
                //        if (count > 10) break;
                //        Console.Write("{0} ", data.ToString("X"));
                //        count++;
                //    }

                //    Console.WriteLine();
                //    Console.WriteLine("-------------------------------");
                //    Console.WriteLine();

                //    count = 0;
                //    foreach (byte data in compressedData)
                //    {
                //        if (count > 10) break;
                //        Console.Write("{0} ", data.ToString("X"));
                //        count++;
                //    }
                //}


                Console.ReadLine();
            }


        }

        // TODO: Compare the 2 adler implementations
        private static byte[] Adler32_(byte[] stream, int offset,
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

        // Reference implementation from RFC 1950. Not optimized.
        private static uint Adler32(byte[] data, int index, int length)
        {
            const uint Base = 65521;
            uint s1 = 1, s2 = 0;

            var end = index + length;
            for (var n = index; n < end; n++)
            {
                s1 = (s1 + data[n]) % Base;
                s2 = (s2 + s1) % Base;
            }

            return (s2 << 16) + s1;
        }

        private static readonly uint[] CrcTable = {
                0x00000000, 0x77073096, 0xEE0E612C, 0x990951BA, 0x076DC419, 0x706AF48F, 0xE963A535, 0x9E6495A3, 0x0EDB8832, 0x79DCB8A4, 0xE0D5E91E, 0x97D2D988, 0x09B64C2B, 0x7EB17CBD, 0xE7B82D07, 0x90BF1D91, 0x1DB71064, 0x6AB020F2, 0xF3B97148, 0x84BE41DE, 0x1ADAD47D, 0x6DDDE4EB, 0xF4D4B551, 0x83D385C7, 0x136C9856, 0x646BA8C0, 0xFD62F97A, 0x8A65C9EC, 0x14015C4F, 0x63066CD9, 0xFA0F3D63, 0x8D080DF5, 0x3B6E20C8, 0x4C69105E, 0xD56041E4, 0xA2677172, 0x3C03E4D1, 0x4B04D447, 0xD20D85FD, 0xA50AB56B, 0x35B5A8FA, 0x42B2986C, 0xDBBBC9D6, 0xACBCF940, 0x32D86CE3, 0x45DF5C75, 0xDCD60DCF, 0xABD13D59, 0x26D930AC, 0x51DE003A, 0xC8D75180, 0xBFD06116, 0x21B4F4B5, 0x56B3C423, 0xCFBA9599, 0xB8BDA50F, 0x2802B89E, 0x5F058808, 0xC60CD9B2, 0xB10BE924, 0x2F6F7C87, 0x58684C11, 0xC1611DAB, 0xB6662D3D,
                0x76DC4190, 0x01DB7106, 0x98D220BC, 0xEFD5102A, 0x71B18589, 0x06B6B51F, 0x9FBFE4A5, 0xE8B8D433, 0x7807C9A2, 0x0F00F934, 0x9609A88E, 0xE10E9818, 0x7F6A0DBB, 0x086D3D2D, 0x91646C97, 0xE6635C01, 0x6B6B51F4, 0x1C6C6162, 0x856530D8, 0xF262004E, 0x6C0695ED, 0x1B01A57B, 0x8208F4C1, 0xF50FC457, 0x65B0D9C6, 0x12B7E950, 0x8BBEB8EA, 0xFCB9887C, 0x62DD1DDF, 0x15DA2D49, 0x8CD37CF3, 0xFBD44C65, 0x4DB26158, 0x3AB551CE, 0xA3BC0074, 0xD4BB30E2, 0x4ADFA541, 0x3DD895D7, 0xA4D1C46D, 0xD3D6F4FB, 0x4369E96A, 0x346ED9FC, 0xAD678846, 0xDA60B8D0, 0x44042D73, 0x33031DE5, 0xAA0A4C5F, 0xDD0D7CC9, 0x5005713C, 0x270241AA, 0xBE0B1010, 0xC90C2086, 0x5768B525, 0x206F85B3, 0xB966D409, 0xCE61E49F, 0x5EDEF90E, 0x29D9C998, 0xB0D09822, 0xC7D7A8B4, 0x59B33D17, 0x2EB40D81, 0xB7BD5C3B, 0xC0BA6CAD,
                0xEDB88320, 0x9ABFB3B6, 0x03B6E20C, 0x74B1D29A, 0xEAD54739, 0x9DD277AF, 0x04DB2615, 0x73DC1683, 0xE3630B12, 0x94643B84, 0x0D6D6A3E, 0x7A6A5AA8, 0xE40ECF0B, 0x9309FF9D, 0x0A00AE27, 0x7D079EB1, 0xF00F9344, 0x8708A3D2, 0x1E01F268, 0x6906C2FE, 0xF762575D, 0x806567CB, 0x196C3671, 0x6E6B06E7, 0xFED41B76, 0x89D32BE0, 0x10DA7A5A, 0x67DD4ACC, 0xF9B9DF6F, 0x8EBEEFF9, 0x17B7BE43, 0x60B08ED5, 0xD6D6A3E8, 0xA1D1937E, 0x38D8C2C4, 0x4FDFF252, 0xD1BB67F1, 0xA6BC5767, 0x3FB506DD, 0x48B2364B, 0xD80D2BDA, 0xAF0A1B4C, 0x36034AF6, 0x41047A60, 0xDF60EFC3, 0xA867DF55, 0x316E8EEF, 0x4669BE79, 0xCB61B38C, 0xBC66831A, 0x256FD2A0, 0x5268E236, 0xCC0C7795, 0xBB0B4703, 0x220216B9, 0x5505262F, 0xC5BA3BBE, 0xB2BD0B28, 0x2BB45A92, 0x5CB36A04, 0xC2D7FFA7, 0xB5D0CF31, 0x2CD99E8B, 0x5BDEAE1D,
                0x9B64C2B0, 0xEC63F226, 0x756AA39C, 0x026D930A, 0x9C0906A9, 0xEB0E363F, 0x72076785, 0x05005713, 0x95BF4A82, 0xE2B87A14, 0x7BB12BAE, 0x0CB61B38, 0x92D28E9B, 0xE5D5BE0D, 0x7CDCEFB7, 0x0BDBDF21, 0x86D3D2D4, 0xF1D4E242, 0x68DDB3F8, 0x1FDA836E, 0x81BE16CD, 0xF6B9265B, 0x6FB077E1, 0x18B74777, 0x88085AE6, 0xFF0F6A70, 0x66063BCA, 0x11010B5C, 0x8F659EFF, 0xF862AE69, 0x616BFFD3, 0x166CCF45, 0xA00AE278, 0xD70DD2EE, 0x4E048354, 0x3903B3C2, 0xA7672661, 0xD06016F7, 0x4969474D, 0x3E6E77DB, 0xAED16A4A, 0xD9D65ADC, 0x40DF0B66, 0x37D83BF0, 0xA9BCAE53, 0xDEBB9EC5, 0x47B2CF7F, 0x30B5FFE9, 0xBDBDF21C, 0xCABAC28A, 0x53B39330, 0x24B4A3A6, 0xBAD03605, 0xCDD70693, 0x54DE5729, 0x23D967BF, 0xB3667A2E, 0xC4614AB8, 0x5D681B02, 0x2A6F2B94, 0xB40BBE37, 0xC30C8EA1, 0x5A05DF1B, 0x2D02EF8D
            };


        // Reference implementation from REC-PNG-20031110. Not optimized.
        private static uint Crc32(byte[] data, int index, int length)
        {
            var c = 0xffffffff;

            var end = index + length;
            for (var n = index; n < end; n++)
            {
                c = CrcTable[(c ^ data[n]) & 0xff] ^ (c >> 8);
            }

            return c ^ 0xffffffff;
        }

        // I'm curious how this crc implementation compares when ported
        //https://www.w3.org/TR/PNG-CRCAppendix.html
    }
}

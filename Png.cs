using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pnglitch
{

    internal class ImageData
    {
        public byte[] FilteredData;
        public byte[] CompressedData;

        private bool _dirty = false;
    }

    internal class FileSignature
    {
        public byte[] RawData = new byte[8];
    }

    class Png
    {


        List<Chunk> Chunks = new List<Chunk>();
        ImageData ImageData = new ImageData();

    }
}

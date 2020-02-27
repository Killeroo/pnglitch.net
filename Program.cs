using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;

namespace pnglitch_sharp
{
    class Program
    {
        static void Main(string[] args)
        {
            //https://github.com/ucnv/pnglitch/blob/d21a2aa82e00e8db62f88eebeef949d861557aec/lib/pnglitch/base.rb#L15
            string path = "image.png";

            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                byte[] buffer = new byte[1];
                ASCIIEncoding temp = new ASCIIEncoding();

                while (stream.Read(buffer, 0, buffer.Length) > 0)
                {
                    Console.Write(temp.GetString(buffer));
                    //stream.rep
                }
            }
            
            Console.Read();
        }
    }
}

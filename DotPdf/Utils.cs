using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotPdf
{
    internal class Utils
    {
        public static byte[] DeflateCompress(string sb)
        {
            using (var ms = new MemoryStream(Encoding.Default.GetBytes(sb)))
            using (var output = new MemoryStream())
            using (var cs = new DeflateStream(output, CompressionMode.Compress))
            {
                // Write the decompressed data to the compression stream
                ms.CopyTo(cs);

                return output.ToArray();
                //cs.Close(); // Important: explicitly close to ensure all data is written
            }
        }
    }
}

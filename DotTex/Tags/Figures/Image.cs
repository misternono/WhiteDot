using DotTex.Tags.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex.Tags
{
    public class Image : LatexTag
    {
        public string FilePath { get; set; }

        public Image(string filePath)
        {
            FilePath = filePath;
        }

        public override string Render()
        {
            return $"\\includegraphics{{{FilePath}}}";
        }
    }
}

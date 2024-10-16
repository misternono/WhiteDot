using DotTex.Tags.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex.Tags
{
    public class Caption : LatexTag
    {
        public string Text { get; set; }

        public Caption(string text)
        {
            Text = text;
        }

        public override string Render()
        {
            return $"\\caption{{{Text}}}";
        }
    }
}

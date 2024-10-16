using DotTex.Tags.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex.Tags
{
    public class BoldText : LatexTag
    {
        public string Content { get; set; }

        public BoldText(string content)
        {
            Content = content;
        }

        public override string Render()
        {
            return $"\\textbf{{{Content}}}";
        }
    }
}

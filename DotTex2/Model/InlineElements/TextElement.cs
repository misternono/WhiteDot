using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex2.Model.InlineElements
{
    public class TextElement : InlineElement
    {
        public string Text { get; set; }
        public FontSettings FontSettings { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex2.Model.InlineElements
{
    public class PlaceholderElement : InlineElement
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public FontSettings FontSettings { get; set; }
    }
}
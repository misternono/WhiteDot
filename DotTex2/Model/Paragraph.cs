using DotTex2.Model.InlineElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex2.Model
{
    public class Paragraph : IDocumentElement
    {
        public List<InlineElement> Content { get; set; } = new List<InlineElement>();
    }
}

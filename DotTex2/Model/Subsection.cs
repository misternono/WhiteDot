using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex2.Model
{
    public class Subsection : IDocumentElement
    {
        public string Title { get; set; }
        public List<IDocumentElement> Content { get; set; } = new List<IDocumentElement>();
    }
}

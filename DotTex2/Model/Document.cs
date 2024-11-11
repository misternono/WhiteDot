using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex2.Model
{
    public class Document
    {
        public DocumentClass DocumentClass { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public DateTime? Date { get; set; } = null;
        public List<IDocumentElement> Elements { get; set; } = new List<IDocumentElement>();
    }

    public class DocumentClass
    {
        public string Name { get; set; }
        public List<string> Options { get; set; } = new List<string>();
    }
}

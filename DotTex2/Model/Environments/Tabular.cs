using DotTex2.Model.InlineElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DotTex2.Model.Environments
{
    public class Tabular : Environment
    {
        public List<List<InlineElement>> Rows { get; set; } = new List<List<InlineElement>>(); // List of rows, each containing a list of cells (inline elements)

        public Tabular()
        {
            Name = "tabular";
        }
    }
}

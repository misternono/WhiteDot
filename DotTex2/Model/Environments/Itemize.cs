using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DotTex2.Model.Environments
{
    public class Itemize : Environment
    { 
        public List<Paragraph> Items { get; set; } = new List<Paragraph>(); // Items in the unordered list

        public Itemize()
        {
            Name = "itemize";
        }
    }
}

using DotTex.Tags.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex.Tags
{
    public class TableCell : LatexTag
    {
        public string Content { get; set; }

        public TableCell(string content)
        {
            Content = content;
        }

        public override string Render()
        {
            return $"{Content} &";
        }
    }
}

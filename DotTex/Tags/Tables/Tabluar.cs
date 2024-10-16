using DotTex.Tags.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex.Tags
{
    public class Tabular : CompositeLatexTag
    {
        public string Alignment { get; set; }

        public Tabular(string alignment)
        {
            Alignment = alignment;
        }

        public override string Render()
        {
            StringBuilder content = new StringBuilder();
            content.AppendLine($"\\begin{{tabular}}{{{Alignment}}}");
            content.Append(base.Render());
            content.AppendLine("\\end{tabular}");
            return content.ToString();
        }
    }
}

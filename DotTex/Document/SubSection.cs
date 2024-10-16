using DotTex.Tags.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex.Document
{
    public class SubSection : CompositeLatexTag
    {
        public string Title { get; set; }

        public SubSection(string title)
        {
            Title = title;
        }

        public override string Render()
        {
            StringBuilder content = new StringBuilder();
            content.AppendLine($"\\subsection{{{Title}}}");
            content.Append(base.Render());
            return content.ToString();
        }
    }
}

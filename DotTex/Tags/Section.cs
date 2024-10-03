using DotTex.Tags.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex.Tags
{
    // Class representing a LaTeX section
    public class Section : CompositeLatexTag
    {
        public string Title { get; set; }

        public Section(string title)
        {
            Title = title;
        }

        public override string Render()
        {
            StringBuilder content = new StringBuilder();
            content.AppendLine($"\\section{{{Title}}}");
            content.Append(base.Render());  // Render child tags
            return content.ToString();
        }
    }
}

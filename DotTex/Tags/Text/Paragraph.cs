using DotTex.Tags.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex.Tags
{
    // Class representing a LaTeX paragraph
    public class Paragraph : LatexTag
    {
        public string Content { get; set; }

        public Paragraph(string content)
        {
            Content = content;
        }

        public override string Render()
        {
            return Content; // No need for special LaTeX tag for simple paragraphs
        }
    }
}

using DotTex.Tags.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex.Document
{
    // Class representing a LaTeX document
    public class Document
    {
        private List<LatexTag> _tags;

        public Document()
        {
            _tags = new List<LatexTag>();
        }

        // Add a LaTeX tag to the document
        public void AddTag(LatexTag tag)
        {
            _tags.Add(tag);
        }

        // Render the entire LaTeX document
        public string Render()
        {
            StringBuilder latexContent = new StringBuilder();
            latexContent.AppendLine("\\documentclass{article}");
            latexContent.AppendLine("\\begin{document}");

            foreach (var tag in _tags)
            {
                latexContent.AppendLine(tag.Render());
            }

            latexContent.AppendLine("\\end{document}");
            return latexContent.ToString();
        }
    }
}

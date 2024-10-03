using System;
using System.Collections.Generic;
using System.Text;

namespace DotTex
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

    // Class representing a LaTeX equation
    public class Equation : LatexTag
    {
        public string Expression { get; set; }

        public Equation(string expression)
        {
            Expression = expression;
        }

        public override string Render()
        {
            return $"\\begin{{equation}}\n{Expression}\n\\end{{equation}}";
        }
    }
}

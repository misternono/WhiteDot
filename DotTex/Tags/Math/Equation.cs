using DotTex.Tags.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex.Tags
{
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

using DotTex.Tags.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex.Tags
{
    public class Summation : LatexTag
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Expression { get; set; }

        public Summation(string from, string to, string expression)
        {
            From = from;
            To = to;
            Expression = expression;
        }

        public override string Render()
        {
            return $"\\sum_{{{From}}}^{{{To}}} {Expression}";
        }
    }
}

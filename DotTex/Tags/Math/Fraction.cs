using DotTex.Tags.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex.Tags
{
    public class Fraction : LatexTag
    {
        public string Numerator { get; set; }
        public string Denominator { get; set; }

        public Fraction(string numerator, string denominator)
        {
            Numerator = numerator;
            Denominator = denominator;
        }

        public override string Render()
        {
            return $"\\frac{{{Numerator}}}{{{Denominator}}}";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex2.Model
{
    public class MathExpression : IDocumentElement
    {
        public string Expression { get; set; }
        public bool IsInline { get; set; }
    }
}

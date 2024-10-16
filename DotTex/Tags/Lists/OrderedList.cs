using DotTex.Tags.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex.Tags
{
    public class OrderedList : CompositeLatexTag
    {
        public override string Render()
        {
            StringBuilder content = new StringBuilder();
            content.AppendLine("\\begin{enumerate}");
            content.Append(base.Render());
            content.AppendLine("\\end{enumerate}");
            return content.ToString();
        }
    }
}

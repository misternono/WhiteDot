using DotTex.Tags.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex.Tags
{
    public class TableRow : CompositeLatexTag
    {
        public override string Render()
        {
            return base.Render() + " \\\\";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex.Tags.Abstract
{
    // Base class for LaTeX tags
    public abstract class LatexTag
    {
        public abstract string Render(); // Abstract method for rendering LaTeX output
    }
}

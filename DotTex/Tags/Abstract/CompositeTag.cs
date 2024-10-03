using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex.Tags.Abstract
{
    public abstract class CompositeLatexTag : LatexTag
    {
        protected List<LatexTag> _childTags;

        public CompositeLatexTag()
        {
            _childTags = new List<LatexTag>();
        }

        public void AddTag(LatexTag tag)
        {
            _childTags.Add(tag);
        }

        public override string Render()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var tag in _childTags)
            {
                builder.AppendLine(tag.Render());
            }
            return builder.ToString();
        }
    }
}

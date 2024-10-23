using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex2.Model
{
    public class FontSettings
    {
        public string FontFamily { get; set; } = "default";
        public string FontSize { get; set; } = "normalsize";
        public bool IsBold { get; set; } = false;
        public bool IsItalic { get; set; } = false;
        public bool IsTypewriter { get; set; } = false;
        public bool IsSmallCaps { get; set; } = false;
        public double LineSpacing { get; set; } = 1.0;
    }
}

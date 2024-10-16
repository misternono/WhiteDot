using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex2.Lexing
{
    public class Token
    {
        public TokenType Type { get; set; }
        public string Value { get; set; }
    }
}

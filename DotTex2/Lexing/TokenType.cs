using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex2.Lexing
{
    public enum TokenType
    {
        Text,
        Command,
        BeginEnvironment,
        EndEnvironment,
        MathStart,
        MathEnd,
        NewLine,
        BracketOpen,
        BracketClose
    }
}

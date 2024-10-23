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
        InlineCommand,
        Command,
        BeginEnvironment,
        EndEnvironment,
        MathStart,
        MathEnd,
        NewLine,
        BracketOpen,
        BracketClose,
        Title,
        Author,
        Date,
        UsePackage,
        DocumentClass,
        Metadata,
        FontCommand,
        FontSetting
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public enum TokenType
    {
        Command,
        BeginEnvironment,
        EndEnvironment,
        Argument,
        Text,
        Unknown
    }

    public class Token
    {
        public TokenType Type { get; set; }
        public string Value { get; set; }

        public Token(TokenType type, string value)
        {
            Type = type;
            Value = value;
        }

        public override string ToString()
        {
            return $"{Type}: {Value}";
        }
    }

    public class LatexLexer
    {
        private static readonly Regex tokenRegex = new Regex(
            @"(\\[a-zA-Z]+)|(\{[^\}]*\})|(\[.*?\])|\\begin\{([^\}]*)\}|\\end\{([^\}]*)\}|([^\\\{\}\[\]]+)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        public List<Token> Tokenize(string input)
        {
            var tokens = new List<Token>();
            var matches = tokenRegex.Matches(input);

            foreach (Match match in matches)
            {
                if (match.Groups[1].Success) // LaTeX command (e.g., \section)
                {
                    tokens.Add(new Token(TokenType.Command, match.Groups[1].Value));
                }
                else if (match.Groups[2].Success) // Argument (e.g., {Title})
                {
                    tokens.Add(new Token(TokenType.Argument, match.Groups[2].Value.Trim('{', '}')));
                }
                else if (match.Groups[4].Success) // Begin environment (e.g., \begin{itemize})
                {
                    tokens.Add(new Token(TokenType.BeginEnvironment, match.Groups[4].Value));
                }
                else if (match.Groups[5].Success) // End environment (e.g., \end{itemize})
                {
                    tokens.Add(new Token(TokenType.EndEnvironment, match.Groups[5].Value));
                }
                else if (match.Groups[6].Success) // Plain text
                {
                    tokens.Add(new Token(TokenType.Text, match.Groups[6].Value.Trim()));
                }
            }

            return tokens;
        }
    }

}

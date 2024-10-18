using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DotTex2.Lexing
{
    public class Lexer
    {
        private static readonly Regex TokenRegex = new Regex(
            @"(\\[a-zA-Z]+)" + // LaTeX commands
            @"|(\\begin\{[a-zA-Z]+\})" + // Begin environment
            @"|(\\end\{[a-zA-Z]+\})" + // End environment
            @"|(\$\$|\$)" + // Math mode (both inline and display)
            @"|(\n)" + // New line
            @"|(\r)" + // New line
            @"|(\{|\})" + // Brackets
            @"|([^\\$\n\{\}]+)", // Plain text
            RegexOptions.Compiled
        );

        public IEnumerable<Token> Tokenize(string input)
        {
            foreach (Match match in TokenRegex.Matches(input))
            {
                if (match.Groups[1].Success) // Command
                {
                    string command = match.Value;
                    CommandType commandType = CommandClassifier.ClassifyCommand(command);
                    yield return new Token
                    {
                        Type = commandType == CommandType.Inline ? TokenType.InlineCommand : TokenType.Command,
                        Value = command
                    };
                }
                else if (match.Groups[2].Success) // Begin environment
                    yield return new Token { Type = TokenType.BeginEnvironment, Value = match.Value };
                else if (match.Groups[3].Success) // End environment
                    yield return new Token { Type = TokenType.EndEnvironment, Value = match.Value };
                else if (match.Groups[4].Success) // Math mode
                    yield return new Token { Type = TokenType.MathStart, Value = match.Value };
                else if (match.Groups[5].Success) // New line
                    yield return new Token { Type = TokenType.NewLine, Value = match.Value };
                else if (match.Groups[6].Success) 
                    yield return new Token { Type = TokenType.NewLine, Value = match.Value };
                else if (match.Groups[7].Success) // Brackets
                {
                    yield return new Token
                    {
                        Type = match.Value == "{" ? TokenType.BracketOpen : TokenType.BracketClose,
                        Value = match.Value
                    };
                }
                else if (match.Groups[8].Success) // Plain text
                    yield return new Token { Type = TokenType.Text, Value = match.Value };
            }
        }
    }
}

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
            @"(\\begin\{[a-zA-Z]+\})" + // Begin environment
            @"|(\\end\{[a-zA-Z]+\})" + // End environment
            @"|(\\(?:text(?:rm|sf|tt|md|bf|up|it|sl|sc)|em|normalfont|rmfamily|sffamily|ttfamily|mdseries|bfseries|upshape|itshape|slshape|scshape|tiny|scriptsize|footnotesize|small|normalsize|large|Large|LARGE|huge|Huge|normalem))" + // Font commands with opening brace
            @"|(\\(?:documentclass|usepackage|setmainfont|setsansfont|setmonofont|fontfamily|fontsize|linespread))" + // Font settings and packages
            @"|(\\[a-zA-Z]+)" + // Other LaTeX commands
            @"|(\$\$|\$)" + // Math mode (both inline and display)
            @"|(\n|\r\n?)" + // New line (including Windows-style)
            @"|(\{|\})" + // Brackets
            @"|([^\\$\n\r\{\}]+)", // Plain text
            RegexOptions.Compiled
        );

        public IEnumerable<Token> Tokenize(string input)
        {
            foreach (Match match in TokenRegex.Matches(input))
            {
                if (match.Groups[1].Success) // Begin environment
                {
                    yield return new Token
                    {
                        Type = TokenType.BeginEnvironment,
                        Value = match.Value,
                        //IsDrawable = false
                    };
                }
                else if (match.Groups[2].Success) // End environment
                {
                    yield return new Token
                    {
                        Type = TokenType.EndEnvironment,
                        Value = match.Value,
                        //IsDrawable = false
                    };
                }
                else if (match.Groups[3].Success) // Font commands
                {
                    var commandType = CommandClassifier.ClassifyCommand(match.Value.Split(new[] { '{', ' ' })[0]);
                    yield return new Token
                    {
                        Type = TokenType.FontCommand,
                        Value = match.Value,
                        //IsDrawable = true,
                        CommandCategory = commandType
                    };
                }
                else if (match.Groups[4].Success) // Font settings
                {
                    yield return new Token
                    {
                        Type = TokenType.FontSetting,
                        Value = match.Value,
                        //IsDrawable = false,
                        CommandCategory = CommandType.FontSetting
                    };
                }
                else if (match.Groups[5].Success) // Other commands
                {
                    string command = match.Value;
                    CommandType commandType = CommandClassifier.ClassifyCommand(command);
                    yield return new Token
                    {
                        Type = commandType == CommandType.Inline ? TokenType.InlineCommand : TokenType.Command,
                        Value = command,
                        //IsDrawable = commandType == CommandType.Inline,
                        CommandCategory = commandType
                    };
                }
                else if (match.Groups[6].Success) // Math mode
                {
                    yield return new Token
                    {
                        Type = TokenType.MathStart,
                        Value = match.Value,
                        //IsDrawable = false
                    };
                }
                else if (match.Groups[7].Success) // New line
                {
                    yield return new Token
                    {
                        Type = TokenType.NewLine,
                        Value = match.Value,
                        //IsDrawable = true
                    };
                }
                else if (match.Groups[8].Success) // Brackets
                {
                    yield return new Token
                    {
                        Type = match.Value == "{" ? TokenType.BracketOpen : TokenType.BracketClose,
                        Value = match.Value,
                        //IsDrawable = false
                    };
                }
                else if (match.Groups[9].Success) // Plain text
                {
                    yield return new Token
                    {
                        Type = TokenType.Text,
                        Value = match.Value,
                        //IsDrawable = true
                    };
                }
            }
        }
    }
}

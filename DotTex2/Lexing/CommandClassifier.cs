using DotTex2.Lexing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class CommandClassifier
{
    private static readonly HashSet<string> KnownInlineCommands = new HashSet<string>
    {
        "\\textbf", "\\textit", "\\underline", "\\emph", "\\footnote", "\\cite",
        "\\ref", "\\label", "\\url", "\\color", "\\textcolor", "\\textsuperscript",
        "\\textsubscript", "\\verb", "\\includegraphics", "\\hyperref"
    };

    private static readonly HashSet<string> KnownBlockCommands = new HashSet<string>
    {
        "\\section", "\\subsection", "\\chapter", "\\paragraph", "\\begin", "\\end",
        "\\documentclass", "\\usepackage", "\\maketitle", "\\tableofcontents",
        "\\bibliography", "\\appendix", "\\figure", "\\table"
    };

    public static readonly Dictionary<string, TokenType> MetadataCommands = new Dictionary<string, TokenType>
        {
            { "\\title", TokenType.Title },
            { "\\author", TokenType.Author },
            { "\\date", TokenType.Date },
            { "\\documentclass", TokenType.DocumentClass },
            { "\\usepackage", TokenType.UsePackage }
        };

    public static void ExtractMetadata(List<Token> tokens, Dictionary<string, string> metadata, Token metadataToken)
    {
        int bracketCount = 0;
        StringBuilder metadataValue = new StringBuilder();

        // Look ahead to capture the content of the metadata command
        for (int i = tokens.Count - 1; i >= 0; i--)
        {
            Token token = tokens[i];

            if (token.Type == TokenType.BracketClose)
                bracketCount++;
            else if (token.Type == TokenType.BracketOpen)
                bracketCount--;

            if (bracketCount > 0)
                metadataValue.Insert(0, token.Value);

            if (bracketCount == 0 && metadataValue.Length > 0)
            {
                metadata[metadataToken.Type.ToString()] = metadataValue.ToString().Trim();
                tokens.RemoveRange(i, tokens.Count - i);
                break;
            }
        }
    }
    public static CommandType ClassifyCommand(string command)
    {
        // Remove the leading backslash if present
        command = command.TrimStart('\\');

        // Check if it's a known inline command
        if (KnownInlineCommands.Contains("\\" + command))
        {
            return CommandType.Inline;
        }

        // Check if it's a known block command
        if (KnownBlockCommands.Contains("\\" + command))
        {
            return CommandType.Block;
        }

        // If not in either list, we need to make an educated guess
        // Most inline commands are short (1-2 words) and often relate to text formatting
        if (command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length <= 2 &&
            (command.StartsWith("text") || command.EndsWith("text") ||
             command.Contains("font") || command.Contains("color")))
        {
            return CommandType.Inline;
        }

        // If it's not clearly inline, default to block
        return CommandType.Block;
    }
}

public enum CommandType
{
    Inline,
    Block
}
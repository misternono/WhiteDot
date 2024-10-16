using DotTex2.Lexing;
using DotTex2.Model.InlineElements;
using DotTex2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.XPath;
using Environment = DotTex2.Model.Environment;
using DotTex2.Model.Environments;

namespace DotTex2.Parsing
{
    public class Parser
    {
        private readonly List<Token> tokens;
        private int currentIndex = 0;

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        public Document Parse()
        {
            var document = new Document();

            while (currentIndex < tokens.Count)
            {
                var element = ParseElement();
                if (element != null)
                {
                    document.Elements.Add(element);
                }
            }

            return document;
        }

        private IDocumentElement ParseElement()
        {
            var token = Consume();

            switch (token.Type)
            {
                case TokenType.Command:
                    switch (token.Value)
                    {
                        case "\\section":
                            return ParseSection();
                        case "\\subsection":
                            return ParseSubsection();
                        case "\\item":
                            return ParseListItem(); // Add handler for \item
                        case "\\textbf":
                            return ParseBoldText();
                        case "\\textit":
                            return ParseItalicText();
                        case "\\cite":
                            return ParseCitation();
                        default:
                            // Handle other commands or ignore
                            break;
                    }
                    break;
                case TokenType.Text:
                    return new Paragraph { Content = new List<InlineElement> { new TextElement { Text = token.Value } } };
                case TokenType.MathStart:
                    return ParseMathExpression();
                case TokenType.BeginEnvironment:
                    return ParseEnvironment();
                case TokenType.NewLine:
                    // Ignore single newlines
                    return null;
            }

            return null;
        }

        private Paragraph ParseListItem()
        {
            return new Paragraph
            {
                Content = ParseInlineContent() // Treat the content following \item as a new paragraph within the list
            };
        }

        private Section ParseSection()
        {
            var section = new Section();
            section.Title = ParseInlineContent().OfType<TextElement>().FirstOrDefault()?.Text ?? "";

            while (currentIndex < tokens.Count && tokens[currentIndex].Type != TokenType.Command)
            {
                var element = ParseElement();
                if (element != null)
                {
                    section.Content.Add(element);
                }
            }

            return section;
        }

        private Subsection ParseSubsection()
        {
            var subsection = new Subsection();
            subsection.Title = ParseInlineContent().OfType<TextElement>().FirstOrDefault()?.Text ?? "";

            while (currentIndex < tokens.Count && tokens[currentIndex].Type != TokenType.Command)
            {
                var element = ParseElement();
                if (element != null)
                {
                    subsection.Content.Add(element);
                }
            }

            return subsection;
        }

        private MathExpression ParseMathExpression()
        {
            var expression = new MathExpression();
            var mathContent = new List<string>();
            expression.IsInline = tokens[currentIndex - 1].Value == "$";

            while (currentIndex < tokens.Count && tokens[currentIndex].Type != TokenType.MathStart)
            {
                mathContent.Add(Consume().Value);
            }

            Consume(TokenType.MathStart); // Consume closing $ or $$

            expression.Expression = string.Join("", mathContent);
            return expression;
        }

        private Environment ParseEnvironment()
        {
            var environmentName = Regex.Match(tokens[currentIndex - 1].Value, @"\\begin\{(.+?)\}").Groups[1].Value;

            Environment environment;

            switch (environmentName)
            {
                case "itemize":
                    environment = new Itemize();
                    while (currentIndex < tokens.Count && !(tokens[currentIndex].Type == TokenType.EndEnvironment && tokens[currentIndex].Value.Contains("itemize")))
                    {
                        var element = ParseElement();
                        if (element != null && element is Paragraph paragraph)
                        {
                            ((Itemize)environment).Items.Add(paragraph); // Add items to the unordered list
                        }
                    }
                    break;

                case "enumerate":
                    environment = new Enumerate();
                    while (currentIndex < tokens.Count && !(tokens[currentIndex].Type == TokenType.EndEnvironment && tokens[currentIndex].Value.Contains("enumerate")))
                    {
                        var element = ParseElement();
                        if (element != null && element is Paragraph paragraph)
                        {
                            ((Enumerate)environment).Items.Add(paragraph); // Add items to the ordered list
                        }
                    }
                    break;

                case "verbatim":
                    environment = new Verbatim();
                    var verbatimContent = new List<string>();
                    while (currentIndex < tokens.Count && !(tokens[currentIndex].Type == TokenType.EndEnvironment && tokens[currentIndex].Value.Contains("verbatim")))
                    {
                        verbatimContent.Add(Consume().Value); // Collect raw content
                    }
                    ((Verbatim)environment).Text = string.Join("\n", verbatimContent);
                    break;

                case "tabular":
                    environment = new Tabular();
                    while (currentIndex < tokens.Count && !(tokens[currentIndex].Type == TokenType.EndEnvironment && tokens[currentIndex].Value.Contains("tabular")))
                    {
                        var row = new List<InlineElement>();
                        while (tokens[currentIndex].Type != TokenType.NewLine && tokens[currentIndex].Type != TokenType.EndEnvironment)
                        {
                            row.AddRange(ParseInlineContent()); // Parse the content inside table cells
                        }
                        ((Tabular)environment).Rows.Add(row); // Add the row to the table
                        Consume(TokenType.NewLine); // Consume the newline between rows
                    }
                    break;

                default:
                    environment = new Environment { Name = environmentName };
                    while (currentIndex < tokens.Count && !(tokens[currentIndex].Type == TokenType.EndEnvironment && tokens[currentIndex].Value.Contains(environmentName)))
                    {
                        var element = ParseElement();
                        if (element != null)
                        {
                            environment.Content.Add(element);
                        }
                    }
                    break;
            }

            Consume(TokenType.EndEnvironment); // Consume \end{environmentName}
            return environment;
        }

        private BoldText ParseBoldText()
        {
            Consume(TokenType.BracketOpen);
            var content = ParseInlineContent();
            Consume(TokenType.BracketClose);

            return new BoldText { Text = string.Join("", content.OfType<TextElement>().Select(t => t.Text)) };
        }


        private ItalicText ParseItalicText()
        {
            Consume(TokenType.BracketOpen);
            var content = ParseInlineContent();
            Consume(TokenType.BracketClose);

            return new ItalicText { Text = string.Join("", content.OfType<TextElement>().Select(t => t.Text)) };
        }

        private Citation ParseCitation()
        {
            Consume(TokenType.BracketOpen);
            var key = Consume(TokenType.Text).Value;
            Consume(TokenType.BracketClose);

            return new Citation { Key = key };
        }

        private List<InlineElement> ParseInlineContent()
        {
            var content = new List<InlineElement>();

            while (currentIndex < tokens.Count &&
                   tokens[currentIndex].Type != TokenType.BracketClose &&
                   tokens[currentIndex].Type != TokenType.NewLine)
            {
                var token = Consume();
                switch (token.Type)
                {
                    case TokenType.Text:
                        content.Add(new TextElement { Text = token.Value });
                        break;
                    case TokenType.Command:
                        if (token.Value == "\\textbf")
                        {
                            content.Add(ParseBoldText());
                        }
                        else if (token.Value == "\\textit")
                        {
                            content.Add(ParseItalicText());
                        }
                        // Add cases for other inline commands if needed
                        break;
                }
            }

            return content;
        }

        private Token Consume(TokenType? expectedType = null)
        {
            if (currentIndex >= tokens.Count)
                throw new InvalidOperationException("Unexpected end of input");

            var token = tokens[currentIndex++];

            // Ignore empty newlines that shouldn't create paragraphs
            if (token.Type == TokenType.NewLine && expectedType == null)
            {
                return Consume(expectedType);
            }

            if (expectedType.HasValue && token.Type != expectedType)
                throw new InvalidOperationException($"Expected {expectedType}, but got {token.Type}");

            return token;
        }
    }
}

﻿using DotTex2.Lexing;
using DotTex2.Model.InlineElements;
using DotTex2.Model;
using System.Text.RegularExpressions;
using Environment = DotTex2.Model.Environment;
using DotTex2.Model.Environments;
using Document = DotTex2.Model.Document;

namespace DotTex2.Parsing
{
    public class Parser
    {
        private readonly List<Token> tokens;
        private int currentIndex = 0;
        private int consecutiveNewLines = 0;
        private bool endedEnvironment = false;
        private FontSettings currentFontSettings = new FontSettings();
        private Document document;

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens;
            this.document = new Document();
        }

        public Document Parse()
        {
            while (currentIndex < tokens.Count)
            {
                var element = ParseElement();
                if (element != null)
                {
                    this.document.Elements.Add(element);
                }
            }

            return this.document;
        }

        private IDocumentElement ParseFontCommand(Token token)
        {
            switch (token.CommandCategory)
            {
                case CommandType.Inline:
                    return ParseInlineFontCommand(token);
                case CommandType.FontStyle:
                    ApplyFontStyle(token.Value);
                    return null;
                default:
                    return null;
            }
        }

        private IDocumentElement ParseInlineFontCommand(Token token)
        {
            Consume(TokenType.BracketOpen);
            var content = ParseInlineContent();
            Consume(TokenType.BracketClose);

            switch (token.Value)
            {
                case "\\textbf":
                    return new TextElement
                    {
                        Text = string.Join("", content.OfType<TextElement>().Select(t => t.Text)),
                        FontSettings = new FontSettings { IsBold = true }
                    };
                case "\\textit":
                    return new TextElement
                    {
                        Text = string.Join("", content.OfType<TextElement>().Select(t => t.Text)),
                        FontSettings = new FontSettings { IsItalic = true }
                    };
                case "\\texttt":
                    return new TextElement
                    {
                        Text = string.Join("", content.OfType<TextElement>().Select(t => t.Text)),
                        FontSettings = new FontSettings { IsTypewriter = true }
                    };
                case "\\textsc":
                    return new TextElement
                    {
                        Text = string.Join("", content.OfType<TextElement>().Select(t => t.Text)),
                        FontSettings = new FontSettings { IsSmallCaps = true }
                    };
                default:
                    return new TextElement
                    {
                        Text = string.Join("", content.OfType<TextElement>().Select(t => t.Text)),
                        FontSettings = currentFontSettings
                    };
            }
        }

        private void ApplyFontStyle(string command)
        {
            currentFontSettings = new FontSettings();
            switch (command)
            {
                case "\\rm":
                case "\\rmfamily":
                    currentFontSettings.FontFamily = "serif";
                    break;
                case "\\sf":
                case "\\sffamily":
                    currentFontSettings.FontFamily = "sans-serif";
                    break;
                case "\\tt":
                case "\\ttfamily":
                    currentFontSettings.FontFamily = "monospace";
                    break;
                case "\\bf":
                case "\\bfseries":
                    currentFontSettings.IsBold = true;
                    break;
                case "\\it":
                case "\\itshape":
                    currentFontSettings.IsItalic = true;
                    break;
                case "\\normalfont":
                    ResetFontSettings();
                    break;
                // Font sizes
                case "\\tiny": currentFontSettings.FontSize = "tiny"; break;
                case "\\scriptsize": currentFontSettings.FontSize = "scriptsize"; break;
                case "\\footnotesize": currentFontSettings.FontSize = "footnotesize"; break;
                case "\\small": currentFontSettings.FontSize = "small"; break;
                case "\\normalsize": currentFontSettings.FontSize = "normalsize"; break;
                case "\\large": currentFontSettings.FontSize = "large"; break;
                case "\\Large": currentFontSettings.FontSize = "Large"; break;
                case "\\LARGE": currentFontSettings.FontSize = "LARGE"; break;
                case "\\huge": currentFontSettings.FontSize = "huge"; break;
                case "\\Huge": currentFontSettings.FontSize = "Huge"; break;
            }
        }

        private void HandleFontSetting(Token token)
        {
            switch (token.Value)
            {
                case string s when s.StartsWith("\\setmainfont"):
                    ParseFontFamilySetting();
                    break;
                case string s when s.StartsWith("\\fontsize"):
                    ParseFontSizeSetting();
                    break;
                case string s when s.StartsWith("\\linespread"):
                    ParseLineSpreadSetting();
                    break;
            }
        }

        private void ParseFontFamilySetting()
        {
            Consume(TokenType.BracketOpen);
            currentFontSettings.FontFamily = Consume(TokenType.Text).Value;
            Consume(TokenType.BracketClose);
        }

        private void ParseFontSizeSetting()
        {
            Consume(TokenType.BracketOpen);
            var size = Consume(TokenType.Text).Value;
            Consume(TokenType.BracketClose);
            currentFontSettings.FontSize = size;
        }

        private void ParseLineSpreadSetting()
        {
            Consume(TokenType.BracketOpen);
            if (double.TryParse(Consume(TokenType.Text).Value, out double spacing))
            {
                currentFontSettings.LineSpacing = spacing;
            }
            Consume(TokenType.BracketClose);
        }

        private void ResetFontSettings()
        {
            currentFontSettings = new FontSettings();
        }

        private IDocumentElement ParseElement()
        {
            if(endedEnvironment)
            {
                //Force exit of consumtion.
                endedEnvironment = false;
                return null;
            }
            var token = Consume();

            switch (token.Type)
            {
                case TokenType.FontCommand:
                    return ParseFontCommand(token);
                case TokenType.FontSetting:
                    HandleFontSetting(token);
                    return null;
                case TokenType.InlineCommand:
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
                        case "\\title":
                            return ParseTitle();
                        case "\\author":
                            return PraseAuthor();
                        case "\\maketitle":
                            return MakeTitle();
                        default:
                            // Handle other commands or ignore
                            break;
                    }
                    break;
                case TokenType.Text:
                    return new Paragraph { Content = new List<InlineElement> { new TextElement { Text = token.Value, FontSettings = currentFontSettings } } };
                case TokenType.MathStart:
                    return ParseMathExpression();
                case TokenType.BeginEnvironment:
                    return ParseEnvironment();
                case TokenType.NewLine:
                    return ParseNewLine();
                case TokenType.Placeholder:
                    return ParsePlaceholder(token);
            }

            return null;
        }

        private IDocumentElement MakeTitle()
        {
            if (string.IsNullOrEmpty(this.document.Title)) throw new Exception("No title is provided.");
            var p = new Paragraph();
            p.Content.Add(new TextElement { Text = this.document.Title, FontSettings = new FontSettings { FontSize = "Huge" } });
            this.document.Elements.Add(p);
            if (!string.IsNullOrEmpty(this.document.Author))
            {
                p = new Paragraph();
                p.Content.Add( new TextElement { Text = this.document.Author, FontSettings = new FontSettings { FontSize = "Large" } } );
                this.document.Elements.Add(p);
            }

            if (this.document.Date != null) p.Content.Add(ParseDate());
            return null;
        }

        private InlineElement ParseDate()
        {
            throw new NotImplementedException();
        }

        private IDocumentElement PraseAuthor()
        {
            Consume(TokenType.BracketOpen);
            var content = ParseInlineContent();
            Consume(TokenType.BracketClose);
            this.document.Author = string.Join("", content.OfType<TextElement>().Select(t => t.Text));
            return null;
        }

        private IDocumentElement ParseTitle()
        {
            Consume(TokenType.BracketOpen);
            var content = ParseInlineContent();
            Consume(TokenType.BracketClose);
            this.document.Title = string.Join("", content.OfType<TextElement>().Select(t => t.Text));
            return null;
        }

        private IDocumentElement ParseNewLine()
        {
            consecutiveNewLines++;

            if (consecutiveNewLines >= 2)
            {
                // Two or more consecutive newlines indicate a paragraph break
                consecutiveNewLines = 0; // Reset the counter
                return new ParagraphBreak();
            }

            // Single newlines are typically ignored in LaTeX, so we return null
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

            while (currentIndex < tokens.Count && tokens[currentIndex].Value != "\\section" && tokens[currentIndex].Value != "\\subsection")
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
                if (element is not ParagraphBreak && element is not null) currentFontSettings = new FontSettings();

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
                        if (tokens[currentIndex].Value != "\\item") { ParseElement(); continue; }
                        var element = ParseElement();
                        if (element != null && element is Paragraph paragraph)
                        {
                            ((Itemize)environment).Content.Add(paragraph); // Add items to the unordered list
                        }
                    }
                    break;

                case "enumerate":
                    environment = new Enumerate();
                    while (currentIndex < tokens.Count && !(tokens[currentIndex].Type == TokenType.EndEnvironment && tokens[currentIndex].Value.Contains("enumerate")))
                    {
                        if (tokens[currentIndex].Value != "\\item") { ParseElement(); continue; }
                        var element = ParseElement();
                        if (element != null && element is Paragraph paragraph)
                        {
                            ((Enumerate)environment).Content.Add(paragraph); // Add items to the ordered list
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
            try
            {
                Consume(TokenType.EndEnvironment); // Consume \end{environmentName}
            }
            catch { }
                endedEnvironment = true;
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
                        content.Add(new TextElement { Text = token.Value, FontSettings = currentFontSettings });
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
            //Consume(TokenType.BracketClose);
            return content;
        }

        private Token Consume(TokenType? expectedType = null)
        {
            if (currentIndex > tokens.Count)
                throw new InvalidOperationException("Unexpected end of input");


            var token = tokens[currentIndex++];

            //// Ignore empty newlines that shouldn't create paragraphs
            //if (token.Type == TokenType.NewLine)
            //{
            //    return Consume(expectedType);
            //}

            if (expectedType.HasValue && token.Type != expectedType)
                throw new InvalidOperationException($"Expected {expectedType}, but got {token.Type}");

            return token;
        }

        private PlaceholderElement ParsePlaceholder(Token token)
        {
            // Extract the ID and content from \placeholder{nif}[9999999Y]
            string value = token.Value;
            
            // Extract ID from between curly braces
            string id = "";
            int idStart = value.IndexOf('{') + 1;
            int idEnd = value.IndexOf('}');
            if (idStart > 0 && idEnd > idStart)
            {
                id = value.Substring(idStart, idEnd - idStart);
            }
            
            // Extract content from between square brackets
            string content = "";
            int contentStart = value.IndexOf('[') + 1;
            int contentEnd = value.IndexOf(']');
            if (contentStart > 0 && contentEnd > contentStart)
            {
                content = value.Substring(contentStart, contentEnd - contentStart);
            }
            
            return new PlaceholderElement
            {
                Id = id,
                Content = content,
                FontSettings = currentFontSettings
            };
        }
    }
}

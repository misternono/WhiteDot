    using System;
    using System.Linq;
    using Xunit;
    using DotTex2.Lexing;
    using DotTex2.Parsing;
    using DotTex2.Model;
    using Newtonsoft.Json.Linq;
    using static System.Collections.Specialized.BitVector32;
    using System.Diagnostics.Metrics;
    using System.Xml.XPath;
using Assert = Xunit.Assert;
using Section = DotTex2.Model.Section;
using DotTex2.Model.InlineElements;

namespace DotTex2.Test
{

        public class LexerTests
        {
            private readonly Lexer _lexer = new Lexer();

            [Fact]
            public void Tokenize_SimpleText_ReturnsCorrectTokens()
            {
                var input = "Hello, world!";
                var tokens = _lexer.Tokenize(input).ToList();

                Assert.Single(tokens);
                Assert.Equal(TokenType.Text, tokens[0].Type);
                Assert.Equal("Hello, world!", tokens[0].Value);
            }

            [Fact]
            public void Tokenize_Command_ReturnsCorrectTokens()
            {
                var input = "\\section{Introduction}";
                var tokens = _lexer.Tokenize(input).ToList();

                Assert.Equal(4, tokens.Count);
                Assert.Equal(TokenType.Command, tokens[0].Type);
                Assert.Equal("\\section", tokens[0].Value);
                Assert.Equal(TokenType.BracketOpen, tokens[1].Type);
                Assert.Equal(TokenType.Text, tokens[2].Type);
                Assert.Equal("Introduction", tokens[2].Value);
                Assert.Equal(TokenType.BracketClose, tokens[3].Type);
            }

            [Fact]
            public void Tokenize_MathExpression_ReturnsCorrectTokens()
            {
                var input = "The equation $E=mc^2$ is famous.";
                var tokens = _lexer.Tokenize(input).ToList();

                Assert.Equal(5, tokens.Count);
                Assert.Equal(TokenType.Text, tokens[0].Type);
                Assert.Equal("The equation ", tokens[0].Value);
                Assert.Equal(TokenType.MathStart, tokens[1].Type);
                Assert.Equal(TokenType.Text, tokens[2].Type);
                Assert.Equal("E=mc^2", tokens[2].Value);
                Assert.Equal(TokenType.MathStart, tokens[3].Type);
                Assert.Equal(TokenType.Text, tokens[4].Type);
                Assert.Equal(" is famous.", tokens[4].Value);
            }
        }

        public class ParserTests
        {
            private Parser CreateParser(string input)
            {
                var lexer = new Lexer();
                var tokens = lexer.Tokenize(input).ToList();
                return new Parser(tokens);
            }

            [Fact]
            public void Parse_SimpleDocument_ReturnsCorrectStructure()
            {
                var input = "\\section{Introduction}\nThis is a paragraph.";
                var parser = CreateParser(input);
                var document = parser.Parse();

                Assert.Single(document.Elements);
                var section = Assert.IsType<Section>(document.Elements[0]);
                Assert.Equal("Introduction", section.Title);
                Assert.Single(section.Content);
                var paragraph = Assert.IsType<Paragraph>(section.Content[0]);
                Assert.Single(paragraph.Content);
                var text = Assert.IsType<TextElement>(paragraph.Content[0]);
                Assert.Equal("This is a paragraph.", text.Text);
            }

            [Fact]
            public void Parse_MathExpression_ReturnsCorrectStructure()
            {
                var input = "The famous equation is $E=mc^2$.";
                var parser = CreateParser(input);
                var document = parser.Parse();

                Assert.Equal(2, document.Elements.Count);
                var paragraph1 = Assert.IsType<Paragraph>(document.Elements[0]);
                Assert.Single(paragraph1.Content);
                var text1 = Assert.IsType<TextElement>(paragraph1.Content[0]);
                Assert.Equal("The famous equation is ", text1.Text);

                var math = Assert.IsType<MathExpression>(document.Elements[1]);
                Assert.True(math.IsInline);
                Assert.Equal("E=mc^2", math.Expression);

                var paragraph2 = Assert.IsType<Paragraph>(document.Elements[2]);
                Assert.Single(paragraph2.Content);
                var text2 = Assert.IsType<TextElement>(paragraph2.Content[0]);
                Assert.Equal(".", text2.Text);
            }

            [Fact]
            public void Parse_BoldText_ReturnsCorrectStructure()
            {
                var input = "This is \\textbf{bold} text.";
                var parser = CreateParser(input);
                var document = parser.Parse();

                Assert.Single(document.Elements);
                var paragraph = Assert.IsType<Paragraph>(document.Elements[0]);
                Assert.Equal(3, paragraph.Content.Count);

                var text1 = Assert.IsType<TextElement>(paragraph.Content[0]);
                Assert.Equal("This is ", text1.Text);

                var bold = Assert.IsType<BoldText>(paragraph.Content[1]);
                Assert.Equal("bold", bold.Text);

                var text2 = Assert.IsType<TextElement>(paragraph.Content[2]);
                Assert.Equal(" text.", text2.Text);
            }
        }
    
}
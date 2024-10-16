using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex.Parser
{
    using DotTex.Tags;
    using DotTex.Document;
    using System;
    using System.Collections.Generic;
    using DotTex.Tags.Abstract;

    public class LatexParser
    {
        private List<Token> tokens;
        private int currentIndex;

        public LatexParser(List<Token> tokens)
        {
            this.tokens = tokens;
            this.currentIndex = 0;
        }

        // Start parsing the document from tokens
        public Document ParseDocument()
        {
            Document doc = new Document();
            while (currentIndex < tokens.Count)
            {
                var section = ParseSection();
                if (section != null)
                {
                    doc.AddTag(section);
                }
            }
            return doc;
        }

        // Parse a section
        private Section ParseSection()
        {
            if (MatchToken(TokenType.Command, "\\section"))
            {
                string title = ConsumeToken(TokenType.Argument).Value;
                Section section = new Section(title);

                while (currentIndex < tokens.Count && !MatchToken(TokenType.Command, "\\section"))
                {
                    var tag = ParseTag();
                    if (tag != null)
                    {
                        section.AddTag(tag);
                    }
                }
                return section;
            }
            return null;
        }

        // Parse individual tags (like paragraphs, equations, etc.)
        private LatexTag ParseTag()
        {
            if (MatchToken(TokenType.Text))
            {
                return new Paragraph(ConsumeToken(TokenType.Text).Value);
            }
            else if (MatchToken(TokenType.BeginEnvironment, "equation"))
            {
                return ParseEquation();
            }
            else if (MatchToken(TokenType.Command, "\\frac"))
            {
                return ParseFraction();
            }

            // Handle other tag types (e.g., lists, tables, figures) here...

            return null;
        }

        // Parse equations
        private Equation ParseEquation()
        {
            string equationContent = "";

            while (!MatchToken(TokenType.EndEnvironment, "equation"))
            {
                equationContent += ConsumeToken().Value + " ";
            }

            ConsumeToken(TokenType.EndEnvironment); // Consume \end{equation}
            return new Equation(equationContent.Trim());
        }

        // Parse fractions
        private Fraction ParseFraction()
        {
            var numerator = ConsumeToken(TokenType.Argument).Value;
            var denominator = ConsumeToken(TokenType.Argument).Value;
            return new Fraction(numerator, denominator);
        }

        // Helper functions for matching and consuming tokens
        private bool MatchToken(TokenType type, string value = null)
        {
            if (currentIndex >= tokens.Count) return false;
            if (tokens[currentIndex].Type != type) return false;
            if (value != null && tokens[currentIndex].Value != value) return false;
            return true;
        }

        private Token ConsumeToken(TokenType type = TokenType.Unknown)
        {
            if (currentIndex >= tokens.Count) return null;
            if (type != TokenType.Unknown && tokens[currentIndex].Type != type) return null;
            return tokens[currentIndex++];
        }
    }

}

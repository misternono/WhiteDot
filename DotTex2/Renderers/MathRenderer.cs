using System.Drawing.Imaging;
using System.Drawing;
using System.Text;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace DotRender.Renderers
{
    public class LaTeXMathRenderer
    {
        private const int BaseFontSize = 20;
        private const int SubScriptFontSize = 12;
        private const int SuperScriptFontSize = 12;
        private const int MaxBitmapWidth = 2000;
        private const int MaxBitmapHeight = 200;

        // Dictionary of LaTeX special characters and symbols
        private static readonly Dictionary<string, string> SpecialCharacters = new Dictionary<string, string>
    {
        { "\\pi", "π" },
        { "\\infty", "∞" },
        { "\\alpha", "α" },
        { "\\beta", "β" },
        { "\\gamma", "γ" },
        { "\\delta", "δ" },
        { "\\epsilon", "ε" },
        { "\\zeta", "ζ" },
        { "\\eta", "η" },
        { "\\theta", "θ" },
        { "\\lambda", "λ" },
        { "\\mu", "μ" },
        { "\\nu", "ν" },
        { "\\xi", "ξ" },
        { "\\sigma", "σ" },
        { "\\tau", "τ" },
        { "\\phi", "φ" },
        { "\\chi", "χ" },
        { "\\psi", "ψ" },
        { "\\omega", "ω" },
        { "\\Gamma", "Γ" },
        { "\\Delta", "Δ" },
        { "\\Theta", "Θ" },
        { "\\Lambda", "Λ" },
        { "\\Xi", "Ξ" },
        { "\\Pi", "Π" },
        { "\\Sigma", "Σ" },
        { "\\Phi", "Φ" },
        { "\\Psi", "Ψ" },
        { "\\Omega", "Ω" }
    };

        public static Bitmap RenderLatexFormula(string formula, float fontSize = 20)
        {
            // Remove the $$ delimiters
            formula = formula.Trim('$');

            // Create a bitmap with enough space
            Bitmap bmp = new Bitmap(MaxBitmapWidth, MaxBitmapHeight);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                // Set up high-quality rendering
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // White background
                g.Clear(Color.White);

                // Track rendering progress
                RenderContext context = new RenderContext
                {
                    Graphics = g,
                    CurrentX = 10,
                    CurrentY = 100,
                    FontSize = fontSize
                };

                RenderMathExpression(ref context, formula);

                return TrimBitmap(bmp, context);
            }
        }

        private struct RenderContext
        {
            public Graphics Graphics;
            public float CurrentX;
            public float CurrentY;
            public float FontSize;
            public bool IsLimit = false;

            public RenderContext()
            {
            }
        }


        private static void RenderMathExpression(ref RenderContext context, string expression)
        {
            for (int i = 0; i < expression.Length; i++)
            {
                char currentChar = expression[i];

                // Check for LaTeX special characters or commands
                if (currentChar == '\\')
                {
                    // Look for the longest matching special character
                    string matchedSymbol = FindLongestSpecialCharacter(expression.Substring(i));

                    if (!string.IsNullOrEmpty(matchedSymbol))
                    {
                        // Render the special character
                        RenderSpecialCharacter(ref context, matchedSymbol);

                        // Skip past the entire LaTeX command
                        i += matchedSymbol.Length - 1;
                        continue;
                    }
                }

                switch (currentChar)
                {
                    case '^':
                        // Recursive superscript handling
                        i++;
                        if (i < expression.Length)
                        {
                            RenderRecursiveSuperscript(ref context, expression, ref i);
                        }
                        break;

                    case '_':
                        // Recursive subscript handling
                        i++;
                        if (i < expression.Length)
                        {
                            RenderRecursiveSubscript(ref context, expression, ref i);
                        }
                        break;

                    case '\\':
                        // LaTeX command handling
                        i = HandleLaTeXCommand(ref context, expression, i);
                        break;

                    default:
                        // Regular character rendering
                        RenderCharacter(ref context, currentChar);
                        break;
                }
            }
        }

        private static void RenderRecursiveSubscript(ref RenderContext context, string expression, ref int index)
        {
            // Extract the subscript expression
            string subscriptExpression = ExtractMathExpression(expression, ref index);

            float fontSixeToReset = context.FontSize;
            float yToReset = context.CurrentY;
            // Render the subscript as a separate bitmap
            context.FontSize = (float)Math.Round(context.FontSize * 0.6f);
            context.CurrentY = context.CurrentY + context.FontSize * 1.6f;
            RenderMathExpression(ref context, subscriptExpression);

            context.CurrentY = yToReset;
            context.FontSize = fontSixeToReset;
        }

        private static void RenderRecursiveSuperscript(ref RenderContext context, string expression, ref int index)
        {
            // Extract the superscript expression
            string superscriptExpression = ExtractMathExpression(expression, ref index);

            // Render the superscript as a separate bitmap
            float fontSizeToReset = context.FontSize;
            context.FontSize = (float)Math.Round(context.FontSize * 0.6f); ;
            float yToReset = context.CurrentY;
            if (context.IsLimit)
            {
                context.CurrentX -= context.FontSize;
                context.IsLimit = false;
                context.CurrentY = context.CurrentY - context.FontSize * 0.4f;
            }
            RenderMathExpression(ref context, superscriptExpression);
            context.CurrentY = yToReset;
            context.FontSize = fontSizeToReset;

        }

        private static string ExtractMathExpression(string expression, ref int index)
        {
            // Handle grouped expressions in {}
            if (index < expression.Length && expression[index] == '{')
            {
                int depth = 1;
                int start = index;
                int end = start + 1;

                // Find matching closing brace
                while (end < expression.Length && depth > 0)
                {
                    if (expression[end] == '{') depth++;
                    if (expression[end] == '}') depth--;
                    end++;
                }

                // Update index to the end of the expression
                index = end - 1;

                // Return the expression inside the braces
                return expression.Substring(start + 1, end - start - 2);
            }

            // Handle single character expressions
            return expression[index].ToString();
        }



        private static string FindLongestSpecialCharacter(string remaining)
        {
            return SpecialCharacters.Keys
                .Where(symbol => remaining.StartsWith(symbol))
                .OrderByDescending(symbol => symbol.Length)
                .FirstOrDefault();
        }

        private static void RenderSpecialCharacter(ref RenderContext g, string symbol)
        {
            // Render special character from the dictionary
            string characterToRender = SpecialCharacters[symbol];

            using (Font font = new Font("Times New Roman", g.FontSize, FontStyle.Italic))
            {
                g.Graphics.DrawString(characterToRender, font, Brushes.Black, g.CurrentX, g.CurrentY);
                g.CurrentX += g.Graphics.MeasureString(characterToRender, font).Width;
            }
        }

        private static void RenderCharacter(ref RenderContext g, char c)
        {
            using (Font font = new Font("Times New Roman", g.FontSize, FontStyle.Italic))
            {
                g.Graphics.DrawString(c.ToString(), font, Brushes.Black, g.CurrentX, g.CurrentY);
                g.CurrentX += g.Graphics.MeasureString(c.ToString(), font).Width;
            }
        }

        private static void RenderSuperscript(ref RenderContext g, char c)
        {
            using (Font font = new Font("Times New Roman", g.FontSize * 0.6f, FontStyle.Italic))
            {
                // Render superscript slightly higher and smaller
                float superX = g.CurrentX;
                float superY = g.CurrentY - g.FontSize * 0.4f;
                g.Graphics.DrawString(c.ToString(), font, Brushes.Black, g.CurrentX, g.CurrentY);
                g.CurrentX += g.Graphics.MeasureString(c.ToString(), font).Width;
            }
        }

        private static void RenderSubscript(ref RenderContext g, char c)
        {
            using (Font font = new Font("Times New Roman", g.FontSize * 0.6f, FontStyle.Italic))
            {
                // Render subscript slightly lower and smaller
                float subX = g.CurrentX;
                float subY = g.CurrentY + g.FontSize * 0.4f;
                g.Graphics.DrawString(c.ToString(), font, Brushes.Black, g.CurrentX, g.CurrentY);
                g.CurrentX += g.Graphics.MeasureString(c.ToString(), font).Width;
            }
        }

        private static int HandleLaTeXCommand(ref RenderContext g, string expression, int startIndex)
        {
            // Basic LaTeX command handling for fractions, integrals, etc.
            string[] commands = new[] { "\\frac", "\\int", "\\sqrt" };

            foreach (string cmd in commands)
            {
                if (expression.Substring(startIndex).StartsWith(cmd))
                {
                    switch (cmd)
                    {
                        case "\\frac":
                            return RenderFraction(ref g, expression, startIndex);

                        case "\\int":
                            return RenderIntegral(ref g, expression, startIndex);

                        case "\\sqrt":
                            return RenderSquareRoot(ref g, expression, startIndex);
                    }
                }
            }

            // If no special command found, render the character
            if (startIndex + 1 < expression.Length)
            {
                RenderCharacter(ref g, expression[startIndex + 1]);
                return startIndex + 1;
            }

            return startIndex;
        }

        private static int RenderFraction(ref RenderContext g, string expression, int startIndex)
        {
            // Simplified fraction rendering
            // Assumes format: \frac{numerator}{denominator}
            int numeratorStart = expression.IndexOf('{', startIndex) + 1;
            int numeratorEnd = expression.IndexOf('}', numeratorStart) + 1;
            int denominatorStart = expression.IndexOf('{', numeratorEnd) + 1;
            int denominatorEnd = expression.IndexOf('}', denominatorStart) + 1;

            string numerator = expression.Substring(numeratorStart, numeratorEnd - numeratorStart);
            string denominator = expression.Substring(denominatorStart, denominatorEnd - denominatorStart);

            float savedX = g.CurrentX;
            float savedY = g.CurrentY;

            if (!denominator.StartsWith("\\")) denominator = denominator.TrimEnd('}');
            if (!numerator.StartsWith("\\")) numerator = numerator.TrimEnd('}');

            // Render numerator (smaller font, centered)
            float numFontSize = g.FontSize * 0.7f;
            //float numX = savedX;
            g.CurrentY = savedY - numFontSize;
            RenderMathExpression(ref g, numerator);
            g.CurrentY = savedY + numFontSize;
            g.CurrentX = savedX;
            // Render denominator (smaller font, centered)
            float denomFontSize = g.FontSize * 0.7f;
            float denomX = savedX;
            float denomY = savedY + g.FontSize * 0.5f;
            RenderMathExpression(ref g, denominator);
            g.CurrentY = savedY;
            g.CurrentX = savedX;
            // Render fraction line
            using (Pen pen = new Pen(Color.Black, 1))
            {
                g.Graphics.DrawLine(pen, savedX, savedY + g.FontSize, savedX + Math.Max(
                    g.Graphics.MeasureString(numerator, new Font("Times New Roman", numFontSize)).Width,
                    g.Graphics.MeasureString(denominator, new Font("Times New Roman", denomFontSize)).Width),
                    savedY + g.FontSize);
            }

            g.CurrentX += Math.Max(
                g.Graphics.MeasureString(numerator, new Font("Times New Roman", numFontSize)).Width,
                g.Graphics.MeasureString(denominator, new Font("Times New Roman", denomFontSize)).Width);

            return denominatorEnd;
        }

        private static int RenderIntegral(ref RenderContext g, string expression, int startIndex)
        {
            // Render integral symbol
            using (Font font = new Font("Times New Roman", g.FontSize * 1.2f, FontStyle.Italic))
            {
                g.Graphics.DrawString("∫", font, Brushes.Black, g.CurrentX, g.CurrentY);
                g.CurrentX += g.Graphics.MeasureString("∫", font).Width;
            }
            g.IsLimit = true;
            return startIndex + 3; // Length of "\int"
        }

        private static int RenderSquareRoot(ref RenderContext g, string expression, int startIndex)
        {
            // Simplified square root rendering
            // Assumes format: \sqrt{expression}
            int contentStart = expression.IndexOf('{', startIndex) + 1;
            int contentEnd = expression.IndexOf('}', contentStart);

            string content = expression.Substring(contentStart, contentEnd - contentStart);

            // Render root symbol
            using (Font font = new Font("Times New Roman", g.FontSize * 1.2f, FontStyle.Italic))
            {
                g.Graphics.DrawString("√", font, Brushes.Black, g.CurrentX, g.CurrentY);
                g.CurrentX += g.Graphics.MeasureString("√", font).Width;
            }

            // Render content under root
            RenderMathExpression(ref g, content);

            return contentEnd;
        }

        private static Bitmap TrimBitmap(Bitmap source, RenderContext context)
        {
            // Find the bounds of the non-white area
            int minX = source.Width, maxX = 0, minY = source.Height, maxY = 0;

            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    Color pixelColor = source.GetPixel(x, y);
                    if (pixelColor.R != 255 || pixelColor.B != 255 || pixelColor.G != 255)
                    {
                        minX = Math.Min(minX, x);
                        maxX = Math.Max(maxX, x);
                        minY = Math.Min(minY, y);
                        maxY = Math.Max(maxY, y);
                    }
                }
            }

            // If no non-white pixels found, return original bitmap
            if (minX == source.Width || maxX == 0 || minY == source.Height || maxY == 0)
                return source;

            // Add a small padding (3-5 pixels) to prevent cutting off edges of characters
            int padding = 4;
            minX = Math.Max(0, minX - padding);
            minY = Math.Max(0, minY - padding);
            maxX = Math.Min(source.Width - 1, maxX + padding);
            maxY = Math.Min(source.Height - 1, maxY + padding);

            // Calculate cropped dimensions
            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            // Create new bitmap with precisely trimmed dimensions
            Bitmap trimmed = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(trimmed))
            {
                g.Clear(Color.White);
                g.DrawImage(source,
                    new Rectangle(0, 0, width, height),  // Destination rectangle
                    new Rectangle(minX, minY, width, height),  // Source rectangle
                    GraphicsUnit.Pixel);
            }

            return trimmed;
        }
    }
}
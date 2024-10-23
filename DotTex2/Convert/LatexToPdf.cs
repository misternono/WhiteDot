using DotTex2.Model;
using DotTex2.Model.Environments;
using DotTex2.Model.InlineElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotTex2.Convert
{
    public class LatexToPdf
    {
        private readonly Dictionary<string, int> fontSizeMap = new Dictionary<string, int>
        {
            {"tiny", 6},
            {"scriptsize", 8},
            {"footnotesize", 9},
            {"small", 10},
            {"normalsize", 12},
            {"large", 14},
            {"Large", 16},
            {"LARGE", 18},
            {"huge", 20},
            {"Huge", 24}
        };

        private StringBuilder pdf = new StringBuilder();
        private int objectCount = 0;
        private bool isNewLine = true;
        private List<int> objectPositions = new List<int>();
        private int currentY = 800; // Start from top of the page
        private int sectionIndex = 0;
        private int subsectionIndex = 0;
        private const int PAGE_HEIGHT = 842;
        private const int PAGE_MARGIN_BOTTOM = 50;
        private const int NEW_PAGE_START_Y = 800;

        private List<string> contentStreams = new List<string>();
        private StringBuilder currentPageContent = new StringBuilder();

        public void GeneratePDF(Document doc, string outputPath)
        {
            pdf.AppendLine("%PDF-1.4");

            // Create initial objects
            int catalogPos = AddObject("<< /Type /Catalog /Pages 2 0 R >>");

            // Render content and collect pages
            RenderContent(doc);

            // Create page tree
            StringBuilder kidsString = new StringBuilder("[");
            int pageCount = contentStreams.Count;
            int firstPageRef = 3; // First page object reference

            for (int i = 0; i < pageCount; i++)
            {
                if (i > 0) kidsString.Append(" ");
                kidsString.Append($"{firstPageRef + (i * 2)} 0 R");
            }
            kidsString.Append("]");

            int pagesPos = AddObject($"<< /Type /Pages /Kids {kidsString} /Count {pageCount} >>");

            // Add each page and its content stream
            for (int i = 0; i < contentStreams.Count; i++)
            {
                int pageContentRef = firstPageRef + (i * 2) + 1;
                AddObject($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 {PAGE_HEIGHT}] /Contents {pageContentRef} 0 R /Resources << /Font << /F1 5 0 R /F1B 6 0 R /F1I 7 0 R >> >> >>");
                AddStreamObject(contentStreams[i]);
            }

            // Add font objects
            int fontPos = AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
            int fontBoldPos = AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");
            int fontItalicPos = AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Oblique >>");
            int fontBoldItalicPos = AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-BoldOblique >>");
            int fontMonoPos = AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>");
            int fontMonoBoldPos = AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Courier-Bold >>");
            int fontMonoItalicPos = AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Courier-Oblique >>");
            int fontMonoBoldItalicPos = AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Courier-BoldOblique >>");

            string fontResources = @"<< /Font 
            << /F1 5 0 R 
               /F1B 6 0 R 
               /F1I 7 0 R 
               /F1BI 8 0 R
               /F2 9 0 R
               /F2B 10 0 R
               /F2I 11 0 R
               /F2BI 12 0 R
            >> >>";

            for (int i = 0; i < contentStreams.Count; i++)
            {
                int pageContentRef = firstPageRef + (i * 2) + 1;
                AddObject($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 {PAGE_HEIGHT}] /Contents {pageContentRef} 0 R /Resources {fontResources} >>");
                AddStreamObject(contentStreams[i]);
            }

            GenerateXrefAndTrailer(outputPath);
        }

        private string GetFontName(FontSettings settings)
        {
            
            string baseFont = settings.IsTypewriter ? "F2" : "F1";
            if (settings.IsBold && settings.IsItalic) return $"{baseFont}BI";
            if (settings.IsBold) return $"{baseFont}B";
            if (settings.IsItalic) return $"{baseFont}I";
            return baseFont;
        }

        private int GetFontSize(FontSettings settings)
        {
            return fontSizeMap.TryGetValue(settings.FontSize, out int size) ? size : 12;
        }

        private void ApplyFontSettings(FontSettings settings)
        {
            if (settings == null) { ApplyDefaultFont(); return; }
            string fontName = GetFontName(settings);
            int fontSize = GetFontSize(settings);
            currentPageContent.Append($"/{fontName} {fontSize} Tf ");
        }

        private void ApplyDefaultFont()
        {
            currentPageContent.Append("/F1 12 Tf ");
        }

        private void CheckPageBreak(int requiredSpace)
        {
            if (currentY - requiredSpace < PAGE_MARGIN_BOTTOM)
            {
                // End current page
                if (!isNewLine)
                {
                    currentPageContent.AppendLine("ET");
                }

                // Save current page content
                contentStreams.Add(currentPageContent.ToString());

                // Start new page
                currentPageContent = new StringBuilder();
                currentPageContent.AppendLine("BT");
                currentY = NEW_PAGE_START_Y;
                isNewLine = false;
            }
        }

        private string RenderContent(Document doc)
        {
            currentPageContent = new StringBuilder();
            currentPageContent.AppendLine("BT");
            isNewLine = false;

            foreach (var element in doc.Elements)
            {
                RenderElement(element);
            }

            if (!isNewLine)
            {
                currentPageContent.AppendLine("ET");
            }

            // Add the last page
            contentStreams.Add(currentPageContent.ToString());
            return currentPageContent.ToString();
        }

        private void RenderElement(IDocumentElement element)
        {
            switch (element)
            {
                case Paragraph p:
                    CheckPageBreak(30); // Estimated space needed for a paragraph line
                    RenderParagraph(p);
                    break;
                case Section s:
                    CheckPageBreak(50); // Space for section heading
                    this.sectionIndex++;
                    this.subsectionIndex = 0;
                    if (!isNewLine)
                    {
                        currentPageContent.AppendLine("ET");
                        currentY -= 20;
                    }
                    isNewLine = true;
                    RenderSection(s);
                    break;
                case Subsection s:
                    CheckPageBreak(40); // Space for subsection heading
                    this.subsectionIndex++;
                    if (!isNewLine)
                    {
                        currentPageContent.AppendLine("ET");
                        currentY -= 20;
                    }
                    isNewLine = true;
                    RenderSubection(s);
                    break;
                case MathExpression m:
                    CheckPageBreak(30); // Space for math expression
                    if (!isNewLine)
                    {
                        currentPageContent.AppendLine("ET");
                        currentY -= 20;
                    }
                    isNewLine = true;
                    RenderMath(m);
                    break;
                case ParagraphBreak pr:
                    if (!isNewLine)
                    {
                        currentPageContent.AppendLine("ET");
                        currentY -= 20;
                    }
                    isNewLine = true;
                    RenderNewLine();
                    break;
                case InlineElement il:
                    CheckPageBreak(20); // Space for inline element
                    RenderInline(il);
                    break;
                case Model.Environment env:
                    switch (env)
                    {
                        case Itemize it:
                            CheckPageBreak(30 * it.Content.Count); // Space for itemize list
                            RenderItemize(it);
                            break;
                        default:
                            foreach (var cont in env.Content)
                            {
                                RenderElement(cont);
                            }
                            break;
                    }
                    break;
                default:
                    Console.WriteLine("unknown" + element.ToString());
                    break;
            }
        }

        private void RenderItemize(Itemize it)
        {
            if (isNewLine)
            {
                currentPageContent.AppendLine("BT");
                ApplyDefaultFont();
                currentPageContent.AppendLine($"50 {currentY} Td");
                isNewLine = false;
            }

            foreach (var cont in it.Content)
            {
                currentPageContent.AppendLine("BT");
                ApplyDefaultFont();
                currentPageContent.AppendLine($"50 {currentY} Td");
                currentPageContent.Append($"({" · "}) Tj ");
                currentY -= 30;
                RenderElement(cont);
            }
        }

        private void RenderParagraph(Paragraph p)
        {
            if (isNewLine)
            {
                currentPageContent.AppendLine("BT");
                ApplyDefaultFont();
                currentPageContent.AppendLine($"50 {currentY} Td");
                isNewLine = false;
            }

            foreach (var inline in p.Content)
            {
                switch (inline)
                {
                    case TextElement t:
                        ApplyFontSettings(t.FontSettings);
                        string text = t.FontSettings.IsSmallCaps
                            ? EscapeText(t.Text).ToUpper()
                            : EscapeText(t.Text);
                        currentPageContent.Append($"({text}) Tj ");
                        ApplyDefaultFont();
                        break;

                    case BoldText b:
                        ApplyFontSettings(b.FontSettings);
                        currentPageContent.Append($"({EscapeText(b.Text)}) Tj ");
                        ApplyDefaultFont();
                        break;

                    case ItalicText i:
                        ApplyFontSettings(i.FontSettings);
                        currentPageContent.Append($"({EscapeText(i.Text)}) Tj ");
                        ApplyDefaultFont();
                        break;

                    case TypewriterText t:
                        ApplyFontSettings(t.FontSettings);
                        currentPageContent.Append($"({EscapeText(t.Text)}) Tj ");
                        ApplyDefaultFont();
                        break;

                    case SmallCapsText s:
                        ApplyFontSettings(s.FontSettings);
                        currentPageContent.Append($"({EscapeText(s.Text.ToUpper())}) Tj ");
                        ApplyDefaultFont();
                        break;
                }
            }
        }

        private void RenderInline(InlineElement p)
        {
            if (isNewLine)
            {
                currentPageContent.AppendLine("BT");
                ApplyDefaultFont();
                currentPageContent.AppendLine($"50 {currentY} Td");
                isNewLine = false;
            }

            switch (p)
            {
                case TextElement t:
                    ApplyFontSettings(t.FontSettings);
                    string text = t.FontSettings.IsSmallCaps
                        ? EscapeText(t.Text).ToUpper()
                        : EscapeText(t.Text);
                    currentPageContent.Append($"({text}) Tj ");
                    break;

                case BoldText b:
                    ApplyFontSettings(b.FontSettings);
                    currentPageContent.Append($"({EscapeText(b.Text)}) Tj ");
                    break;

                case ItalicText i:
                    ApplyFontSettings(i.FontSettings);
                    currentPageContent.Append($"({EscapeText(i.Text)}) Tj ");
                    break;

                case TypewriterText t:
                    ApplyFontSettings(t.FontSettings);
                    currentPageContent.Append($"({EscapeText(t.Text)}) Tj ");
                    break;

                case SmallCapsText s:
                    ApplyFontSettings(s.FontSettings);
                    currentPageContent.Append($"({EscapeText(s.Text.ToUpper())}) Tj ");
                    break;
            }
            ApplyDefaultFont();
        }

        private void RenderSection(Section s)
        {
            currentPageContent.AppendLine("BT");
            currentPageContent.AppendLine("/F1B 16 Tf"); // Sections use bold font by default
            currentPageContent.AppendLine($"50 {currentY} Td");
            currentPageContent.AppendLine($"({this.sectionIndex}. {EscapeText(s.Title)}) Tj");
            currentPageContent.AppendLine("ET");
            currentY -= 30;

            foreach (var sectionElement in s.Content)
            {
                RenderElement(sectionElement);
            }
        }

        private void RenderSubection(Subsection s)
        {
            currentPageContent.AppendLine("BT");
            currentPageContent.AppendLine("/F1B 14 Tf"); // Subsections use bold font by default
            currentPageContent.AppendLine($"50 {currentY} Td");
            currentPageContent.AppendLine($"({this.sectionIndex}.{this.subsectionIndex} {EscapeText(s.Title)}) Tj");
            currentPageContent.AppendLine("ET");
            currentY -= 30;

            foreach (var sectionElement in s.Content)
            {
                RenderElement(sectionElement);
            }
        }

        private void RenderNewLine()
        {
            //content.AppendLine("BT");
            //content.AppendLine("/F1 12 Tf");
            //content.AppendLine($"50 {currentY} Td");
            //content.AppendLine("T*");
            currentPageContent.AppendLine("ET");
            currentY -= 15;
        }

        private void RenderMath(MathExpression math)
        {
            currentPageContent.AppendLine("BT");
            currentPageContent.AppendLine("/F1I 12 Tf"); // Math uses italic by default
            currentPageContent.AppendLine($"50 {currentY} Td");
            currentPageContent.AppendLine($"({EscapeText(math.Expression)}) Tj");
            currentPageContent.AppendLine("ET");
            currentY -= 20;
        }


        // Escapes text for PDF format, e.g., handling parentheses
        private string EscapeText(string text)
        {
            return text.Replace("\\", "\\\\")
                       .Replace("(", "\\(")
                       .Replace(")", "\\)")
                       .Replace("\n", "\\n");
        }

        // Adds an object to the PDF and returns its position in the document
        private int AddObject(string content)
        {
            int position = pdf.Length;
            pdf.AppendLine($"{++objectCount} 0 obj");
            pdf.AppendLine(content);
            pdf.AppendLine("endobj");
            objectPositions.Add(position);
            return position;
        }

        private int AddStreamObject(string streamContent)
        {
            int position = pdf.Length;
            pdf.AppendLine($"{++objectCount} 0 obj");
            pdf.AppendLine($"<< /Length {streamContent.Length} >>");
            pdf.AppendLine("stream");
            pdf.AppendLine(streamContent);
            pdf.AppendLine("endstream");
            pdf.AppendLine("endobj");
            objectPositions.Add(position);
            return position;
        }

        private void GenerateXrefAndTrailer(string outputPath)
        {
            int xrefPosition = pdf.Length;
            pdf.AppendLine("xref");
            pdf.AppendLine($"0 {objectCount + 1}");
            pdf.AppendLine("0000000000 65535 f ");

            foreach (var pos in objectPositions)
            {
                pdf.AppendLine(pos.ToString("D10") + " 00000 n ");
            }

            pdf.AppendLine("trailer");
            pdf.AppendLine($"<< /Size {objectCount + 1} /Root 1 0 R >>");
            pdf.AppendLine("startxref");
            pdf.AppendLine(xrefPosition.ToString());
            pdf.AppendLine("%%EOF");

            File.WriteAllText(outputPath, pdf.ToString());
        }
    }
}

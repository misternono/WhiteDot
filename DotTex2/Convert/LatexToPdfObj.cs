using DotPdf.PdfGenerator;
using DotTex2.Model;
using DotTex2.Model.Environments;
using DotTex2.Model.InlineElements;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotTex2.Convert
{
    public class LatexToPdfObj
    {
        private readonly Dictionary<string, float> fontSizeMap = new Dictionary<string, float>
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

        private PdfDocument _pdfDocument;
        private PdfPage _currentPage;
        private int sectionIndex = 0;
        private int subsectionIndex = 0;

        public void GeneratePDF(Document doc, string outputPath)
        {
            _pdfDocument = new PdfDocument();

            // Subscribe to page changes
            _pdfDocument.OnPageChanged += (newPage) =>
            {
                _currentPage = newPage;
            };

            // Add standard fonts
            _pdfDocument.AddStandardFont("Times-Roman", "F1");
            _pdfDocument.AddStandardFont("Times-Bold", "F1B");
            _pdfDocument.AddStandardFont("Times-Italic", "F1I");
            _pdfDocument.AddStandardFont("Times-BoldItalic", "F1BI");

            // Create first page
            _currentPage = _pdfDocument.AddPage();

            // Render content
            RenderContent(doc);

            // Generate PDF
            var pdfContent = _pdfDocument.GeneratePdf();
            File.WriteAllText(outputPath, pdfContent);
        }

        private void RenderContent(Document doc)
        {
            foreach (var element in doc.Elements)
            {
                if (element is ParagraphBreak) continue;
                RenderElement(element);
            }
        }

        private void RenderElement(IDocumentElement element)
        {
            switch (element)
            {
                case Paragraph p:
                    RenderParagraph(p);
                    break;

                case Section s:
                    this.sectionIndex++;
                    this.subsectionIndex = 0;
                    RenderSection(s);
                    break;

                case Subsection s:
                    this.subsectionIndex++;
                    RenderSubsection(s);
                    break;

                case MathExpression m:
                    RenderMath(m);
                    break;

                case InlineElement il:
                    RenderInline(il);
                    break;

                case Model.Environment env:
                    switch (env)
                    {
                        case Itemize it:
                            RenderItemize(it);
                            break;

                        case Enumerate en:
                            RenderEnumerate(en);
                            break;

                        case Verbatim ve:
                            RenderVerbatim(ve);
                            break;

                        default:
                            foreach (var cont in env.Content)
                            {
                                RenderElement(cont);
                            }
                            break;
                    }
                    break;
            }
        }

        private void RenderParagraph(Paragraph p)
        {
            _currentPage.AddText(text =>
            {
                float baseLineHeight = 14;  // Base line height
                float paragraphSpacing = 6;  // Extra space between paragraphs

                // Start with normal font as default
                text.SetFont("F1", 12);

                foreach (var inline in p.Content)
                {
                    // Track the maximum font size in this line for proper spacing
                    float currentLineHeight = baseLineHeight;

                    switch (inline)
                    {
                        case TextElement t:
                            if (t.FontSettings != null)
                            {
                                // Get font size and update line height if needed
                                float size = fontSizeMap.TryGetValue(t.FontSettings.FontSize, out float mappedSize)
                                    ? mappedSize : 12;
                                currentLineHeight = Math.Max(currentLineHeight, size * 1.2f);

                                ApplyTextElementFont(text, t.FontSettings);
                            }
                            text.ShowText(t.Text);
                            break;

                        case BoldText b:
                            text.SetFont("F1B", 12);
                            text.ShowText(b.Text);
                            text.SetFont("F1", 12);  // Reset to normal
                            break;

                        case ItalicText i:
                            text.SetFont("F1I", 12);
                            text.ShowText(i.Text);
                            text.SetFont("F1", 12);  // Reset to normal
                            break;

                        case TypewriterText t:
                            float twSize = 10;  // Typewriter text typically smaller
                            text.SetFont("F1", twSize);
                            text.ShowText(t.Text);
                            text.SetFont("F1", 12);  // Reset to normal
                            currentLineHeight = Math.Max(currentLineHeight, twSize * 1.2f);
                            break;

                        case SmallCapsText s:
                            float scSize = 10;  // Small caps typically smaller
                            text.SetFont("F1", scSize);
                            text.ShowText(s.Text.ToUpper());
                            text.SetFont("F1", 12);  // Reset to normal
                            currentLineHeight = Math.Max(currentLineHeight, scSize * 1.2f);
                            break;
                    }
                }

                // Add paragraph spacing after content
                _currentPage.CurrentY -= paragraphSpacing;
            });
        }

        private void RenderInline(InlineElement inline)
        {
            _currentPage.AddText(text =>
            {
                switch (inline)
                {
                    case TextElement t:
                        ApplyTextElementFont(text, t.FontSettings);
                        text.ShowText(t.Text);
                        break;

                    case BoldText b:
                        text.SetFont("F1B", 12);
                        text.ShowText(b.Text);
                        break;

                    case ItalicText i:
                        text.SetFont("F1I", 12);
                        text.ShowText(i.Text);
                        break;

                    case TypewriterText t:
                        text.ShowText(t.Text);
                        break;

                    case SmallCapsText s:
                        text.ShowText(s.Text.ToUpper());
                        break;
                }
            });
        }

        private void RenderSection(Section s)
        {
            _currentPage.AddText(text =>
            {
                text.SetFont("F1B", 16);
                text.ShowText($"{sectionIndex}. {s.Title}");
            });

            foreach (var element in s.Content)
            {
                RenderElement(element);
            }
        }

        private void RenderSubsection(Subsection s)
        {
            _currentPage.AddText(text =>
            {
                text.SetFont("F1B", 14);
                text.ShowText($"{sectionIndex}.{subsectionIndex} {s.Title}");
            });

            foreach (var element in s.Content)
            {
                RenderElement(element);
            }
        }

        private const float LIST_INDENT = 20.0f;
        private const float MARKER_WIDTH = 15.0f;

        private void RenderItemize(Itemize it)
        {
            float originalX = _currentPage.CurrentX;
            int counter = 0;

            foreach (var item in it.Content)
            {
                if (item is ParagraphBreak) continue;
                counter++;

                _currentPage.AddText(text =>
                {
                    text.SetFont("F1", 12);
                    var pastPos = text.GetCurrentPosition();
                    // Move to indented position
                    text.SetTextPosition(pastPos.Item1 + LIST_INDENT, pastPos.Item2);
                    // Show number with proper alignment
                    text.ShowText($"- ");
                    var newPos = text.GetCurrentPosition();
                    // Position for item text, leaving space after number
                    float itemX = pastPos.Item1 + LIST_INDENT + MARKER_WIDTH;
                    text.SetTextPosition(itemX, pastPos.Item2);

                    // Render the item content
                    RenderListItemContent(text, item);
                    text.SetTextPosition(pastPos.Item1, newPos.Item2);
                });
            }
        }

        private void RenderEnumerate(Enumerate en)
        {
            float originalX = _currentPage.CurrentX;
            int counter = 0;

            foreach (var item in en.Content)
            {
                if (item is ParagraphBreak) continue;
                counter++;

                _currentPage.AddText(text =>
                {
                    text.SetFont("F1", 12);
                    var pastPos = text.GetCurrentPosition();
                    // Move to indented position
                    //text.SetTextPosition(originalX + LIST_INDENT, _currentPage.CurrentY);
                    text.SetTextPosition(pastPos.Item1 + LIST_INDENT, pastPos.Item2);
                    // Show number with proper alignment
                    text.ShowText($"{counter}.");
                    var newPos = text.GetCurrentPosition();
                    // Position for item text, leaving space after number
                    float itemX = pastPos.Item1 + LIST_INDENT + MARKER_WIDTH;
                    text.SetTextPosition(itemX, pastPos.Item2);

                    // Render the item content
                    RenderListItemContent(text, item);
                    text.SetTextPosition(pastPos.Item1, newPos.Item2);
                });
            }

            // Reset to original X position after list
            //text.SetTextPosition(originalX, _currentPage.CurrentY);
        }

        private void RenderListItemContent(PdfTextObject text, IDocumentElement item)
        {
            // Save current font settings
            string currentFont = "F1";
            float currentSize = 12;

            // Handle different types of list item content
            switch (item)
            {
                case TextElement t:
                    ApplyTextElementFont(text, t.FontSettings);
                    text.ShowText(t.Text);
                    break;

                case BoldText b:
                    text.SetFont("F1B", currentSize);
                    text.ShowText(b.Text);
                    break;

                case ItalicText i:
                    text.SetFont("F1I", currentSize);
                    text.ShowText(i.Text);
                    break;

                default:
                    RenderElement(item);
                    break;
            }

            // Restore original font settings
            text.SetFont(currentFont, currentSize);
        }

        private void RenderVerbatim(Verbatim ve)
        {
            foreach (var line in ve.Text.Split(System.Environment.NewLine, StringSplitOptions.None))
            {
                _currentPage.AddText(text =>
                {
                    text.SetFont("F1", 12);
                    text.ShowText(line);
                });
            }
        }

        private void RenderMath(MathExpression math)
        {
            _currentPage.AddText(text =>
            {
                text.SetFont("F1", 12);
                text.ShowText(math.Expression);  // For now, just render as plain text
            });
        }

        private void ApplyTextElementFont(PdfTextObject text, FontSettings settings)
        {
            if (settings == null) return;

            string fontName = "F1";
            if (settings.IsBold && settings.IsItalic) fontName = "F1BI";
            else if (settings.IsBold) fontName = "F1B";
            else if (settings.IsItalic) fontName = "F1I";

            float fontSize = fontSizeMap.TryGetValue(settings.FontSize, out float size) ? size : 12;
            text.SetFont(fontName, fontSize);
        }
    }
}
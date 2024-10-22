using DotTex2.Model;
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
        private StringBuilder pdf = new StringBuilder();
        private int objectCount = 0;
        private bool isNewLine = true;
        private List<int> objectPositions = new List<int>();
        private int currentY = 800; // Start from top of the page

        public void GeneratePDF(Document doc, string outputPath)
        {
            pdf.AppendLine("%PDF-1.4");

            int catalogPos = AddObject("<< /Type /Catalog /Pages 2 0 R >>");
            int pagesPos = AddObject("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
            int pagePos = AddObject("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R /F1B 6 0 R /F1I 7 0 R >> >> >>");

            string contentStream = RenderContent(doc);
            int contentStreamPos = AddStreamObject(contentStream);

            int fontPos = AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
            int fontBoldPos = AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");
            int fontItalicPos = AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Oblique >>");

            GenerateXrefAndTrailer(outputPath);
        }

        private string RenderContent(Document doc)
        {
            StringBuilder content = new StringBuilder();
            content.AppendLine("BT");

            //// Render title
            //content.AppendLine("/F1B 24 Tf");
            //content.AppendLine($"50 {currentY} Td");
            //content.AppendLine($"({EscapeText(doc.Title)}) Tj");
            //currentY -= 30;

            //// Render author
            //content.AppendLine("/F1 14 Tf");
            //content.AppendLine($"0 {currentY} Td");
            //content.AppendLine($"({EscapeText(doc.Author)}) Tj");
            //currentY -= 20;

            // Render date
            //content.AppendLine($"0 {currentY} Td");
            //content.AppendLine($"({EscapeText(doc.Date.ToString("MMMM d, yyyy"))}) Tj");
            //currentY -= 40;

            foreach (var element in doc.Elements)
            {
                RenderElement(element, content);
            }

            content.AppendLine("ET");
            return content.ToString();
        }

        private void RenderElement(IDocumentElement element, StringBuilder content)
        {
            switch (element)
            {
                case Paragraph p:
                    RenderParagraph(p, content);
                    break;
                case Section s:
                    if (!isNewLine)
                    {
                        content.AppendLine("ET");
                        currentY -= 20;
                    }
                    isNewLine = true;
                    RenderSection(s, content);
                    break;
                case Subsection s:
                    if (!isNewLine)
                    {
                        content.AppendLine("ET");
                        currentY -= 20;
                    }
                    isNewLine = true;
                    RenderSubection(s, content);
                    break;
                case MathExpression m:
                    if (!isNewLine)
                    {
                        content.AppendLine("ET");
                        currentY -= 20;
                    }
                    isNewLine = true;
                    RenderMath(m, content);
                    break;
                case ParagraphBreak pr:
                    if (!isNewLine)
                    {
                        content.AppendLine("ET");
                        currentY -= 20;
                    }
                    isNewLine = true;
                    RenderNewLine(content);
                    break;
                case InlineElement il:
                    RenderInline(il, content);
                    break;
                case Model.Environment env:
                    foreach (var cont in env.Content)
                    {
                        RenderElement(cont, content);
                    }
                    break;
            }
        }

        private void RenderParagraph(Paragraph p, StringBuilder content)
        {
            if (isNewLine)
            {
                content.AppendLine("BT");
                content.AppendLine("/F1 12 Tf");
                content.AppendLine($"50 {currentY} Td");
                isNewLine = false;
            }

            foreach (var inline in p.Content)
            {
                switch (inline)
                {
                    case TextElement t:
                        content.Append($"({EscapeText(t.Text)}) Tj ");
                        break;
                    case BoldText b:
                        content.Append("/F1B 12 Tf ");
                        content.Append($"({EscapeText(b.Text)}) Tj ");
                        content.Append("/F1 12 Tf ");
                        break;
                    case ItalicText i:
                        content.Append("/F1I 12 Tf ");
                        content.Append($"({EscapeText(i.Text)}) Tj ");
                        content.Append("/F1 12 Tf ");
                        break;
                }
            }
        }

        private void RenderInline(InlineElement p, StringBuilder content)
        {
            if (isNewLine)
            {
                content.AppendLine("BT");
                content.AppendLine("/F1 12 Tf");
                content.AppendLine($"50 {currentY} Td");
                isNewLine = false;
            }


            switch (p)
            {
                case TextElement t:
                    content.Append($"({EscapeText(t.Text)}) Tj ");
                    break;
                case BoldText b:
                    content.Append("/F1B 12 Tf ");
                    content.Append($"({EscapeText(b.Text)}) Tj ");
                    content.Append("/F1 12 Tf ");
                    break;
                case ItalicText i:
                    content.Append("/F1I 12 Tf ");
                    content.Append($"({EscapeText(i.Text)}) Tj ");
                    content.Append("/F1 12 Tf ");
                    break;
            }

        }

        private void RenderSection(Section s, StringBuilder content)
        {
            content.AppendLine("BT");
            content.AppendLine("/F1B 16 Tf");
            content.AppendLine($"50 {currentY} Td");
            content.AppendLine($"({EscapeText(s.Title)}) Tj");
            content.AppendLine("ET");
            currentY -= 30;

            foreach (var sectionElement in s.Content)
            {
                RenderElement(sectionElement, content);
            }
        }

        private void RenderSubection(Subsection s, StringBuilder content)
        {
            content.AppendLine("BT");
            content.AppendLine("/F1B 16 Tf");
            content.AppendLine($"50 {currentY} Td");
            content.AppendLine($"({EscapeText(s.Title)}) Tj");
            content.AppendLine("ET");
            currentY -= 30;

            foreach (var sectionElement in s.Content)
            {
                RenderElement(sectionElement, content);
            }
        }

        private void RenderNewLine(StringBuilder content)
        {
            //content.AppendLine("BT");
            //content.AppendLine("/F1 12 Tf");
            //content.AppendLine($"50 {currentY} Td");
            //content.AppendLine("T*");
            content.AppendLine("ET");
            currentY -= 15;
        }

        private void RenderMath(MathExpression math, StringBuilder content)
        {
            content.AppendLine("BT");
            content.AppendLine("/F1I 12 Tf");
            content.AppendLine($"50 {currentY} Td");
            content.AppendLine($"({EscapeText(math.Expression)}) Tj");
            content.AppendLine("ET");
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

        // Adds a stream object (content streams in PDF) and returns its position
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

        // Generates the xref table and trailer
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

            // Trailer
            pdf.AppendLine("trailer");
            pdf.AppendLine($"<< /Size {objectCount + 1} /Root 1 0 R >>");
            pdf.AppendLine("startxref");
            pdf.AppendLine(xrefPosition.ToString());
            pdf.AppendLine("%%EOF");

            // Write the final PDF content to the file
            File.WriteAllText(outputPath, pdf.ToString());
        }
    }
}

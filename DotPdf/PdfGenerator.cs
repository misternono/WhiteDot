namespace DotPdf
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Metadata;
    using System.Resources;
    using System.Text;

    namespace PdfGenerator
    {
        public static class PageMetrics
        {
            public static float PageWidth { get; set; } = 595.28f;  // A4 width in points (210mm)
            public static float PageHeight { get; set; } = 841.89f; // A4 height in points (297mm)
            public static float TopMargin { get; set; } = 785.89f;  // Height - 2cm margin
            public static float BottomMargin { get; set; } = 56.69f;  // 2cm margin
            public static float LeftMargin { get; set; } = 56.69f;    // 2cm margin
            public static float RightMargin { get; set; } = 538.59f;  // Width - 2cm margin
            public static float DefaultLineHeight { get; set; } = 14;

            public static bool IsAtBottom(float y) => y <= BottomMargin;
        }

        public class TextCursor
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float LineHeight { get; set; }

            public TextCursor(float x, float y, float lineHeight = 14)
            {
                X = x;
                Y = y;
                LineHeight = lineHeight;
            }

            public void NewLine()
            {
                X = PageMetrics.LeftMargin;
                Y -= LineHeight;
            }
        }
        public class PdfContentStream : PdfObject
        {
            public StringBuilder Content { get; } = new StringBuilder();

            public override string ToPdfString()
            {
                var content = Content.ToString();

                var dict = new PdfDictionary
                {
                    { "Length", Encoding.UTF8.GetByteCount(content) }
                };

                return $"{ObjectNumber} 0 obj\n{dict}\nstream\n{content}endstream\nendobj\n";
            }
        }

        public class PdfTextObject : IDisposable
        {
            private StringBuilder content;
            private PdfPage page;
            private readonly PdfDocument document;
            private float fontSize = 12;
            private string currentFont = null;
            private PdfContentStream currentStream;
            private bool inTextBlock = false;
            private float lastX, lastY;

            public PdfTextObject(PdfDocument doc, PdfPage page, PdfContentStream stream)
            {
                this.document = doc;
                this.page = page;
                this.currentStream = stream;
                this.content = stream.Content;
                this.lastX = page.CurrentX;
                this.lastY = page.CurrentY;
            }

            private void EnsureTextBlock()
            {
                if (!inTextBlock)
                {
                    content.AppendLine("BT");
                    inTextBlock = true;

                    // Reapply font if we have one
                    if (currentFont != null)
                    {
                        content.AppendLine($"/{currentFont} {fontSize:F2} Tf");
                    }

                    // Set initial position
                    SetTextPosition(lastX, lastY);
                }
            }

            public void SetFont(string fontName, float size)
            {
                if (currentFont != fontName || fontSize != size)
                {
                    fontSize = size;
                    currentFont = fontName;
                    if (inTextBlock)
                    {
                        content.AppendLine($"/{fontName} {size:F2} Tf");
                    }
                }
            }

            public void SetTextPosition(float x, float y)
            {
                lastX = x;
                lastY = y;
                page.CurrentX = x;
                page.CurrentY = y;

                if (inTextBlock)
                {
                    content.AppendLine($"{x:F2} {y:F2} Td");
                }
            }

            public (float, float) GetCurrentPosition()
            {
                return ((float)lastX, (float)lastY);
            }

            public void ShowText(string text)
            {
                if (string.IsNullOrEmpty(text)) return;

                if (PageMetrics.IsAtBottom(page.CurrentY))
                {
                    CreateNewPage();
                    ShowText(text);
                    return;
                }

                EnsureTextBlock();

                var escaped = text.Replace("\\", "\\\\")
                                 .Replace("(", "\\(")
                                 .Replace(")", "\\)")
                                 .Replace("\n", "\\n");

                content.AppendLine($"({escaped}) Tj");

                // Update position
                lastY -= fontSize * 1.2f;
                SetTextPosition(lastX, lastY);
            }

            private void CreateNewPage()
            {
                End();  // End current text block

                // Create new page
                var newPage = document.AddPage();
                page = newPage;
                currentStream = (PdfContentStream)newPage.Contents;
                content = currentStream.Content;

                // Reset position for new page
                lastX = PageMetrics.LeftMargin;
                lastY = PageMetrics.TopMargin;
                inTextBlock = false;
            }

            public void End()
            {
                if (inTextBlock)
                {
                    content.AppendLine("ET");
                    inTextBlock = false;
                }
            }

            public void Dispose()
            {
                End();
            }
        }


        public static class PdfPageExtensions
        {
            public static void AddText(this PdfPage page, Action<PdfTextObject> textBuilder)
            {
                // Ensure page has its own content stream
                if (page.Contents == null)
                {
                    page.Contents = page.Document.CreateContentStream();
                }

                var textObject = new PdfTextObject(page.Document, page, (PdfContentStream)page.Contents);
                textBuilder(textObject);
                textObject.End();
            }
        }


        public static class PdfDocumentExtensions
        {
            public static PdfFont AddStandardFont(this PdfDocument doc, string fontName, string resourceName)
            {
                var page = doc.CurrentPage ?? doc.AddPage();
                var font = new PdfFont
                {
                    ObjectNumber = doc.GetNextObjectNumber(),
                    FontName = fontName,
                    ResourceName = resourceName
                };
                page.Resources.Add(font);
                return font;
            }
        }

        // Base class for all PDF objects
        public abstract class PdfObject
        {
            public int ObjectNumber { get; set; }
            public int GenerationNumber { get; set; }

            // Byte offset in the file - needed for xref table
            public long ByteOffset { get; set; }

            // Every PDF object must be able to serialize itself
            public abstract string ToPdfString();
        }

        // Represents a PDF dictionary
        public class PdfDictionary : Dictionary<string, object>
        {
            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append("<<");
                foreach (var kvp in this)
                {
                    if (kvp.Value == null) continue;

                    sb.Append($" /{kvp.Key} ");

                    // Handle different value types
                    if (kvp.Value is PdfObject obj)
                    {
                        sb.Append($"{obj.ObjectNumber} 0 R");
                    }
                    else if (kvp.Value is int[] intArray)
                    {
                        sb.Append($"[{string.Join(" ", intArray)}]");
                    }
                    else if (kvp.Value is float[] floatArray)
                    {
                        sb.Append($"[{string.Join(" ", floatArray.Select(f => f.ToString("F2")))}]");
                    }
                    else if (kvp.Value is List<PdfPage> pages)
                    {
                        sb.Append("[");
                        sb.Append(string.Join(" ", pages.Select(p => $"{p.ObjectNumber} 0 R")));
                        sb.Append("]");
                    }
                    else if (kvp.Value is string str)
                    {
                        // Check if it's a raw array string
                        if (str.StartsWith("[") && str.EndsWith("]"))
                        {
                            sb.Append(str); // Output array as-is
                        }
                        else
                        {
                            sb.Append($"/{str}");
                        }
                    }
                    else if (kvp.Value is PdfDictionary dict)
                    {
                        sb.Append(dict.ToString());
                    }
                    else
                    {
                        var valueStr = kvp.Value.ToString();
                        if (!string.IsNullOrEmpty(valueStr))
                        {
                            sb.Append(valueStr);
                        }
                    }
                }
                sb.Append(" >>");
                return sb.ToString();
            }
        }

        // Represents a PDF stream object
        public class PdfStream : PdfObject
        {
            public PdfDictionary Dictionary { get; set; }
            public byte[] Data { get; set; }

            public override string ToPdfString()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"{ObjectNumber} 0 obj");
                Dictionary["Length"] = Data.Length;
                sb.AppendLine(Dictionary.ToString());
                sb.AppendLine("stream");
                // Note: In real implementation, need to handle binary data properly
                sb.Append(Encoding.ASCII.GetString(Data));
                sb.AppendLine("\nendstream");
                sb.AppendLine("endobj");
                return sb.ToString();
            }
        }

        // Represents a PDF catalog object
        public class PdfCatalog : PdfObject
        {
            public PdfPages Pages { get; set; }

            public override string ToPdfString()
            {
                var dict = new PdfDictionary
            {
                { "Type", "Catalog" },
                { "Pages", Pages }
            };

                return $"{ObjectNumber} 0 obj\n{dict}\nendobj\n";
            }
        }

        // Represents a PDF pages collection
        public class PdfPages : PdfObject
        {
            public List<PdfPage> Kids { get; set; } = new List<PdfPage>();

            public override string ToPdfString()
            {
                var dict = new PdfDictionary
            {
                { "Type", "Pages" },
                { "Count", Kids.Count }
            };
                // Convert Kids list to PDF array format
                var kidsArray = new StringBuilder("[");
                foreach (var kid in Kids)
                {
                    kidsArray.Append($"{kid.ObjectNumber} 0 R ");
                }
                kidsArray.Append("]");
                dict["Kids"] = kidsArray.ToString();

                return $"{ObjectNumber} 0 obj\n{dict}\nendobj\n";
            }
        }

        // Represents a single PDF page
        public partial class PdfPage : PdfObject
        {
            public PdfDocument Document { get; set; }
            public PdfPages Parent { get; set; }
            public List<PdfResource> Resources { get; set; } = new List<PdfResource>();
            public PdfObject Contents { get; set; }

            // Track current position in page
            public float CurrentX { get; set; } = 50;
            public float CurrentY { get; set; } = 800;

            public override string ToPdfString()
            {
                var dict = new PdfDictionary
        {
            { "Type", "Page" },
            { "Parent", Parent },
            { "MediaBox", $"[0 0 {PageMetrics.PageWidth:F2} {PageMetrics.PageHeight:F2}]" },
            { "Contents", Contents }
        };

                if (Resources.Any())
                {
                    var resourceDict = new PdfDictionary();
                    // Group resources by type (Font, XObject, etc.)
                    foreach (var group in Resources.GroupBy(r => r.ResourceType))
                    {
                        var typeDict = new PdfDictionary();
                        foreach (var resource in group)
                        {
                            typeDict[resource.ResourceName] = resource;
                        }
                        resourceDict[group.Key] = typeDict;
                    }
                    dict["Resources"] = resourceDict;
                }

                return $"{ObjectNumber} 0 obj\n{dict}\nendobj\n";
            }
        }

        public partial class PdfPage
        {
            //public PdfDocument Document { get; set; }
            //public PdfObject Contents { get; set; }

            //public override string ToPdfString()
            //{
            //    var dict = new PdfDictionary
            //{
            //    { "Type", "Page" },
            //    { "Parent", Parent },
            //    { "MediaBox", $"[0 0 {PageMetrics.PageWidth:F2} {PageMetrics.PageHeight:F2}]" },
            //    { "Contents", Contents }
            //};

            //    // Build resources dictionary
            //    var resourceDict = new PdfDictionary();
            //    foreach (var resource in Resources)
            //    {
            //        if (!resourceDict.ContainsKey(resource.ResourceType))
            //            resourceDict[resource.ResourceType] = new PdfDictionary();

            //        ((PdfDictionary)resourceDict[resource.ResourceType])[resource.ResourceName] = resource;
            //    }
            //    dict["Resources"] = resourceDict.ToString();

            //    return $"{ObjectNumber} 0 obj\n{dict}\nendobj\n";
            //}
        }

        public partial class PdfDocument
        {
            public event Action<PdfPage> OnPageChanged;
            public PdfPage CurrentPage { get; private set; }

            private Dictionary<string, PdfResource> documentResources = new Dictionary<string, PdfResource>();

            public PdfFont AddStandardFont(string fontName, string resourceName)
            {
                // Create font only if it doesn't exist
                if (!documentResources.ContainsKey(resourceName))
                {
                    var font = new PdfFont
                    {
                        ObjectNumber = GetNextObjectNumber(),
                        FontName = fontName,
                        ResourceName = resourceName
                    };
                    documentResources[resourceName] = font;
                    objects.Add(font);
                }
                return (PdfFont)documentResources[resourceName];
            }

            public int GetNextObjectNumber()
            {
                return nextObjectNumber++;
            }

            public PdfContentStream CreateContentStream()
            {
                var stream = new PdfContentStream
                {
                    ObjectNumber = GetNextObjectNumber()
                };
                objects.Add(stream);
                return stream;
            }

            public PdfPage AddPage()
            {
                CurrentPage = new PdfPage
                {
                    ObjectNumber = nextObjectNumber++,
                    Parent = catalog.Pages,
                    Document = this,
                    CurrentX = PageMetrics.LeftMargin,
                    CurrentY = PageMetrics.TopMargin
                };

                // Add all document resources to the page
                foreach (var resource in documentResources.Values)
                {
                    CurrentPage.Resources.Add(resource);
                }

                // Create and assign content stream
                var stream = CreateContentStream();
                CurrentPage.Contents = stream;

                catalog.Pages.Kids.Add(CurrentPage);
                objects.Add(CurrentPage);

                OnPageChanged?.Invoke(CurrentPage);

                return CurrentPage;
            }
        }

        // Base class for PDF resources (fonts, images, etc.)
        public abstract class PdfResource : PdfObject
        {
            public string ResourceType { get; set; }  // "Font", "XObject", etc.
            public string ResourceName { get; set; }  // "F1", "Im1", etc.
        }

        // Example of a Font resource
        public class PdfFont : PdfResource
        {
            public string FontName { get; set; }
            public string Subtype { get; set; } = "Type1";
            public string Encoding { get; set; } = "WinAnsiEncoding";

            public PdfFont()
            {
                ResourceType = "Font";
            }

            public override string ToPdfString()
            {
                var dict = new PdfDictionary
        {
            { "Type", ResourceType },
            { "Subtype", Subtype },
            { "BaseFont", FontName },
            { "Encoding", Encoding }
        };

                return $"{ObjectNumber} 0 obj\n{dict}\nendobj\n";
            }
        }

        // Main PDF document class
        public partial class PdfDocument
        {
            private List<PdfObject> objects = new List<PdfObject>();
            private int nextObjectNumber = 1;
            private PdfCatalog catalog;

            //public PdfPage CurrentPage { get; private set; }

            //public int GetNextObjectNumber()
            //{
            //    return nextObjectNumber++;
            //}

            //public PdfPage AddPage()
            //{
            //    CurrentPage = new PdfPage
            //    {
            //        ObjectNumber = nextObjectNumber++,
            //        Parent = catalog.Pages
            //    };
            //    catalog.Pages.Kids.Add(CurrentPage);
            //    objects.Add(CurrentPage);
            //    return CurrentPage;
            //}

            public PdfDocument()
            {
                // Initialize basic PDF structure
                catalog = new PdfCatalog
                {
                    ObjectNumber = nextObjectNumber++,
                    Pages = new PdfPages { ObjectNumber = nextObjectNumber++ }
                };
                objects.Add(catalog);
                objects.Add(catalog.Pages);
            }

            public string GeneratePdf()
            {
                var sb = new StringBuilder();

                // PDF header
                sb.AppendLine("%PDF-1.6");
                sb.AppendLine("%âãÏÓ"); // Binary file marker

                // Track current position for xref table
                long currentPosition = sb.Length;

                // Sort objects to ensure proper order (catalog first, then pages, then fonts, then content)
                var sortedObjects = new List<PdfObject>();

                // Add catalog first
                sortedObjects.Add(catalog);

                // Add pages object
                sortedObjects.Add(catalog.Pages);

                // Add fonts before pages that reference them
                var fontObjects = objects.Where(o => o is PdfFont).OrderBy(o => o.ObjectNumber);
                sortedObjects.AddRange(fontObjects);

                // Add content streams
                var contentStreams = objects.Where(o => o is PdfContentStream).OrderBy(o => o.ObjectNumber);
                sortedObjects.AddRange(contentStreams);

                // Add pages last since they reference both fonts and content
                var pageObjects = objects.Where(o => o is PdfPage).OrderBy(o => o.ObjectNumber);
                sortedObjects.AddRange(pageObjects);

                // Write all objects
                foreach (var obj in sortedObjects)
                {
                    // Record the byte offset for xref
                    obj.ByteOffset = currentPosition;

                    var objString = obj.ToPdfString();
                    sb.Append(objString);

                    // Update position
                    currentPosition += Encoding.UTF8.GetByteCount(objString);
                }

                // Write xref table
                var xrefStart = currentPosition;
                WriteXrefTable(sb, sortedObjects);

                // Write trailer
                sb.AppendLine("trailer");
                var trailer = new PdfDictionary
        {
            { "Size", nextObjectNumber },
            { "Root", catalog },
        };
                sb.AppendLine(trailer.ToString());
                sb.AppendLine("startxref");
                sb.AppendLine(xrefStart.ToString());
                sb.AppendLine("%%EOF");

                return sb.ToString();
            }

            private void WriteXrefTable(StringBuilder sb, List<PdfObject> sortedObjects)
            {
                sb.AppendLine("xref");
                sb.AppendLine($"0 {nextObjectNumber}");

                // Object 0 is special
                sb.AppendLine("0000000000 65535 f ");  // Note the space after 'f'

                // Write entries for all objects in order
                foreach (var obj in sortedObjects)
                {
                    sb.AppendLine($"{obj.ByteOffset:D10} {obj.GenerationNumber:D5} n ");  // Note the space after 'n'
                }
            }
        }
    }
}

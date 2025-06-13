namespace DotPdf
{
    using System;
    using System.Collections.Generic;
    using System.IO.Compression;
    using System.Reflection.Metadata;
    using System.Resources;
    using System.Text;
    using WhiteDot.Licensing;

    namespace PdfGenerator
    {
        public class XrefEntry
        {
            public long Offset { get; set; }
            public int ObjectNumber { get; set; }
            public int GenerationNumber { get; set; }
            public bool IsInUse { get; set; }

            public override string ToString()
            {
                // Format: "0000000000 00000 n " (note the space at the end)
                return $"{Offset:D10} {GenerationNumber:D5} {(IsInUse ? 'n' : 'f')} ";
            }
        }
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
            private List<byte[]> contentChunks = new List<byte[]>();
            public bool UseCompression { get; set; } = true;

            // Calculate Adler-32 checksum
            private uint CalculateAdler32(byte[] data)
            {
                const uint MOD_ADLER = 65521;
                uint a = 1, b = 0;
                // Process each byte in the data
                foreach (byte bt in data)
                {
                    a = (a + bt) % MOD_ADLER;
                    b = (b + a) % MOD_ADLER;
                }
                // Combine the two 16-bit values into a 32-bit Adler-32 checksum
                return (b << 16) + a;
            }

            // Add binary data directly
            public void WriteBytes(byte[] data)
            {
                contentChunks.Add(data);
            }

            // Helper for writing text content that needs to be converted to bytes
            public void WriteText(string text)
            {
                contentChunks.Add(Encoding.ASCII.GetBytes(text + "\n"));
            }

            private void AnalyzeCompressedData(byte[] data)
            {
                System.Diagnostics.Debug.WriteLine("Stream Analysis:");
                System.Diagnostics.Debug.WriteLine($"Total length: {data.Length}");

                // Check zlib header
                System.Diagnostics.Debug.WriteLine($"Zlib header: {data[0]:X2} {data[1]:X2}");

                // Look at first few bytes after header
                System.Diagnostics.Debug.WriteLine("First 10 bytes after header:");
                for (int i = 2; i < Math.Min(12, data.Length); i++)
                {
                    System.Diagnostics.Debug.WriteLine($"Byte {i}: {data[i]:X2} (bin: {Convert.ToString(data[i], 2).PadLeft(8, '0')})");
                }

                // Look at last 4 bytes (Adler32)
                if (data.Length >= 4)
                {
                    var adler = data.Skip(data.Length - 4).Take(4).ToArray();
                    System.Diagnostics.Debug.WriteLine($"Adler32: {BitConverter.ToString(adler)}");
                }
            }
            // Compresses all content chunks into a single byte array
            private byte[] GetCompressedContent()
            {
                // First, combine all chunks into one memory stream
                using (var uncompressedStream = new MemoryStream())
                {
                    foreach (var chunk in contentChunks)
                    {
                        uncompressedStream.Write(chunk, 0, chunk.Length);
                    }

                    if (!UseCompression)
                    {
                        return uncompressedStream.ToArray();
                    }

                    // Create a stream for the final compressed output (including zlib wrapper)
                    using (var finalStream = new MemoryStream())
                    {
                        // Write zlib header
                        finalStream.WriteByte(0x78); // CMF byte
                        finalStream.WriteByte(0xDA); // FLG byte

                        // Create temporary stream for the raw deflate data
                        using (var tempStream = new MemoryStream())
                        {
                            // Reset position of uncompressed data
                            var len = uncompressedStream.Length;
                            uncompressedStream.Position = 0;

                            //var adler = new Adler32Computer();

                            //adler.AddData(uncompressedStream.ToArray());
                            uncompressedStream.Position = 0;

                            // Compress the data using Deflate
                            using (var deflateStream = new DeflateStream(tempStream, CompressionLevel.Optimal, true))
                            {
                                uncompressedStream.CopyTo(deflateStream);
                            }

                            // Get the compressed data
                            var compressedData = tempStream.ToArray();

                            byte[] adlerBytes = new byte[4];

                            //adler.CopyHashToBigEndianSpan(adlerBytes);

                            // Write the compressed data to our final stream
                            finalStream.Write(compressedData, 0, compressedData.Length);
                            //finalStream.Write(adlerBytes, 0, 4);

                            // Calculate and write Adler-32 checksum of the uncompressed data
                            uncompressedStream.Position = 0;
                            var uncompressedData = uncompressedStream.ToArray();
                            uint adler32 = CalculateAdler32(uncompressedData);

                            // Write Adler-32 checksum (big-endian)
                            finalStream.WriteByte((byte)(adler32 >> 24));
                            finalStream.WriteByte((byte)(adler32 >> 16));
                            finalStream.WriteByte((byte)(adler32 >> 8));
                            finalStream.WriteByte((byte)adler32);

                            return finalStream.ToArray();
                        }
                    }
                }
            }

            public override string ToPdfString()
            {
                // Get the compressed content first to calculate length
                var content = GetCompressedContent();

                var dict = new PdfDictionary
        {
            { "Length", content.Length }
        };

                // Add compression filter if enabled
                if (UseCompression)
                {
                    dict["Filter"] = "FlateDecode";
                }

                // Return the object structure with a placeholder for the stream content
                return $"{ObjectNumber} 0 obj\n{dict}stream\n<<STREAM_CONTENT_PLACEHOLDER>>\nendstream\nendobj\n";
            }

            // Write the actual stream content to the output
            public void WriteToStream(Stream outputStream)
            {
                var content = GetCompressedContent();
                outputStream.Write(content, 0, content.Length);
            }

            // Get the total length of the stream content
            public int GetLength()
            {
                var content = GetCompressedContent();
                return content.Length;
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
                this.lastX = page.CurrentX;
                this.lastY = page.CurrentY;
            }

            private void EnsureTextBlock()
            {
                if (!inTextBlock)
                {
                    currentStream.WriteText("BT");
                    inTextBlock = true;

                    // Reapply font if we have one
                    if (currentFont != null)
                    {
                        currentStream.WriteText($"/{currentFont} {fontSize:F2} Tf");
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
                    currentStream.WriteText($"{x:F2} {y:F2} Td");
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

                currentStream.WriteText($"({escaped}) Tj");

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
                content = new StringBuilder();

                // Reset position for new page
                lastX = PageMetrics.LeftMargin;
                lastY = PageMetrics.TopMargin;
                inTextBlock = false;
            }

            public void End()
            {
                if (inTextBlock)
                {
                    currentStream.WriteText("ET");
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
                // Check for a valid license
                //LicenseManager.Instance.RequireLicense(ProductType.DotPdf);

                // Initialize basic PDF structure
                catalog = new PdfCatalog
                {
                    ObjectNumber = nextObjectNumber++,
                    Pages = new PdfPages { ObjectNumber = nextObjectNumber++ }
                };
                objects.Add(catalog);
                objects.Add(catalog.Pages);
            }

            public void GeneratePdf(Stream outputStream)
            {
                // Recheck license before generating PDF
                //LicenseManager.Instance.RequireLicense(ProductType.DotPdf);

                // Create list to track xref entries in order of appearance
                var xrefEntries = new List<XrefEntry>();

                // Add the special first entry (object 0)
                xrefEntries.Add(new XrefEntry
                {
                    Offset = 0,
                    ObjectNumber = 0,
                    GenerationNumber = 65535,
                    IsInUse = false
                });

                // Write PDF header
                var header = Encoding.ASCII.GetBytes("%PDF-1.6\n");
                outputStream.Write(header, 0, header.Length);
                outputStream.Write([0x25, 0xD3, 0xEB, 0xE9, 0xE1, 0x0A], 0, 6);

                // Track current position for xref table
                var currentPosition = outputStream.Position;

                // Sort and prepare objects (same as before)
                var sortedObjects = PrepareObjectsList();

                // Write all objects and track their positions
                foreach (var obj in sortedObjects)
                {
                    // Record the object's position
                    var xrefEntry = new XrefEntry
                    {
                        Offset = currentPosition,
                        ObjectNumber = obj.ObjectNumber,
                        GenerationNumber = obj.GenerationNumber,
                        IsInUse = true
                    };
                    xrefEntries.Add(xrefEntry);

                    if (obj is PdfContentStream contentStream)
                    {
                        // Handle content stream writing...
                        var objString = contentStream.ToPdfString();
                        var beforeStream = objString.Substring(0, objString.IndexOf("<<STREAM_CONTENT_PLACEHOLDER>>"));
                        var afterStream = objString.Substring(objString.IndexOf("<<STREAM_CONTENT_PLACEHOLDER>>") +
                                                            "<<STREAM_CONTENT_PLACEHOLDER>>".Length);

                        var headerBytes = Encoding.ASCII.GetBytes(beforeStream);
                        outputStream.Write(headerBytes, 0, headerBytes.Length);
                        contentStream.WriteToStream(outputStream);
                        var footerBytes = Encoding.ASCII.GetBytes(afterStream);
                        outputStream.Write(footerBytes, 0, footerBytes.Length);

                        currentPosition += headerBytes.Length + contentStream.GetLength() + footerBytes.Length ;
                    }
                    else
                    {
                        var objString = obj.ToPdfString();
                        var objBytes = Encoding.ASCII.GetBytes(objString);
                        outputStream.Write(objBytes, 0, objBytes.Length);
                        currentPosition += objBytes.Length;
                    }
                }

                // Write xref table using our collected entries
                WriteXrefTableAndTrailer(outputStream, xrefEntries, currentPosition);
            }


            private List<PdfObject> PrepareObjectsList()
            {
                // Create a new list to store objects in their proper order
                var sortedObjects = new List<PdfObject>();

                // First, we need to identify all indirect objects in the document
                var allObjects = new HashSet<PdfObject>
                {
                    // Start by adding the catalog since it's our root object
                    catalog,

                    // Add the Pages tree node
                    catalog.Pages
                };

                // Add all pages and their associated objects
                foreach (var page in catalog.Pages.Kids)
                {
                    allObjects.Add(page);

                    // Add page's content stream
                    if (page.Contents != null)
                    {
                        allObjects.Add(page.Contents);
                    }

                    // Add all resources used by the page
                    foreach (var resource in page.Resources)
                    {
                        allObjects.Add(resource);
                    }
                }

                // Now we'll sort these objects in the correct order for the PDF file

                // 1. Document Catalog (root) must come first
                sortedObjects.Add(catalog);

                // 2. Pages tree node follows the catalog
                sortedObjects.Add(catalog.Pages);

                // 3. Document-level resources (fonts, color spaces, etc.)
                var documentResources = allObjects
                    .Where(obj => obj is PdfResource)
                    .OrderBy(obj => obj.ObjectNumber);
                sortedObjects.AddRange(documentResources);

                // 4. Page content streams
                // Content streams should come before their parent pages
                var contentStreams = allObjects
                    .Where(obj => obj is PdfContentStream)
                    .OrderBy(obj => obj.ObjectNumber);
                sortedObjects.AddRange(contentStreams);

                // 5. Page objects themselves
                var pageObjects = catalog.Pages.Kids
                    .OrderBy(obj => obj.ObjectNumber);
                sortedObjects.AddRange(pageObjects);

                // Verify that all objects have been included
                var missingObjects = allObjects.Except(sortedObjects).ToList();
                if (missingObjects.Any())
                {
                    // Add any objects we might have missed
                    // This is a safety check to ensure no objects are left behind
                    sortedObjects.AddRange(missingObjects);

                    // Log a warning since this shouldn't normally happen
                    System.Diagnostics.Debug.WriteLine("Warning: Some objects were not properly categorized in PrepareObjectsList");
                }

                // Verify object number continuity
                ValidateObjectNumbers(sortedObjects);

                return sortedObjects;
            }

            private void ValidateObjectNumbers(List<PdfObject> objects)
            {
                // Keep track of object numbers we've seen
                var objectNumbers = new HashSet<int>();

                foreach (var obj in objects)
                {
                    // Check for duplicate object numbers
                    if (!objectNumbers.Add(obj.ObjectNumber))
                    {
                        throw new InvalidOperationException(
                            $"Duplicate object number found: {obj.ObjectNumber}");
                    }

                    // Ensure object number is positive
                    if (obj.ObjectNumber <= 0)
                    {
                        throw new InvalidOperationException(
                            $"Invalid object number: {obj.ObjectNumber}");
                    }
                }

                // Check for gaps in object numbering
                var expectedNumbers = Enumerable.Range(1, objects.Max(o => o.ObjectNumber));
                var missingNumbers = expectedNumbers.Except(objectNumbers);

                if (missingNumbers.Any())
                {
                    throw new InvalidOperationException(
                        $"Gap in object numbers detected. Missing numbers: {string.Join(", ", missingNumbers)}");
                }
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

            private void WriteXrefTableAndTrailer(Stream outputStream, List<XrefEntry> xrefEntries, long xrefOffset)
            {
                void WriteString(string text)
                {
                    var bytes = Encoding.ASCII.GetBytes(text + "\n");
                    outputStream.Write(bytes, 0, bytes.Length);
                }

                // Begin xref section
                WriteString("xref");
                WriteString($"0 {xrefEntries.Count}");

                // Write all entries by obj order
                xrefEntries = xrefEntries.OrderBy(x => x.ObjectNumber).ToList();

                foreach (var entry in xrefEntries)
                {
                    WriteString(entry.ToString());
                }

                // Write trailer dictionary
                WriteString("trailer");
                var trailerDict = new PdfDictionary
    {
        { "Size", xrefEntries.Count },
        { "Root", catalog }
    };
                WriteString(trailerDict.ToString());

                // Write cross-reference table offset
                WriteString("startxref");
                WriteString(xrefOffset.ToString());

                // Write PDF end-of-file marker
                WriteString("%%EOF");
            }

            // Helper method to generate document information dictionary
            private PdfDictionary GetDocumentInfo()
            {
                return new PdfDictionary
    {
        { "Producer", "DotPdf Library" },
        { "CreationDate", $"(D:{DateTime.Now:yyyyMMddHHmmss}Z)" },
        { "ModDate", $"(D:{DateTime.Now:yyyyMMddHHmmss}Z)" }
    };
            }

            // Helper method to generate file ID array
            private string GenerateFileID()
            {
                // Generate a unique ID based on document content and time
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    // Combine current time, document size, and first page content
                    var idString = $"{DateTime.Now.Ticks}_{nextObjectNumber}";
                    var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(idString));

                    // Convert hash to hex string
                    var id = BitConverter.ToString(hash).Replace("-", "").ToLower();

                    // Return ID array with two copies of the same ID
                    // PDF spec recommends two identical IDs for newly created files
                    return $"[<{id}> <{id}>]";
                }
            }
        }
    }
}

using DotTex2.Lexing;
using DotTex2.Model;
using DotTex2.Model.InlineElements;
using DotTex2.Parsing;
using static System.Runtime.InteropServices.JavaScript.JSType;
using DotTex2.Convert;

class Program
{
    static void Main(string[] args)
    {

        Console.WriteLine("LaTeX Parser Demo");
        Console.WriteLine("=================");

        // Sample LaTeX document
        string latexContent = @"\documentclass{article}
\title{Demo Document}
\author{John Doe}

\begin{document}

\maketitle

\section{Introduction}
This is a simple document to demonstrate the \textbf{LaTeX Parser} library.

\subsection{Math Example}
Here's a famous equation: $E = mc^2$

\section{Features}
\begin{itemize}
\item Parsing of basic LaTeX structures
\item Support for inline math
\item Conversion to other formats
\end{itemize}

\end{document}";

        Console.WriteLine("Parsing LaTeX content...");
        var lexer = new Lexer();
        var tokens = lexer.Tokenize(latexContent).ToList();
        var parser = new Parser(tokens);
        var document = parser.Parse();

        Console.WriteLine("\nDocument Structure:");
        PrintDocumentStructure(document);
        var conv= new LatexToPdf();
        conv.GeneratePDF(document, "C:\\Users\\admin\\Documents\\test.pdf");
        Console.WriteLine("\nConverting to HTML...");
        //var htmlConverter = new HTMLConverter();
        //string html = htmlConverter.Convert(document);
        //Console.WriteLine(html);

        Console.WriteLine("\nDemonstrating document manipulation...");
        AddNewSection(document);

        Console.WriteLine("\nUpdated Document Structure:");
        PrintDocumentStructure(document);

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static void PrintDocumentStructure(Document doc, string indent = "")
    {
        foreach (var element in doc.Elements)
        {
            switch (element)
            {
                case Section section:
                    Console.WriteLine($"{indent}Section: {section.Title}");
                    PrintDocumentStructure(new Document { Elements = section.Content }, indent + "  ");
                    break;
                case Subsection subsection:
                    Console.WriteLine($"{indent}Subsection: {subsection.Title}");
                    PrintDocumentStructure(new Document { Elements = subsection.Content }, indent + "  ");
                    break;
                case Paragraph paragraph:
                    Console.WriteLine($"{indent}Paragraph: {string.Join("", paragraph.Content.OfType<TextElement>().Select(t => t.Text))}");
                    break;
                case MathExpression math:
                    Console.WriteLine($"{indent}Math: {math.Expression}");
                    break;
                case BoldText bt:
                    Console.WriteLine($"{indent}BoldText: {bt.Text}");
                    break;
                case DotTex2.Model.Environment env:
                    Console.WriteLine($"{indent}Environment: {env.Name}");
                    PrintDocumentStructure(new Document { Elements = env.Content }, indent + "  ");
                    break;
                default:
                    Console.WriteLine($"{indent}Unknown element: {element.GetType().Name}");
                    break;
            }
        }
    }

    static void AddNewSection(Document doc)
    {
        var newSection = new Section
        {
            Title = "New Section"
        };
        newSection.Content.Add(new Paragraph
        {
            Content = { new TextElement { Text = "This is a new section added programmatically." } }
        });
        doc.Elements.Add(newSection);
        Console.WriteLine("Added new section: 'New Section'");
    }
}


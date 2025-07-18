﻿using DotTex2.Lexing;
using DotTex2.Model;
using DotTex2.Model.InlineElements;
using DotTex2.Parsing;
using static System.Runtime.InteropServices.JavaScript.JSType;
using DotTex2.Convert;
using WhiteDot.Licensing;

class Program
{
    static void Main(string[] args)
    {

        Console.WriteLine("LaTeX Parser Demo");
        Console.WriteLine("=================");

        // Sample LaTeX document
        string latexContent = @"

\begin{document}

\section{\textsf{Introduction to Font Styles}}

This section demonstrates various \textbf{font styles} and \textit{formatting options} 
available in \LaTeX. Here's some \texttt{monospace text} and \textsc{small caps text}.

\subsection{Font Families}
{\rmfamily This text is in Roman Family}\\
{\sffamily This text is in Sans Serif Family}\\
{\ttfamily This text is in Typewriter Family}

\subsection{Font Sizes}
{\tiny Tiny text} \\
{\scriptsize Script size text} \\
{\footnotesize Footnote size text} \\
{\small Small text} \\
{\normalsize Normal size text} \\
{\large Large text} \\
{\Large Larger text} \\
{\LARGE Even larger text} \\
{\huge Huge text} \\
{\Huge Massive text}

\placeholder{nif}[9999999Y]
\placeholder{name}[John Doe]

\section{Mathematical Expressions}

Inline math: $E = mc^2$ and display math:
$$\int_{0}^{\infty} e^{-x^2} dx = \frac{\sqrt{\pi}}{2}$$

\section{Lists and Environments}

\begin{itemize}
    \item\textbf{Bold list item}
    \item\textit{Italic list item}
    \item{\large Large list item}
\end{itemize}

\begin{enumerate}
    \item{\sffamily First numbered item}
    \item{\ttfamily Second numbered item}
    \item{\scshape Third numbered item}
\end{enumerate}

\section{Table with Different Fonts}


\section{Verbatim Environment}
\begin{verbatim}
This is verbatim text
  It preserves spacing
    And formatting
\end{verbatim}

\section{\textsf{Mixed Formatting}}

{\large\bfseries This is large bold text.}

{\sffamily\itshape This is sans-serif italic text.}

{\ttfamily\bfseries This is bold typewriter text.}

{\normalsize\scshape This is normal-sized small caps text.}

\end{document}";

        //bool licenseValid = LicenseManagerExtensions.Initialize("C:\\repos\\WhiteDot\\WhiteDot.LicenseGenerator\\bin\\Debug\\net9.0\\acme-license.json");
        Console.WriteLine("Parsing LaTeX content...");
        var lexer = new Lexer();
        var tokens = lexer.Tokenize(latexContent).ToList();
        var parser = new Parser(tokens);
        var document = parser.Parse();

        Console.WriteLine("\nDocument Structure:");
        PrintDocumentStructure(document);
        var conv= new LatexToPdfObj();
        conv.SetPlaceholderValues(new Dictionary<string, string> {
    { "nif", "12345678X" },
    { "name", "John Winkler" }
});
        conv.GeneratePDF(document, "C:\\Users\\wnoah\\Desktop\\test2.pdf");
        //Console.WriteLine("\nConverting to HTML...");
        ////var htmlConverter = new HTMLConverter();
        ////string html = htmlConverter.Convert(document);
        ////Console.WriteLine(html);

        //Console.WriteLine("\nDemonstrating document manipulation...");
        //AddNewSection(document);

        //Console.WriteLine("\nUpdated Document Structure:");
        //PrintDocumentStructure(document);

        //Console.WriteLine("\nPress any key to exit...");
        //Console.ReadKey();
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


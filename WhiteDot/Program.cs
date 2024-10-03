// See https://aka.ms/new-console-template for more information
using static System.Collections.Specialized.BitVector32;
using System.Reflection.Metadata;
using DotTex;
using Document = DotTex.Document;
using Section = DotTex.Section;

class Program
{
    static void Main(string[] args)
    {
        // Create a document
        Document doc = new Document();

        // Add a section
        Section section1 = new Section("Introduction");
        section1.AddTag(new Paragraph("This is the introduction paragraph in a LaTeX document."));
        section1.AddTag(new Equation("E = mc^2"));

        // Add a subsection to the section
        Section subsection = new Section("Background");
        subsection.AddTag(new Paragraph("This is a subsection with more details."));
        section1.AddTag(subsection);

        // Add the section to the document
        doc.AddTag(section1);

        // Render the document to LaTeX
        string latexOutput = doc.Render();
        Console.WriteLine(latexOutput);
    }
}

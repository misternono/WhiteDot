using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DotTex2.Model.Environments
{

    public class Verbatim : Environment
    {
        public string Text { get; set; } // Verbatim content as plain text

        public Verbatim()
        {
            Name = "verbatim";
        }
    }


}

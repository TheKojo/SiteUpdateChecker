using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteChecker
{
    public class HTMLElement
    {
        public string TagType { get; set; }

        public string Id {  get; set; }

        public string Class { get; set; }

        public string TextContents { get; set; }

        public List<HTMLElement> Children { get; set; } = new List<HTMLElement>();   

        public bool isOpen { get; set; }   
    }
}

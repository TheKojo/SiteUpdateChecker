using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SiteChecker
{
    public class Website
    {

        private string url;

        public Website(string url, string cookie, string alertSoundPath)
        {
            Url = url;
            Cookie = cookie;
            AlertSoundPath = alertSoundPath;  
        }

        public string Url {
            get { return url; }   
            set { 
                url = value;
                int urlNameIdxStart = value.IndexOf("www.") + 4;
                int urlNameIdxEnd = value.IndexOf(".", urlNameIdxStart);
                Name = value.Substring(urlNameIdxStart, urlNameIdxEnd - urlNameIdxStart);
            }  
        }

        public string Cookie { get; set; }

        public string AlertSoundPath { get; set; }

        public string Name;

        public string OutputPath
        {
            get { return "D://" + Name + ".txt"; }
        }

        public List<HTMLElement> HTMLTree { get; set; }

    }

    public class HTMLData
    {
        public HTMLData(string data)
        {
            Data = data;
        }
        //public int CharIndex { get; set; }

        public string Data { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using CommandLine;

namespace ImageExtractor
{
    class Program
    {
        public static void Main(string[] args)
        {
            var commandLineOptions = new CommandLineOption();

            if(!Parser.Default.ParseArguments(args, commandLineOptions))
            {
                return;
            }

            var pageXml = new PageXml(commandLineOptions.XmlFilePath);
            var extractor = new ImageExtractor(pageXml);

            extractor.Extract(commandLineOptions.ImageFilePath, commandLineOptions.OutputFolderPath);
        }
    }
}

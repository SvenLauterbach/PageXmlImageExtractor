using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageExtractor
{
    public class CommandLineOption
    {
        [Option('i', "ImageFilePath", Required = true, HelpText = "Specifies the path to the image to extract the glyphs from.")]
        public string ImageFilePath { get; set; }

        [Option('x', "XmlFilePath", Required = true, HelpText = "Specifies the path to the aletheia page xml file.")]
        public string XmlFilePath { get; set; }

        [Option('o', "OutputFolderPath", Required = true, HelpText = "The Folder to put the glyph images in.")]
        public string OutputFolderPath { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ImageExtractor
{
    /// <summary>
    /// Class for representing a page xml file created by Aletheia (page xml version 3)
    /// </summary>
    public class PageXml
    {
        private string pageXmlPath;
        private XDocument aletheiaDoc;
        private XmlReader reader;
        private XNamespace pageXmlNamespace;

        public PageXml(string path)
        {
            pageXmlPath = path;
            aletheiaDoc = XDocument.Load(pageXmlPath);
            reader = aletheiaDoc.CreateReader();
            pageXmlNamespace = GetNamespace();
        }

        /// <summary>
        /// Parses all glyphs from the page xml file.
        /// </summary>
        /// <returns>List of glyph objects with ID and Unicode propagated.</returns>
        public List<Glyph> GetGlyphs()
        {
            /*
             * For some odd reason we have to specify our own xml prefix for the namespace,
             * because .NET didn't accept a URI for the default namespace xmlns. 
             * 
             */
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(reader.NameTable);
            namespaceManager.AddNamespace("aletheia", pageXmlNamespace.NamespaceName);

            var Extracts = from REC in aletheiaDoc.Descendants(pageXmlNamespace + "Glyph")
                           select new Glyph
                           {
                               ID = (string)(REC.Attribute("id") ?? new XAttribute("id", string.Empty)),
                               Unicode = REC.XPathSelectElement("./aletheia:TextEquiv/aletheia:Unicode", namespaceManager) != null
                                       ? REC.XPathSelectElement("./aletheia:TextEquiv/aletheia:Unicode", namespaceManager).Value
                                       : string.Empty,
                               PointsString = REC.XPathSelectElement("./aletheia:Coords", namespaceManager) != null
                                            ? REC.XPathSelectElement("./aletheia:Coords", namespaceManager).Attribute("points").Value
                                            : string.Empty
                           };

            return Extracts.ToList();
        }

        /// <summary>
        /// Returns the URI of the xmlns Namespace from the xml file in <paramref name="path"/>
        /// </summary>
        /// <param name="path">The path to the xml file.</param>
        /// <returns>The URI of the default Namespace</returns>
        private string GetNamespace()
        {

            /*
             * snippet by Scott Hanselman:
             * http://www.hanselman.com/blog/GetNamespacesFromAnXMLDocumentWithXPathDocumentAndLINQToXML.aspx
             */
            var namespaces = aletheiaDoc.Root.Attributes().
                                Where(a => a.IsNamespaceDeclaration).
                                GroupBy(a => a.Name.Namespace == XNamespace.None ? String.Empty : a.Name.LocalName,
                                        a => XNamespace.Get(a.Value)).
                                ToDictionary(g => g.Key,
                                             g => g.First());


            if (namespaces[""] == null)
            {
                //ToDo: Exception/Error Message (xml file is not a valid page.xml)
            }

            return namespaces[""].NamespaceName;
        }
    }
}

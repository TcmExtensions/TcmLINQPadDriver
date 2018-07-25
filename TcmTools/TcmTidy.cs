using System.IO;
using System.Text;
using System.Xml;
using TidyNet;

/// <summary>
/// This class wrap TidyNet to convert HTML into XHTML that can be stored in a rich-text field in Tridion.
/// </summary>
public static class TcmTidy
{
    public const string XhtmlNamespace = "http://www.w3.org/1999/xhtml";
    public static string ConvertHtmlToXhtml(string source)
    {
        MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(source));
        MemoryStream output = new MemoryStream();

        TidyMessageCollection tmc = new TidyMessageCollection();
        Tidy tidy = new Tidy();

        tidy.Options.DocType = DocType.Omit;
        tidy.Options.DropFontTags = true;
        tidy.Options.LogicalEmphasis = true;
        tidy.Options.Xhtml = true;
        tidy.Options.XmlOut = true;
        tidy.Options.MakeClean = true;
        tidy.Options.TidyMark = false;
        tidy.Options.NumEntities = true;

        tidy.Parse(input, output, tmc);

        XmlDocument x = new XmlDocument();
        XmlDocument xhtml = new XmlDocument();
        xhtml.LoadXml("<body />");
        XmlNode xhtmlBody = xhtml.SelectSingleNode("/body");

        x.LoadXml(Encoding.UTF8.GetString(output.ToArray()));
        XmlAttribute ns = x.CreateAttribute("xmlns");
        ns.Value = XhtmlNamespace;
        XmlNode body = x.SelectSingleNode("/html/body");
        foreach (XmlNode node in body.ChildNodes)
        {
            if (node.NodeType == XmlNodeType.Element)
                node.Attributes.Append(ns);

            xhtmlBody.AppendChild(xhtml.ImportNode(node, true));
        }
        return xhtmlBody.InnerXml;
    }
}

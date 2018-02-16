using System.Xml;


namespace Doc.Odf
{
    public class ManifestFile : DocumentElement
    {
        //manifest:file-entry
        public ManifestFile(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        public ManifestFile(ODFDocument document)
            : base(document, document.xmlManifest.CreateElement(Service.ManifestFile, Service.nsManifest))
        {
        }
        //manifest:media-type
        public string MediaType
        {
            get { return Element.GetAttribute("manifest:media-type"); }
            set { Service.SetAttribute(Element, "manifest:media-type", Service.nsManifest, value); }
        }
        //manifest:full-path
        public string FullPath
        {
            get { return Element.GetAttribute("manifest:full-path"); }
            set { Service.SetAttribute(Element, "manifest:full-path", Service.nsManifest, value); }
        }
        //manifest:version
        public string Version
        {
            get { return Element.GetAttribute("manifest:version"); }
            set { Service.SetAttribute(Element, "manifest:version", Service.nsManifest, value); }
        }
    }

}

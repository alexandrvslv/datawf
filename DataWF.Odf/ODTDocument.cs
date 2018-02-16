using System;
using System.Collections.Generic;
using Xwt.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using ICSharpCode.SharpZipLib.Zip;


namespace Doc.Odf
{
    public enum PlaceholdeType
    {
        Text,
        TextBox,
        Image,
        Table,
        Object
    }

    public class ODFDocument
    {
        private const string fMainfest = "META-INF/manifest.xml";
        private const string fContent = "content.xml";
        private const string fStyles = "styles.xml";
        private const string fMeta = "meta.xml";
        private const string fSetting = "settings.xml";
        private const string fLayoutCache = "layout-cache";

        public XmlDocument xmlSettings;
        public XmlDocument xmlStyles;
        public XmlDocument xmlContent;
        public XmlDocument xmlManifest;
        public XmlDocument xmlMeta;

        protected DocumentSettings documentSettings;
        protected DocumentStyles documentStyles;
        protected DocumentContent documentContent;
        protected DocumentMeta documentMeta;
        protected DocumentManifest documentManifest;

        public Dictionary<string, Dictionary<string, object>> files;


        public ODFDocument(byte[] value)
        {
            Load(value);
        }

        public string AddImage(Image image)
        {
            using (var stream = new MemoryStream())
            {
                image.Save(stream, ImageFileType.Png);
                string name = "Pictures/" + Guid.NewGuid().ToString() + ".png";
                AddFile("image/png", name, stream.ToArray());
                return name;
            }
        }
        private void AddFile(string type, string path, byte[] data)
        {
            var paramD = new Dictionary<string, object>();
            paramD.Add("IsDirectory", false);
            paramD.Add("Data", data);
            //
            files.Add(path, paramD);
            ManifestFile file = new ManifestFile(this);
            file.FullPath = path;
            file.MediaType = type;
            Manifest.Add(file);
        }

        public DocumentContent Content
        {
            get { return documentContent; }
        }
        public DocumentStyles Styles
        {
            get { return documentStyles; }
        }
        public DocumentMeta Meta
        {
            get { return documentMeta; }
        }
        public DocumentManifest Manifest
        {
            get { return documentManifest; }
        }
        public DocumentSettings Settings
        {
            get { return documentSettings; }
        }

        public BaseStyle GetDefaultStyle(StyleFamily Family)
        {
            foreach (BaseStyle xe in this.documentStyles.Styles)
                if (string.IsNullOrEmpty(xe.Name) && xe is DefaultStyle && ((DefaultStyle)xe).Family == Family)
                    return xe;
            return null;
        }

        public BaseStyle GetStyle(string name)
        {
            if (name == null || name == "")
                return null;// this.documentStyles.Styles[Service.DefaultStyle] as BaseStyle;
            foreach (BaseStyle xe in this.documentContent.AutomaticStyles)
                if (xe.Name == name)
                    return xe;
            foreach (BaseStyle xe in this.documentStyles.Styles)
                if (xe.Name == name)
                    return xe;

            return null;
        }
        public FontFace GetFont(string FontName)
        {
            if (FontName == null || FontName == "")
                return null;
            foreach (FontFace xe in this.documentContent.FontFaces)
                if (xe.Name == FontName)
                    return xe;
            foreach (FontFace xe in this.Styles.FontFaces)
                if (xe.Name == FontName)
                    return xe;
            return null;
        }
        public PageLayout GetPageLayout(string PageLayoutName)
        {
            if (PageLayoutName == null || PageLayoutName == "")
                return null;

            foreach (BaseStyle xe in this.documentContent.AutomaticStyles)
                if (xe.Name == PageLayoutName)
                    return (PageLayout)xe;
            foreach (BaseStyle xe in this.Styles.AutomaticStyles)
                if (xe.Name == PageLayoutName)
                    return (PageLayout)xe;
            return null;
        }

        public List<Placeholder> GetPlaceholders()
        {
            return Service.GetPlaceholders(documentContent.Body);
        }

        public List<ParagraphStyle> GetParagraphStyles()
        {
            return Service.GetParagraphStyles(documentContent.AutomaticStyles);
        }
        public List<TextStyle> GetTextStyles()
        {
            return Service.GetTextStyles(documentContent.AutomaticStyles);
        }
        public List<CellStyle> GetCellStyles()
        {
            return Service.GetCellStyles(documentContent.AutomaticStyles);
        }
        public List<ColumnStyle> GetColumnStyles()
        {
            return Service.GetColumnStyles(documentContent.AutomaticStyles);
        }
        public List<GraphicStyle> GetGraphicStyles()
        {
            return Service.GetGraphicStyles(documentContent.AutomaticStyles);
        }
        public List<TableStyle> GetTableStyles()
        {
            return Service.GetTableStyles(documentContent.AutomaticStyles);
        }
        internal List<SectionStyle> GetSectionStyles()
        {
            return Service.GetSectionStyles(documentContent.AutomaticStyles);
        }
        public List<FrameImage> GetImages()
        {
            return Service.GetImages(documentContent.Body);
        }

        public List<Table> GetTables()
        {
            return Service.GetTables(documentContent.Body);
        }

        public List<Table> Tables
        {
            get { return GetTables(); }
        }
        public void InsertRange(List<object> values, BaseItem afterElement, bool replace)
        {
            BaseItem bi = null;
            foreach (object val in values)
            {
                bool flag = false;
                if (bi == null)
                {
                    if (replace)
                        flag = true;
                    bi = afterElement;
                }
                if (val is string)
                    bi = InsertText((string)val, bi, flag);
                else if (val is BaseItem)
                {
                    if (flag)
                        bi.Owner.Replace(bi, val as BaseItem);
                    else
                        bi.Owner.InsertAfter(bi, val as BaseItem);
                    bi = val as BaseItem;
                }
            }
        }
        public BaseItem InsertText(string text, BaseItem afterElement, bool replace)
        {
            BaseItem lastElement = null;
            Paragraph paragraph = afterElement as Paragraph;
            if (paragraph == null)
                paragraph = (Paragraph)Service.GetParent(afterElement, typeof(Paragraph));
            if (paragraph == null)
                return null;
            IStyledElement s = GetStyle(afterElement);

            string[] split = text.Split(new string[] { "\n" }, StringSplitOptions.None);
            if (split.Length == 0)
                return null;
            text = split[0];
            if (split.Length > 1)
            {
                Service.SeparateItem(paragraph, afterElement);
                Paragraph pp = paragraph;
                for (int i = 1; i < split.Length; i++)
                {
                    Paragraph p = (Paragraph)pp.Clone();
                    if (s.Style is ParagraphStyle)
                        p.Style = s.Style as ParagraphStyle;
                    else
                    {
                        ParagraphStyle ps = new ParagraphStyle(this);
                        ps.ParentStyleName = ((DefaultStyle)s.Style).Name;
                        p.Style = ps;
                    }
                    p.Clear();
                    List<BaseItem> list = ParseText(afterElement, split[i]);
                    foreach (BaseItem bi in list)
                    {
                        lastElement = bi;
                        p.Add(lastElement);
                    }
                    paragraph.Owner.InsertAfter(pp, p);
                    pp = p;

                }

            }
            List<BaseItem> toReplace = ParseText(afterElement, text);
            for (int i = 0; i < toReplace.Count; i++)
            {
                BaseItem bi = toReplace[i];
                if (lastElement == null)
                    lastElement = bi;
                if (afterElement != paragraph)
                {
                    if (replace && i == 0)
                        afterElement.Owner.Replace(afterElement, bi);
                    else
                        afterElement.Owner.InsertAfter(afterElement, bi);
                    afterElement = bi;
                }
                else
                {
                    paragraph.Add(bi);
                }
            }
            return lastElement;
        }

        protected List<FormatedString> GetFormatedStrings(string s)
        {
            MatchCollection mm;
            //<(?<tag>\w*)>(?<text>.*)</\k<tag>>
            string patterm = @"\[(?<tag>\w*)\](?<text>(?s).*?)\[(/\k<tag>)\]";

            string sub = string.Empty;
            FormatedString fs = null;

            mm = Regex.Matches(s, patterm, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            List<FormatedString> list = new List<FormatedString>();
            foreach (Match m in mm)
            {
                if (list.Count == 0)
                {
                    sub = s.Substring(0, m.Index);
                    list.Add(new FormatedString(sub, 0, m.Index));
                }
                else
                {
                    fs = list[list.Count - 1];
                    sub = s.Substring(fs.Index + fs.Length, m.Index - (fs.Index + fs.Length));
                    list.Add(new FormatedString(sub, fs.Index + fs.Length, sub.Length));
                }
                list.Add(new FormatedString(m.Groups["text"].Value, m.Index, m.Length, m.Groups["tag"].Value.ToLowerInvariant()));
            }
            if (list.Count > 0)
                fs = list[list.Count - 1];
            else
                fs = new FormatedString("", 0, 0, "");
            sub = s.Substring(fs.Index + fs.Length, s.Length - (fs.Index + fs.Length));
            list.Add(new FormatedString(sub, fs.Index + fs.Length, sub.Length));
            return list;
        }
        public IStyledElement GetStyle(BaseItem owner)
        {
            IStyledElement s = owner as IStyledElement;
            if (s == null)
                s = Service.GetParent(owner, typeof(IStyledElement)) as IStyledElement;
            return s;
        }
        public List<BaseItem> ParseText(BaseItem owner, string text)
        {
            List<BaseItem> bi = new List<BaseItem>();

            IStyledElement s = GetStyle(owner);
            TextStyle ts = s.Style as TextStyle;

            List<FormatedString> list = GetFormatedStrings(text);
            foreach (FormatedString str in list)
            {
                if (str.Length == 0)
                    continue;
                TextElement te = new TextElement(this, str.Value);
                if (str.Format != null && str.Format.Length != 0 && ts != null)
                {
                    TextStyle style = new TextStyle(this);
                    style.ParentStyle = ts;

                    if (str.Format == "i")
                        style.TextProperties.FontStyle = FontStyles.Italic;
                    if (str.Format == "b")
                        style.TextProperties.FontWeight = FontWheights.wBold;

                    TextSpan span = new TextSpan(this);
                    span.Add(te);
                    span.Style = style;
                    bi.Add(span);
                }
                else
                    bi.Add(te);
            }
            // bi.Add(new TextElement(this, text));

            return bi;
        }
        public void InsertDocument(TextDocument te, DocumentElement afterElement)
        {

            foreach (BaseItem bi in te.Content.Body.Text)
            {
                this.Content.Body.Text.InsertAfter(afterElement, (BaseItem)bi.Clone());
            }
            foreach (BaseStyle bs in te.Content.AutomaticStyles)
            {
                if (bs.Name != "" && this.Content.AutomaticStyles["style:name", bs.Name] == null)
                    this.Content.AutomaticStyles.Add((BaseItem)bs.Clone());
            }
            foreach (BaseStyle bs in te.Styles.Styles)
            {
                if (bs.Name != "" && this.Styles.Styles["style:name", bs.Name] == null)
                    this.Styles.Styles.Add((BaseItem)bs.Clone());
            }
            foreach (KeyValuePair<string, Dictionary<string, object>> pair in te.files)
                if (!this.files.ContainsKey(pair.Key))
                    this.files.Add(pair.Key, pair.Value);

            foreach (BaseItem b in te.Manifest)
            {
                bool flag = false;
                foreach (BaseItem bd in this.Manifest)
                    if (bd.XmlContent == b.XmlContent)
                    {
                        flag = true;
                        break;
                    }
                if (!flag)
                {
                    this.Manifest.Add((BaseItem)b.Clone());
                }
            }
        }

        public void Load(byte[] value)
        {
            if (value == null)
                return;
            MemoryStream mainStream = new MemoryStream(value);
            ZipFile zipfile = new ZipFile(mainStream);

            xmlStyles = new XmlDocument();

            xmlContent = new XmlDocument();

            xmlManifest = new XmlDocument();

            xmlMeta = new XmlDocument();

            xmlSettings = new XmlDocument();

            files = new Dictionary<string, Dictionary<string, object>>();
            foreach (ZipEntry zipEntry in zipfile)
            {
                Stream streamOfFile = zipfile.GetInputStream(zipEntry);

                if (zipEntry.Name == fContent)
                    xmlContent.Load(streamOfFile);
                else if (zipEntry.Name == fStyles)
                    xmlStyles.Load(streamOfFile);
                else if (zipEntry.Name == fMeta)
                    xmlMeta.Load(streamOfFile);
                else if (zipEntry.Name == fMainfest)
                    xmlManifest.Load(streamOfFile);
                else if (zipEntry.Name == fSetting)
                    xmlSettings.Load(streamOfFile);

                byte[] data = new byte[zipEntry.Size];
                try
                {
                    streamOfFile.Read(data, 0, data.Length);
                }
                catch
                {

                }
                streamOfFile.Close();
                Dictionary<string, object> list = new Dictionary<string, object>();
                list.Add("ZipFileIndex", zipEntry.ZipFileIndex);
                list.Add("IsDirectory", zipEntry.IsDirectory);
                list.Add("Data", data);
                files.Add(zipEntry.Name, list);
            }


            mainStream.Close();
            mainStream.Dispose();
            zipfile.Close();

            documentStyles = (DocumentStyles)Service.Fabrica(this, xmlStyles.DocumentElement);
            documentContent = (DocumentContent)Service.Fabrica(this, xmlContent.DocumentElement);

            if (xmlMeta.DocumentElement != null)
                documentMeta = (DocumentMeta)Service.Fabrica(this, xmlMeta.DocumentElement);

            if (xmlManifest.DocumentElement != null)
                documentManifest = (DocumentManifest)Service.Fabrica(this, xmlManifest.DocumentElement);

            if (xmlSettings.DocumentElement != null)
                documentSettings = (DocumentSettings)Service.Fabrica(this, xmlSettings.DocumentElement);
        }

        internal class XmlWriterQuote : XmlTextWriter
        {
            public XmlWriterQuote(Stream stream, System.Text.Encoding encoding)
                : base(stream, encoding)
            {

            }

            public override void WriteString(string text)
            {
                text = text.Replace("'", "&apos;");
                base.WriteString(text);
            }

            public override void WriteStartAttribute(string prefix, string localName, string ns)
            {
                base.WriteStartAttribute(prefix, localName, ns);
            }

            public override void WriteValue(string value)
            {
                value = value.Replace("'", "&apos;");
                base.WriteValue(value);
            }

            public new void WriteAttributeString(string local, string value)
            {
                value = value.Replace("'", "&apos;");
                base.WriteAttributeString(local, value);
            }

            public new void WriteAttributeString(string local, string ns, string value)
            {
                value = value.Replace("'", "&apos;");
                base.WriteAttributeString(local, ns, value);
            }

            public new void WriteAttributeString(string pref, string local, string ns, string value)
            {
                value = value.Replace("'", "&apos;");
                base.WriteAttributeString(pref, local, ns, value);
            }

            public override void WriteAttributes(XmlReader reader, bool defattr)
            {
                base.WriteAttributes(reader, defattr);
            }
        }

        public byte[] UnLoad()
        {
            MemoryStream memoryStream = new MemoryStream();
            //System.Text.StringBuilder sb = new System.Text.StringBuilder();
            //XmlWriter writer = XmlWriter.Create(sb);
            //sb = sb.Replace("'", "&apos;");

            //xmlContent.For
            xmlContent.Save(memoryStream);
            memoryStream.Close();
            files[fContent]["Data"] = memoryStream.ToArray();// System.Text.Encoding.UTF8.GetBytes(sb.ToString())
            memoryStream.Dispose();

            memoryStream = new MemoryStream();
            xmlManifest.Save(memoryStream);
            memoryStream.Close();
            files[fMainfest]["Data"] = memoryStream.ToArray();
            memoryStream.Dispose();

            if (xmlMeta.DocumentElement != null)
            {
                memoryStream = new MemoryStream();
                xmlMeta.Save(memoryStream);
                memoryStream.Close();
                files[fMeta]["Data"] = memoryStream.ToArray();
                memoryStream.Dispose();
            }

            if (xmlSettings.DocumentElement != null)
            {
                memoryStream = new MemoryStream();
                xmlSettings.Save(memoryStream);
                memoryStream.Close();
                files[fSetting]["Data"] = memoryStream.ToArray();
                memoryStream.Dispose();
            }

            memoryStream = new MemoryStream();
            xmlStyles.Save(memoryStream);
            memoryStream.Close();
            files[fStyles]["Data"] = memoryStream.ToArray();
            memoryStream.Dispose();

            MemoryStream mainStream = new MemoryStream();

            ZipOutputStream zo = new ZipOutputStream(mainStream);
            zo.UseZip64 = UseZip64.Off;
            foreach (string key in files.Keys)
            {
                ZipEntry ze = new ZipEntry(key);
                //ze.IsDirectory = ;

                zo.PutNextEntry(ze);
                if (!(bool)files[key]["IsDirectory"])
                    zo.Write((byte[])files[key]["Data"], 0, ((byte[])files[key]["Data"]).Length);
            }
            zo.Finish();
            mainStream.Close();

            return mainStream.ToArray();
        }

        public void Save(string fileName)
        {
            File.WriteAllBytes(fileName, this.UnLoad());
        }

        public Frame GetFrame(Image im, double maxWidth)
        {
            Frame frame = new Frame(this);
            frame.Width.Data = (im.Width / ((BitmapImage)im).PixelWidth) * 2.54D;
            frame.Height.Data = (im.Height / ((BitmapImage)im).PixelHeight) * 2.54D;
            if (frame.Width.Data > maxWidth)
            {
                var coef = maxWidth / frame.Width.Data;
                frame.Width.Data = frame.Width.Data * coef;
                frame.Height.Data = frame.Height.Data * coef;
            }
            frame.Z = 1;
            FrameImage fi = new FrameImage(this);
            fi.HRef = AddImage(im);
            frame.Add(fi);
            return frame;
        }

        public void Destroy()
        {
            throw new NotImplementedException();
        }
    }

}

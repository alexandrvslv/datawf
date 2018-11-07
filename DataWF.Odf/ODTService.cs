using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;


namespace Doc.Odf
{
    public interface ITextContainer
    {
        string Value { get; set; }
    }

    public interface ITextual
    {
        string Value { get; set; }
    }

    public interface IStyledElement
    {
        DefaultStyle Style { get; }
    }

    public interface IStyleTextProperty
    {
        TextProperties TextProperties { get; }
    }

    public class Service
    {
        public static void SetAttribute(XmlElement node, string name, string ns, string value)
        {
            if (node.GetAttributeNode(name) == null)
            {
                string[] split = name.Split(new char[] { ':' });
                XmlAttribute attribute = node.OwnerDocument.CreateAttribute(split[0], split[1], ns);
                node.SetAttributeNode(attribute);
            }
            node.SetAttribute(name, value);
        }
        public static Cell BaseToCell(BaseItem bi)
        {
            return bi as Cell;
        }
        public static Column BaseToColumn(BaseItem bi)
        {
            return bi as Column;
        }
        public static Row BaseToRow(BaseItem bi)
        {
            return bi as Row;
        }
        public static GraphicStyle BaseToGraphic(BaseItem bi)
        {
            return bi as GraphicStyle;
        }
        public static List<Table> GetTables(DocumentElementCollection element)
        {
            return GetChildsRecursive(element, typeof(Table)).ConvertAll<Table>(new Converter<BaseItem, Table>(BaseToTable));
        }
        internal static List<GraphicStyle> GetGraphicStyles(DocumentElementCollection element)
        {
            return GetChildsRecursive(element, typeof(GraphicStyle)).ConvertAll<GraphicStyle>(new Converter<BaseItem, GraphicStyle>(BaseToGraphic));
        }
        public static Table BaseToTable(BaseItem bi)
        {
            return bi as Table;
        }
        public static List<FrameImage> GetImages(DocumentElementCollection element)
        {
            return GetChildsRecursive(element, typeof(FrameImage)).ConvertAll<FrameImage>(new Converter<BaseItem, FrameImage>(BaseToImage));
        }
        public static FrameImage BaseToImage(BaseItem bi)
        {
            return bi as FrameImage;
        }
        public static List<TableStyle> GetTableStyles(DocumentElementCollection element)
        {
            return GetChildsRecursive(element, typeof(TableStyle)).ConvertAll<TableStyle>(new Converter<BaseItem, TableStyle>(BaseToTableStyle));
        }
        public static TableStyle BaseToTableStyle(BaseItem bi)
        {
            return bi as TableStyle;
        }
        public static List<ParagraphStyle> GetParagraphStyles(DocumentElementCollection element)
        {
            return GetChildsRecursive(element, typeof(ParagraphStyle)).ConvertAll<ParagraphStyle>(new Converter<BaseItem, ParagraphStyle>(BaseToParagraphStyle));
        }
        public static List<TextStyle> GetTextStyles(DocumentElementCollection element)
        {
            return GetChildsRecursive(element, typeof(TextStyle)).ConvertAll<TextStyle>(new Converter<BaseItem, TextStyle>(BaseToTextStyle));
        }
        public static List<CellStyle> GetCellStyles(DocumentElementCollection element)
        {
            return GetChildsRecursive(element, typeof(CellStyle)).ConvertAll<CellStyle>(new Converter<BaseItem, CellStyle>(BaseToCellStyle));
        }
        public static List<ColumnStyle> GetColumnStyles(DocumentElementCollection element)
        {
            return GetChildsRecursive(element, typeof(ColumnStyle)).ConvertAll<ColumnStyle>(new Converter<BaseItem, ColumnStyle>(BaseToColumnStyle));
        }
        public static List<SectionStyle> GetSectionStyles(DocumentElementCollection element)
        {
            return GetChildsRecursive(element, typeof(SectionStyle)).ConvertAll<SectionStyle>(new Converter<BaseItem, SectionStyle>(BaseToSectionStyle)); ;
        }
        public static SectionStyle BaseToSectionStyle(BaseItem bi)
        {
            return bi as SectionStyle;
        }
        public static ParagraphStyle BaseToParagraphStyle(BaseItem bi)
        {
            return bi as ParagraphStyle;
        }
        public static CellStyle BaseToCellStyle(BaseItem bi)
        {
            return bi as CellStyle;
        }
        public static ColumnStyle BaseToColumnStyle(BaseItem bi)
        {
            return bi as ColumnStyle;
        }
        public static TextStyle BaseToTextStyle(BaseItem bi)
        {
            return bi as TextStyle;
        }
        public static List<Placeholder> GetPlaceholders(DocumentElementCollection element)
        {
            return GetChildsRecursive(element, typeof(Placeholder)).ConvertAll<Placeholder>(new Converter<BaseItem, Placeholder>(BaseToPlaceholder));
        }
        public static Placeholder BaseToPlaceholder(BaseItem bi)
        {
            return bi as Placeholder;
        }
        public static void Replace(BaseItem oldItem, BaseItem newItem)
        {
            DocumentElementCollection owner = oldItem.Owner;
            owner.Replace(oldItem, newItem);
        }
        public static BaseItem GetParent(BaseItem bi, Type type)
        {
            if (bi.Owner == null) return null;
            if (!type.IsInterface)
            {
                if (bi.Owner.GetType() == type) return bi.Owner;
                else return GetParent(bi.Owner, type);
            }
            else
                if (bi.Owner.GetType().GetInterface(type.Name) != null) return bi.Owner;
            else return GetParent(bi.Owner, type);
        }
        public static List<BaseItem> GetChildsRecursive(BaseItem bi, Type type)
        {
            List<BaseItem> lbi = new List<BaseItem>();
            if (bi.GetType() == type) lbi.Add(bi);
            else if (bi is DocumentElementCollection)
                foreach (BaseItem i in (DocumentElementCollection)bi)
                    lbi.AddRange(GetChildsRecursive(i, type));
            return lbi;
        }
        public static List<BaseItem> GetChilds(BaseItem bi, Type type)
        {
            List<BaseItem> lbi = new List<BaseItem>();
            if (bi is DocumentElementCollection)
                foreach (BaseItem i in (DocumentElementCollection)bi)
                    if (i.GetType() == type) lbi.Add(i);
            return lbi;
        }
        public static TextDocument NewEmptyDocument()
        {
            return new TextDocument();
        }
        public static string ParceName(string prefix, string name)
        {
            return prefix + ":" + name;
        }
        public static string GetText(DocumentElementCollection item)
        {
            var sez = new StringBuilder();
            foreach (BaseItem bi in item)
                if (bi is ITextContainer) sez.Append(((ITextContainer)bi).Value);
                else if (bi is ITextual) sez.Append(((ITextual)bi).Value);
            return sez.ToString();
        }
        //public static object GetTextPropertyByParent(TextProperties si,string propertyName)
        //{
        //     Type type = si.GetType(); 
        //    if (si.GetType() != type) return null;
        //    PropertyInfo pi = type.GetProperty(propertyName);
        //    if (pi == null) return null;
        //    object val = pi.GetValue(si, null);
        //    if(val)
        //}
        public static XmlElement GetParent(XmlElement node, string nodeName)
        {
            if (node.Name == nodeName) return node;
            return GetParent(node.ParentNode as XmlElement, nodeName);
        }
        public static BaseItem Fabrica(ODFDocument d, XmlNode xn)
        {
            if (xn is XmlText) return new TextElement(d, (XmlText)xn);
            XmlElement xe = xn as XmlElement;
            BaseItem it = null;
            if (xe == null)
            {

            }
            if (xe.Name == FontFace) it = new FontFace(d, xe);

            else if (xe.Name == DateStyle) it = new DateStyle(d, xe);
            else if (xe.Name == TimeStyle) it = new TimeStyle(d, xe);
            else if (xe.Name == TableOfContent) it = new TableOfContent(d, xe);
            else if (xe.Name == TableOfContentSource) it = new TableOfContentSource(d, xe);
            else if (xe.Name == TableOfContentEntryTemplate) it = new TableOfContentEntryTemplate(d, xe);
            else if (xe.Name == IndexTitleTemplate) it = new IndexTitleTemplate(d, xe);
            else if (xe.Name == IndexEntryLinkStart) it = new IndexEntryLinkStart(d, xe);
            else if (xe.Name == IndexEntrySpan) it = new IndexEntrySpan(d, xe);
            else if (xe.Name == IndexEntryText) it = new IndexEntryText(d, xe);
            else if (xe.Name == IndexEntryTabStop) it = new IndexEntryTabStop(d, xe);
            else if (xe.Name == IndexEntryPageNumber) it = new IndexEntryPageNumber(d, xe);
            else if (xe.Name == IndexEntryLinkEnd) it = new IndexEntryLinkEnd(d, xe);
            else if (xe.Name == IndexBody) it = new IndexBody(d, xe);

            else if (xe.Name == TextBibliographyConfiguration) it = new TextBibliographyConfiguration(d, xe);
            else if (xe.Name == TextSortKey) it = new TextSortKey(d, xe);

            else if (xe.Name == DrawTextBox) it = new DrawTextBox(d, xe);
            else if (xe.Name == SoftPageBreak) it = new SoftPageBreak(d, xe);
            else if (xe.Name == TextHeader) it = new TextHeader(d, xe);
            else if (xe.Name == TextLink) it = new TextLink(d, xe);
            else if (xe.Name == TextSection) it = new TextSection(d, xe);
            else if (xe.Name == TextSpace) it = new TextSpace(d, xe);
            else if (xe.Name == TextTab) it = new TextTab(d, xe);
            else if (xe.Name == PageLayout) it = new PageLayout(d, xe);
            else if (xe.Name == PageLayoutProperties) it = new PageLayoutProperties(d, xe);

            else if (xe.Name == MasterPage) it = new MasterPage(d, xe);

            else if (xe.Name == Frame) it = new Frame(d, xe);
            else if (xe.Name == FrameImage) it = new FrameImage(d, xe);

            else if (xe.Name == List) it = new List(d, xe);
            else if (xe.Name == ListItem) it = new ListItem(d, xe);

            else if (xe.Name == NotesConfiguration) it = new NotesConfiguration(d, xe);
            else if (xe.Name == LineNumberingConfiguration) it = new LineNumberingConfiguration(d, xe);

            else if (xe.Name == ListStyle) it = new ListStyle(d, xe);
            else if (xe.Name == ListLevelStyleBullet) it = new ListLevelStyleBullet(d, xe);
            else if (xe.Name == ListLevelStyleNumber) it = new ListLevelStyleNumber(d, xe);
            else if (xe.Name == ListLevelProperties) it = new ListLevelProperties(d, xe);
            else if (xe.Name == ListLevelLableAligment) it = new ListLevelLableAligment(d, xe);
            else if (xe.Name == OutlineStyle) it = new OutlineStyle(d, xe);
            else if (xe.Name == OutlineLevelStyle) it = new OutlineLevelStyle(d, xe);

            else if (xe.Name == ParagraphProperties) it = new ParagraphProperties(d, xe);
            else if (xe.Name == TabStops) it = new TabStops(d, xe);
            else if (xe.Name == TabStop) it = new TabStop(d, xe);
            else if (xe.Name == TextProperties) it = new TextProperties(d, xe);
            else if (xe.Name == TableProperties) it = new TableProperties(d, xe);
            else if (xe.Name == ColumnProperties) it = new ColumnProperties(d, xe);
            else if (xe.Name == CellProperties) it = new CellProperties(d, xe);
            else if (xe.Name == GraphicProperties) it = new GraphicProperties(d, xe);
            else if (xe.Name == SectionProperties) it = new SectionProperties(d, xe);
            else if (xe.Name == BackgroundImageStyle) it = new BackgroundImageStyle(d, xe);
            else if (xe.Name == ColumnsStyle) it = new ColumnsStyle(d, xe);

            else if (xe.Name == Paragraph) it = new Paragraph(d, xe);
            else if (xe.Name == TextSpan) it = new TextSpan(d, xe);
            else if (xe.Name == Placeholder) it = new Placeholder(d, xe);
            else if (xe.Name == Table) it = new Table(d, xe);
            else if (xe.Name == CoveredCell) it = new CoveredCell(d, xe);
            else if (xe.Name == Column) it = new Column(d, xe);
            else if (xe.Name == Row) it = new Row(d, xe);
            else if (xe.Name == Cell) it = new Cell(d, xe);
            else if (xe.Name == TableCalculationSettings) it = new TableCalculationSettings(d, xe);
            else if (xe.Name == DocumentStyles) it = new DocumentStyles(d, xe);
            else if (xe.Name == Styles) it = new Styles(d, xe);

            else if (xe.Name == Style)
            {
                string buf = xe.GetAttribute("style:family");
                if (buf == "graphic") it = new GraphicStyle(d, xe);
                else if (buf == "paragraph") it = new ParagraphStyle(d, xe);
                else if (buf == "table") it = new TableStyle(d, xe);
                else if (buf == "table-row") it = new RowStyle(d, xe);
                else if (buf == "table-column") it = new ColumnStyle(d, xe);
                else if (buf == "table-cell") it = new CellStyle(d, xe);
                else if (buf == "text") it = new TextStyle(d, xe);
                else if (buf == "section") it = new SectionStyle(d, xe);
                else it = new DefaultStyle(d, xe);
            }
            else if (xe.Name == CurrencyStyle) it = new CurrencyStyle(d, xe);
            else if (xe.Name == NumberStyle) it = new NumberStyle(d, xe);
            else if (xe.Name == NumberTextStyle) it = new NumberTextStyle(d, xe);
            else if (xe.Name == Number) it = new Number(d, xe);
            else if (xe.Name == NumberText) it = new NumberText(d, xe);
            else if (xe.Name == DefaultStyle) it = new DefaultStyle(d, xe);
            else if (xe.Name == FontFaceDeclarations) it = new FontFaceDeclarations(d, xe);
            else if (xe.Name == DocumentContent) it = new DocumentContent(d, xe);
            else if (xe.Name == Scripts) it = new Scripts(d, xe);
            else if (xe.Name == AutomaticStyles) it = new AutomaticStyles(d, xe);
            else if (xe.Name == MasterStyles) it = new MasterStyles(d, xe);

            else if (xe.Name == DocumentSpreadSheet) it = new DocumentSpreadSheet(d, xe);
            else if (xe.Name == DocumentBody) it = new DocumentBody(d, xe);
            else if (xe.Name == DocumentText) it = new BodyText(d, xe);
            else if (xe.Name == TextSequeceDeclarations) it = new TextSequenceDeclarations(d, xe);
            else if (xe.Name == TextSequeceDeclaration) it = new TextSequenceDeclaration(d, xe);


            else if (xe.Name == DocumentMeta) it = new DocumentMeta(d, xe);
            else if (xe.Name == Meta) it = new Meta(d, xe);
            else if (xe.Name == MetaInitialCreator) it = new MetaInitialCreator(d, xe);
            else if (xe.Name == MetaCreationDate) it = new MetaCreationDate(d, xe);
            else if (xe.Name == MetaDate) it = new MetaDate(d, xe);
            else if (xe.Name == MetaCreator) it = new MetaCreator(d, xe);
            else if (xe.Name == MetaEditingDuration) it = new MetaEditiongDuration(d, xe);
            else if (xe.Name == MetaEditingCycles) it = new MetaEditiongCycles(d, xe);
            else if (xe.Name == MetaGenerator) it = new MetaGenerator(d, xe);
            else if (xe.Name == MetaStatistic) it = new MetaStatistic(d, xe);

            else if (xe.Name == DocumentManifest) it = new DocumentManifest(d, xe);
            else if (xe.Name == ManifestFile) it = new ManifestFile(d, xe);

            else if (xe.Name == DocumentSettings) it = new DocumentSettings(d, xe);
            else if (xe.Name == Settings) it = new Settings(d, xe);
            else if (xe.Name == ConfigItemSet) it = new ConfigItemSet(d, xe);
            else if (xe.Name == ConfigItem) it = new ConfigItem(d, xe);
            else if (xe.Name == ConfigItemMapIndexed) it = new ConfigItemMapIndexed(d, xe);
            else if (xe.Name == ConfigItemMapEntry) it = new ConfigItemMapEntry(d, xe);

            else it = new DocumentElementCollection(d, xe);
            return it;
        }
        public static BaseItem Fabrica(ODFDocument document, XmlNode xmlElement, DocumentElementCollection ownerItem)
        {
            BaseItem item = Fabrica(document, xmlElement);
            item.Owner = ownerItem;
            return item;
        }
        public const string PageLayout = "style:page-layout";
        public const string PageLayoutProperties = "style:page-layout-properties";
        public const string TextSpace = "text:s";
        public const string TextTab = "text:tab";


        public const string SoftPageBreak = "text:soft-page-break";


        public const string MasterPage = "style:master-page";


        public const string DateStyle = "number:date-style";
        public const string TimeStyle = "number:time-style";

        public const string Frame = "draw:frame";
        public const string FrameImage = "draw:image";

        public const string List = "text:list";
        public const string ListItem = "text:list-item";

        public const string NotesConfiguration = "text:notes-configuration";
        public const string LineNumberingConfiguration = "text:linenumbering-configuration";


        public const string ListStyle = "text:list-style";
        public const string ListLevelStyleBullet = "text:list-level-style-bullet";
        public const string ListLevelStyleNumber = "text:list-level-style-number";
        public const string ListLevelProperties = "style:list-level-properties";
        public const string ListLevelLableAligment = "style:list-level-label-alignment";
        public const string OutlineStyle = "text:outline-style";
        public const string OutlineLevelStyle = "text:outline-level-style";

        public const string FontFace = "style:font-face";

        public const string TableProperties = "style:table-properties";
        public const string ColumnProperties = "style:table-column-properties";
        public const string CellProperties = "style:table-cell-properties";
        public const string RowProperties = "style:table-row-properties";

        public const string GraphicProperties = "style:graphic-properties";
        public const string ParagraphProperties = "style:paragraph-properties";
        public const string TextProperties = "style:text-properties";
        public const string TabStops = "style:tab-stops";
        public const string TabStop = "style:tab-stop";

        public const string Table = "table:table";
        public const string Column = "table:table-column";
        public const string CoveredCell = "table:covered-table-cell";
        public const string Row = "table:table-row";
        public const string Cell = "table:table-cell";
        public const string Placeholder = "text:placeholder";
        public const string Paragraph = "text:p";
        public const string TextSpan = "text:span";

        public const string TextLink = "text:a";
        public const string TextHeader = "text:h";
        public const string TextSection = "text:section";
        //<text:section text:style-name="Sect2" text:name="Section7" 
        public const string TableCalculationSettings = "table:calculation-settings";
        public const string TableOfContent = "text:table-of-content";
        public const string TableOfContentSource = "text:table-of-content-source";
        public const string TableOfContentEntryTemplate = "text:table-of-content-entry-template";
        public const string IndexTitleTemplate = "text:index-title-template";
        public const string IndexEntryLinkStart = "text:index-entry-link-start";
        public const string IndexEntrySpan = "text:index-entry-span";
        public const string IndexEntryText = "text:index-entry-text";
        public const string IndexEntryTabStop = "text:index-entry-tab-stop";
        public const string IndexEntryPageNumber = "text:index-entry-page-number";
        public const string IndexEntryLinkEnd = "text:index-entry-link-end";
        public const string IndexBody = "text:index-body";

        public const string CurrencyStyle = "number:currency-style";
        public const string NumberStyle = "number:number-style";
        public const string NumberTextStyle = "number:text-style";
        public const string Number = "number:number";
        public const string NumberText = "number:text";
        public const string SectionStyle = "style:style";
        public const string SectionProperties = "style:section-properties";
        public const string BackgroundImageStyle = "style:background-image";
        public const string ColumnsStyle = "style:columns";


        public const string DocumentStyles = "office:document-styles";
        public const string Styles = "office:styles";
        public const string Style = "style:style";
        public const string DefaultStyle = "style:default-style";

        public const string FontFaceDeclarations = "office:font-face-decls";


        public const string AutomaticStyles = "office:automatic-styles";
        public const string MasterStyles = "office:master-styles";

        public const string DocumentSpreadSheet = "office:spreadsheet";
        public const string DocumentContent = "office:document-content";
        public const string DocumentBody = "office:body";
        public const string Streedsheet = "office:streedsheet";
        public const string Scripts = "office:scripts";
        public const string DocumentText = "office:text";
        public const string TextSequeceDeclarations = "text:sequence-decls";
        public const string TextSequeceDeclaration = "text:sequence-decl";

        public const string DocumentMeta = "office:document-meta";
        public const string Meta = "office:meta";
        public const string MetaInitialCreator = "meta:initial-creator";
        public const string MetaCreationDate = "meta:creation-date";
        public const string MetaDate = "dc:date";
        public const string MetaCreator = "dc:creator";
        public const string MetaEditingDuration = "meta:editing-duration";
        public const string MetaEditingCycles = "meta:editing-cycles";
        public const string MetaGenerator = "meta:generator";
        public const string MetaStatistic = "meta:document-statistic";

        public const string DocumentManifest = "manifest:manifest";
        public const string ManifestFile = "manifest:file-entry";

        public const string DocumentSettings = "office:document-settings";
        public const string Settings = "office:settings";
        public const string ConfigItemSet = "config:config-item-set";
        public const string ConfigItem = "config:config-item";
        public const string ConfigItemMapIndexed = "config:config-item-map-indexed";
        public const string ConfigItemMapEntry = "config:config-item-map-entry";

        public const string TextBibliographyConfiguration = "text:bibliography-configuration";
        public const string TextSortKey = "text:sort-key";

        public const string DrawTextBox = "draw:text-box";

        //namespace
        public const string nsManifest = "urn:oasis:names:tc:opendocument:xmlns:manifest:1.0";
        public const string nsTable = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";
        public const string nsOffice = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";
        public const string nsText = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";
        public const string nsDraw = "urn:oasis:names:tc:opendocument:xmlns:drawing:1.0";
        public const string nsSvg = "urn:oasis:names:tc:opendocument:xmlns:svg-compatible:1.0";
        public const string nsStyle = "urn:oasis:names:tc:opendocument:xmlns:style:1.0";
        public const string nsFO = "urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0";
        public const string nsMeta = "urn:oasis:names:tc:opendocument:xmlns:meta:1.0";
        public const string nsDataStyle = "urn:oasis:names:tc:opendocument:xmlns:datastyle:1.0";
        public const string nsXLink = "http://www.w3.org/1999/xlink";


        public static void SeparateItem(DocumentElementCollection toSeparate, BaseItem separator)
        {
            DocumentElementCollection r = SeparateSubItem(toSeparate, separator);
            if (r != null)
                toSeparate.Owner.InsertAfter(toSeparate, r);
        }

        public static DocumentElementCollection SeparateSubItem(DocumentElementCollection toSeparate, BaseItem separator)
        {
            if (toSeparate == separator) return null;
            BaseItem entry = GetEntryElement(toSeparate, separator);
            DocumentElementCollection result = null;
            if (entry == separator)
            {
                if (separator.IsLastElement) return null;
                result = toSeparate.Clone() as DocumentElementCollection;
                result.Clear();

                int index = toSeparate.IndexOf(separator) + 1;
                while (index < toSeparate.Count)
                {
                    BaseItem bi = toSeparate[index];
                    toSeparate.Remove(bi);
                    result.Add(bi);
                    //index++;
                }
            }
            else if (!entry.IsLastElement)
            {
                result = toSeparate.Clone() as DocumentElementCollection;
                result.Clear();

                DocumentElementCollection item = SeparateSubItem((DocumentElementCollection)entry, separator);
                if (item != null) result.Add(item);

                int index = toSeparate.IndexOf(entry) + 1;
                while (index < toSeparate.Count)
                {
                    BaseItem bi = toSeparate[index];
                    toSeparate.Remove(bi);
                    result.Add(bi);
                    //index++;
                }

            }
            return result;
        }

        public static BaseItem GetEntryElement(DocumentElementCollection paragraph, BaseItem afterElement)
        {
            if (paragraph.Contains(afterElement)) return afterElement;
            foreach (BaseItem bi in (DocumentElementCollection)paragraph)
            {
                if (bi is DocumentElementCollection)
                {
                    BaseItem entry = GetEntryElement((DocumentElementCollection)bi, afterElement);
                    if (entry == afterElement) return bi;
                }
            }
            return null;
        }
    }
}
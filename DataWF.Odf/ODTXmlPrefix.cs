


namespace Doc.Odf
{
	public class office
	{
		public static readonly string Name = "office";
		public static readonly string DocumentContent = "document-content";
		public static readonly string Scripts = "scripts";
		public static readonly string FontFaceDecls = "font-face-decls";
		public static readonly string AutomaticStyles = "automatic-styles";
		public static readonly string Styles = "styles";
		public static readonly string Text = "text";
		public static readonly string Body = "body";
		public static readonly string DocumentStyles = "document-styles";
	}
	public class style
	{
		public static readonly string Name = "style";
		public static readonly string Style = "style";
		public static readonly string FontFace = "font-face";
		public static readonly string DefaultStyle = "default-style";
		public static readonly string DefaultPageLayout = "default-page-layout";
		public static readonly string PageLayout = "page-layout";
		public static readonly string PageLayoutProperties = "page-layout-properties";
		public static readonly string ParagraphProperties = "paragraph-properties";
		public static readonly string TextProperties = "text-properties";
		public static readonly string GraphicProperties = "graphic-properties";
		public static readonly string TableProperties = "table-properties";
		public static readonly string TableRowProperties = "table-row-properties";
		public static readonly string ListLevelProperties = "list-level-properties";
	}
	public class svg
	{
		public static readonly string Name = "svg";
	}
	public class text
	{
		public static readonly string Name = "text";
		public static readonly string Paragraph = "p";
		public static readonly string List = "list";
		public static readonly string Span = "span";
	}
	public class fo
	{
		public static readonly string Name = "fo";
	}
	public class Atributes
	{
		public static readonly string name = "name";
		public class style
		{
			public static readonly string Family = "family";
			public static readonly string Class = "class";

			public static readonly string FontName = "font-name";
			public static readonly string FontNameAsian = "font-name-asian";
			public static readonly string FontCharset = "font-charset";
			public static readonly string FontFamilyGeneric = "font-family-generic";
			public static readonly string FontPitch = "font-pitch";
			public static readonly string FontWeightAsian = "font-weight-asian";
			public static readonly string FontWeightComplex = "font-weight-complex";
			public static readonly string UseWindowFontColor = "use-window-font-color";

			public static readonly string ParentStyleName = "parent-style-name";
			public static readonly string JustifySingleWord = "justify-single-word";
			public static readonly string AutoTextIndent = "auto-text-indent";
			public static readonly string ListStyleName = "list-style-name";
			public static readonly string WritingMode = "writing-mode";
			public static readonly string NextStyleName = "next-style-name";
			//public readonly static string NextStyleName = "next-style-name";

			public static readonly string LayoutGridColor = "layout-grid-color";
			public static readonly string LayoutGridLines = "layout-grid-lines";
			public static readonly string LayoutGridBaseHeight = "layout-grid-base-height";
			public static readonly string LayoutGridRubyHeight = "layout-grid-ruby-height";
			public static readonly string LayoutGridMode = "layout-grid-mode";


			public static readonly string TextUnderlineStyle = "text-underline-style";
			public static readonly string TextUnderlineWidth = "text-underline-width";
			public static readonly string TextUnderlineColor = "text-underline-color";
			
		}
		public class svg
		{
			public static readonly string FontFamily = "font-family";
		}
		public class text
		{
			public static readonly string StyleName = "style-name";
		}

		public class fo
		{
			public static readonly string KeepTogether = "keep-together";
			public static readonly string Orphans = "orphans";

			public static readonly string TextAlign = "text-align";
			public static readonly string TextIndent = "text-indent";

			public static readonly string MarginLeft = "margin-left";
			public static readonly string MarginTop = "margin-top";
			public static readonly string MarginBottom = "margin-bottom";
			public static readonly string MarginRight = "margin-right";

			public static readonly string PageWidth = "page-width";
			public static readonly string PageHeight = "page-height";

			public static readonly string FontSize = "font-size";
			public static readonly string FontWeight = "font-weight";
			public static readonly string Language = "language";
		}
	}
	//"text";
	//"table";
	//"draw";
	//"fo";
	//"xlink";
	//"dc";
	//"meta";
	//"number";            
	//"chart";
	//"dr3d";
	//"math";
	//"form";
	//"script";
}

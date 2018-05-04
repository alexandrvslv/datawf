using DataWF.Data;
using DataWF.Module.Flow;
using DataWF.Gui;
using DataWF.Common;
using Xwt;

namespace DataWF.Module.FlowGui
{
    public class DocumentFinder : ToolWindow
    {
        private DocumentListView list = new DocumentListView();

        public DocumentFinder()
        {
            list.AllowPreview = false;
            list.LabelText = "rezult";
            list.Name = "list";
            list.ShowPreview = false;

            Localizing();

            Target = list;
            Mode = ToolShowMode.Dialog;
            //Param = new DocumentSearch();
            //var items = FlowEnvir.Config.TemplateParam.View.GetItems();
            //items.Sort(new ComparerAccess(typeof(TemplateParam), "ToString", ListSortDirection.Ascending));
            //foreach (TemplateParam p in items)
            //    if (p.ParamType == ParamType.PColumn && p.Access.View && templateParams.Items[p.Code] == null)
            //    {
            //        var item = new TagMenuItem();
            //        item.Name = p.Code;
            //        item.Text = p.Name;
            //        item.Tag = p;
            //        templateParams.Items.Add(item);
            //    }
        }


        public void Localizing()
        {
            Title = Locale.Get("DocumentFinder", "Finder");
            list.Localize();
        }

        //public object Picture
        //{
        //    get { return Localize.GetImage("DocumentFinder", "Finder"); }
        //}

        public DocumentListView List
        {
            get { return list; }
        }

        public bool VisibleAccept
        {
            get { return toolAccept.Visible; }
            set { toolAccept.Visible = value; }
        }
    }
}

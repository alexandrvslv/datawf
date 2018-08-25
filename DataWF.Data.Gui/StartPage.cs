using System;
using DataWF.Common;
using DataWF.Gui;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Data.Gui
{
	public class StartPage : VPanel, IDockContent
	{
		private Label label;
		private GroupBox box;
		private LayoutList listProjects;
		private LayoutList listLinks;

		public StartPage()
		{
			listProjects = new LayoutList
			{
				EditState = EditListState.Edit,
				Name = "Projects",
				ListSource = GuiEnvironment.ProjectsInfo
			};
			listProjects.ListInfo.HotSelection = true;
			listProjects.CellMouseClick += ListRecentCellMouseClick;

			listLinks = new LayoutList
			{
				Mode = LayoutListMode.List,
				Name = "Links",
				ListSource = GuiEnvironment.WebLinks
			};
			listLinks.ListInfo.HotSelection = true;

			box = new GroupBox(
				new GroupBoxItem { Column = 0, Widget = listProjects, FillHeight = true, Name = "Projects", Width = 370, Height = 400 },
				new GroupBoxItem { Column = 1, Widget = listLinks, FillWidth = true, FillHeight = true, Name = "Links", Width = 370, Height = 400 })
			{ Name = "StartPage" };

			label = new Label
			{
				Font = Font.WithSize(22D),
				Name = "label",
				Text = "Data\\Document Workflow Solution"
			};

			PackStart(label, false, false);
			PackStart(box, true, true);
			Name = "StartPage";

			Localize();
		}

		#region IDockModule implementation
		public DockType DockType
		{
			get { return DockType.Content; }
		}

		public bool HideOnClose
		{
			get { return true; }
		}

        public void Activating()
        {
            throw new NotImplementedException();
        }

        public bool Closing()
        {
            throw new NotImplementedException();
        }

        #endregion

        public override void Localize()
		{
			base.Localize();
			label.Text = Locale.Get("StartPage", "Data\\Document Workflow Solution");
			GuiService.Localize(this, "StartPage", "Welcome", GlyphType.Home);
		}

		private void ListRecentCellMouseClick(object sender, LayoutHitTestEventArgs e)
		{
			GuiService.Main.CurrentProject = listProjects.SelectedItem as ProjectHandler;
		}

	}
}


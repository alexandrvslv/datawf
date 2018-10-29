using System.Threading.Tasks;
using DataWF.Common;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Module.Common;
using DataWF.Module.Messanger;

namespace DataWF.Module.MessangerGui
{

    public class Messanger : VPanel, IDockContent, ISync, IReadOnly
    {
        private DBItem staff;
        private MessageList list;
        private MessageEditor spliter;
        private MessageLayoutList plist;

        public Messanger()
        {
            plist = new MessageLayoutList()
            {
                EditMode = EditModes.ByClick,
                EditState = EditListState.ReadOnly,
                Name = "plist",
                Text = "Messages"
            };
            plist.CellDoubleClick += ListCellDoubleClick;


            spliter = new MessageEditor() { Name = "spliter", HeightRequest = 80 };

            Name = "MessangerDialog";
            PackStart(plist, true, true);
            PackStart(spliter, false, true);

            Localize();
        }

        public bool ReadOnly
        {
            get { return false; }
            set { }
        }

        public void Sync()
        {
            if (list != null)
                list.Load(DBLoadParam.Load | DBLoadParam.Synchronize);
        }

        public async Task SyncAsync()
        {
            await Task.Run(() => Sync()).ConfigureAwait(false);
        }

        public DockType DockType
        {
            get { return DockType.Right; }
        }

        public DBItem Staff
        {
            get { return staff; }
            set
            {
                if (staff == value)
                    return;
                staff = value;
                Text = staff.ToString();
                Name = "Messanger" + staff.PrimaryId;
                spliter.Staff = staff;
                spliter.toolUsers.Visible = false;

                list = new MessageList(staff as User, (User)GuiEnvironment.CurrentUser);
                plist.ListSource = list;
                //list.Table.LoadComplete += TableLoadComplete;
            }
        }

        public string MessageText
        {
            get { return spliter.MessageText; }
            set { spliter.MessageText = value; }
        }

        private void TableLoadComplete(object sender, DBLoadCompleteEventArgs e)
        {
            //GuiService.Context.Post((object p) => { plist.QueueDraw(true, true); }, null);
        }

        public bool HideOnClose
        {
            get { return false; }
        }

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, "Messager", "Messages", GlyphType.SignIn);
        }

        private void ListCellDoubleClick(object sender, LayoutHitTestEventArgs e)
        {
            var item = plist.SelectedItem as Message;
        }

        protected override void Dispose(bool disposing)
        {
            if (list != null)
                list.Dispose();
            base.Dispose(disposing);
        }

        public bool Closing()
        {
            return true;
        }

        public void Activating()
        {
        }
    }
}

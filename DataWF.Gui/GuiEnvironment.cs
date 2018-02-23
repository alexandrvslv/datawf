using DataWF.Common;
using System;
using System.IO;

namespace DataWF.Gui
{
    public class GuiEnvironment : IDisposable
    {
        private static GuiEnvironment instance = new GuiEnvironment();
        static GuiEnvironment()
        {
            Instance.Styles.GenerateDefault();
        }

        public static GuiEnvironment Instance
        {
            get { return instance; }
        }

        public static LayoutListInfoCache ListsInfo
        {
            get { return instance.Lists; }
        }

        public static LayoutFieldInfoCache FiledsInfo
        {
            get { return instance.Fields; }
        }

        public static ProjectHandleList ProjectsInfo
        {
            get { return instance.Projects; }
        }

        public static PListStyles StylesInfo
        {
            get { return instance.Styles; }
        }

        public static WebLinkList WebLinks
        {
            get { return instance.Links; }
        }

        public static void Save(string name = "gui.xml")
        {
            instance.SaveDirectory(Helper.GetDirectory(), name);
        }

        public void SaveDirectory(string path, string name = "gui.xml")
        {
            SaveFile(Path.Combine(path, name));
        }

        public void SaveFile(string file)
        {
            Serialization.Serialize(this, file);
        }

        public static void Load(string name = "gui.xml")
        {
            LocaleImage.ImageCache += (item) =>
            {
                using (var stream = new MemoryStream(item.Data))
                    return Xwt.Drawing.Image.FromStream(stream);
            };
            instance.LoadDirectory(Helper.GetDirectory(), name);
        }

        public void LoadDirectory(string path, string name = "gui.xml")
        {
            LoadFile(Path.Combine(path, name));
        }

        public void LoadFile(string file)
        {
            Serialization.Deserialize(file, this);
        }

        public LayoutFieldInfoCache Fields { get; set; } = new LayoutFieldInfoCache();
        public LayoutListInfoCache Lists { get; set; } = new LayoutListInfoCache();
        public ProjectHandleList Projects { get; set; } = new ProjectHandleList();
        public PListStyles Styles { get; set; } = new PListStyles();
        public WebLinkList Links { get; set; } = new WebLinkList();

        public void Dispose()
        {
            Styles.Dispose();
        }
    }


}


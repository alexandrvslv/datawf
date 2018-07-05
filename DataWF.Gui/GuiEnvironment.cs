using DataWF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace DataWF.Gui
{
    public partial class GuiEnvironment : IDisposable
    {
        private static GuiEnvironment instance = new GuiEnvironment();
        public static Dictionary<Type, Func<ILayoutCell, ILayoutCellEditor>> CellEditorFabric = new Dictionary<Type, Func<ILayoutCell, ILayoutCellEditor>>();

        static GuiEnvironment()
        {
            Instance.Themes.GenerateDefault();

            CellEditorFabric[typeof(string)] = cell =>
            {
                ILayoutCellEditor editor = null;
                if (cell.Name == nameof(object.ToString))
                {
                    editor = new CellEditorHeader();
                }
                else if (cell.Format == "Path")
                {
                    editor = new CellEditorPath();
                }
                else if (cell.Password)
                {
                    editor = new CellEditorPassword();
                }
                else
                {
                    editor = new CellEditorText { MultiLine = true };
                }
                return editor;
            };
            CellEditorFabric[typeof(System.Security.SecureString)] = cell =>
            {
                return new CellEditorPassword();
            };
            CellEditorFabric[typeof(byte[])] = cell =>
            {
                var editor = new CellEditorFile();
                string property = cell.Name;
                int index = property.LastIndexOf(".", StringComparison.Ordinal);
                if (index >= 0)
                    property = property.Substring(index);
                editor.PropertyFileName = property + "Name";
                return editor;
            };
            CellEditorFabric[typeof(bool)] = CellEditorFabric[typeof(bool?)] = cell =>
            {
                return new CellEditorCheck
                {
                    ValueTrue = true,
                    ValueFalse = false,
                    ValueNull = null,
                    TreeState = false
                };
            };
            CellEditorFabric[typeof(DateTime)] = cell =>
            {
                return new CellEditorDate() { Format = cell.Format };
            };
            CellEditorFabric[typeof(DateInterval)] = cell =>
            {
                return new CellEditorDate() { Format = cell.Format, TwoDate = true };
            };
            CellEditorFabric[typeof(Xwt.CheckBoxState)] = cell =>
            {
                return new CellEditorCheck
                {
                    ValueTrue = Xwt.CheckBoxState.On,
                    ValueFalse = Xwt.CheckBoxState.Off,
                    ValueNull = Xwt.CheckBoxState.Mixed,
                    TreeState = true
                };
            };
            CellEditorFabric[typeof(CheckedState)] = cell =>
            {
                return new CellEditorCheck
                {
                    ValueTrue = CheckedState.Checked,
                    ValueFalse = CheckedState.Unchecked,
                    ValueNull = CheckedState.Indeterminate,
                    TreeState = true
                };
            };
            CellEditorFabric[typeof(System.Net.IPAddress)] = cell =>
            {
                return new CellEditorNetTree();
            };
            CellEditorFabric[typeof(System.Globalization.CultureInfo)] = cell =>
            {
                return new CellEditorList { DataSource = Locale.Instance.Cultures };
            };
            CellEditorFabric[typeof(System.Text.EncodingInfo)] = cell =>
            {
                return new CellEditorList { DataSource = System.Text.Encoding.GetEncodings() };
            };
            CellEditorFabric[typeof(Xwt.Drawing.Image)] = cell =>
            {
                return new CellEditorImage();
            };
            CellEditorFabric[typeof(Xwt.Drawing.Color)] = cell =>
            {
                return new CellEditorColor();
            };
            CellEditorFabric[typeof(Xwt.Drawing.Font)] = cell =>
            {
                return new CellEditorFont();
            };
            CellEditorFabric[typeof(Enum)] = cell =>
            {
                return new CellEditorEnum();
            };
            CellEditorFabric[typeof(CellStyle)] = cell =>
            {
                return new CellEditorListEditor() { DataSource = GuiEnvironment.Theme };
            };

            LocaleImage.ImageCache += (item) =>
            {
                using (var stream = new MemoryStream(item.Data))
                    return Xwt.Drawing.Image.FromStream(stream);
            };

            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                OnAssemblyLoad(null, new AssemblyLoadEventArgs(assembly));
            }
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs e)
        {
            if (e.LoadedAssembly.GetCustomAttributes<AssemblyMetadataAttribute>().Any(m => m.Key == "gui"))// || m.Key == "module"
            {
                foreach (var item in e.LoadedAssembly?.GetExportedTypes())
                {
                    if (TypeHelper.IsInterface(item, typeof(IModuleInitialize)))
                    {
                        try
                        {
                            var imodule = (IModuleInitialize)EmitInvoker.CreateObject(item);
                            imodule.Initialize();
                        }
                        catch (Exception ex)
                        {
                            Helper.OnException(ex);
                        }
                    }
                }
            }
        }

        public static ILayoutCellEditor GetCellEditor(ILayoutCell cell)
        {
            var type = cell?.DataType;
            if (type == null)
                return null;
            if (TypeHelper.IsNullable(type))
                type = type.GetGenericArguments().First();
            ILayoutCellEditor editor = null;
            if (CellEditorFabric.TryGetValue(type, out var generator))
            {
                editor = generator(cell);
            }
            if (editor == null)
            {
                foreach (var entry in CellEditorFabric)
                {
                    if (TypeHelper.IsBaseType(type, entry.Key))
                    {
                        editor = entry.Value(cell);
                    }
                }
            }

            //if (type.IsEnum)
            //{
            //    editor = new CellEditorEnum();
            //}
            if (editor == null)
            {
                if (TypeHelper.IsList(type))
                {
                    editor = new CellEditorFields() { Header = type.Name };
                }
                else if (GuiService.IsCompound(type))
                {
                    editor = new CellEditorFields() { Header = type.Name };
                }
                else
                {
                    editor = new CellEditorText()
                    {
                        Format = cell.Format,
                        MultiLine = false,
                        DropDownWindow = false
                    };
                }
            }
            editor.DataType = type;
            return editor;
        }

        public static GuiEnvironment Instance
        {
            get { return instance; }
        }

        public static ProjectHandleList ProjectsInfo
        {
            get { return instance.Projects; }
        }

        public static GuiTheme Theme
        {
            get { return instance.CurrentTheme; }
        }

        public static WebLinkList WebLinks
        {
            get { return instance.Links; }
        }

        public static void Save(string name = "gui.xml")
        {
            instance.SaveDirectory(Helper.GetDirectory(), name);
        }

        private GuiTheme theme;
        private string themeName = "Light";

        public ProjectHandleList Projects { get; set; } = new ProjectHandleList();

        public WebLinkList Links { get; set; } = new WebLinkList();

        public GuiThemeList Themes { get; set; } = new GuiThemeList();

        public string ThemeName
        {
            get { return themeName; }
            set
            {
                if (value != themeName)
                {
                    themeName = value;
                    theme = null;
                }
            }
        }

        [XmlIgnore]
        public GuiTheme CurrentTheme
        {
            get { return theme ?? (theme = Themes[ThemeName]); }
            set
            {
                if (CurrentTheme != value)
                {
                    theme = value;
                    ThemeName = value?.Name;
                }
            }
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
            instance.LoadDirectory(Helper.GetDirectory(), name);
            Helper.LogWorkingSet("UI Info");
        }

        public void LoadDirectory(string path, string name = "gui.xml")
        {
            LoadFile(Path.Combine(path, name));
        }

        public void LoadFile(string file)
        {
            Serialization.Deserialize(file, this, false);
        }

        public void Dispose()
        {
            CurrentTheme.Dispose();
        }
    }


}


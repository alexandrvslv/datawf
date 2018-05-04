using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using Xwt;
using System.Linq;
using Mono.Cecil;

namespace DataWF.Data.Gui
{

	public class Main : MainWindow
	{
		public Main()
		{            
			CheckAssemblies();
		}

		protected override void FieldsEditorLogClick(object sender, ListEditorEventArgs e)
		{

		}

		protected override void FieldsEditorStatusClick(object sender, ListEditorEventArgs e)
		{

		}

		public StartPage StartPage
		{
			get { return (StartPage)GetControl(typeof(StartPage).Name); }
		}

  		private void CheckAssemblies()
		{
			CheckAssembly(Assembly.GetEntryAssembly());
			var list = new List<Assembly>();
			string[] asseblies = Directory.GetFiles(Helper.GetDirectory(), "*.dll");
			foreach (string dll in asseblies)
			{
				AssemblyDefinition assemblyDefinition = null;
				try { assemblyDefinition = AssemblyDefinition.ReadAssembly(dll); }
				catch { continue; }
				var moduleAttribute = assemblyDefinition.CustomAttributes
														.Where(item => item.AttributeType.Name == nameof(AssemblyMetadataAttribute))
														.Select(item => item.ConstructorArguments.Select(sitem => sitem.Value.ToString()).ToArray());
				if (moduleAttribute.Any(item => item[0] == "gui"))
				{
					list.Add(Assembly.LoadFile(dll));
				}
			}

			foreach (var assembly in list)
			{
				try
				{
					CheckAssembly(assembly);
				}
				catch (Exception ex)
				{
					Helper.OnException(ex);
					continue;
				}
			}
		}

		private void CheckAssembly(Assembly assembly)
		{
			var hasModule = false;
			Helper.Logs.Add(new StateInfo("Main Form", "Assembly Loadind", assembly.FullName));
			foreach (Type type in assembly.GetExportedTypes())
			{
				if (TypeHelper.IsInterface(type, typeof(IDockContent)))
				{
					Helper.Logs.Add(new StateInfo("Main Form", "Module Initialize", Locale.Get(type)));
					try
					{
						foreach (var attribute in type.GetCustomAttributes<ModuleAttribute>(false))
						{
							if (attribute.IsModule)
							{
								AddModuleWidget((IDockContent)EmitInvoker.CreateObject(type, true));
								hasModule = true;
							}
						}
					}
					catch (Exception ex)
					{
						Helper.Logs.Add(new StateInfo("Main Form", ex.Message, ex.StackTrace, StatusType.Error));
					}
				}
				if (TypeHelper.IsInterface(type, typeof(IProjectEditor)))
				{
					try
					{
						foreach (ProjectAttribute attr in type.GetCustomAttributes<ProjectAttribute>(false))
						{
							ProjectType ptype = new ProjectType(type, attr);
							menuProjectCreate.DropDown.Items.Add(BuildButton(ptype));
							editors.Add(ptype);
						}
					}
					catch (Exception ex)
					{
						Helper.Logs.Add(new StateInfo("Main Form", ex.Message, ex.StackTrace, StatusType.Error));
					}
				}
			}
			if (hasModule)
			{
				menuView.DropDown.Items.Add(new ToolSeparator());
			}
		}

		private void AddModuleWidget(IDockContent module)
		{
			if (GetControl(module.GetType().Name) != null)
			{
				return;
			}
			menuView.DropDown.Items.Add(BuildMenuItem((Widget)module));
		}      
  
	}

	public class ToolWidgetHandler : ToolItem
	{
		public ToolWidgetHandler(EventHandler click) : base(click)
		{
			DisplayStyle = ToolItemDisplayStyle.ImageAndText;
			indent = 0;
		}

		public Widget Widget { get; set; }

		public override void Localize()
		{ }
	}

}

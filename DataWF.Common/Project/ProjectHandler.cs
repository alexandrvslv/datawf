using System;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;

namespace DataWF.Common
{
	public class ProjectHandleList : SelectableList<ProjectHandler>
	{
		public ProjectHandleList()
		{
			Indexes.Add(new Invoker<ProjectHandler, string>(nameof(ProjectHandler.FileName),
																  (item) => item.FileName));
		}
	}

	public class ProjectHandler
	{
		private IProjectEditor editor;
		private object project;
		private bool synch;
		private PropertyChangedEventHandler handler;
		//serialisable
		private ProjectType type;
		private string fileName;
		private DateTime stamp = DateTime.Now;

		public ProjectHandler()
		{
			handler = new PropertyChangedEventHandler(OnProjectChanged);
		}

		public override string ToString()
		{
			return Name;
		}

		[XmlIgnore]
		public bool Synch
		{
			get { return synch; }
			set
			{
				synch = value;
			}
		}

		[Browsable(false)]
		public ProjectType Type
		{
			get { return type; }
			set
			{
				if (type == value)
					return;
				type = value;
				if (FileName == null)
					FileName = type.Name + type.Filter;
				if (Project == null || Project.GetType() != type.Project)
					Project = EmitInvoker.CreateObject(type.Project, true);
				if (Editor == null || Editor.GetType() != type.Editor)
					Editor = EmitInvoker.CreateObject(type.Editor, true) as IProjectEditor;

			}
		}

		public string TypeName
		{
			get { return type == null ? null : type.Name; }
		}


		public object Project
		{
			get { return project; }
			set
			{
				if (project == value)
					return;
				if (project is INotifyPropertyChanged)
					((INotifyPropertyChanged)project).PropertyChanged -= handler;
				project = value;
				if (project is INotifyPropertyChanged)
					((INotifyPropertyChanged)project).PropertyChanged += handler;
			}
		}

		public string Name
		{
			get
			{
				if (fileName == null)
					fileName = "new.prj";
				return Path.GetFileNameWithoutExtension(fileName);
			}
		}

		public string FileName
		{
			get { return fileName; }
			set
			{
				if (fileName == value)
					return;
				fileName = value;
			}
		}

		[XmlIgnore]
		public IProjectEditor Editor
		{
			get { return editor; }
			set
			{
				if (editor == value)
					return;
				editor = value;
				editor.Project = this;
			}
		}

		public DateTime Stamp
		{
			get { return stamp; }
			set
			{
				if (stamp == value)
					return;
				stamp = value;
			}
		}

		public void Load()
		{
			if (File.Exists(FileName))
			{
				if (Project is IFileSerialize)
					((IFileSerialize)Project).Load(FileName);
				else
					Project = Serialization.Deserialize(FileName, Project);
			}
			if (Editor == null && type != null)
				Editor = EmitInvoker.CreateObject(type.Editor, true) as IProjectEditor;
			synch = true;
		}

		public void Save()
		{
			if (Project is IFileSerialize)
				((IFileSerialize)Project).Save(FileName);
			else if (Project != null)
				Serialization.Serialize(Project, FileName);

			synch = true;
		}

		public void SaveFile(string file)
		{
			fileName = file;
			Save();
		}

		public void SaveDirectory(string path)
		{
			fileName = Path.Combine(path, Name + Path.GetExtension(fileName));
			Save();
		}

		public void LoadFile(string file)
		{
			fileName = file;
			Load();
		}

		public void LoadDirectory(string path)
		{
			fileName = Path.Combine(path, Name + Path.GetExtension(fileName));
			Load();
		}

		public void OnProjectChanged(object sender, PropertyChangedEventArgs args)
		{
			Synch = false;
		}
	}
}


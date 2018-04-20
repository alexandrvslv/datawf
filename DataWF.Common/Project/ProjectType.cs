using System;

namespace DataWF.Common
{
    public class ProjectType
    {
        private Type editor;
        private Type project;
        private string filter;

        public ProjectType()
        { }

        public ProjectType(Type typeEditor, Type typeProject, string filter)
        {
            this.editor = typeEditor;
            this.project = typeProject;
            this.filter = filter;
        }

        public ProjectType(Type typeEditor, ProjectAttribute attribute)
            : this(typeEditor, attribute.Type, attribute.FileFilter)
        {
        }

        public Type Editor { get { return editor; } }
        public Type Project { get { return project; } }
        public string Filter { get { return filter; } }

        public string Name
        {
            get { return Locale.Get(project); }
        }
    }
}

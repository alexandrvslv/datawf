using System;

namespace DataWF.Common
{
    public class ProjectType
    {
        public ProjectType()
        { }

        public ProjectType(Type typeEditor, Type typeProject, string filter)
        {
            Editor = typeEditor;
            Project = typeProject;
            Filter = filter;
        }

        public ProjectType(Type typeEditor, ProjectAttribute attribute)
            : this(typeEditor, attribute.Type, attribute.FileFilter)
        {
        }

        public Type Editor { get; }
        public Type Project { get; }
        public string Filter { get; }

        public string Name
        {
            get { return Locale.Get(Project); }
        }
    }
}

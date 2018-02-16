using System;

namespace DataWF.Gui
{

    public class LayoutValueChangedEventArgs : LayoutValueEventArgs
    {
        private ILayoutEditor editor;
        public LayoutValueChangedEventArgs(ILayoutEditor editor)
        {
            this.editor = editor;
        }

        public ILayoutEditor Editor
        {
            get { return editor; }
            set { editor = value; }
        }
    }
}


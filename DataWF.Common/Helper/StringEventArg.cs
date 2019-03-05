using System;

namespace DataWF.Common
{
    public class StringEventArgs : EventArgs
    {
        public StringEventArgs()
        { }

        public StringEventArgs(string str)
        {
            String = str;
        }

        public string String { get; set; }
    }
}

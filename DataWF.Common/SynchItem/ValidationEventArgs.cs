using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Common
{
    public class ValidationEventArgs : EventArgs
    {
        public ValidationEventArgs(object item)
        {
            Item = item;
            Fail = false;
        }

        public ValidationEventArgs(object item, string member, List<string> messages)
        {
            Item = item;
            Member = member;
            Messages = messages;
            Fail = true;
        }

        public object Item { get; }
        public string Member { get; }
        public List<string> Messages { get; }
        public bool Fail { get; }
        public bool Handled { get; set; }
    }

}

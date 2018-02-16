using System;

namespace DataWF.Common
{
    public class SerializationNotifyEventArgs : EventArgs
    {
        private SerializeType type;
        private string fileName;
        private object obj;

        public SerializationNotifyEventArgs(object obj, SerializeType type, string fileName)
        {
            this.obj = obj;
            this.type = type;
            this.fileName = fileName;
        }

        public object Obj
        {
            get { return obj; }
            set { obj = value; }
        }

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        public SerializeType Type
        {
            get { return type; }
            set { type = value; }
        }
    }

}

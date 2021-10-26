using System;
using System.ComponentModel;
using DataWF.Common;
using DataWF.Data;
using System.IO;

namespace DataWF.Module.Flow
{
    [Flags()]
    public enum DocInitType
    {
        Default = 0,
        References = 1,
        Data = 4,
        Workflow = 8,
        Customer = 16,
        Comment = 32,
        Refing = 64,
        Refed = 128
        //Logs,
    }

    public delegate void DocumentDataLoadDelegate(object sender, DocumentEventArgs args);

    public delegate void DocumentInitializeDelegate(object sender, DocumentInitializeEventArgs args);

    public delegate void DocumentSaveDelegate(object sender, DocumentEventArgs args);

    public delegate void DocumentCreateDelegate(object sender, DocumentCreateEventArgs args);

    public delegate void DocumentDeleteDelegate(object sender, DocumentEventArgs args);

    public class DocumentEventArgs : EventArgs
    {
        protected Document document;

        public Document Document
        {
            get => document;
            set => document = value;
        }

        public DocumentEventArgs()
        { }

        public DocumentEventArgs(Document document)
        {
            this.document = document;
        }
    }

    public class DocumentCreateEventArgs : DocumentEventArgs
    {
        protected Template template;
        protected Document parent;

        public DocumentCreateEventArgs()
        { }

        public DocumentCreateEventArgs(Document document, Template template, Document parentDocument)
            : base(document)
        {
            this.template = template;
            this.parent = parentDocument;
        }

        public Template Template
        {
            get => template;
            set => template = value;
        }

        public Document Parent
        {
            get => parent;
            set => parent = value;
        }
    }

    public class DocumentInitializeEventArgs : DocumentEventArgs
    {
        private DocInitType type;

        public DocumentInitializeEventArgs()
        { }

        public DocumentInitializeEventArgs(Document document, DocInitType type)
            : base(document)
        {
            this.type = type;
        }

        public DocInitType Type
        {
            get => type;
            set => type = value;
        }
    }
}

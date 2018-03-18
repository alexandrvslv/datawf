/*
 DocumentEvents.cs
 
 Author:
      Alexandr <$alexandr_vslv@mail.ru>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
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
        Refed = 1,
        Refing = 2,
        Data = 4,
        Workflow = 8,
        Customer = 16
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
            get { return document; }
            set { document = value; }
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
            get { return template; }
            set { template = value; }
        }

        public Document Parent
        {
            get { return parent; }
            set { parent = value; }
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
            get { return type; }
            set { type = value; }
        }
    }
}

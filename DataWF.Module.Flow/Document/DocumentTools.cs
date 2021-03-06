﻿using DataWF.Data;
using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DataWF.Module.Common;

namespace DataWF.Module.Flow
{
    public interface IDocuments
    {
        IEnumerable Documents { get; set; }
    }

    public class ExecuteDocumentArg : EventArgs
    {
        Document document;
        DBProcedure precedure;
        object result;
        object tag;

        public ExecuteDocumentArg(Document document, DBProcedure precedure, object result, object tag)
        {
            this.precedure = precedure;
            this.result = result;
            this.document = document;
            this.tag = tag;
        }

        public DBProcedure Procedure
        {
            get => precedure;
            set => precedure = value;
        }

        public object Result
        {
            get => result;
            set => result = value;
        }

        public object Tag
        {
            get => tag;
            set => tag = value;
        }

        public Document Document
        {
            get => document;
            set => document = value;
        }
    }

    public delegate void ExecuteDocumentCallback(ExecuteDocumentArg arg);

}

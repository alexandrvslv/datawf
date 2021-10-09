//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
using DataWF.Common;
using System;
using System.Collections.Generic;

namespace DataWF.Data
{
    public interface IDBSchema : IDBSchemaItem, IModelSchema
    {
        new DBTableList Tables { get; set; }
        new DBProvider Provider { get; }
        
        Version Version { get; set; }
        DBConnection Connection { get; set; }
        DBSystem System { get; }
        DBTableGroupList TableGroups { get; set; }
        DBSequenceList Sequences { get; set; }
        DBProcedureList Procedures { get; set; }
        DBSchemaList Schems { get; }

        IDBSchemaLog LogSchema { get; set; }
        bool IsSynchronizing { get; }

        void ExecuteCreateDatabase();
        void ExecuteCreateSchema();
        void ExecuteDropDatabase();

        DBTable GetTable(Type type, bool generate = false);
        DBTable<T> GetTable<T>(bool generate = false) where T : DBItem;
        IDBTable GetVirtualTable<T>(int itemType) where T : DBItem;

        DBTable ParseTable(string code);
        DBColumn ParseColumn(string code);
        DBProcedure ParseProcedure(string code, string category = "General");

        IEnumerable<DBForeignKey> GetAllForeignConstraints(DBTable target);
    }
}
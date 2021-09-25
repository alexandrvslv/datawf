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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using DataWF.Common;

namespace DataWF.Data
{
    public interface IDBTable : IDBSchemaItem, ICollection, IDisposable
    {
        DBItem this[int index] { get; }
        DBColumn PrimaryKey { get; }
        DBColumn<byte[]> AccessKey { get; }
        DBColumn<string> CodeKey { get; }
        DBColumn<DateTime> DateCreateKey { get; }
        DBColumn ElementTypeKey { get; }
        DBColumn<byte[]> FileKey { get; }
        DBColumn<long?> FileOIDKey { get; }
        DBColumn<string> FileNameKey { get; }
        DBColumn<DateTime?> FileLastWriteKey { get; }
        DBColumn GroupKey { get; }
        DBColumn<byte[]> ImageKey { get; }
        DBColumn<int> ItemTypeKey { get; }
        DBColumn<DateTime> StampKey { get; }
        DBColumn<DateTime?> ReplicateStampKey { get; }
        DBColumn<DBStatus> StatusKey { get; }
        DBSystem System { get; }
        IComparer DefaultComparer { get; set; }
        int BlockSize { get; set; }
        List<DBForeignKey> ChildRelations { get; }
        DBColumnGroupList ColumnGroups { get; set; }
        DBColumnList<DBColumn> Columns { get; set; }
        DBConstraintList<DBConstraint> Constraints { get; set; }
        DBForeignList Foreigns { get; set; }
        DBIndexList Indexes { get; set; }
        DBConnection Connection { get; }
        IDBTableView DefaultItemsView { get; }
        DBTableGroup Group { get; set; }
        string GroupName { get; set; }
        bool IsCaching { get; set; }
        bool IsEdited { get; }
        bool IsLoging { get; set; }
        bool IsVirtual { get; }
        IDBTable ParentTable { get; }
        DBItemType ItemType { get; }
        int ItemTypeIndex { get; set; }
        Dictionary<int, DBItemType> ItemTypes { get; set; }
        IDBTableLog LogTable { get; set; }
        string LogTableName { get; set; }
        string SubQuery { get; set; }
        DBSequence Sequence { get; set; }
        string SequenceName { get; set; }
        string SqlName { get; }
        TableGenerator Generator { get; }
        DBTableType Type { get; set; }
        IQQuery FilterQuery { get; set; }
        //bool IsSynchronized { get; }
        string ItemTypeName { get; }

        event EventHandler<DBLoadColumnsEventArgs> LoadColumns;
        event EventHandler<DBLoadCompleteEventArgs> LoadComplete;
        event EventHandler<DBLoadProgressEventArgs> LoadProgress;
        event EventHandler<DBItemEventArgs> RowUpdated;
        event EventHandler<DBItemEventArgs> RowUpdating;

        void Accept(DBItem item);
        void Add(DBItem item);
        void AcceptAll(IUserIdentity user);
        void RejectAll(IUserIdentity user);
        void DeleteById(object id);
        bool Remove(DBItem item);
        void Reload(object id, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null);
        T NewItem<T>(DBUpdateState state = DBUpdateState.Insert, bool def = true) where T : DBItem;
        DBItem NewItem(DBUpdateState state = DBUpdateState.Insert, bool def = true);
        DBItem NewItem(DBUpdateState state, bool def, int typeIndex);
        IEnumerable<DBItem> GetChangedItems();
        int GetCount(DBTransaction transaction, string where);
        Task Save(IEnumerable<DBItem> rows = null);
        Task Save(DBTransaction transaction, IEnumerable<DBItem> rows = null);
        Task<bool> Save(DBItem item, DBTransaction transaction);

        void AddVirtualTable(IDBTable view);
        IDBTable GetVirtualTable(Type type);
        DBTable<T> GetVirtualTable<T>() where T : DBItem;
        IEnumerable<IDBTable> GetVirtualTables();
        void RemoveVirtual(IDBTable view);

        string BuildQuery(string whereFilter, string alias, DBLoadParam loadParam = DBLoadParam.None, string function = null);

        void AddView(IDBTableView view);
        void RemoveView(IDBTableView view);
        IDBTableView CreateView(string query = "", DBViewKeys mode = DBViewKeys.None, DBStatus filter = DBStatus.Empty);

        DBColumnGroup GetOrCreateColumnGroup(string code);
        DBColumn GetOrCreateColumn(string name, Type type, ref bool newCol);
        DBColumn GetOrCreateColumn(string code, Type type);
        IEnumerable<DBColumn> GetTypeColumns<T>();
        IEnumerable<DBColumn> GetTypeColumns(Type t);
        IEnumerable<DBColumn> GetReferenceColumns();
        DBColumn<string> GetCultureColumn(string group, CultureInfo culture);
        DBColumn GetColumn(string name);
        DBColumn<T> GetColumn<T>(string name);
        DBColumn<T> GetColumn<T>(string property, ref DBColumn<T> cache);
        DBColumn GetColumnOrProperty(string nameOrProperty);
        DBColumn GetColumnByProperty(string property);
        DBColumn GetColumnByProperty(string property, ref DBColumn cache);
        DBColumn<T> GetColumnByProperty<T>(string property);
        DBColumn<T> GetColumnByProperty<T>(string property, ref DBColumn<T> cache);
        IEnumerable<DBColumn> GetQueryColumns(DBLoadParam param = DBLoadParam.None);
        IEnumerable<DBColumn> GetColumns(ICollection<string> columns);
        DBColumn<string> GetCultureColumn(string group);
        DBReferencing GetReferencing(string property);
        IEnumerable<DBReferencing> GetReferencingByProperty(Type type);
        IEnumerable<DBReferencing> GetAllReferencing(Type valueType);

        DBSequence GenerateSequence(string sequenceName = null);
        long GenerateId(DBTransaction transaction);
        void RefreshSequence(bool truncate = false);
        void RefreshSequence(DBTransaction transaction, bool truncate = false);

        void Generate(DBTableInfo tableInfo);
        IDBTableLog GenerateLogTable();

        IEnumerable<DBForeignKey> GetChildRelations();
        IEnumerable<IDBTable> GetChildTables();
        IEnumerable<IDBTable> GetParentTables();
        DBItemType GetItemType(int typeIndex);
        int GetTypeIndex(Type type);


        T LoadByCode<T>(string code, DBColumn<string> column, DBLoadParam param, DBTransaction transaction = null) where T : DBItem;
        T LoadById<T>(object id, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null) where T : DBItem;
        List<T> LoadById<T>(List<string> ids, DBTransaction transaction) where T : DBItem;
        T LoadById<T, K>(K? id, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null) where K : struct where T : DBItem;
        IEnumerable<T> LoadByKey<T>(object key, DBColumn column, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null) where T : DBItem;
        IEnumerable<T> LoadByKey<T, K>(K key, DBColumn<K> column, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null) where T : DBItem;
        IEnumerable<T> Load<T>(string whereText, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null) where T : DBItem;
        IEnumerable<T> Load<T>(IDbCommand command, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null) where T : DBItem;
        IEnumerable<T> Load<T>(IQQuery query, DBTransaction transaction = null) where T : DBItem;
        IEnumerable<T> Load<T>(IQQuery<T> query, DBTransaction transaction = null) where T : DBItem;

        IEnumerable<T> Select<T>(IQQuery qQuery) where T : DBItem;
        IEnumerable<T> Select<T>(IQQuery<T> qQuery) where T : DBItem;

        IQQuery<T> Query<T>(DBLoadParam loadParam = DBLoadParam.None) where T : DBItem;
        IQQuery<T> Query<T>(string filter, DBLoadParam loadParam = DBLoadParam.None) where T : DBItem;
        IQQuery<T> Query<T>(IQQuery baseQuery) where T : DBItem;
        void Trunc();

        void LoadFile();
        void LoadFile(string fileName);
        void SaveFile();
        void SaveFile(string fileName);
    }
}
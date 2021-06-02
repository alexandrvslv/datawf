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
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using DataWF.Common;

namespace DataWF.Data
{
    public interface IDBTable : IDBSchemaItem, ICollection<DBItem>, IDisposable
    {
        DBItem this[int index] { get; }
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
        DBColumn PrimaryKey { get; }
        DBColumn<DateTime> StampKey { get; }
        DBColumn<DateTime?> ReplicateStampKey { get; }
        DBColumn<DBStatus> StatusKey { get; }
        DBSystem System { get; }
        int BlockSize { get; set; }
        List<DBForeignKey> ChildRelations { get; }
        DBColumnGroupList ColumnGroups { get; set; }
        DBColumnList<DBColumn> Columns { get; set; }
        DBConstraintList<DBConstraint> Constraints { get; set; }
        DBForeignList Foreigns { get; set; }
        DBIndexList Indexes { get; set; }
        string ComDelete { get; set; }
        string ComInsert { get; set; }
        string ComUpdate { get; set; }
        DBConnection Connection { get; }
        IDBTableView DefaultItemsView { get; }
        DBTableGroup Group { get; set; }
        string GroupName { get; set; }
        bool IsCaching { get; set; }
        bool IsEdited { get; }
        bool IsLoging { get; set; }
        bool IsVirtual { get; }
        DBItemType ItemType { get; }
        int ItemTypeIndex { get; set; }
        Dictionary<int, DBItemType> ItemTypes { get; set; }
        IDBLogTable LogTable { get; set; }

        IEnumerable<DBReferencing> GetReferencing(Type valueType);

        string LogTableName { get; set; }
        string Query { get; set; }
        DBSequence Sequence { get; set; }
        string SequenceName { get; set; }
        string SqlName { get; }
        TableGenerator Generator { get; }
        DBTableType Type { get; set; }

        event EventHandler<DBLoadColumnsEventArgs> LoadColumns;
        event EventHandler<DBLoadCompleteEventArgs> LoadComplete;
        event EventHandler<DBLoadProgressEventArgs> LoadProgress;
        event EventHandler<DBItemEventArgs> RowUpdated;
        event EventHandler<DBItemEventArgs> RowUpdating;

        void Accept(DBItem item);
        void AcceptChanges(IUserIdentity user);
        void AddView(IDBTableView view);
        void AddVirtual(DBTable view);
        string BuildQuery(string whereFilter, string alias, IEnumerable<DBColumn> cols, string function = null);
        DBColumn CheckColumn(string name, Type type, ref bool newCol);
        void CheckColumns(DBTransaction transaction);

        IEnumerable<DBColumn> GetTypeColumns<T>();
        IEnumerable<DBColumn> GetTypeColumns(Type t);
        IEnumerable<DBColumn> GetRefColumns();
        IEnumerable<DBReferencing> GetPropertyReferencing(Type type);
        IDbCommand CreatePrimaryKeyCommmand(object id, IEnumerable<DBColumn> cols = null);
        IDBTableView CreateItemsView(string query = "", DBViewKeys mode = DBViewKeys.None, DBStatus filter = DBStatus.Empty);
        string CreateQuery(string whereText, string alias, IEnumerable<DBColumn> cols = null);
        void DeleteById(object id);
        string FormatQColumn(DBColumn column, string tableAlias);
        string FormatQTable(string alias);
        void Generate(DBTableInfo tableInfo);
        void GenerateDefaultColumns();
        IDBLogTable GenerateLogTable();
        DBSequence GenerateSequence(string sequenceName = null);
        long GenerateId(DBTransaction transaction);
        DBTable GetVirtualTable(Type type);
        void GetAllChildTables(List<DBTable> parents);
        void GetAllParentTables(List<DBTable> parents);
        IEnumerable<DBItem> GetChangedItems();
        IEnumerable<DBForeignKey> GetChildRelations();
        IEnumerable<DBTable> GetVirtualTables();
        IEnumerable<DBTable> GetChildTables();
        DBColumn<string> GetCultureColumn(string group, CultureInfo culture);
        DBItemType GetItemType(int typeIndex);
        DBColumn<string> GetNameKey(string group);
        IEnumerable<DBTable> GetParentTables();
        int GetRowCount(DBTransaction transaction, string where);
        string GetRowText(object id);
        string GetRowText(object id, bool allColumns, bool showColumn, string separator);
        string GetRowText(object id, IEnumerable<DBColumn> parameters);
        string GetRowText(object id, IEnumerable<DBColumn> parametrs, bool showColumn, string separator);
        QEnum GetStatusEnum(DBStatus status);
        QParam GetStatusParam(DBStatus status);
        int GetTypeIndex(Type type);
        QParam GetTypeParam(Type type);
        DBColumn InitColumn(Type type, string code);
        DBColumnGroup InitColumnGroup(string code);
        void LoadFile();
        void LoadFile(string fileName);
        DBItem LoadItemByCode(string code, DBColumn<string> column, DBLoadParam param, DBTransaction transaction = null);
        DBItem LoadItemById(object id, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null);
        List<DBItem> LoadItemsById(List<string> ids, DBTransaction transaction);
        DBItem LoadItemById<K>(K? id, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null) where K : struct;
        DBItem LoadItemByKey(object key, DBColumn column, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null);
        DBItem LoadItemByKey<K>(K key, DBColumn<K> column, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null);
        DBItem LoadItemFromReader(DBTransaction transaction);
        IEnumerable<DBItem> LoadItems(string whereText = null, DBLoadParam param = DBLoadParam.None, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null);
        IEnumerable<DBItem> LoadItems(IDbCommand command, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null);
        IEnumerable<DBItem> LoadItems(QQuery query, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null);
        IEnumerable<DBItem> LoadItemsCache(string filter, DBLoadParam loadParam = DBLoadParam.Referencing, DBTransaction transaction = null);
        void LoadReferenceBlock(IDbCommand command, DBTransaction transaction);
        void LoadReferencingBlock(IDbCommand command, DBTransaction transaction);
        T NewItem<T>(DBUpdateState state = DBUpdateState.Insert, bool def = true) where T : DBItem;
        DBItem NewItem(DBUpdateState state = DBUpdateState.Insert, bool def = true);
        DBItem NewItem(DBUpdateState state, bool def, int typeIndex);
        int NextHash();
        void OnItemChanged<V>(DBItem item, string proeprty, DBColumn<V> column, V value);
        void OnItemChanging<V>(DBItem item, string proeprty, DBColumn<V> column, V value);
        void OnUpdated(DBItemEventArgs e);
        bool OnUpdating(DBItemEventArgs e);
        bool IsSerializeableColumn(DBColumn column, Type type);
        DBColumn ParseColumn(string name);
        DBColumn ParseColumnProperty(string property);
        IEnumerable<DBColumn> ParseColumns(ICollection<string> columns);
        DBColumn ParseProperty(string property);
        DBColumn ParseProperty(string property, ref DBColumn cache);
        void RefreshSequence(bool truncate = false);
        void RefreshSequence(DBTransaction transaction, bool truncate = false);
        void RejectChanges(IUserIdentity user);
        void ReloadItem(object id, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null);
        void RemoveView(IDBTableView view);
        void RemoveVirtual(DBTable view);
        Task Save(IEnumerable<DBItem> rows = null);
        Task Save(DBTransaction transaction, IEnumerable<DBItem> rows = null);
        void SaveFile();
        void SaveFile(string fileName);
        Task<bool> SaveItem(DBItem item, DBTransaction transaction);
        IEnumerable<DBItem> SelectItems(DBColumn column, CompareType comparer, object val);
        IEnumerable<DBItem> SelectItems(QQuery qQuery);
        IEnumerable<DBItem> SelectItems(string qQuery);
        IEnumerable SelectValues(DBItem item, QQuery query, CompareType compare);

        bool ParseQuery(string filter, out QQuery query);
        void Trunc();
    }
}
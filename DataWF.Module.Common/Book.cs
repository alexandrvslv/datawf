using DataWF.Common;
using DataWF.Data;
using System.Runtime.Serialization;

namespace DataWF.Module.Common
{
    [Table("rbook", "Reference Book", BlockSize = 100, Type = typeof(BookTable<>)), InvokerGenerator]
    public partial class Book : DBGroupItem
    {
        public Book(DBTable table) : base(table)
        { }

        public BookTable<Book> BookTable => (BookTable<Book>)Table;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue<int>(BookTable.IdKey);
            set => SetValue(value, BookTable.IdKey);
        }

        [Column("code", 512, Keys = DBColumnKeys.Code)]
        public string Code
        {
            get => GetValue<string>(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
        }

        [Column("group_id", Keys = DBColumnKeys.Group)]
        public int? ParentId
        {
            get => GetGroupValue<int?>();
            set => SetGroupValue(value);
        }

        [Reference(nameof(ParentId))]
        public Book Parent
        {
            get => GetGroupReference<Book>();
            set => SetGroupReference(value);
        }

        [Column("name", 1024, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }

        [CultureKey(nameof(Name))]
        public string NameEN
        {
            get => GetValue<string>(BookTable.NameENKey);
            set => SetValue(value, BookTable.NameENKey);
        }

        [CultureKey(nameof(Name))]
        public string NameRU
        {
            get => GetValue<string>(BookTable.NameRUKey);
            set => SetValue(value, BookTable.NameRUKey);
        }

        [Column("book_value")]
        public string Value
        {
            get => GetValue<string>(BookTable.ValueKey);
            set => SetValue(value, BookTable.ValueKey);
        }

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue<int?>(BookTable.ExternalIdKey);
            set => SetValue(value, BookTable.ExternalIdKey);
        }
    }
}

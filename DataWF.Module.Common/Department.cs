using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Counterpart;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Common
{

    [Table("rdepartment", "User", BlockSize = 100)]
    public class Department : DBGroupItem, IComparable, IDisposable
    {
        public static readonly DBTable<Department> DBTable = GetTable<Department>();
        public static readonly DBColumn NameENKey = DBTable.ParseProperty(nameof(NameEN));
        public static readonly DBColumn NameRUKey = DBTable.ParseProperty(nameof(NameRU));
        public static readonly DBColumn CompanyKey = DBTable.ParseProperty(nameof(Company));
        public static readonly DBColumn ExternalIdKey = DBTable.ParseProperty(nameof(ExternalId));
        private Company company;

        public Department()
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Column("company_id"), Browsable(false)]
        public int? CompanyId
        {
            get => GetValue<int?>(CompanyKey);
            set => SetValue(value, CompanyKey);
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get => GetReference(CompanyKey, ref company);
            set => SetReference(company = value, CompanyKey);
        }

        [Column("parent_id", Keys = DBColumnKeys.Group), Index("rdepartment_parent_id"), Browsable(false)]
        public int? ParentId
        {
            get => GetGroupValue<int?>();
            set => SetGroupValue(value);
        }

        [Reference(nameof(ParentId))]
        public Department Parent
        {
            get => GetGroupReference<Department>();
            set => SetGroupReference(value);
        }

        [Column("code", 256, Keys = DBColumnKeys.Code | DBColumnKeys.Indexing), Index("rdepartment_code", false)]
        public string Code
        {
            get => GetValue<string>(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
        }
        
        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue<int?>(ExternalIdKey);
            set => SetValue(value, ExternalIdKey);
        }

        [Column("name", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }

        public string NameEN
        {
            get => GetValue<string>(NameENKey);
            set => SetValue(value, NameENKey);
        }

        public string NameRU
        {
            get => GetValue<string>(NameRUKey);
            set => SetValue(value, NameRUKey);
        }

        public override AccessValue Access
        {
            get => base.Access != Table.Access ? base.Access
                  : Parent?.Access ?? Table.Access;
        }

        [ControllerMethod]
        public IEnumerable<Position> GetPositions()
        {
            return GetReferencing<Position>(nameof(Position.DepartmentId), DBLoadParam.None);
        }

        [ControllerMethod]
        public IEnumerable<User> GetUsers()
        {
            return GetReferencing<User>(nameof(User.DepartmentId), DBLoadParam.None);
        }
    }
}

using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Counterpart;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Common
{

    [Table("rposition", "User")]
    public class Position : DBGroupItem
    {
        public static readonly DBTable<Position> DBTable = GetTable<Position>();
        public static readonly DBColumn DepartmentKey = DBTable.ParseProperty(nameof(Department));
        public static readonly DBColumn NameENKey = DBTable.ParseProperty(nameof(NameEN));
        public static readonly DBColumn NameRUKey = DBTable.ParseProperty(nameof(NameRU));
        public static readonly DBColumn CompanyKey = DBTable.ParseProperty(nameof(Company));
        public static readonly DBColumn ExternalIdKey = DBTable.ParseProperty(nameof(ExternalId));

        private Department department;
        private Company company;

        public Position()
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

        [Column("department_id"), Index("rposition_department_id"), Browsable(false)]
        public int? DepartmentId
        {
            get => GetValue<int?>(DepartmentKey);
            set => SetValue(value, DepartmentKey);
        }

        [Reference(nameof(DepartmentId))]
        public Department Department
        {
            get => GetReference(DepartmentKey, ref department);
            set => SetReference(department = value, DepartmentKey);
        }

        [Column("parent_id", Keys = DBColumnKeys.Group), Index("rposition_parent_id"), Browsable(false)]
        public int? ParentId
        {
            get => GetGroupValue<int?>();
            set => SetGroupValue(value);
        }

        [Reference(nameof(ParentId))]
        public Position Parent
        {
            get => GetGroupReference<Position>();
            set => SetGroupReference(value);
        }

        [Column("code", 40, Keys = DBColumnKeys.Code | DBColumnKeys.Indexing)]
        [Index("rposition_code", true)]
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
                  : Department?.Access ?? Parent?.Access ?? Table.Access;
        }

        [ControllerMethod]
        public IEnumerable<User> GetUsers()
        {
            return GetReferencing<User>(nameof(User.PositionId), DBLoadParam.None);
        }
    }
}

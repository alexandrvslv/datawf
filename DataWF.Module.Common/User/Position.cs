using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Counterpart;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Common
{

    [Table("rposition", "User")]
    public sealed partial class Position : DBGroupItem
    {
        private Department department;
        private Company company;

        public PositionTable PositionTable => (PositionTable)Table;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Column("company_id"), Browsable(false)]
        public int? CompanyId
        {
            get => GetValue(Table.CompanyIdKey);
            set => SetValue(value, Table.CompanyIdKey);
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get => GetReference(Table.CompanyIdKey, ref company);
            set => SetReference(company = value, Table.CompanyIdKey);
        }

        [Column("department_id"), Index("rposition_department_id"), Browsable(false)]
        public int? DepartmentId
        {
            get => GetValue(Table.DepartmentIdKey);
            set => SetValue(value, Table.DepartmentIdKey);
        }

        [Reference(nameof(DepartmentId))]
        public Department Department
        {
            get => GetReference(Table.DepartmentIdKey, ref department);
            set => SetReference(department = value, Table.DepartmentIdKey);
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
            get => GetValue(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
        }

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue(Table.ExternalIdKey);
            set => SetValue(value, Table.ExternalIdKey);
        }

        [Column("name", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }

        [CultureKey(nameof(Name))]
        public string NameEN
        {
            get => GetValue(Table.NameENKey);
            set => SetValue(value, Table.NameENKey);
        }

        [CultureKey(nameof(Name))]
        public string NameRU
        {
            get => GetValue(Table.NameRUKey);
            set => SetValue(value, Table.NameRUKey);
        }

        public override AccessValue Access
        {
            get => InternalAccess ?? Parent?.InternalAccess ?? Department?.InternalAccess ?? Table.Access;
        }

        [ControllerMethod]
        public IEnumerable<User> GetUsers()
        {
            return GetReferencing<User>(nameof(User.PositionId), DBLoadParam.None);
        }
    }
}

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
    public sealed partial class Department : DBGroupItem, IComparable, IDisposable
    {
        private Company company;

        public Department(DBTable table) : base(table)
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue<int>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Column("company_id"), Browsable(false)]
        public int? CompanyId
        {
            get => GetValue<int?>(Table.CompanyIdKey);
            set => SetValue(value, Table.CompanyIdKey);
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get => GetReference(Table.CompanyIdKey, ref company);
            set => SetReference(company = value, Table.CompanyIdKey);
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
            get => GetValue<int?>(Table.ExternalIdKey);
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
            get => GetValue<string>(Table.NameENKey);
            set => SetValue(value, Table.NameENKey);
        }

        [CultureKey(nameof(Name))]
        public string NameRU
        {
            get => GetValue<string>(Table.NameRUKey);
            set => SetValue(value, Table.NameRUKey);
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

using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Counterpart;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Common
{

    [Table("rposition", "User"), InvokerGenerator]
    public partial class Position : DBGroupItem
    {
        private Department department;
        private Company company;

        public Position()
        { }

        public PositionTable PositionTable => (PositionTable)Table;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(PositionTable.IdKey);
            set => SetValue(value, PositionTable.IdKey);
        }

        [Column("company_id"), Browsable(false)]
        public int? CompanyId
        {
            get => GetValue<int?>(PositionTable.CompanyKey);
            set => SetValue(value, PositionTable.CompanyKey);
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get => GetReference(PositionTable.CompanyKey, ref company);
            set => SetReference(company = value, PositionTable.CompanyKey);
        }

        [Column("department_id"), Index("rposition_department_id"), Browsable(false)]
        public int? DepartmentId
        {
            get => GetValue<int?>(PositionTable.DepartmentKey);
            set => SetValue(value, PositionTable.DepartmentKey);
        }

        [Reference(nameof(DepartmentId))]
        public Department Department
        {
            get => GetReference(PositionTable.DepartmentKey, ref department);
            set => SetReference(department = value, PositionTable.DepartmentKey);
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
            get => GetValue<string>(PositionTable.CodeKey);
            set => SetValue(value, PositionTable.CodeKey);
        }

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue<int?>(PositionTable.ExternalKey);
            set => SetValue(value, PositionTable.ExternalKey);
        }

        [Column("name", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }

        [CultureKey]
        public string NameEN
        {
            get => GetValue<string>(PositionTable.NameENKey);
            set => SetValue(value, PositionTable.NameENKey);
        }

        [CultureKey]
        public string NameRU
        {
            get => GetValue<string>(PositionTable.NameRUKey);
            set => SetValue(value, PositionTable.NameRUKey);
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

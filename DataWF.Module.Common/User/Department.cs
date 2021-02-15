﻿using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Counterpart;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Common
{

    [Table("rdepartment", "User", BlockSize = 100), InvokerGenerator]
    public sealed partial class Department : DBGroupItem, IComparable, IDisposable
    {
        private Company company;

        public Department(DBTable table) : base(table)
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(DepartmentTable.IdKey);
            set => SetValue(value, DepartmentTable.IdKey);
        }

        [Column("company_id"), Browsable(false)]
        public int? CompanyId
        {
            get => GetValue<int?>(DepartmentTable.CompanyIdKey);
            set => SetValue(value, DepartmentTable.CompanyIdKey);
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get => GetReference(DepartmentTable.CompanyIdKey, ref company);
            set => SetReference(company = value, DepartmentTable.CompanyIdKey);
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
            get => GetValue<string>(DepartmentTable.CodeKey);
            set => SetValue(value, DepartmentTable.CodeKey);
        }

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue<int?>(DepartmentTable.ExternalIdKey);
            set => SetValue(value, DepartmentTable.ExternalIdKey);
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
            get => GetValue<string>(DepartmentTable.NameENKey);
            set => SetValue(value, DepartmentTable.NameENKey);
        }

        [CultureKey(nameof(Name))]
        public string NameRU
        {
            get => GetValue<string>(DepartmentTable.NameRUKey);
            set => SetValue(value, DepartmentTable.NameRUKey);
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

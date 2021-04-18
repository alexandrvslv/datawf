using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Counterpart;
using MailKit.Net.Smtp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataWF.Module.Common
{

    [Table("ruser", "User", BlockSize = 100, Type = typeof(UserTable))]
    public sealed partial class User : DBUser, IComparable, IDisposable
    {
        private bool online = false;
        private Company company;
        private Department department;
        private Position position;
        private HashSet<IAccessIdentity> identities;
        private AccessValue cacheAccess;
        private Address address;

        public User(DBTable table) : base(table)
        { }

        [JsonIgnore]
        public UserReg LogStart { get; set; }

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue<int?>(Table.ExternalIdKey);
            set => SetValue(value, Table.ExternalIdKey);
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

        [Column("abbreviation", 4, Keys = DBColumnKeys.Indexing), Index("ruser_abbreviation", true)]
        public string Abbreviation
        {
            get => GetValue<string>(Table.AbbreviationKey);
            set => SetValue(value, Table.AbbreviationKey);
        }

        [Column("department_id"), Browsable(false)]
        public int? DepartmentId
        {
            get => GetValue<int?>(Table.DepartmentIdKey);
            set => SetValue(value, Table.DepartmentIdKey);
        }

        [Reference(nameof(DepartmentId))]
        public Department Department
        {
            get => GetReference(Table.DepartmentIdKey, ref department);
            set => SetReference(department = value, Table.DepartmentIdKey);
        }

        [Column("position_id"), Browsable(false)]
        public int? PositionId
        {
            get => GetValue<int?>(Table.PositionIdKey);
            set => SetValue(value, Table.PositionIdKey);
        }

        [Reference(nameof(PositionId))]
        public Position Position
        {
            get => GetReference<Position>(Table.PositionIdKey, ref position);
            set
            {
                SetReference(position = value, Table.PositionIdKey);
                Department = value?.Department;
            }
        }

        [ReadOnly(true)]
        [DefaultValue(false), Column("super")]
        public bool? Super
        {
            get => GetValue<bool?>(Table.SuperKey);
            set => SetValue(value, Table.SuperKey);
        }

        [Browsable(false)]
        public bool Online
        {
            get => online;
            set
            {
                if (online == value)
                    return;
                online = value;
                OnPropertyChanged<bool>();
            }
        }

        [Column("phone", 1024), Index("ruser_phone", false)]
        public string Phone
        {
            get => GetValue<string>(Table.PhoneKey);
            set => SetValue(value, Table.PhoneKey);
        }

        public bool IsBlock
        {
            get => Status != DBStatus.Actual;
            set => Status = value ? DBStatus.Actual : DBStatus.Error;
        }

        [Column("is_temp_pass")]
        public bool? IsTempPassword
        {
            get => GetValue<bool?>(Table.IsTempPasswordKey);
            set => SetValue(value, Table.IsTempPasswordKey);
        }

        [Column("password", 512, Keys = DBColumnKeys.Password), PasswordPropertyText(true)]
        public string Password
        {
            get => GetValue<string>(Table.PasswordKey);
            set
            {
                if (value == null)
                {
                    SetValue(value, Table.PasswordKey);
                    return;
                }
                var rez = Table.ValidateText(this, value);
                if (!string.IsNullOrEmpty(rez))
                {
                    throw new ArgumentException(rez);
                }
                IsTempPassword = false;
                SetValue(Helper.GetSha512(value), Table.PasswordKey);
            }
        }

        [Browsable(false)]
        public override AccessValue Access
        {
            get => base.Access != Table.Access ? base.Access
                  : Department?.Access ?? Position?.Access ?? Table.Access;
        }

        [Browsable(false)]
        public string AccessToken { get; set; }

        [Browsable(false), Column("token_refresh", 2048, Keys = DBColumnKeys.Password | DBColumnKeys.NoLog)]
        public string RefreshToken
        {
            get => GetValue<string>(Table.RefreshTokenKey);
            set => SetValue(value, Table.RefreshTokenKey);
        }

        [Column("auth_type")]
        public UserAuthType? AuthType
        {
            get => GetValue<UserAuthType?>(Table.AuthTypeKey) ?? UserAuthType.SMTP;
            set => SetValue(value, Table.AuthTypeKey);
        }

        public override bool IsAuthenticated => string.IsNullOrEmpty(AccessToken);

        [Column("name", size: 1024, Keys = DBColumnKeys.Culture | DBColumnKeys.View)]
        public override string Name { get => GetName(); set => SetName(value); }

        [CultureKey(nameof(Name))]
        public string NameRU
        {
            get => GetValue<string>(Table.NameRUKey);
            set => SetValue(value, Table.NameRUKey);
        }

        [CultureKey(nameof(Name))]
        public string NameEN
        {
            get => GetValue<string>(Table.NameENKey);
            set => SetValue(value, Table.NameENKey);
        }

        [Column("address_id"), Browsable(false)]
        public int? AddressId
        {
            get => GetValue<int?>(Table.AddressIdKey);
            set => SetValue(value, Table.AddressIdKey);
        }

        [Reference(nameof(AddressId))]
        public Address Address
        {
            get => GetReference(Table.AddressIdKey, ref address);
            set => SetReference(address = value, Table.AddressIdKey);
        }
        public override string AuthenticationType => AuthType?.ToString();

        public override HashSet<IAccessIdentity> Groups
        {
            get
            {
                var access = Access;
                if (identities == null)
                {
                    cacheAccess = access;
                    identities = new HashSet<IAccessIdentity>(Super ?? false
                            ? (IEnumerable<IAccessIdentity>)Schema.GetTable<UserGroup>()
                            : access.Items.Where(p => p.Create).Select(p => p.Identity));
                    identities.Add(this);
                }
                else if (access != cacheAccess)
                {
                    cacheAccess = access;
                    FillCache();
                }
                return identities;
            }
        }
        private void FillCache()
        {
            identities.Clear();
            identities.Add(this);
            foreach (var group in Super ?? false
                ? (IEnumerable<IAccessIdentity>)Schema.GetTable<UserGroup>()
                : cacheAccess.Items.Where(p => p.Create).Select(p => p.Identity))
            {
                identities.Add(group);
            }
        }
        public override void Dispose()
        {
            base.Dispose();
        }

        public new int CompareTo(object obj)
        {
            return base.CompareTo(obj);// this.ToString().CompareTo(obj.ToString());
        }
    }

    public enum UserAuthType
    {
        Internal = 1,
        SMTP = 2,
        LDAP = 3
    }
}

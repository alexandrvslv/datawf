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

    [Table("ruser", "User", BlockSize = 100, Type = typeof(UserTable)), InvokerGenerator]
    public sealed partial class User : DBUser, IComparable, IDisposable
    {
        private bool online = false;
        private Company company;
        private Department department;
        private Position position;
        private List<IAccessIdentity> identities;
        private AccessValue cacheAccess;
        private Address address;

        public User(DBTable table) : base(table)
        { }

        [JsonIgnore]
        public UserReg LogStart { get; set; }

        [JsonIgnore]
        public UserTable UserTable => (UserTable)Table;

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue<int?>(UserTable.ExternalIdKey);
            set => SetValue(value, UserTable.ExternalIdKey);
        }

        [Column("company_id"), Browsable(false)]
        public int? CompanyId
        {
            get => GetValue<int?>(UserTable.CompanyIdKey);
            set => SetValue(value, UserTable.CompanyIdKey);
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get => GetReference(UserTable.CompanyIdKey, ref company);
            set => SetReference(company = value, UserTable.CompanyIdKey);
        }

        [Column("abbreviation", 4, Keys = DBColumnKeys.Indexing), Index("ruser_abbreviation", true)]
        public string Abbreviation
        {
            get => GetValue<string>(UserTable.AbbreviationKey);
            set => SetValue(value, UserTable.AbbreviationKey);
        }

        [Column("department_id"), Browsable(false)]
        public int? DepartmentId
        {
            get => GetValue<int?>(UserTable.DepartmentIdKey);
            set => SetValue(value, UserTable.DepartmentIdKey);
        }

        [Reference(nameof(DepartmentId))]
        public Department Department
        {
            get => GetReference(UserTable.DepartmentIdKey, ref department);
            set => SetReference(department = value, UserTable.DepartmentIdKey);
        }

        [Column("position_id"), Browsable(false)]
        public int? PositionId
        {
            get => GetValue<int?>(UserTable.PositionIdKey);
            set => SetValue(value, UserTable.PositionIdKey);
        }

        [Reference(nameof(PositionId))]
        public Position Position
        {
            get => GetReference<Position>(UserTable.PositionIdKey, ref position);
            set
            {
                SetReference(position = value, UserTable.PositionIdKey);
                Department = value?.Department;
            }
        }

        [ReadOnly(true)]
        [DefaultValue(false), Column("super")]
        public bool? Super
        {
            get => GetValue<bool?>(UserTable.SuperKey);
            set => SetValue(value, UserTable.SuperKey);
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
            get => GetValue<string>(UserTable.PhoneKey);
            set => SetValue(value, UserTable.PhoneKey);
        }

        public bool IsBlock
        {
            get => Status != DBStatus.Actual;
            set => Status = value ? DBStatus.Actual : DBStatus.Error;
        }

        [Column("is_temp_pass")]
        public bool? IsTempPassword
        {
            get => GetValue<bool?>(UserTable.IsTempPasswordKey);
            set => SetValue(value, UserTable.IsTempPasswordKey);
        }

        [Column("password", 512, Keys = DBColumnKeys.Password), PasswordPropertyText(true)]
        public string Password
        {
            get => GetValue<string>(UserTable.PasswordKey);
            set
            {
                if (value == null)
                {
                    SetValue(value, UserTable.PasswordKey);
                    return;
                }
                var rez = UserTable.ValidateText(this, value);
                if (!string.IsNullOrEmpty(rez))
                {
                    throw new ArgumentException(rez);
                }
                IsTempPassword = false;
                SetValue(Helper.GetSha512(value), UserTable.PasswordKey);
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
            get => GetValue<string>(UserTable.RefreshTokenKey);
            set => SetValue(value, UserTable.RefreshTokenKey);
        }

        [Column("auth_type")]
        public UserAuthType? AuthType
        {
            get => GetValue<UserAuthType?>(UserTable.AuthTypeKey) ?? UserAuthType.SMTP;
            set => SetValue(value, UserTable.AuthTypeKey);
        }

        public override bool IsAuthenticated => string.IsNullOrEmpty(AccessToken);

        [Column("name", size: 1024, Keys = DBColumnKeys.Culture | DBColumnKeys.View)]
        public override string Name { get => GetName(); set => SetName(value); }

        [CultureKey(nameof(Name))]
        public string NameRU
        {
            get => GetValue<string>(UserTable.NameRUKey);
            set => SetValue(value, UserTable.NameRUKey);
        }

        [CultureKey(nameof(Name))]
        public string NameEN
        {
            get => GetValue<string>(UserTable.NameENKey);
            set => SetValue(value, UserTable.NameENKey);
        }

        [Column("address_id"), Browsable(false)]
        public int? AddressId
        {
            get => GetValue<int?>(UserTable.AddressIdKey);
            set => SetValue(value, UserTable.AddressIdKey);
        }

        [Reference(nameof(AddressId))]
        public Address Address
        {
            get => GetReference(UserTable.AddressIdKey, ref address);
            set => SetReference(address = value, UserTable.AddressIdKey);
        }
        public override string AuthenticationType => AuthType?.ToString();

        public override IEnumerable<IAccessIdentity> Groups
        {
            get
            {
                var access = Access;
                if (identities == null)
                {
                    cacheAccess = access;
                    identities = new List<IAccessIdentity>(Super ?? false
                            ? (IEnumerable<IAccessIdentity>)Schema.GetTable<UserGroup>()
                            : access.Items.Where(p => p.Create).Select(p => p.Identity));
                    identities.Add(this);
                }
                else if (access != cacheAccess)
                {
                    cacheAccess = access;
                    identities.Clear();
                    identities.Add(this);
                    identities.AddRange(Super ?? false
                        ? (IEnumerable<IAccessIdentity>)Schema.GetTable<UserGroup>()
                        : access.Items.Where(p => p.Create).Select(p => p.Identity));
                }
                return identities;
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

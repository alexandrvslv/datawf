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
using System.Collections.Generic;
using System.Security.Principal;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using DataWF.Common;

namespace DataWF.Data
{
    [AbstractTable]
    public abstract partial class DBUser : DBItem, IUserIdentity
    {
        public DBUser(DBTable table) : base(table)
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue<int>(Table.IdKey);
            set => SetValue<int>(value, Table.IdKey);
        }

        [Column("login", 256, Keys = DBColumnKeys.Code | DBColumnKeys.Indexing), Index("ruser_login", true)]
        public string Login
        {
            get => GetValue<string>(Table.LoginKey);
            set => SetValue(value, Table.LoginKey);
        }

        [Column("email", 1024, Keys = DBColumnKeys.Indexing), Index("ruser_email", true)]
        public string EMail
        {
            get => GetValue<string>(Table.EMailKey);
            set => SetValue(value, Table.EMailKey);
        }

        [JsonIgnore, XmlIgnore]
        public abstract HashSet<IAccessIdentity> Groups { get; }

        [JsonIgnore, XmlIgnore]
        public abstract string AuthenticationType { get; }

        [JsonIgnore, XmlIgnore]
        public abstract bool IsAuthenticated { get; }
        public abstract string Name { get; set; }

        string IIdentity.Name => EMail;
    }
}

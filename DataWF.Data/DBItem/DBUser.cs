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
    public abstract class DBUser : DBItem, IUserIdentity
    {
        [Column("unid", Keys = DBColumnKeys.Primary)]
        public abstract int? Id { get; set; }

        [Column("login", 256, Keys = DBColumnKeys.Code | DBColumnKeys.Indexing), Index("ruser_login", true)]
        public abstract string Login { get; set; }

        [Column("email", 1024, Keys = DBColumnKeys.Indexing), Index("ruser_email", true)]
        public abstract string EMail { get; set; }

        [JsonIgnore, XmlIgnore]
        public abstract string Name { get; set; }

        [JsonIgnore, XmlIgnore]
        public abstract HashSet<IAccessIdentity> Groups { get; }

        [JsonIgnore, XmlIgnore]
        public abstract string AuthenticationType { get; }

        [JsonIgnore, XmlIgnore]
        public abstract bool IsAuthenticated { get; }

        string IIdentity.Name => EMail;
    }
}

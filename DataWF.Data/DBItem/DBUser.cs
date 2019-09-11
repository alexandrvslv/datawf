﻿/*
 DBTable.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using System.Security.Principal;
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

        public abstract string Name { get; set; }


        public abstract IEnumerable<IAccessGroup> Groups { get; }

        public abstract string AuthenticationType { get; }

        public abstract bool IsAuthenticated { get; }

        string IIdentity.Name => EMail;
    }
}
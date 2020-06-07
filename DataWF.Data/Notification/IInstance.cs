﻿/*
 User.cs
 
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
using DataWF.Common;
using System.Net;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public interface IInstance
    {
        bool? Active { get; set; }
        IPEndPoint EndPoint { get; set; }
        string Host { get; set; }
        int? Id { get; set; }
        bool IsCurrent { get; }
        int? Port { get; set; }
        long ReceiveCount { get; set; }
        long ReceiveLength { get; set; }
        long SendCount { get; set; }
        long SendLength { get; set; }

        void Delete();
        Task Save(IUserIdentity user);
    }
}
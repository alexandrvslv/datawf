/*
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
using DataWF.Data;
using System.Collections.Generic;

namespace DataWF.Module.Common
{
    public static class UserExtension
    {
        public static IEnumerable<User> GetUsers(this DBItem item, DBItem filter = null)
        {
            foreach (var access in item.Access.Items)
            {
                if (access.Create && (filter == null || filter.Access.Get(access.Group).Edit))
                {
                    foreach (User user in User.DBTable)
                    {
                        if (user.Access.Get(access.Group).Create)
                            yield return user;
                    }
                }
            }
        }

        public static IEnumerable<Position> GetPositions(this DBItem item, DBItem filter = null)
        {
            foreach (var access in item.Access.Items)
            {
                if (access.Create && (filter == null || filter.Access.Get(access.Group).Edit))
                {
                    foreach (Position position in Position.DBTable)
                    {
                        if (position.Access.Get(access.Group).Create)
                            yield return position;
                    }
                }
            }
        }

        public static IEnumerable<Department> GetDepartment(this DBItem item, DBItem filter = null)
        {
            foreach (var access in item.Access.Items)
            {
                if (access.Create && (filter == null || filter.Access.Get(access.Group).Edit))
                {
                    foreach (Department department in Department.DBTable)
                    {
                        if (department.Access.Get(access.Group).Create)
                            yield return department;
                    }
                }
            }
        }
    }
}

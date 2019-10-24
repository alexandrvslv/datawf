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
using System.Linq;
using System.Collections.Generic;

namespace DataWF.Module.Common
{
    public static class UserExtension
    {
        public static IEnumerable<User> GetUsers(this DBItem item, DBItem filter = null)
        {
            foreach (User user in User.DBTable)
            {
                foreach (var group in user.Groups)
                {
                    var access = item.Access.Get(group);
                    if (access.Create && (filter == null || filter.Access.Get(access.Group).Update))
                    {
                        yield return user;
                        break;
                    }

                }
            }
        }

        public static IEnumerable<Position> GetPositions(this DBItem item, DBItem filter = null)
        {
            foreach (Position position in Position.DBTable)
            {
                foreach (var access in position.Access.Items.Where(p => p.Create))
                {
                    if (item.Access.Get(access.Group).Create)
                    {
                        if (filter == null || filter.Access.Get(access.Group).Update)
                        {
                            yield return position;
                            break;
                        }
                    }
                }
            }
        }

        public static IEnumerable<Department> GetDepartments(this DBItem item, DBItem filter = null)
        {
            foreach (Department department in Department.DBTable)
            {
                foreach (var access in department.Access.Items.Where(p => p.Create))
                {
                    if (item.Access.Get(access.Group).Create)
                    {
                        if ((filter == null || filter.Access.Get(access.Group).Update))
                        {
                            yield return department;
                            break;
                        }
                    }
                }
            }
        }
    }
}

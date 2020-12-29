using DataWF.Data;
using System.Linq;
using System.Collections.Generic;

namespace DataWF.Module.Common
{
    public static class UserExtension
    {
        public static IEnumerable<User> GetUsers(this DBItem item, DBItem filter = null)
        {
            var users = (UserTable)item.Schema.GetTable<User>();
            foreach (User user in users)
            {
                foreach (var group in user.Groups)
                {
                    var access = item.Access.Get(group);
                    if (access.Create && (filter == null || filter.Access.Get(access.Identity).Update))
                    {
                        yield return user;
                        break;
                    }

                }
            }
        }

        public static IEnumerable<Position> GetPositions(this DBItem item, DBItem filter = null)
        {
            var positions = (PositionTable)item.Schema.GetTable<Position>();
            foreach (Position position in positions)
            {
                foreach (var access in position.Access.Items.Where(p => p.Create))
                {
                    if (item.Access.Get(access.Identity).Create)
                    {
                        if (filter == null || filter.Access.Get(access.Identity).Update)
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
            var departments = (DepartmentTable)item.Schema.GetTable<Department>();
            foreach (Department department in departments)
            {
                foreach (var access in department.Access.Items.Where(p => p.Create))
                {
                    if (item.Access.Get(access.Identity).Create)
                    {
                        if ((filter == null || filter.Access.Get(access.Identity).Update))
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

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
            foreach (Position position in Position.DBTable)
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
            foreach (Department department in Department.DBTable)
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

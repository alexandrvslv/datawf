using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Module.Counterpart;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace DataWF.Test.Module.Common
{
    [TestFixture()]
    public class Generate
    {
        [Test()]
        public async Task Initialize()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var schema = new DBSchema("common_database");
            schema.Generate(new[] {
                typeof(Customer).Assembly,
                typeof(User).Assembly });
            Assert.IsNotNull(schema);
            DBService.Schems.Add(schema);

            var bookTable = schema.GetTable<Book>();
            var userGroupTable = (UserGroupTable)schema.GetTable<UserGroup>();
            var groupPermissionTable = (GroupPermissionTable)schema.GetTable<GroupPermission>();
            var userTable = (UserTable)schema.GetTable<User>();
            var positionTable = (PositionTable)schema.GetTable<Position>();
            var userRegTable = (UserRegTable)schema.GetTable<UserReg>();
            var companyTable = (CompanyTable)schema.GetTable<Company>();

            Assert.IsNotNull(bookTable);
            Assert.IsNotNull(userGroupTable);
            Assert.IsNotNull(groupPermissionTable);
            Assert.IsNotNull(userTable);
            Assert.IsNotNull(positionTable);
            Assert.IsNotNull(userRegTable);
            Assert.IsNotNull(companyTable);
            DBService.Save();
            schema.Connection = new DBConnection
            {
                Name = "test.common",
                System = DBSystem.SQLite,
                DataBase = "test.common"
            };
            schema.DropDatabase();
            schema.CreateDatabase();

            var group = new UserGroup(userGroupTable)
            {
                Number = "GP1",
                Name = "Group1"
            };
            await group.Save();

            var position = new Position(positionTable)
            {
                Code = "PS1",
                Name = "Position"
            };

            var user = new User(userTable)
            {
                Login = "test",
                Name = "Test User",
                Password = "UserCommon1!",
                Position = position,
                AuthType = UserAuthType.Internal,
                Access = new AccessValue(new[] { new AccessItem(group, AccessType.Create) })
            };
            await user.Save();

            await userTable.StartSession("test", "UserCommon1!");

            await groupPermissionTable.CachePermission();

        }
    }
}

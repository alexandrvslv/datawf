using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Module.Counterpart;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace DataWF.Test.Module.Common
{
    [Provider]
    [SchemaEntry(typeof(CommonSchema))]
    public partial class TestProvider : DBProvider
    { }

   [TestFixture()]
    public class Generate
    {
        [Test()]
        public async Task Initialize()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var provider = new TestProvider();

            var schema = provider.CommonSchema;

            var bookTable = schema.Book;
            var userGroupTable = schema.UserGroup;
            var groupPermissionTable = schema.GroupPermission;
            var userTable = schema.User;
            var positionTable = schema.Position;
            var userRegTable = schema.UserReg;
            var companyTable = schema.Company;

            Assert.IsNotNull(bookTable);
            Assert.IsNotNull(userGroupTable);
            Assert.IsNotNull(groupPermissionTable);
            Assert.IsNotNull(userTable);
            Assert.IsNotNull(positionTable);
            Assert.IsNotNull(userRegTable);
            Assert.IsNotNull(companyTable);

            schema.Connection = new DBConnection
            {
                Name = "test.common",
                System = DBSystem.SQLite,
                DataBase = "test.common"
            };
            schema.ExecuteDropDatabase();
            schema.ExecuteCreateDatabase();

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
                Access = new AccessValue(new[] { new AccessItem(group, AccessType.Create) }, provider)
            };
            await user.Save();

            await userTable.StartSession("test", "UserCommon1!");

            await groupPermissionTable.CachePermission();

        }


    }
}

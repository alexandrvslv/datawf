using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using NUnit.Framework;
using System;

namespace DataWF.Test.Module.Common
{
    [TestFixture()]
    public class Generate
    {
        [Test()]
        public void Initialize()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var schema = DBSchema.Generate(typeof(User).Assembly, "common_database");
            Assert.IsNotNull(schema);
            Assert.IsNotNull(Book.DBTable);
            Assert.IsNotNull(UserGroup.DBTable);
            Assert.IsNotNull(GroupPermission.DBTable);
            Assert.IsNotNull(User.DBTable);
            Assert.IsNotNull(Position.DBTable);
            Assert.IsNotNull(UserLog.DBTable);
            DBService.Save();
            schema.Connection = new DBConnection
            {
                Name = "test.common",
                System = DBSystem.SQLite,
                DataBase = "test.common"
            };
            schema.DropDatabase();
            schema.CreateDatabase();

            var group = new UserGroup()
            {
                Number = "GP1",
                Name = "Group1"
            };
            group.Save();

            var position = new Position()
            {
                Code = "PS1",
                Name = "Position"
            };

            var user = new User()
            {
                Login = "test",
                Name = "Test User",
                Password = "UserCommon1!",
                Position = position,
                Access = new AccessValue(new[] { new AccessItem(group, AccessType.Create) })
            };
            user.Save();

            User.StartSession("test", "UserCommon1!");

            GroupPermission.CachePermission();

        }
    }
}

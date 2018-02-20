using NUnit.Framework;
using System;
using DataWF.Data;
using DataWF.Module.Common;
using System.Linq;
using DataWF.Common;

namespace DataWF.Test.Module.Common
{
    [TestFixture()]
    public class Generate
    {
        [Test()]
        public void Initialize()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var schema = DBService.Generate(typeof(User).Assembly);
            Assert.IsNotNull(schema);
            Assert.IsNotNull(UserGroup.DBTable);
            Assert.IsNotNull(GroupPermission.DBTable);
            Assert.IsNotNull(User.DBTable);
            Assert.IsNotNull(Position.DBTable);
            Assert.IsNotNull(UserLog.DBTable);
            DBService.Save();
            schema.Connection = new DBConnection
            {
                Name = "test.common",
                Host = "localhost",
                Port = 5432,
                User = "test",
                Password = "test",
                System = DBSystem.Postgres,
                Schema = "public"
            };

            schema.CreateDatabase();

            var group = new UserGroup()
            {
                Number = "GP1",
                Name = "Group1"
            };
            group.Save();

            var position = new Position()
            {
                Number = "PS1",
                Name = "Position"
            };

            var user = new User()
            {
                UserType = UserTypes.Persone,
                Login = "test",
                Name = "Test User",
                Password = "UserCommon1!",
                Position = position,
                Access = new AccessValue(new[] { new AccessItem(group, AccessType.Create) })
            };
            user.Save();

            User.SetCurrent("test", "UserCommon1!");

            GroupPermission.CachePermission();

        }
    }
}

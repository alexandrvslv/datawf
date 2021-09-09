using DataWF.Common;
using DataWF.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DataWF.Module.Common
{
    public partial class UserApplicationTable<T>
    {
        [ControllerMethod(Anonymous = true)]
        public async Task<UserApplication> Register([ControllerParameter(ControllerParameterType.Body)] UserApplication application)
        {
            var users = (UserTable)Schema.GetTable<User>();
            if (users.GetByEmail(application.EMail) != null)
            {
                throw new ArgumentException($"User with specified email: {application.EMail} already exist!", nameof(UserApplication.EMail));
            }

            var query = Query().Where(EMailKey, application.EMail)
                .And(TypeKey, application.Type)
                .And(StatusKey, CompareType.In, new[] { DBStatus.Actual, DBStatus.New });
            var exist = Load(query);
            if (exist.Count() > 0)
            {
                throw new ArgumentException($"Application with specified email: {application.EMail} already in process!", nameof(UserApplication.EMail));
            }
            await application.Save((IUserIdentity)null);
            _ = application.OnRegistered();
            return application;
        }
    }
}

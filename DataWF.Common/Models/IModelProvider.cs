using System;
using System.Security.Claims;

namespace DataWF.Common
{
    public interface IModelProvider : IAccessProvider
    {
        INamedOutList<IModelSchema> Schems { get; }

        IModelSchema GetSchema(string name);
        T GetSchema<T>() where T : IModelSchema;

        IModelTable<T> GetTable<T>();
        IModelTable GetTable(string name);
        IModelTable GetTable(Type type);
        IModelTable GetTable(Type type, int typeId);

        IUserIdentity GetUser(ClaimsPrincipal user);
        IUserIdentity GetUser(string email);
    }
}
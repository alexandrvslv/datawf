using DataWF.Access;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace DataWF.Models
{
    public class ModelProvider : IModelProvider
    {
        public ModelProvider()
        {
            Schems = new ModelSchemaList<IModelSchema> { Provider = this };
        }

        public ModelSchemaList<IModelSchema> Schems { get; }

        IModelItemList<IModelSchema> IModelProvider.Schems => Schems;        

        protected virtual void Add(IModelSchema schema) => Schems.Add(schema);

        public IModelSchema GetSchema(string name) => Schems[name];

        public T GetSchema<T>() where T : IModelSchema => Schems.OfType<T>().FirstOrDefault();

        public IWebClient GetWebClient(string name)
        {
            foreach (var schema in Schems.OfType<IWebSchema>())
            {
                var client = schema.GetClient(name);
                if (client != null)
                    return client;
            }
            return null;
        }

        public IWebTable<T> GetWebTable<T>()
        {
            foreach (var schema in Schems.OfType<IWebSchema>())
            {
                var table = schema.GetTable<T>();
                if (table != null)
                    return table;
            }
            return null;
        }

        public IWebTable GetWebTable(Type type)
        {
            foreach (var schema in Schems.OfType<IWebSchema>())
            {
                var table = schema.GetTable(type);
                if (table != null)
                    return table;
            }
            return null;
        }

        public IWebTable GetWebTable(Type type, int typeId)
        {
            foreach (var schema in Schems.OfType<IWebSchema>())
            {
                var table = schema.GetTable(type, typeId);
                if (table != null)
                    return table;
            }
            return null;
        }

        public IModelTable<T> GetTable<T>()
        {
            foreach (var schema in Schems)
            {
                var table = schema.GetTable<T>();
                if (table != null)
                    return table;
            }

            return null;
        }

        public IModelTable GetTable(string name)
        {
            foreach (var schema in Schems)
            {
                var table = schema.GetTable(name);
                if (table != null)
                    return table;
            }
            return null;
        }

        public IModelTable GetTable(Type type)
        {
            foreach (var schema in Schems)
            {
                var table = schema.GetTable(type);
                if (table != null)
                    return table;
            }
            return null;
        }

        public IModelTable GetTable(Type type, int typeId)
        {
            foreach (var schema in Schems)
            {
                var table = schema.GetTable(type, typeId);
                if (table != null)
                    return table;
            }
            return null;
        }

        public IUserIdentity GetUser(ClaimsPrincipal claims)
        {
            var emailClaim = claims?.FindFirst(ClaimTypes.Email);
            return emailClaim != null ? GetUser(emailClaim.Value) : null;
        }

        public virtual IUserIdentity GetUser(string email)
        {
            var userTable = GetTable(typeof(IUserIdentity));
            if (userTable == null)
                throw new InvalidOperationException("No User Tables found!");
            return userTable.TypeOf<IUserIdentity>().FirstOrDefault(p => p.Name == email);
        }

        public virtual IUserIdentity GetUser(int id)
        {
            var userTable = GetTable(typeof(IUserIdentity));
            if (userTable == null)
                throw new InvalidOperationException("No User Tables found!");
            return userTable.TypeOf<IUserIdentity>().FirstOrDefault(p => p.Id == id);
        }

        public virtual IUserGroupIdentity GetUserGroup(int id)
        {
            var groupTable = GetTable(typeof(IUserGroupIdentity));
            if (groupTable == null)
                throw new InvalidOperationException("No User Group Table found!");
            return groupTable.TypeOf<IUserGroupIdentity>().FirstOrDefault(p => p.Id == id);
        }

        public virtual IProjectIdentity GetProject(int id)
        {
            var projectTable = GetTable(typeof(IProjectIdentity));
            if (projectTable == null)
                throw new InvalidOperationException("No Project Table found!");
            return projectTable.TypeOf<IProjectIdentity>().FirstOrDefault(p => p.Id == id);
        }

        public virtual ICompanyIdentity GetCompany(int id)
        {
            var projectTable = GetTable(typeof(ICompanyIdentity));
            if (projectTable == null)
                throw new InvalidOperationException("No Company Table found!");
            return projectTable.TypeOf<ICompanyIdentity>().FirstOrDefault(p => p.Id == id);
        }

        public virtual IEnumerable<IAccessIdentity> GetGroups()
        {
            var groupTable = GetTable(typeof(IUserGroupIdentity));
            if (groupTable == null)
                throw new InvalidOperationException("No User Group Table found!");
            return groupTable.TypeOf<IUserGroupIdentity>();
        }

        public IAccessIdentity GetAccessIdentity(int id, IdentityType type)
        {
            switch (type)
            {
                case IdentityType.User: return GetUser(id);
                case IdentityType.Group: return GetUserGroup(id);
                case IdentityType.Project: return GetProject(id);
                case IdentityType.Company: return GetCompany(id);
                default: return null;
            }
        }

        public virtual IEnumerable<IAccessIdentity> GetSpecialUserGroups(IUserIdentity user)
        {
            return Enumerable.Empty<IAccessIdentity>();
        }
    }
}
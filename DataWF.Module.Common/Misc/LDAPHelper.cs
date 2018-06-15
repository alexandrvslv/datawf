﻿/*
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
using DataWF.Common;
using System;
using System.Collections.Generic;
using Novell.Directory.Ldap;
using System.Diagnostics;

namespace DataWF.Module.Common
{
    public class LDAPHelper
    {
        public static List<User> LoadADUsers(string userName, string password)
        {
            var users = new List<User>();
            try
            {
                var domain = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
                var domain1 = domain.Substring(0, domain.IndexOf('.'));
                var domain2 = domain.Substring(domain.IndexOf('.') + 1);
                var ldapDom = $"dc={domain1},dc={domain2}";
                var userDN = $"{userName},{ldapDom}";//$"cn={userName},o={domain1}";
                var attributes = new string[] { "cn", "company", "lastLongon", "lastLongoff", "mail", "mailNickname", "name", "title", "userPrincipalName" };
                using (var conn = new LdapConnection())
                {
                    conn.Connect(domain, LdapConnection.DEFAULT_PORT);
                    conn.Bind(userDN, password);
                    var results = conn.Search(ldapDom, //search base
                        LdapConnection.SCOPE_SUB, //scope 
                        "(&(objectCategory=person)(objectClass=user)(!(userAccountControl:1.2.840.113556.1.4.803:=2)))", //filter
                        attributes, //attributes 
                        false); //types only 
                    while (results.HasMore())
                    {
                        try
                        {

                            var resultRecord = results.Next();
                            var attribute = resultRecord.getAttribute("mailNickname");
                            if (attribute != null)
                            {
                                Position position = null;
                                var positionName = resultRecord.getAttribute("title")?.StringValue;
                                if (!string.IsNullOrEmpty(positionName))
                                {
                                    position = Position.DBTable.LoadByCode(positionName);
                                    if (position == null)
                                    {
                                        position = new Position();
                                    }
                                    position.Code = positionName;
                                    position.Name = positionName;
                                    position.Save();
                                }

                                var user = User.DBTable.LoadByCode(attribute.StringValue, User.DBTable.ParseProperty(nameof(User.Login)), DBLoadParam.None);
                                if (user == null)
                                {
                                    user = new User();
                                }
                                user.Position = position;
                                user.Login = attribute.StringValue;
                                user.EMail = resultRecord.getAttribute("mail")?.StringValue;
                                user.Name = resultRecord.getAttribute("name")?.StringValue;
                                user.Save();
                            }

                        }
                        catch (LdapException e)
                        {
                            Debug.WriteLine(e.Message);
                        }
                    }
                    conn.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
            }
            return users;
        }

    }
}
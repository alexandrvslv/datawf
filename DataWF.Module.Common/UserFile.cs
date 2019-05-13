﻿using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataWF.Module.Common
{
    [Table("ruser_file", "User", BlockSize = 100)]
    public class UserFile : DBItem
    {
        private static DBTable<UserFile> dbTable;
        private User user;

        private static DBColumn idKey = DBColumn.EmptyKey;
        private static DBColumn dataNameKey = DBColumn.EmptyKey;
        private static DBColumn userKey = DBColumn.EmptyKey;

        public static DBColumn IdKey => DBTable.ParseProperty(nameof(Id), ref idKey);
        public static DBColumn DataNameKey => DBTable.ParseProperty(nameof(DataName), ref dataNameKey);
        public static DBColumn UserKey => DBTable.ParseProperty(nameof(UserId), ref userKey);



        public static DBTable<UserFile> DBTable => dbTable ?? (dbTable = GetTable<UserFile>());

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Column("user_file", Keys = DBColumnKeys.File)]
        public byte[] Data
        {
            get { return GetProperty<byte[]>(); }
            set { SetProperty(value); }
        }

        [Column("user_file_name", 1024, Keys = DBColumnKeys.FileName | DBColumnKeys.View | DBColumnKeys.Code)]
        public string DataName
        {
            get => GetValue<string>(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
        }

        [Column("user_id")]
        public int? UserId
        {
            get => GetProperty<int?>();
            set => SetProperty(value);
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get => GetPropertyReference(ref user);
            set => user = SetPropertyReference(value);
        }
    }
}

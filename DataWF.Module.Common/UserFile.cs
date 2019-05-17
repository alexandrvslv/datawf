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
        private static DBColumn isAvatarKey = DBColumn.EmptyKey;

        public static DBColumn IdKey => DBTable.ParseProperty(nameof(Id), ref idKey);
        public static DBColumn DataNameKey => DBTable.ParseProperty(nameof(DataName), ref dataNameKey);
        public static DBColumn UserKey => DBTable.ParseProperty(nameof(UserId), ref userKey);
        public static DBColumn IsAvatarKey = DBTable.ParseProperty(nameof(IsAvatar), ref isAvatarKey);

        public UserFile()
        {
        }

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
            get { return GetValue<byte[]>(Table.FileKey); }
            set { SetValue(value, Table.FileKey); }
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
            get => GetValue<int?>(UserKey);
            set => SetValue(value, UserKey);
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get { return GetReference(UserKey, ref user); }
            set { SetReference(user = value, UserKey); }
        }

        [Column("is_avatar")]
        public bool? IsAvatar
        {
            get => GetValue<bool?>(IsAvatarKey);
            set => SetValue(value, IsAvatarKey);
        }
    }
}

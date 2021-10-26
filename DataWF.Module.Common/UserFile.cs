using DataWF.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataWF.Module.Common
{
    [Table("ruser_file", "User", BlockSize = 100)]
    public class UserFile : DBItem
    {
        public static readonly DBTable<UserFile> DBTable = GetTable<UserFile>();
        public static readonly DBColumn IdKey = DBTable.ParseProperty(nameof(Id));
        public static readonly DBColumn DataNameKey = DBTable.ParseProperty(nameof(DataName));
        public static readonly DBColumn UserKey = DBTable.ParseProperty(nameof(UserId));
        public static readonly DBColumn IsAvatarKey = DBTable.ParseProperty(nameof(IsAvatar));
        public static readonly DBColumn DataLastWriteKey = DBTable.ParseProperty(nameof(DataLastWrite));

        private User user;

        public UserFile()
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Column("user_file", Keys = DBColumnKeys.File)]
        public byte[] Data
        {
            get => GetValue<byte[]>(Table.FileKey);
            set => SetValue(value, Table.FileKey);
        }

        [Column("user_file_name", 1024, Keys = DBColumnKeys.FileName | DBColumnKeys.View | DBColumnKeys.Code)]
        public string DataName
        {
            get => GetValue<string>(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
        }

        [Column("user_file_last_write", Keys = DBColumnKeys.FileLastWrite)]
        public DateTime? DataLastWrite
        {
            get => GetValue<DateTime?>(DataLastWriteKey) ?? Stamp;
            set => SetValue(value, DataLastWriteKey);
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
            get => GetReference(UserKey, ref user);
            set => SetReference(user = value, UserKey);
        }

        [Column("is_avatar")]
        public bool? IsAvatar
        {
            get => GetValue<bool?>(IsAvatarKey);
            set => SetValue(value, IsAvatarKey);
        }
    }
}

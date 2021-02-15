using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataWF.Module.Common
{
    [Table("ruser_file", "User", BlockSize = 100), InvokerGenerator]
    public sealed partial class UserFile : DBItem
    {
        private User user;

        public UserFile(DBTable table) : base(table)
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(UserFileTable.IdKey);
            set => SetValue(value, UserFileTable.IdKey);
        }

        [Column("user_file", Keys = DBColumnKeys.File)]
        public byte[] Data
        {
            get => GetValue<byte[]>(UserFileTable.FileKey);
            set => SetValue(value, UserFileTable.FileKey);
        }

        [Column("user_file_name", 1024, Keys = DBColumnKeys.FileName | DBColumnKeys.View | DBColumnKeys.Code)]
        public string DataName
        {
            get => GetValue<string>(UserFileTable.CodeKey);
            set => SetValue(value, UserFileTable.CodeKey);
        }

        [Column("user_file_last_write", Keys = DBColumnKeys.FileLastWrite)]
        public DateTime? DataLastWrite
        {
            get => GetValue<DateTime?>(UserFileTable.DataLastWriteKey) ?? Stamp;
            set => SetValue(value, UserFileTable.DataLastWriteKey);
        }

        [Column("user_id")]
        public int? UserId
        {
            get => GetValue<int?>(UserFileTable.UserIdKey);
            set => SetValue(value, UserFileTable.UserIdKey);
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get => GetReference(UserFileTable.UserIdKey, ref user);
            set => SetReference(user = value, UserFileTable.UserIdKey);
        }

        [Column("is_avatar")]
        public bool? IsAvatar
        {
            get => GetValue<bool?>(UserFileTable.IsAvatarKey);
            set => SetValue(value, UserFileTable.IsAvatarKey);
        }
    }
}

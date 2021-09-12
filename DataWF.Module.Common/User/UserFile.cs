﻿using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataWF.Module.Common
{
    [Table("ruser_file", "User", BlockSize = 100)]
    public sealed partial class UserFile : DBItem
    {
        private User user;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Column("user_file", Keys = DBColumnKeys.File)]
        public byte[] Data
        {
            get => GetValue(Table.FileKey);
            set => SetValue(value, Table.FileKey);
        }

        [Column("user_file_name", 1024, Keys = DBColumnKeys.FileName | DBColumnKeys.View | DBColumnKeys.Code)]
        public string DataName
        {
            get => GetValue(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
        }

        [Column("user_file_last_write", Keys = DBColumnKeys.FileLastWrite)]
        public DateTime? DataLastWrite
        {
            get => GetValue(Table.DataLastWriteKey) ?? Stamp;
            set => SetValue(value, Table.DataLastWriteKey);
        }

        [Column("user_id")]
        public int? UserId
        {
            get => GetValue(Table.UserIdKey);
            set => SetValue(value, Table.UserIdKey);
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get => GetReference(Table.UserIdKey, ref user);
            set => SetReference(user = value, Table.UserIdKey);
        }

        [Column("is_avatar")]
        public bool? IsAvatar
        {
            get => GetValue(Table.IsAvatarKey);
            set => SetValue(value, Table.IsAvatarKey);
        }
    }
}

/*
 Template.cs
 
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

using DataWF.Common;
using DataWF.Data;
using System.IO;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    [DataContract, Table("rtemplate_file", "Template", BlockSize = 100)]
    public class TemplateFile : DBItem
    {
        public static DBTable<TemplateFile> DBTable
        {
            get { return GetTable<TemplateFile>(); }
        }

        public TemplateFile()
        {
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [DataMember, Column("template_file", Keys = DBColumnKeys.File)]
        public byte[] Data
        {
            get { return GetValue<byte[]>(Table.FileKey); }
            set { SetValue(value, Table.FileKey); }
        }

        [DataMember, Column("template_file_name", 1024, Keys = DBColumnKeys.FileName | DBColumnKeys.View | DBColumnKeys.Code)]
        public string DataName
        {
            get { return GetValue<string>(Table.FileNameKey); }
            set { SetValue(value, Table.FileNameKey); }
        }

        public string FileType
        {
            get { return Path.GetExtension(DataName); }
        }

        public Stream GetMemoryStream(DBTransaction transaction)
        {
            return GetZipMemoryStream(table.FileKey, transaction);
        }

        public FileStream GetFileStream(DBTransaction transaction)
        {
            return GetZipFileStream(table.FileKey, Helper.GetDocumentsFullPath(DataName, nameof(TemplateFile) + Id), transaction);
        }
    }
}

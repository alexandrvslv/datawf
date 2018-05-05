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

using System.ComponentModel;
using System.IO;
using DataWF.Data;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    [DataContract, Table("rtemplate_data", "Template", BlockSize = 100)]
    public class TemplateData : DBItem
    {
        public static DBTable<TemplateData> DBTable
        {
            get { return DBService.GetTable<TemplateData>(); }
        }

        public TemplateData()
        {
            Build(DBTable);
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Browsable(false)]
        [DataMember, Column("template_id")]
        public int? TemplateId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(TemplateId))]
        public Template Template
        {
            get { return GetPropertyReference<Template>(); }
            set { SetPropertyReference(value); }
        }

        [DataMember, Column("template_file")]
        public byte[] Data
        {
            get { return GetProperty<byte[]>(); }
            set { SetProperty(value); }
        }

        [DataMember, Column("template_file_name", 1024)]
        public string DataName
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public string FileType
        {
            get { return Path.GetExtension(DataName); }
        }
    }
}

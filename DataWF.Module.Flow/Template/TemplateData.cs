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

using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{

    [DataContract, Table("rtemplate_data", "Template", BlockSize = 100)]
    public class TemplateData : DBItem
    {
        private static DBTable<TemplateData> dbTable;
        private static DBColumn templateKey = DBColumn.EmptyKey;
        private static DBColumn fileKey = DBColumn.EmptyKey;
        private static DBColumn autoGenerateKey = DBColumn.EmptyKey;

        public static DBTable<TemplateData> DBTable => dbTable ?? (dbTable = GetTable<TemplateData>());
        public static DBColumn TemplateKey => DBTable.ParseProperty(nameof(TemplateId), ref templateKey);
        public static DBColumn FileKey => DBTable.ParseProperty(nameof(FileId), ref fileKey);
        public static DBColumn AutoGenerateKey => DBTable.ParseProperty(nameof(AutoGenerate), ref autoGenerateKey);

        private Template template;
        private TemplateFile templateFile;

        public TemplateData()
        {
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Browsable(false)]
        [DataMember, Column("template_id"), Index("rtemplate_data_index", true)]
        public int? TemplateId
        {
            get { return GetValue<int?>(TemplateKey); }
            set { SetValue(value, TemplateKey); }
        }

        [Reference(nameof(TemplateId))]
        public Template Template
        {
            get { return GetReference(TemplateKey, ref template); }
            set { template = SetReference(value, TemplateKey); }
        }

        [Browsable(false)]
        [Column("file_id", Keys = DBColumnKeys.View), Index("rtemplate_data_index", true)]
        public int? FileId
        {
            get { return GetValue<int?>(FileKey); }
            set { SetValue(value, FileKey); }
        }

        [Reference(nameof(FileId))]
        public TemplateFile File
        {
            get { return GetReference(FileKey, ref templateFile); }
            set { templateFile = SetReference(value, FileKey); }
        }

        [Column("auto_generate")]
        public bool? AutoGenerate
        {
            get { return GetValue<bool?>(AutoGenerateKey); }
            set { SetValue(value, AutoGenerateKey); }
        }
    }
}

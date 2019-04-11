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
using System.Linq;

namespace DataWF.Module.Flow
{
    [Table("rtemplate_reference", "Template")]
    public class TemplateReference : DBItem
    {
        private static DBTable<TemplateReference> dbTable;
        private static DBColumn templateKey = DBColumn.EmptyKey;
        private static DBColumn referenceKey = DBColumn.EmptyKey;

        public static DBTable<TemplateReference> DBTable => dbTable ?? (dbTable = GetTable<TemplateReference>());
        public static DBColumn TemplateKey => DBTable.ParseProperty(nameof(TemplateId), ref templateKey);
        public static DBColumn ReferenceKey => DBTable.ParseProperty(nameof(ReferenceId), ref referenceKey);

        private Template template;
        private Template reference;

        public TemplateReference()
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Column("template_id"), Index("rtemplate_reference_index", true)]
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

        [Column("reference_id"), Index("rtemplate_reference_index", true)]
        public int? ReferenceId
        {
            get { return GetValue<int?>(ReferenceKey); }
            set { SetValue(value, ReferenceKey); }
        }

        [Reference(nameof(ReferenceId))]
        public Template Reference
        {
            get { return GetReference(ReferenceKey, ref reference); }
            set { reference = SetReference(value, ReferenceKey); }
        }

        
    }
}

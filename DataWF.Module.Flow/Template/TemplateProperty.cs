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

namespace DataWF.Module.Flow
{
    [Table("rtemplate_property", "Template")]
    public class TemplateProperty : DBItem
    {
        private static DBTable<TemplateProperty> dbTable;
        private static DBColumn templateKey = DBColumn.EmptyKey;
        private static DBColumn propertyNameKey = DBColumn.EmptyKey;

        public static DBTable<TemplateProperty> DBTable => dbTable ?? (dbTable = GetTable<TemplateProperty>());
        public static DBColumn TemplateKey => DBTable.ParseProperty(nameof(TemplateId), ref templateKey);
        public static DBColumn PropertyNameKey => DBTable.ParseProperty(nameof(PropertyName), ref propertyNameKey);

        private Template template;

        public TemplateProperty()
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Column("template_id"), Index("rtemplate_property_index", true)]
        public int? TemplateId
        {
            get { return GetValue<int?>(TemplateKey); }
            set { SetValue(value, TemplateKey); }
        }

        [Reference(nameof(TemplateId))]
        public Template Template
        {
            get { return GetReference(TemplateKey, ref template); }
            set { SetReference(template = value, TemplateKey); }
        }

        [Column("property_name", 1024), Index("rtemplate_property_index", true)]
        public string PropertyName
        {
            get { return GetValue<string>(PropertyNameKey); }
            set { SetValue(value, PropertyNameKey); }
        }

    }
}

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
        public static readonly DBTable<TemplateProperty> DBTable = GetTable<TemplateProperty>();
        public static readonly DBColumn TemplateKey = DBTable.ParseProperty(nameof(TemplateId));
        public static readonly DBColumn PropertyNameKey = DBTable.ParseProperty(nameof(PropertyName));

        private Template template;

        public TemplateProperty()
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Column("template_id"), Index("rtemplate_property_index", true)]
        public int? TemplateId
        {
            get => GetValue<int?>(TemplateKey);
            set => SetValue(value, TemplateKey);
        }

        [Reference(nameof(TemplateId))]
        public Template Template
        {
            get => GetReference(TemplateKey, ref template);
            set => SetReference(template = value, TemplateKey);
        }

        [Column("property_name", 1024), Index("rtemplate_property_index", true)]
        public string PropertyName
        {
            get => GetValue<string>(PropertyNameKey);
            set => SetValue(value, PropertyNameKey);
        }

    }
}

/*
 DBService.cs
 
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
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class SqlDocument
    {
        private string text = "";
        private string schemaName;
        private DBSchema schema;

        public SqlDocument()
        {
            //Schema = DBService.DefaultSchema;
        }

        [XmlText]
        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        [Browsable(false)]
        public string SchemaName
        {
            get { return schemaName; }
            set { schemaName = value; }
        }

        [XmlIgnore, JsonIgnore]
        public DBSchema Schema
        {
            get { return schema ?? (schema = DBService.Schems[schemaName]); }
            set
            {
                if (schema != value)
                {
                    schema = value;
                    schemaName = schema?.Name;
                }
            }
        }
    }

}

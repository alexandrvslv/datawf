/*
 DBExport.cs
 
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
using System;

namespace DataWF.Data
{
    public class DBExportEnvironment : IFileSerialize
    {
        private DateTime stamp = DateTime.Now;

        public DBExportEnvironment()
        {
        }

        public DateTime Stamp
        {
            get { return stamp; }
            set { stamp = value; }
        }

        public DBExportList List { get; } = new DBExportList();

        #region IFSerialize implementation
        public void Save(string file)
        {
            Serialization.Serialize(this, file);
        }

        public void Save()
        {
            Save(FileName);
        }

        public void Load(string file)
        {
            Serialization.Deserialize(file, this);
        }

        public void Load()
        {
            Load(FileName);
        }

        public string FileName
        {
            get { return "export.xml"; }
            set { }
        }
        #endregion
    }
}

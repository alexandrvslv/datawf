/*
 Document.cs
 
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
using System;
using System.ComponentModel;

namespace DataWF.Module.Flow
{
    public class DocumentHandler : INotifyPropertyChanged
    {
        [NonSerialized()]
        private Document document;
        private string id;
        private DateTime date = DateTime.Now;

        public DocumentHandler()
        {
        }

        public override string ToString()
        {
            return Document == null ? base.ToString() : Document.ToString();
        }

        public DateTime Date
        {
            get => date;
            set
            {
                if (date == value)
                    return;
                date = value;
                OnPropertyChanged(nameof(Date));
            }
        }

        public string Id
        {
            get => id;
            set
            {
                if (id == value)
                    return;
                id = value;
                document = null;
                OnPropertyChanged(nameof(Id));
            }
        }

        public Document Document
        {
            get
            {
                if (document == null && id != null)
                    document = Document.DBTable.LoadById(id);
                return document;
            }
            set
            {
                Id = value?.PrimaryId.ToString();
                document = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}

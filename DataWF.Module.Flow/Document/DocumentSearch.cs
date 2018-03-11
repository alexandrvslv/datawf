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
using DataWF.Data;
using DataWF.Common;
using System.Collections.Generic;
using System.Linq;
using DataWF.Module.Common;

namespace DataWF.Module.Flow
{
    public class DocumentSearch : INotifyPropertyChanged, IDisposable
    {
        protected List<QParam> list = new List<QParam>();
        protected CheckedState work = CheckedState.Indeterminate;
        protected string template;
        protected string stage;
        protected string user;
        protected string number;
        protected DateInterval date;
        protected DocumentSearchDate dtype = DocumentSearchDate.Create;
        protected string description;

        public DocumentSearch()
            : base()
        {
        }

        public DateInterval Date
        {
            get { return date; }
            set
            {
                if (date == value)
                    return;
                date = value;
                OnPropertyChanged(nameof(Date));
            }
        }

        public DocumentSearchDate DateType
        {
            get { return dtype; }
            set
            {
                if (dtype == value)
                    return;
                dtype = value;
                OnPropertyChanged(nameof(DateType));
            }
        }

        public CheckedState IsWork
        {
            get { return work; }
            set
            {
                if (work == value)
                    return;
                work = value;
                OnPropertyChanged(nameof(IsWork));
            }
        }

        [Browsable(false)]
        public bool IsCurrent
        {
            get { return User == User.CurrentUser && IsWork == CheckedState.Checked; }
            set
            {
                user = User.CurrentUser?.Id.ToString();
                work = CheckedState.Checked;
                OnPropertyChanged("");
            }
        }

        public string Number
        {
            get { return number; }
            set
            {
                if (value == number)
                    return;
                number = value;
                OnPropertyChanged(nameof(Number));
            }
        }

        public Template Template
        {
            get { return Template.DBTable?.LoadById(template); }
            set
            {
                string id = null;
                if (value != null)
                    id = value.PrimaryId.ToString();
                if (id == template)
                    return;
                template = id;
                OnPropertyChanged(nameof(Template));
            }
        }

        public DBItem Stage
        {
            get
            {
                if (stage == null)
                    return null;
                int index = stage.IndexOf(':');
                return FlowEnvironment.Config.Schema.Tables[stage.Substring(0, index)].LoadItemById(stage.Substring(index + 1));
            }
            set
            {
                string id = null;
                string table = null;
                if (value != null)
                {
                    table = value.Table.Name;
                    id = value.PrimaryId.ToString();
                }
                if (id != stage)
                {
                    stage = id == null ? null : table + ":" + id;
                    OnPropertyChanged(nameof(Stage));
                }
            }
        }

        public User User
        {
            get { return User.DBTable.LoadById(user); }
            set
            {
                string id = null;
                if (value != null)
                    id = value.PrimaryId.ToString();
                if (id != user)
                {
                    user = id;
                    OnPropertyChanged(nameof(User));
                }
            }
        }

        [Browsable(false)]
        public bool StaffEmpty
        {
            get { return User == null; }
        }

        public string Description
        {
            get { return description; }
            set
            {
                description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        //[Browsable(false)]
        public List<QParam> Attributes
        {
            get { return list; }
        }

        public void Clear()
        {
            //document.Clear();
            list.Clear();
            work = CheckedState.Indeterminate;
            template = null;
            stage = null;
            user = null;
            number = null;
            date = new DateInterval();
            dtype = DocumentSearchDate.Create;
            description = null;
            OnPropertyChanged(string.Empty);
        }

        public void SetParam(DBItem value)
        {
            if (value == null)
                return;
            else if (value is Template)
                Template = (Template)value;
            else if (value is Stage || value is Work)
                Stage = value;
            else if (value is User)
                User = (User)value;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        public void Dispose()
        {

        }

        public QQuery QDoc = null;
        public QQuery QWork = null;

        public void Parse()
        {
            //var dconf = FlowEnvir.Config.Document;
            //var wconf = FlowEnvir.Config.DocumentWork;

            QDoc = new QQuery(string.Empty, Document.DBTable);
            foreach (var item in list)
                QDoc.Parameters.Add(item);
            //exp.Results.Add(new QColumn(dconf.Table.PrimaryKey.Code));
            ///exp.Results.Add(new QColumn(Config.Document.Table.StampKey.Code));

            QWork = new QQuery(string.Empty, DocumentWork.DBTable);
            QWork.Columns.Add(new QColumn(DocumentWork.DBTable.ParseColumn(nameof(DocumentWork.DocumentId))));

            if (IsCurrent)
                QDoc.BuildPropertyParam(nameof(Document.WorkId), CompareType.IsNot, DBNull.Value);


            if (Number != null && Number.Length > 0)
            {
                QParam group = null;
                if (Helper.IsDecimal(Number))
                {
                    group = QDoc.Parameters.Add();
                    group.Parameters.Add(QQuery.CreateParam(Document.DBTable.PrimaryKey, Number));
                }
                var paramNumber = QQuery.CreateParam(Document.DBTable.CodeKey, Number, CompareType.Like, group != null ? LogicType.Or : LogicType.And);
                if (group != null)
                    group.Parameters.Add(paramNumber);
                else
                    QDoc.Parameters.Add(paramNumber);
            }

            if (Description != null && Description.Length != 0)
                QDoc.BuildParam(Document.DBTable.Columns["name_ru"], Description);

            if (Date != null)
            {
                if (DateType == DocumentSearchDate.Document)
                    QDoc.BuildPropertyParam(nameof(Document.DocumentDate), CompareType.Equal, Date);
                else if (DateType == DocumentSearchDate.Create)
                    QDoc.BuildPropertyParam(nameof(Document.Date), CompareType.Equal, Date);
                else if (DateType == DocumentSearchDate.WorkBegin)
                    QWork.BuildPropertyParam(nameof(DocumentWork.Date), CompareType.Equal, Date);
                else if (DateType == DocumentSearchDate.WorkEnd)
                    QWork.BuildPropertyParam(nameof(DocumentWork.DateComplete), CompareType.Equal, Date);
                else if (DateType == DocumentSearchDate.History)
                {
                    var QLog = new QQuery("", UserLog.DBTable);
                    QLog.Columns.Add(new QColumn(UserLog.DBTable.ParseProperty(nameof(UserLog.DocumentId))));
                    QLog.BuildPropertyParam(nameof(UserLog.Date), CompareType.Equal, Date);
                    QLog.BuildPropertyParam(nameof(UserLog.UserId), CompareType.In, User.IsCompaund ? User.GetSubGroupFull<User>(true) : User.GetParents<User>(true));
                    QDoc.BuildParam(Document.DBTable.PrimaryKey, QLog);
                }
            }
            if (Template != null)
                QDoc.BuildPropertyParam(nameof(Document.TemplateId), CompareType.In, Template.GetSubGroupFull<Template>(true));

            if (Stage != null)
            {
                if (Stage is Work)
                {
                    QQuery QStage = new QQuery(string.Empty, Flow.Stage.DBTable);
                    QStage.BuildPropertyParam(nameof(Flow.Stage.WorkId), CompareType.Equal, Stage.PrimaryId);
                    QWork.BuildPropertyParam(nameof(DocumentWork.StageId), CompareType.In, QStage);
                }
                else
                {
                    QWork.BuildPropertyParam(nameof(DocumentWork.StageId), CompareType.Equal, Stage.PrimaryId);
                }
            }


            if (User != null && !User.IsCurrent)
                QWork.BuildPropertyParam(nameof(DocumentWork.UserId), CompareType.In, User.IsCompaund ? User.GetSubGroupFull<User>(true) : User.GetParents<User>(true));

            if (QWork.Parameters.Count > 0)
            {
                if (IsWork != CheckedState.Indeterminate && dtype != DocumentSearchDate.WorkEnd)
                    QWork.BuildPropertyParam(nameof(DocumentWork.IsComplete), CompareType.Equal, IsWork == CheckedState.Checked);
                QDoc.BuildParam(Document.DBTable.PrimaryKey, QWork);
            }
            else if (IsWork != CheckedState.Indeterminate)
                QDoc.BuildPropertyParam(nameof(Document.IsComplete), CompareType.Equal, IsWork == CheckedState.Checked);
        }

        public bool IsEmpty
        {
            get
            {
                return list.Count == 0 && work == CheckedState.Indeterminate &&
                template == null && stage == null && user == null && number == null && date == null && description == null;
            }
        }
    }
}

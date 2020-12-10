using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Module.Counterpart;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DataWF.Module.Flow
{
    public class DocumentFilter : INotifyPropertyChanged, IDisposable
    {
        protected List<QParam> list = new List<QParam>();
        protected CheckedState work = CheckedState.Indeterminate;
        private DBItem cacheUser;
        private DBItem cacheStage;
        private Customer customerCache;
        private string customer;
        private Template templateCache;
        protected string template;
        private Document referencingCache;
        private string referencing;
        protected string stage;
        protected string staff;
        protected string id;
        protected string number;
        protected DateInterval? date;
        protected DocumentSearchDate dtype = DocumentSearchDate.Create;
        protected string title;
        public QQuery QDoc;
        public QQuery QWork;
        private QParam paramCompleate;
        private QParam paramWork;
        private QParam paramWorkId;
        private QParam paramId;
        private QParam paramNumber;
        private QParam paramTitle;
        private QParam paramDate;
        private QParam paramTemplate;
        private QParam paramStage;
        private QParam paramStaff;
        private QParam paramCustomer;
        private QParam paramReferencing;

        public DocumentFilter()
            : base()
        {
            QDoc = new QQuery(string.Empty, Document.DBTable);
            QWork = new QQuery(string.Empty, DocumentWork.DBTable);
            QWork.Columns.Add(new QColumn(DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.DocumentId))));
            paramWork = new QParam(Document.DBTable.PrimaryKey, CompareType.In, QWork);
            paramWorkId = new QParam(Document.DBTable.ParseProperty(nameof(Document.CurrentWorkId)), CompareType.IsNot, null);
        }

        public DocumentSearchDate DateType
        {
            get => dtype;
            set
            {
                if (dtype == value)
                    return;
                dtype = value;

                var temp = date;
                date = null;
                Date = temp;
                OnPropertyChanged(nameof(DateType));
            }
        }

        public DateInterval? Date
        {
            get => date;
            set
            {
                if (date == value)
                    return;
                date = value;
                if (date != null)
                {
                    if (paramDate == null)
                    {
                        paramDate = QDoc.BuildPropertyParam(nameof(Document.DocumentDate), CompareType.Between, Date);
                        paramDate.IsDefault = true;
                    }
                    else
                    {
                        switch (DateType)
                        {
                            case DocumentSearchDate.Document: paramDate.LeftColumn = Document.DBTable.ParseProperty(nameof(Document.DocumentDate)); break;
                            case DocumentSearchDate.Create: paramDate.LeftColumn = Document.DBTable.ParseProperty(nameof(Document.DateCreate)); break;
                            case DocumentSearchDate.WorkBegin: paramDate.LeftColumn = DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.DateCreate)); break;
                            case DocumentSearchDate.WorkEnd: paramDate.LeftColumn = DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.DateComplete)); break;
                        }
                        paramDate.RightValue = Date;
                    }
                }
                OnPropertyChanged(nameof(Date));
            }
        }

        public CheckedState IsWork
        {
            get => work;
            set
            {
                if (work == value)
                    return;
                work = value;
                if (paramCompleate == null)
                {
                    paramCompleate = new QParam(Document.DBTable.ParseProperty(nameof(Document.IsComplete)), CompareType.Equal, IsWork != CheckedState.Checked);
                    paramCompleate.IsDefault = true;
                }
                else
                {
                    paramCompleate.RightValue = IsWork != CheckedState.Checked;
                }
                OnPropertyChanged(nameof(IsWork));
            }
        }

        public Document Referencing
        {
            get => referencingCache ?? (referencingCache = Document.DBTable.LoadById(referencing));
            set
            {
                if (Referencing == value)
                    return;
                referencingCache = value;
                referencing = value?.PrimaryId.ToString();
                if (referencing != null)
                {
                    if (paramReferencing == null)
                    {
                        paramReferencing = Document.CreateRefsParam(value.Id);
                        paramReferencing.IsDefault = true;
                    }
                    else
                    {
                        foreach (QParam param in paramReferencing.Parameters)
                        {
                            ((QQuery)param.RightItem).Parameters.First().RightValue = value.Id;
                        }
                    }
                }
                OnPropertyChanged(nameof(Referencing));
            }
        }

        [Browsable(false)]
        public bool IsCurrent
        {
            get => Staff == CurrentUser && IsWork == CheckedState.Checked;
            set
            {
                Staff = value ? CurrentUser : null;
                if (value)
                {
                    IsWork = CheckedState.Checked;
                }
                // OnPropertyChanged(nameof(IsCurrent));
            }
        }

        public Customer Customer
        {
            get => customerCache ?? (customerCache = Customer.DBTable.LoadById(customer));
            set
            {
                if (Customer == value)
                {
                    return;
                }
                customerCache = value;
                customer = value?.PrimaryId.ToString();
                if (value != null)
                {
                    if (paramCustomer == null)
                    {
                        paramCustomer = new QParam(Document.DBTable.ParseProperty(nameof(Document.Customer)), CompareType.Equal, value.Id);
                        paramCustomer.IsDefault = true;
                    }
                    else
                    {
                        paramCustomer.RightValue = value.Id;
                    }
                }
                OnPropertyChanged(nameof(Customer));
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
                if (value != null)
                {
                    if (paramId == null)
                    {
                        paramId = new QParam(Document.DBTable.PrimaryKey, Id);
                        paramId.IsDefault = true;
                    }
                    else
                    {
                        paramId.RightValue = Id;
                    }
                }
                OnPropertyChanged(nameof(Id));
            }
        }

        public string Number
        {
            get => number;
            set
            {
                if (value == number)
                    return;
                number = value;
                if (!string.IsNullOrEmpty(value))
                {
                    if (paramNumber == null)
                    {
                        paramNumber = new QParam(Document.DBTable.CodeKey, CompareType.Like, $"%{Number}%");
                        paramNumber.IsDefault = true;
                    }
                    else
                    {
                        paramNumber.RightValue = $"%{Number}%";
                    }
                    customer = null;
                    staff = null;
                    stage = null;
                    title = null;
                }
                OnPropertyChanged(nameof(Number));
            }
        }

        public Template Template
        {
            get => templateCache ?? (templateCache = Template.DBTable?.LoadById(template));
            set
            {
                string id = value?.PrimaryId.ToString();
                if (id == template)
                    return;
                template = id;
                templateCache = value;
                if (value != null)
                {
                    if (paramTemplate == null)
                    {
                        paramTemplate = new QParam(Document.DBTable.ParseProperty(nameof(Document.TemplateId)), CompareType.In, Template.GetSubGroupFull(true));
                        paramTemplate.IsDefault = true;
                    }
                    else
                    {
                        paramTemplate.RightValue = Template.GetSubGroupFull(true);
                    }
                }
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
                return cacheStage ?? (cacheStage = Work.DBTable.Schema.Tables[stage.Substring(0, index)].LoadItemById(stage.Substring(index + 1)));
            }
            set
            {
                if (Stage == value)
                    return;
                cacheStage = value;
                stage = value == null ? null : $"{value.Table.Name}:{ value.PrimaryId}";

                var column = Stage is Work
                    ? DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.WorkId))
                    : DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.StageId));
                if (value != null)
                {
                    if (paramStage == null)
                    {
                        paramStage = new QParam(column, Stage.PrimaryId);
                        paramStage.IsDefault = true;
                    }
                    else
                    {
                        paramStage.LeftColumn = column;
                        paramStage.RightValue = Stage.PrimaryId;
                    }
                }

                OnPropertyChanged(nameof(Stage));
            }
        }

        public DBItem Staff
        {
            get
            {
                if (staff == null)
                    return null;
                int index = staff.IndexOf(':');
                return cacheUser ?? (cacheUser = UserGroup.DBTable.Schema.Tables[staff.Substring(0, index)].LoadItemById(staff.Substring(index + 1)));
            }
            set
            {
                if (Staff == value)
                    return;
                cacheUser = value;
                staff = value == null ? null : $"{value.Table.Name}:{ value.PrimaryId}";
                var column = Staff is Department
                    ? DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.DepartmentId))
                    : Staff is Position
                    ? DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.PositionId))
                    : DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.UserId));
                if (value != null)
                {
                    if (paramStaff == null)
                    {
                        paramStaff = new QParam(column, Staff.PrimaryId);
                        paramStaff.IsDefault = true;
                    }
                    else
                    {
                        paramStaff.LeftColumn = column;
                        paramStaff.RightValue = Staff.PrimaryId;
                    }
                }
                OnPropertyChanged(nameof(Staff));
            }
        }

        public string Title
        {
            get => title;
            set
            {
                if (title == value)
                    return;
                title = value;
                if (!string.IsNullOrEmpty(value))
                {
                    if (paramTitle == null)
                    {
                        paramTitle = QDoc.BuildNameParam(nameof(Document.Title), CompareType.Like, $"%{Title}%");
                        paramTitle.IsDefault = true;
                    }
                    else
                    {
                        foreach (QParam param in paramTitle.Parameters)
                        {
                            param.RightValue = $"%{Title}%";
                        }
                    }
                    customer = null;
                    staff = null;
                    stage = null;
                    number = null;
                }
                OnPropertyChanged(nameof(Title));
            }
        }

        //[Browsable(false)]
        public List<QParam> Attributes
        {
            get { return list; }
        }

        public void Clear()
        {
            list.Clear();
            work = CheckedState.Indeterminate;
            template = null;
            stage = null;
            staff = null;
            id = null;
            number = null;
            date = null;
            dtype = DocumentSearchDate.Create;
            title = null;
            //IsCurrent = true;
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
            else if (value is User || value is Department || value is Position)
                Staff = value;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            Parse();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public void Dispose()
        {
            QDoc.Dispose();
            QWork.Dispose();
        }

        private void Parse()
        {
            QDoc.Parameters.Clear();
            foreach (var item in list)
            {
                QDoc.Parameters.Add(item);
            }
            QWork.Parameters.Clear();
            if (IsCurrent)
            {
                QDoc.Parameters.Add(paramWorkId);
            }
            if (!string.IsNullOrEmpty(Id))
            {
                QDoc.Parameters.Add(paramId);
            }
            if (!string.IsNullOrEmpty(Number))
            {
                QDoc.Parameters.Add(paramNumber);
            }
            if (!string.IsNullOrEmpty(Title))
            {
                QDoc.Parameters.Add(paramTitle);
            }
            if (Template != null)
            {
                QDoc.Parameters.Add(paramTemplate);
            }
            if (Customer != null)
            {
                QDoc.Parameters.Add(paramCustomer);
            }
            if (Date != null)
            {
                if (DateType == DocumentSearchDate.Document || DateType == DocumentSearchDate.Create)
                {
                    QDoc.Parameters.Add(paramDate);
                }
                else
                {
                    QWork.Parameters.Add(paramDate);
                }
            }
            if (Stage != null)
            {
                QWork.Parameters.Add(paramStage);
            }
            if (Staff != null && Staff != CurrentUser)
            {
                QWork.Parameters.Add(paramStaff);
            }
            if (IsWork != CheckedState.Indeterminate && !IsCurrent)
            {
                QDoc.Parameters.Add(paramCompleate);
            }
            if (Referencing != null)
            {
                QDoc.Parameters.Add(paramReferencing);
            }
            if (QWork.Parameters.Count > 0)
            {
                QDoc.Parameters.Add(paramWork);
            }
        }

        public bool IsEmpty
        {
            get { return QDoc.Parameters.Count == 0; }
        }

        public DBItem CurrentUser { get; set; }
    }
}

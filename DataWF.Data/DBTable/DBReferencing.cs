//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
using DataWF.Common;
using DataWF.Data;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [InvokerGenerator(Instance = true)]
    public partial class DBReferencing : DBTableItem
    {
        private DBTable referenceTable;
        private DBColumn referenceColumn;
        private string referenceTableName;
        private string referenceColumnName;
        private bool forceLoadReference;
        private PropertyInfo propertyInfo;
        private bool isSerializable;
        private JsonEncodedText? jsonName;

        [Browsable(false), XmlIgnore, JsonIgnore]
        public JsonEncodedText JsonName { get => jsonName ??= JsonEncodedText.Encode(Name, JavaScriptEncoder.UnsafeRelaxedJsonEscaping); }

        [XmlIgnore, JsonIgnore]
        public ReferencingGenerator Generator { get; internal set; }

        [XmlIgnore, JsonIgnore]
        public IInvoker PropertyInvoker { get; internal set; }

        [XmlIgnore, JsonIgnore]
        public PropertyInfo PropertyInfo
        {
            get => propertyInfo;
            internal set
            {
                propertyInfo = value;
                if (value != null)
                {
                    PropertyInvoker = EmitInvoker.Initialize(value, true);
                    IsSerializable = !TypeHelper.IsNonSerialize(value);
                }
            }
        }

        [JsonIgnore, XmlIgnore]
        public DBTable ReferenceTable
        {
            get => referenceTable ??= Schema?.GetTable(ReferenceTableName);
            set
            {
                if (referenceTable != value)
                {
                    referenceTable = value;
                    ReferenceTableName = value?.FullName;
                    OnPropertyChanged();
                }
            }
        }

        public string ReferenceTableName
        {
            get => referenceTableName;
            set
            {
                if (!string.Equals(referenceTableName, value, StringComparison.Ordinal))
                {
                    referenceTable = null;
                    referenceTableName = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore, XmlIgnore]
        public DBColumn ReferenceColumn
        {
            get => referenceColumn ??= ReferenceTable?.GetColumn(ReferenceColumnName);
            set
            {
                if (referenceColumn != value)
                {
                    referenceColumn = value;
                    ReferenceColumnName = value?.Name;
                    OnPropertyChanged();
                }
            }
        }

        public string ReferenceColumnName
        {
            get => referenceColumnName;
            set
            {
                if (!string.Equals(referenceColumnName, value, StringComparison.Ordinal))
                {
                    referenceColumn = null;
                    referenceColumnName = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ForceLoadReference
        {
            get => forceLoadReference;
            set
            {
                if (forceLoadReference != value)
                {
                    forceLoadReference = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSerializable
        {
            get => isSerializable;
            set
            {
                if (isSerializable != value)
                {
                    isSerializable = value;
                    OnPropertyChanged();
                }
            }
        }

        public override object Clone()
        {
            return new DBReferencing
            {
                Name = Name,
                ReferenceTableName = ReferenceTableName,
                ReferenceColumnName = ReferenceColumnName
            };
        }

        public override string FormatSql(DDLType ddlType, bool dependency = false)
        {
            return string.Empty;
        }
    }
}
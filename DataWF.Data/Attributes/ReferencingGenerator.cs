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
using System.Reflection;

[assembly: Invoker(typeof(ReferencingGenerator), nameof(ReferencingGenerator.PropertyName), typeof(ReferencingGenerator.PropertyNameInvoker))]
namespace DataWF.Data
{
    public class ReferencingGenerator
    {
        public ReferencingGenerator(TableGenerator table, PropertyInfo property, ReferencingAttribute referencingAttribuite)
        {
            if (!property.PropertyType.IsGenericType)
            {
                throw new InvalidOperationException($"{nameof(ReferencingAttribute)} Property type expect generic enumerable!");
            }

            var referenceTable = DBTable.GetTableAttributeInherit(property.PropertyType.GetGenericArguments()[0]);
            if (referenceTable == null)
            {
                throw new InvalidOperationException($"{nameof(ReferencingAttribute)} Property type expect {nameof(TableAttribute)}!");
            }

            var referenceColumn = referenceTable.GetColumnByProperty(referencingAttribuite.ReferenceProperty);
            if (referenceColumn == null)
            {
                throw new InvalidOperationException($"{nameof(ReferencingAttribute.ReferenceProperty)} expect {nameof(ColumnAttribute)}!");
            }
            Attribute = referencingAttribuite;
            Table = table;
            PropertyInfo = property;
            ReferenceTable = referenceTable;
            ReferenceColumn = referenceColumn;
            PropertyInvoker = EmitInvoker.Initialize(property, true);
        }

        public ReferencingAttribute Attribute { get; set; }
        public TableGenerator Table { get; set; }
        public PropertyInfo PropertyInfo { get; set; }
        public string PropertyName => PropertyInfo.Name;
        public TableGenerator ReferenceTable { get; set; }
        public ColumnGenerator ReferenceColumn { get; set; }
        public IInvoker PropertyInvoker { get; set; }

        public override string ToString()
        {
            return $"{PropertyName} {ReferenceTable?.Table}";
        }

        public class PropertyNameInvoker : Invoker<ReferencingGenerator, string>
        {
            public static readonly PropertyNameInvoker Instance = new PropertyNameInvoker();
            public override string Name => nameof(ReferencingGenerator.PropertyName);

            public override bool CanWrite => false;

            public override string GetValue(ReferencingGenerator target) => target.PropertyName;

            public override void SetValue(ReferencingGenerator target, string value) { }
        }
    }


}
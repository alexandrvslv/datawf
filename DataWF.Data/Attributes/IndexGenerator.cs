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
using System.Collections.Generic;

[assembly: Invoker(typeof(IndexGenerator), nameof(IndexGenerator.IndexName), typeof(IndexGenerator.IndexNameInvoker))]
[assembly: Invoker(typeof(IndexGenerator), nameof(IndexGenerator.Index), typeof(IndexGenerator.IndexInvoker))]
[assembly: Invoker(typeof(IndexGenerator), nameof(IndexGenerator.Attribute), typeof(IndexGenerator.AttributeInvoker))]
namespace DataWF.Data
{
    public class IndexGenerator
    {
        DBIndex cacheIndex;

        public IndexAttribute Attribute { get; set; }

        public string IndexName => Attribute?.IndexName;

        public List<ColumnGenerator> Columns { get; } = new List<ColumnGenerator>();

        public DBIndex Index
        {
            get => cacheIndex ?? (cacheIndex = Table?.Table?.Indexes[Attribute.IndexName]);
            set => cacheIndex = value;
        }

        public TableGenerator Table { get; set; }

        public DBIndex Generate()
        {
            if (Index != null)
                return Index;
            Index = new DBIndex()
            {
                Name = IndexName,
                Unique = Attribute.Unique,
                Table = Table.Table
            };
            foreach (var column in Columns)
            {
                Index.Columns.Add(column.Column);
            }
            Table.Table.Indexes.Add(Index);
            return Index;
        }

        public class IndexNameInvoker : Invoker<IndexGenerator, string>
        {
            public static readonly IndexNameInvoker Instance = new IndexNameInvoker();
            public override string Name => nameof(IndexGenerator.IndexName);

            public override bool CanWrite => false;

            public override string GetValue(IndexGenerator target) => target.IndexName;

            public override void SetValue(IndexGenerator target, string value) { }
        }

        public class IndexInvoker : Invoker<IndexGenerator, DBIndex>
        {
            public static readonly IndexInvoker Instance = new IndexInvoker();
            public override string Name => nameof(IndexGenerator.Index);

            public override bool CanWrite => false;

            public override DBIndex GetValue(IndexGenerator target) => target.Index;

            public override void SetValue(IndexGenerator target, DBIndex value) { target.Index = value; }
        }

        public class AttributeInvoker : Invoker<IndexGenerator, IndexAttribute>
        {
            public static readonly AttributeInvoker Instance = new AttributeInvoker();
            public override string Name => nameof(IndexGenerator.Attribute);

            public override bool CanWrite => false;

            public override IndexAttribute GetValue(IndexGenerator target) => target.Attribute;

            public override void SetValue(IndexGenerator target, IndexAttribute value) { target.Attribute = value; }
        }
    }


}

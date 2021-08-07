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
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Data
{
    public class ReplicationSettings
    {
        public RSInstance Instance { get; set; }
        public SelectableList<RSSchema> Schems { get; set; }
        public SelectableList<RSInstance> Instances { get; set; }

        public RSInstance GetInstance(string url)
        {
            return Instances.FirstOrDefault(p => string.Equals(p.Url, url, StringComparison.OrdinalIgnoreCase));
        }

        public RSInstance GetInstance(Uri url)
        {
            return Instances.FirstOrDefault(p => UrlComparer.Instance.Equals(p.UrlValue, url));
        }

        public RSSchema GetSchema(IDBSchema schema)
        {
            return Schems.FirstOrDefault(p => p.Schema == schema);
        }
    }

    public class UrlComparer : IEqualityComparer<Uri>
    {
        public static readonly UrlComparer Instance = new UrlComparer();
        public bool Equals(Uri x, Uri y)
        {
            if (x.Port != y.Port
                || !string.Equals(x.Scheme, y.Scheme))
                return false;
            return (string.Equals(x.Host, "localhost", StringComparison.OrdinalIgnoreCase)
               || string.Equals(x.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
               && (string.Equals(y.Host, "localhost", StringComparison.OrdinalIgnoreCase)
               || string.Equals(y.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase));
        }

        public int GetHashCode(Uri obj)
        {
            return obj.GetHashCode();
        }
    }
}

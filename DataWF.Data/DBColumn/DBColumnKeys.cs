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
using System;

namespace DataWF.Data
{
    [Flags()]
    public enum DBColumnKeys
    {
        None = 0,
        Primary = 1 << 0,
        Increment = 1 << 1,
        Stamp = 1 << 2,
        Date = 1 << 3,
        Group = 1 << 4,
        Code = 1 << 5,
        ItemType = 1 << 6,
        Image = 1 << 7,
        System = 1 << 8,
        Unique = 1 << 9,
        State = 1 << 10,
        Notnull = 1 << 11,
        Boolean = 1 << 12,
        Indexing = 1 << 13,
        Access = 1 << 14,
        View = 1 << 15,
        Culture = 1 << 16,
        Encript = 1 << 17,
        Password = 1 << 18,
        Reference = 1 << 19,
        TableReference = 1 << 20,
        NoLog = 1 << 21,
        ElementType = 1 << 22,
        File = 1 << 23,
        FileName = 1 << 24,
        FileLOB = 1 << 25,
        FileLastWrite = 1 << 26,
        UtcDate = 1 << 27
    }
}
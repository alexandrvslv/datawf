// 
// SpanStack.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Mono.TextEditor.Highlighting
{
    /// <summary>
    /// A fast stack used by spans in highlighting that is:
    /// * cloneable in constant time
    /// * equatable in constant time
    /// * when enumerating the items go from top to bottom (the .NET stack implementation does the opposite)
    /// </summary>
    public class CloneableStack<T> : IEnumerable<T>, ICollection<T>, ICloneable, IEquatable<CloneableStack<T>>
    {
        int count;
        StackItem top;

        #region IEnumerable[T] implementation
        public IEnumerator<T> GetEnumerator()
        {
            return new StackItemEnumerator(top);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new StackItemEnumerator(top);
        }
        #endregion

        #region ICloneable implementation
        public CloneableStack<T> Clone()
        {
            return new CloneableStack<T>
            {
                count = count,
                top = top
            };
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }
        #endregion

        public void Push(T item)
        {
            top = new StackItem(top, item);
            count++;
        }

        public T Peek()
        {
            return top.Item;
        }

        public T Pop()
        {
            T result = top.Item;
            top = top.Parent;
            count--;
            return result;
        }

        #region IEquatable[T] implementation
        public bool Equals(CloneableStack<T> other)
        {
            return ReferenceEquals(top, other.top);
        }
        #endregion

        #region ICollection[T] implementation
        void ICollection<T>.Add(T item)
        {
            Push(item);
        }

        void ICollection<T>.Clear()
        {
            top = null;
            count = 0;
        }

        bool ICollection<T>.Contains(T item)
        {
            foreach (T t in this)
            {
                if (t.Equals(item))
                    return true;
            }
            return false;
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            int idx = arrayIndex;
            foreach (T t in this)
            {
                array[idx++] = t;
            }
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get
            {
                return count;
            }
        }

        bool ICollection<T>.IsReadOnly
        {
            get
            {
                return false;
            }
        }
        #endregion

        class StackItem
        {
            public readonly StackItem Parent;
            public readonly T Item;

            public StackItem(StackItem parent, T item)
            {
                this.Parent = parent;
                this.Item = item;
            }
        }

        class StackItemEnumerator : IEnumerator<T>
        {
            StackItem cur, first;

            public StackItemEnumerator(StackItem cur)
            {
                this.cur = first = new StackItem(cur, default(T));
            }

            #region IDisposable implementation
            void IDisposable.Dispose()
            {
                cur = first = null;
            }
            #endregion

            #region IEnumerator implementation
            bool IEnumerator.MoveNext()
            {
                if (cur == null)
                    return false;
                cur = cur.Parent;
                return cur != null;
            }

            void IEnumerator.Reset()
            {
                cur = first;
            }

            object IEnumerator.Current
            {
                get
                {
                    return cur.Item;
                }
            }
            #endregion

            #region IEnumerator[T] implementation
            T IEnumerator<T>.Current
            {
                get
                {
                    return cur.Item;
                }
            }
            #endregion
        }
    }
}


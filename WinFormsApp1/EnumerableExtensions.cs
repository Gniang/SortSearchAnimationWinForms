using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsApp1
{
    public static partial class EnumerableExtensions
    {
        /// <summary>
        /// インデックス付要素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public struct IndexedEnumerable<T> : IEnumerable<(T item, int index)>
        {
            private IEnumerable<T> _e;

            public IndexedEnumerable(IEnumerable<T> e) { _e = e; }

            public IndexedEnumerator<T> GetEnumerator() => new IndexedEnumerator<T>(_e.GetEnumerator());

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            IEnumerator<(T item, int index)> IEnumerable<(T item, int index)>.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// インデックス付要素
        /// </summary>
        public struct IndexedEnumerator<T> : IEnumerator<(T item, int index)>
        {
            public (T item, int index) Current => (_e.Current, _i);

            int _i;
            IEnumerator<T> _e;

            internal IndexedEnumerator(IEnumerator<T> e)
            {
                _i = -1;
                _e = e;
            }

            public bool MoveNext()
            {
                _i++;
                return _e.MoveNext();
            }

            object IEnumerator.Current => Current;
            public void Dispose() { }
            public void Reset() { throw new NotImplementedException(); }
        }

        /// <summary>
        /// インデックス付要素を取得する
        /// </summary>
        public static IndexedEnumerable<T> Indexed<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return new IndexedEnumerable<T>(source);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsApp1
{

    /// <summary>処理を途中で止めるツール</summary>
    public class StepWaiter : ISteper
    {
        public const bool EnableStepWaiter = true;

        private Task waiter = Task.CompletedTask;
        private TaskCompletionSource tcs = new TaskCompletionSource();

        public async ValueTask Wait()
        {
            if (EnableStepWaiter)
            {
                tcs = new TaskCompletionSource();
                waiter = tcs.Task;
                await waiter;
            }
            else
            {
                return;
            }
        }

        public void Next()
        {
            if (!tcs.Task.IsCompleted)
            {
                tcs.SetResult();
            }
        }
    }
    public interface ISteper
    {
        void Next();
    }


    /// <summary>ソート状況をトラッキングするためのデータ</summary>
    public class QuickSortProgress : StepWaiter, ISortProgress
    {
        public BTree<IList<int>> Tree { get; set; }
        public int StepCounter { get; private set; }
        public IList<int> SortingItems => Tree.Flatten().SelectMany(x => x).ToArray();
        public IList<int> SortRanges { get; set; } = Array.Empty<int>();

        public (int Base, int Comparing)? Comparings { get; private set; }

        public void CompareFinished()
        {
            Comparings = null;
        }
        public void Compare(int baseVal, int comapringVal)
        {
            Comparings = (baseVal, comapringVal);
        }
        public void CompareNext(int baseVal, int comapringVal)
        {
            StepCounter++;
            Comparings = (baseVal, comapringVal);
        }
    }
    /// <summary>ソート状況をトラッキングするためのデータ</summary>
    public class SelectionSortProgress : StepWaiter, ISortProgress
    {
        public IList<int> SortingItems { get; set; } = Array.Empty<int>();
        public IList<int> SortRanges { get; set; } = Array.Empty<int>();
        public int StepCounter { get; private set; } = 0;
        public (int Base, int Comparing)? Comparings { get; private set; }

        public void CompareNext(int baseVal, int comapringVal)
        {
            StepCounter++;
            Comparings = (baseVal, comapringVal);
        }
        public void CompareFinished()
        {
            Comparings = null;
        }
    }

    /// <summary>ソート状況をトラッキングするためのデータ</summary>
    public interface ISortProgress : ISteper
    {
        IList<int> SortingItems { get; }
        IList<int> SortRanges { get; }
        (int Base, int Comparing)? Comparings { get; }

        int StepCounter { get; }

    }

    /// <summary>2分探索状況をトラッキングするためのデータ</summary>
    public class BinarySearchProgress : StepWaiter
    {
        public int Min { get; private set; } = 0;
        public int Max { get; private set; } = int.MaxValue;
        public int Mid => (Min + Max) / 2;

        public void SetStatus(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }

    /// <summary>2分木 クイックソートのデータ管理に仕方なくそれっぽいものを用意。なぜ標準ライブラリにないのか。</summary>
    public class BTree<T>
    {
        public T Value { get; set; }
        public BTree<T>? Left { get; private set; }
        public BTree<T>? Right { get; private set; }
        public BTree<T>? Parent { get; private set; }

        public BTree(T value)
        {
            this.Value = value;
        }

        [MemberNotNull(nameof(Left))]
        public BTree<T> SetLeft(T value)
        {
            var node = new BTree<T>(value) { Parent = this };
            this.Left = node;
            return node;
        }

        [MemberNotNull(nameof(Right))]
        public BTree<T> SetRight(T value)
        {
            var node = new BTree<T>(value) { Parent = this };
            this.Right = node;
            return node;
        }

        /// <summary>木構造を折りたたんで、一列に並べる。　左-親-右 の順</summary>
        public IEnumerable<T> Flatten()
        {
            if (Left != null && Right != null)
            {
                return Left.Flatten().Concat(new T[] { Value }).Concat(Right.Flatten());
            }
            else if (Left != null)
            {
                return Left.Flatten().Concat(new T[] { Value });
            }
            else if (Right != null)
            {
                return (new T[] { Value }).Concat(Right.Flatten());
            }
            return new[] { Value };
        }
    }

}

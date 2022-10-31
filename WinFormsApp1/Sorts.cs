using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsApp1
{
    public static class Sorts
    {

        /// <summary>クイックソート (進捗機能あり) 「SortProgress, BTree<IList<int>>」はソート処理に関係ない進捗機能用。</summary>
        public static async ValueTask<List<int>> QuickSort(IList<int> values, QuickSortProgress p, BTree<IList<int>> partOfSortArea)
        {

            if (values == null || !values.Any())
            {
                return new List<int>();
            }

            var firstElement = values[0];
            var rest = values.Skip(1);

            var smallerElements = new List<int>();
            var largerElements = new List<int>();
            foreach (var x in rest)
            {
                if (x < firstElement)
                {
                    smallerElements.Add(x);
                }
                else
                {
                    largerElements.Add(x);
                };


                // 進捗表示 値が大きいとき後ろに追いやるので、それを視覚化する。（小さいときは移動しないので表示スキップ）
                p.Compare(firstElement, x);
                if (x >= firstElement) await p.Wait();
                var restOfTargets = rest.Except(smallerElements.Concat(largerElements));
                partOfSortArea.Value = new int[] { firstElement }
                    .Concat(smallerElements)
                    .Concat(restOfTargets)
                    .Concat(largerElements)
                    .ToArray();
                p.CompareNext(firstElement, x);
                await p.Wait();
            }
            p.CompareFinished();



            partOfSortArea.Value = new[] { firstElement };
            partOfSortArea.SetLeft(smallerElements);
            partOfSortArea.SetRight(largerElements);
            //p.SortRanges = values;
            //await p.Wait();



            p.SortRanges = smallerElements;
            var sortedSmallers = await QuickSort(smallerElements, p, partOfSortArea.Left);
            partOfSortArea.SetLeft(sortedSmallers);

            p.SortRanges = largerElements;
            var sortedLargers = await QuickSort(largerElements, p, partOfSortArea.Right);
            partOfSortArea.SetRight(sortedLargers);



            var sorted = sortedSmallers
                .Concat(new int[] { firstElement })
                .Concat(sortedLargers)
                .ToList();

            //p.SortRanges = sorted;
            //await p.Wait();
            return sorted;
        }



        /// <summary>選択ソート (進捗機能あり) unstable</summary>
        public static async ValueTask<List<int>> SelectionSort(IList<int> values, SelectionSortProgress p)
        {

            if (values == null || !values.Any())
            {
                return new List<int>();
            }

            List<int>? rests = values.ToList();
            var result = new List<int>();
            foreach (var i in Enumerable.Range(0, values.Count))
            {
                var min = int.MaxValue;
                int minIdx = 0;
                p.SortingItems = result.Concat(rests).ToArray();
                p.SortRanges = p.SortingItems.Skip(i).ToList();
                foreach (var j in Enumerable.Range(0, rests.Count))
                {
                    if (min >= rests[j])
                    {
                        min = rests[j];
                        minIdx = j;
                    }
                    p.CompareNext(min, rests[j]);
                    await p.Wait();

                }
                result.Add(min);
                rests.RemoveAt(minIdx);
            }

            await p.Wait();
            return result;
        }
    }
}

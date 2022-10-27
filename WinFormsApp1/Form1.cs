using SkiaSharp;
using System.Collections.ObjectModel;
using WinFormsApp1;
using static WinFormsApp1.Form1;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public class Item
        {
            public int Idx { get; set; }
            public int Hoge { get; set; }
        }

        //
        // fields 
        //
        private readonly ToolTip tooltip;
        private List<Item> items;
        private int? searchTargetIdx;
        private BinarySearchProgress? binarySearchProgress;
        private IEnumerable<DrawItem> drawItems;
        private SortProgress? sortProgress;

        /// <summary>クイックソート (進捗機能あり) 「SortProgress, BTree<IList<int>>」はソート処理に関係ない進捗機能用。都度配列を作ってるのも進捗表示のため。</summary>
        public async ValueTask<List<int>> QuickSort(IList<int> values, SortProgress p, BTree<IList<int>> partOfSortArea)
        {

            if (values == null || !values.Any())
            {
                return new List<int>();
            }

            var firstElement = values[0];
            var rest = values.Skip(1).ToArray();

            var smallerElements = rest.Where(i => i < firstElement).ToArray();
            var largerElements = rest.Where(i => i >= firstElement).ToArray();

            partOfSortArea.Value = new[] { firstElement };
            partOfSortArea.SetLeft(smallerElements);
            partOfSortArea.SetRight(largerElements);
            p.SortRange = values;
            await p.Wait();

            p.SortRange = smallerElements;
            var sortedSmallers = await QuickSort(smallerElements, p, partOfSortArea.Left);
            partOfSortArea.SetLeft(sortedSmallers);

            p.SortRange = largerElements;
            var sortedLargers = await QuickSort(largerElements, p, partOfSortArea.Right);
            partOfSortArea.SetRight(sortedLargers);

            var sorted = sortedSmallers
                .Concat(new int[] { firstElement })
                .Concat(sortedLargers)
                .ToList();

            p.SortRange = sorted;
            await p.Wait();
            return sorted;
        }

        /// <summary>バイナリサーチ（進捗機能あり）「BinarySearchProgress」は探索処理に関係ない進捗機能用</summary>
        private async ValueTask<(bool isFind, int? findIndex)> BinarySearch(int t, IList<int> a, BinarySearchProgress p)
        {
            return await BinarySearchCore(t, a, 0, a.Count, p);
        }

        /// <summary>バイナリサーチ 内部処理</summary>
        private async ValueTask<(bool isFind, int? findIndex)> BinarySearchCore(int t, IList<int> a, int min, int max, BinarySearchProgress p)
        {
            p.SetStatus(min, max);
            await p.Wait();

            int mid = (max + min) / 2;
            if (a[mid] == t)
            {
                return (true, mid);
            }
            if (a[mid] > t)
            {
                return await BinarySearchCore(t, a, min, mid - 1, p);
            }
            if (max >= min)
            {
                return await BinarySearchCore(t, a, mid + 1, max, p);
            }
            return (false, null);
        }

        public Form1()
        {
            //
            // データ準備 適当に1000件
            //
            var r = new Random();
            var baseItems = CreateItems(Enumerable.Range(0, 500));
            this.items = baseItems;


            //
            // 画面表示コントロールの準備
            //
            // ソート操作欄
            var sortPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
            };
            var sortStartBtn = new Button
            {
                Margin = new Padding(20, 0, 0, 0),
                Width = 120,
                Text = "Setup Sort"
            };
            var sortStepBtn = new Button
            {
                Margin = new Padding(20, 0, 0, 0),
                Width = 120,
                Text = "Sort Next Step"
            };
            var randomSortBtn = new Button
            {
                Margin = new Padding(50, 0, 0, 0),
                Width = 120,
                Text = "To Random Order"
            };

            // 検索操作欄
            var searchPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
            };
            var binarySearchTargetTxt = new TextBox
            {
                Width = 220,
                PlaceholderText = $"0~{items.Count - 1}の範囲で検索対象を入力"
            };

            var searchStartBtn = new Button
            {
                Margin = new Padding(20, 0, 0, 0),
                Width = 120,
                Text = "Setup Binary Search"
            };
            var searchStepBtn = new Button
            {
                Margin = new Padding(50, 0, 0, 0),
                Width = 120,
                Text = "Search Next Step"
            };
            // データ描画欄
            var pic = new PictureBox()
            {
                Dock = DockStyle.Fill,
            };
            sortPanel.Controls.AddRange(new Control[] { sortStartBtn, sortStepBtn, randomSortBtn });
            searchPanel.Controls.AddRange(new Control[] { binarySearchTargetTxt, searchStartBtn, searchStepBtn });
            this.Controls.AddRange(new Control[] { pic, searchPanel, sortPanel });
            this.WindowState = FormWindowState.Maximized;
            this.tooltip = new ToolTip();


            //
            // 操作イベントの定義
            //

            // ソート処理の開始
            sortStartBtn.Click += async (s, e) =>
            {
                var it = items.Select(x => x.Idx).ToArray();
                var p = new SortProgress() { Tree = new BTree<IList<int>>(it), SortRange = it };
                this.sortProgress = p;
                pic.Refresh();
                var sorted = await QuickSort(it, p, p.Tree);
                this.items = CreateItems(sorted);
                sortProgress = null;
                pic.Refresh();
            };

            // ソート処理を1ステップ進める
            sortStepBtn.Click += (s, e) =>
            {
                if (sortProgress != null)
                {
                    this.sortProgress.Next();
                    pic.Refresh();
                }
            };

            // データをランダム順に並び変える
            randomSortBtn.Click += (s, e) =>
            {
                this.items = this.items
                    .Select(x => (r: r.Next(), v: x))
                    .OrderBy(x => x.r)
                    .Select(x => new Item() { Hoge = x.v.Hoge, Idx = x.v.Idx })
                    .ToList();
                sortProgress = null;
                pic.Refresh();
            };

            // テキストの数値番号が示すデータを検索対象に設定する
            binarySearchTargetTxt.TextChanged += (s, e) =>
            {
                if (int.TryParse(binarySearchTargetTxt.Text, out int i))
                {
                    searchTargetIdx = i;
                }
                else
                {
                    searchTargetIdx = null;
                }
                pic.Refresh();
            };

            // 検索処理を開始する
            searchStartBtn.Click += async (s, e) =>
            {
                if (this.searchTargetIdx.HasValue)
                {
                    var p = new BinarySearchProgress();
                    p.SetStatus(0, items.Count);
                    this.binarySearchProgress = p;
                    pic.Refresh();
                    await BinarySearch(this.searchTargetIdx.Value, items.Select(x => x.Idx).ToArray(), p);
                }
            };

            // 検索処理を次の1ステップ進める
            searchStepBtn.Click += (s, e) =>
            {
                if (binarySearchProgress != null)
                {
                    this.binarySearchProgress.Next();
                    pic.Refresh();
                }
            };

            // 画面サイズ変えたら再描画する
            pic.Resize += (s, e) =>
            {
                pic.Refresh();
            };

            // データ表示
            pic.Paint += (s, e) =>
            {
                var bsp = this.binarySearchProgress;
                var sp = this.sortProgress;
                var target = this.searchTargetIdx;

                IList<Item> items = this.items;
                if (sp != null)
                {
                    items = CreateItems(sp.Tree.Flatten().SelectMany(x => x));
                }
                // 表示初期化
                var g = e.Graphics;
                var c = e.ClipRectangle;
                g.Clear(BackColor);

                // 並べられるの表示データを作成する（色と表示位置（形））
                var w = (float)c.Width / items.Count;
                var drawItems = items
                    .Select((x, idx) =>
                    {
                        // 描画図形の領域を求める
                        var h = c.Height * ((float)x.Idx / items.Count);
                        var area = new RectangleF() { X = idx * w, Y = c.Height - h, Height = h, Width = w };

                        // 条件に応じて色決める
                        // ソート時
                        //   ソート操作範囲：シアン
                        // 検索時
                        //   検索対象：オレンジ、検索対象が見つかった：マゼンタ、
                        //   2分探索範囲 Min側：緑、 Mid:黄色、 Max側：青
                        var fillColor = Brushes.White;
                        if (idx == target)
                        {
                            fillColor = Brushes.Orange;
                        }
                        if (sp != null)
                        {
                            if (sp.SortRange.Contains(idx))
                            {
                                fillColor = Brushes.Cyan;
                            }
                        }
                        else if (bsp != null)
                        {
                            if (x.Idx == bsp.Mid && idx == target)
                            {
                                fillColor = Brushes.Magenta;
                            }
                            else if (x.Idx == target)
                            {
                                fillColor = Brushes.Orange;
                            }
                            else if (idx == bsp.Mid)
                            {
                                fillColor = Brushes.Yellow;
                            }
                            else if (bsp.Min <= idx && idx < bsp.Mid)
                            {
                                fillColor = Brushes.PaleGreen;
                            }
                            else if (bsp.Mid < idx && idx <= bsp.Max)
                            {
                                fillColor = Brushes.PaleTurquoise;
                            }
                        }
                        return new DrawItem() { FillColor = fillColor, Area = area, Item = x };
                    });
                this.drawItems = drawItems;

                // 作った表示データを描画する(色味ごとにまとめて描画)
                foreach (var gp in drawItems.GroupBy(x => x.FillColor))
                {
                    var rects = gp.Select(x => x.Area).ToArray();
                    g.FillRectangles(gp.Key, rects);
                    g.DrawRectangles(Pens.LightGray, rects);
                }
            };

            // マウス操作時にデータの番号をツールチップで出す
            pic.MouseMove += (s, e) =>
            {
                if (this.drawItems != null)
                {
                    var d = this.drawItems
                        .FirstOrDefault(x => x.Area.Contains(e.Location));
                    if (d != null)
                    {
                        tooltip.Show($"item:{d.Item.Idx}", pic, e.X - 25, e.Y - 18);
                    }
                    else
                    {
                        tooltip.Hide(pic);
                    }
                }
            };

        }

        private List<Item> CreateItems(IEnumerable<int> items)
        {
            return items.Select(x => new Item { Idx = x, Hoge = x }).ToList();
        }

        /// <summary>描画データ</summary>
        public class DrawItem
        {
            public RectangleF Area { get; init; }
            public Item Item { get; init; }
            public Brush FillColor { get; init; }
        }

        /// <summary>処理を途中で止めるツール</summary>
        public class StepWaiter
        {
            private Task waiter = Task.CompletedTask;
            private TaskCompletionSource tcs = new TaskCompletionSource();

            public async ValueTask Wait()
            {
                tcs = new TaskCompletionSource();
                waiter = tcs.Task;
                await waiter;
            }

            public void Next()
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.SetResult();
                }
            }
        }

        /// <summary>ソート状況をトラッキングするためのデータ</summary>
        public class SortProgress : StepWaiter
        {
            public BTree<IList<int>> Tree { get; set; }
            public IList<int> SortRange { get; set; } = Array.Empty<int>();
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

            public BTree<T> SetLeft(T value)
            {
                var node = new BTree<T>(value) { Parent = this };
                this.Left = node;
                return node;
            }
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
}
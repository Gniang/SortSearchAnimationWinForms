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
            public int SortingValue { get; set; }
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
        private ISortProgress? sortProgress;



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
            // ソートの進捗描画は同じ数値のデータが複数あると、変な動きをする
            // →対応するとソート処理に関係ないデータ構造や構文が話に混ざってくるので、あえて無視してる。

            //
            // データ準備 適当数
            //
            var r = new Random();
            var baseItems = CreateItems(Enumerable.Range(0, 30));
            this.items = baseItems;


            //
            // 画面表示コントロールの準備
            //
            // ソート操作欄
            this.Font = new Font("Yu Gothic UI", 12);
            var sortPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                WrapContents = false,
                Height = 60,
            };
            var quickSortStartBtn = new Button
            {
                Padding = new Padding(20, 0, 0, 0),
                Width = 120,
                Text = "Setup QuickSort",
            };
            var selectionSortStartBtn = new Button
            {
                Margin = new Padding(20, 0, 0, 0),
                Width = 120,
                Text = "Setup SelecitonSort"
            };
            var sortStepBtn = new Button
            {
                Margin = new Padding(30, 0, 0, 0),
                Width = 120,
                Text = "Sort Next Step"
            };
            var randomSortBtn = new Button
            {
                Margin = new Padding(50, 0, 0, 0),
                Width = 120,
                Text = "To Random Order"
            };
            var sortInfoTxt = new TextBoxEx
            {
                Margin = new Padding(50, 0, 0, 0),
                Width = 300,
                ReadOnly = true,
                BackColor = Color.White,
            };

            // 検索操作欄
            var searchPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                WrapContents = false,
                Height = 60,
            };
            var binarySearchTargetTxt = new TextBoxEx
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

            Action<List<Control>> autoHeight = (items) =>
            {
                items.ForEach(x => x.Height = x.Parent.Height - x.Parent.Padding.Vertical);
            };
            sortPanel.Controls.AddRange(new Control[] { quickSortStartBtn, selectionSortStartBtn, sortStepBtn, randomSortBtn, sortInfoTxt });
            sortPanel.Resize += (s, e) => { autoHeight.Invoke(new List<Control>(sortPanel.Controls.Cast<Control>())); };
            searchPanel.Controls.AddRange(new Control[] { binarySearchTargetTxt, searchStartBtn, searchStepBtn });
            searchPanel.Resize += (s, e) => { autoHeight.Invoke(new List<Control>(searchPanel.Controls.Cast<Control>())); };
            this.Controls.AddRange(new Control[] { pic, searchPanel, sortPanel });
            this.WindowState = FormWindowState.Maximized;
            this.MinimumSize = new Size(720, 480);
            this.tooltip = new ToolTip();
            this.Load += (s, e) => { randomSortBtn.Focus(); };

            //
            // 操作イベントの定義
            //
            Action showSortInfo = () =>
            {
                if (this.sortProgress != null)
                {
                    sortInfoTxt.Text = $"ループ処理：{this.sortProgress?.StepCounter}回目";
                }
            };
            // ソート処理の開始
            quickSortStartBtn.Click += async (s, e) =>
            {
                var it = items.Select(x => x.SortingValue).ToArray();
                var p = new QuickSortProgress() { Tree = new BTree<IList<int>>(it), SortRanges = it };
                this.sortProgress = p;
                pic.Refresh();
                sortStepBtn.Focus();
                showSortInfo.Invoke();
                var sorted = await Sorts.QuickSort(it, p, p.Tree);
                this.items = CreateItems(sorted);
                sortProgress = null;
                pic.Refresh();
            };
            selectionSortStartBtn.Click += async (s, e) =>
            {
                var it = items.Select(x => x.SortingValue).ToArray();
                var p = new SelectionSortProgress() { SortingItems = it, SortRanges = it };
                this.sortProgress = p;
                pic.Refresh();
                sortStepBtn.Focus();
                showSortInfo.Invoke();
                var sorted = await Sorts.SelectionSort(it, p);
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
                    showSortInfo.Invoke();
                }
            };

            // データをランダム順に並び変える
            randomSortBtn.Click += (s, e) =>
            {
                this.items = this.items
                    .Select(x => (r: r.Next(), v: x))
                    .OrderBy(x => x.r)
                    .Select(x => new Item() { Hoge = x.v.Hoge, SortingValue = x.v.SortingValue })
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
                    searchStepBtn.Focus();
                    await BinarySearch(this.searchTargetIdx.Value, items.Select(x => x.SortingValue).ToArray(), p);
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
                    items = CreateItems(sp.SortingItems);
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
                        //高さ0のデータができないよう＋１かさ増し
                        var h = c.Height * (((float)x.SortingValue + 1) / (items.Count + 1));
                        var area = new RectangleF() { X = idx * w, Y = c.Height - h, Height = h, Width = w };

                        // 条件に応じて色決める
                        // ソート時
                        //   ソート操作範囲：シアン, ソート中の比較してる2点： 基準値_マゼンタ, 比較値_オレンジ
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

                            if (sp.Comparings?.Base == x.SortingValue)
                            {
                                fillColor = Brushes.Magenta;
                            }
                            else if (sp.Comparings?.Comparing == x.SortingValue)
                            {
                                fillColor = Brushes.Orange;
                            }
                            else if (sp.SortRanges.Contains(x.SortingValue))
                            {
                                fillColor = Brushes.Cyan;
                            }
                        }
                        else if (bsp != null)
                        {
                            if (x.SortingValue == bsp.Mid && idx == target)
                            {
                                fillColor = Brushes.Magenta;
                            }
                            else if (x.SortingValue == target)
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
                        tooltip.Show($"item:{d.Item.SortingValue}", pic, e.X - 25, e.Y - 18);
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
            return items.Select(x => new Item { SortingValue = x, Hoge = x }).ToList();
        }

        /// <summary>描画データ</summary>
        public class DrawItem
        {
            public RectangleF Area { get; init; }
            public Item Item { get; init; }
            public Brush FillColor { get; init; }
        }

    }
}

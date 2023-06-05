using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfLib;

namespace NoteApp
{
    /// <summary>
    /// FuncPlotxaml.xaml の相互作用ロジック
    /// </summary>
    public partial class FuncPlot : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private WindowState mWindowState = WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)

        public enum FUNCTYPE {                              //  関数の種類(一般、媒介変数,極方程式)
            Normal, Parametric, Polar
        };
        public FUNCTYPE mFuncType = FUNCTYPE.Normal;
        public List<string> mFuncList;                      //  計算式リスト

        public string mXminStr = "0";                       //  グラフ表示エリア
        public string mXmaxStr = "100";
        public string mYminStr = "0";
        public string mYmaxStr = "100";
        public string mDivCountStr = "50";                  //  関数グラフの分割数
        public double mXmin = 0;                            //  グラフ表示エリア
        public double mXmax = 10;
        public double mYmin = 0;
        public double mYmax = 100;
        public double mTmin = 0;
        public double mTmax = 100;
        public int mDivCount = 50;                          //  関数グラフの分割数
        public bool mAutoHeight = true;                     //  Y軸値自動
        public bool mAspectFix = false;                     //  アスペクト比固定
        public Brush mBackColor = Brushes.White;
        private List<List<Point>> mPlotDatas;               //  表示用座標(x,y)データ
        private double mTextSize = 13;
        private YWorldShapes ydraw;                         //  グラフィックライブラリ
        private YLib ylib = new YLib();                     //  単なるライブラリ

        public FuncPlot()
        {
            InitializeComponent();

            mWindowWidth = Width;
            mWindowHeight = Height;
            mPrevWindowWidth = mWindowWidth;

            WindowFormLoad();       //  Windowの位置とサイズを復元

            ydraw = new YWorldShapes(canvas);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (mFuncList != null)
                tbFunction.Text = string.Join(";", mFuncList);
            if (mFuncType == FUNCTYPE.Normal) rbNormal.IsChecked = true;
            else if (mFuncType == FUNCTYPE.Parametric) rbParametric.IsChecked = true;
            else if (mFuncType == FUNCTYPE.Polar) rbPolar.IsChecked = true;
            if (mFuncType == FUNCTYPE.Normal)
                lbXmin.Content = "範囲 x min";
            else
                lbXmin.Content = "範囲 t min";
            tbXmin.Text = mXminStr;
            tbXmax.Text = mXmaxStr;
            tbDivCount.Text = mDivCountStr;
            tbYmin.Text = mYminStr;
            tbYmax.Text = mYmaxStr;
            cbAutoHeight.IsChecked = mAutoHeight;
            tbYmin.IsEnabled = !mAutoHeight;
            tbYmax.IsEnabled = !mAutoHeight;
            cbAspectFix.IsChecked = mAspectFix;
            cbBackColor.ItemsSource = YDrawingShapes.mColorTitle;
            cbBackColor.SelectedIndex = YDrawingShapes.mColor.FindIndex(p => mBackColor == p);

            execute();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowFormSave();       //  ウィンドの位置と大きさを保存
        }


        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
            if (WindowState != mWindowState &&
                WindowState == WindowState.Maximized) {
                //  ウィンドウの最大化時
                mWindowWidth  = SystemParameters.WorkArea.Width;
                mWindowHeight = SystemParameters.WorkArea.Height;
            } else if (this.WindowState != mWindowState ||
                mWindowWidth != Width || mWindowHeight != Height) {
                //  ウィンドウサイズが変わった時
                mWindowWidth = Width;
                mWindowHeight = Height;
            } else {
                //  ウィンドウサイズが変わらない時は何もしない
                mWindowState = WindowState;
                return;
            }

            //  ウィンドウの大きさに合わせてコントロールの幅を変更する
            double dx = mWindowWidth - mPrevWindowWidth;
            mPrevWindowWidth = mWindowWidth;

            mWindowState = WindowState;
            drawGraph();        //  グラフ表示
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.FuncPlotWidth < 100 ||
                Properties.Settings.Default.FuncPlotHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.FuncPlotHeight) {
                Properties.Settings.Default.FuncPlotWidth = mWindowWidth;
                Properties.Settings.Default.FuncPlotHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.FuncPlotTop;
                Left = Properties.Settings.Default.FuncPlotLeft;
                Width = Properties.Settings.Default.FuncPlotWidth;
                Height = Properties.Settings.Default.FuncPlotHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.FuncPlotTop = Top;
            Properties.Settings.Default.FuncPlotLeft = Left;
            Properties.Settings.Default.FuncPlotWidth = Width;
            Properties.Settings.Default.FuncPlotHeight = Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// [実行]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btExecute_Click(object sender, RoutedEventArgs e)
        {
            execute();
        }

        /// <summary>
        /// グラフをクリップボードにコピー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btCopy_Click(object sender, RoutedEventArgs e)
        {
            BitmapSource bitmapSource = ylib.canvas2Bitmap(canvas);
            Clipboard.SetImage(bitmapSource);
        }

        /// <summary>
        /// 関数形式選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbFunctionType_Click(object sender, RoutedEventArgs e)
        {
            if (rbNormal.IsChecked == true) mFuncType = FUNCTYPE.Normal;
            else if (rbParametric.IsChecked == true) mFuncType = FUNCTYPE.Parametric;
            else if (rbPolar.IsChecked == true) mFuncType = FUNCTYPE.Polar;
            if (mFuncType == FUNCTYPE.Normal)
                lbXmin.Content = "範囲 x min";
            else
                lbXmin.Content = "範囲 t min";
        }

        /// <summary>
        /// [Y方向自動]チェックボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbAutoHeight_Click(object sender, RoutedEventArgs e)
        {
            mAutoHeight = cbAutoHeight.IsChecked == true;
            tbYmin.IsEnabled = !mAutoHeight;
            tbYmax.IsEnabled = !mAutoHeight;
        }

        /// <summary>
        /// [アスペクト比]チェックボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbAspectFix_Click(object sender, RoutedEventArgs e)
        {
            execute();
        }

        /// <summary>
        /// [背景色]コンボボックス
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbBackColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = cbBackColor.SelectedIndex;
            if (0 <= index) {
                mBackColor = YDrawingShapes.mColor[index];
                execute();
            }
        }

        /// <summary>
        /// グラフの作成と表示
        /// </summary>
        private void execute()
        {
            YCalc calc = new YCalc();
            mXmin = mTmin = calc.expression(tbXmin.Text);
            mXmax = mTmax = calc.expression(tbXmax.Text);
            mDivCount = (int)calc.expression(tbDivCount.Text);

            if (rbParametric.IsChecked == true) {
                //  パラメトリック方程式
                makeParametricData(tbFunction.Text);
            } else if (rbPolar.IsChecked == true) {
                //  極方程式
                makePolarData(tbFunction.Text);
            } else {
                //  一般的な方程式
                makeFunctionData(tbFunction.Text);
            }
            if (0 < mPlotDatas.Count) {
                //  グラフ表示
                drawGraph();
            }

        }

        /// <summary>
        /// 直交座標(y=f(x))のグラフデータを作成
        /// </summary>
        /// <param name="function">関数リスト</param>
        private void makeFunctionData(string function)
        {
            //  表示関数と変数リストに分ける
            string[] buf = function.Split(';');
            List<string> funcList = new List<string>();
            List<string> argList = new List<string>();
            foreach (string s in buf) {
                if (0 <= s.Replace(" ", "").IndexOf("[y]=")) {
                    funcList.Add(s);
                } else {
                    argList.Add(s);
                }
            }

            YCalc calc = new YCalc();
            mPlotDatas = new List<List<Point>>();
            double xStep = (mXmax - mXmin) / mDivCount;
            Dictionary<string, string> argDic = calc.getArgDic(argList);

            mYmin = double.MaxValue;
            mYmax = double.MinValue;
            string errorMsg = "";

            for (int i = 0; i < funcList.Count; i++) {
                string express = calc.expressString(funcList[i], argDic);
                express = express.Substring(express.IndexOf("=") + 1);
                List<Point> plotDataList = new List<Point>();
                calc.setExpression(express);
                for (double x = mXmin; x < mXmax + xStep; x+= xStep) {
                    if (mXmax < x) x = mXmax;
                    calc.setArgValue("[x]", "(" + x + ")");
                    double y = calc.calculate();
                    if (!calc.mError) {
                        plotDataList.Add(new Point(x, y));
                        if (!double.IsInfinity(y) && !double.IsNaN(y)) {
                            mYmin = Math.Min(mYmin, y);
                            mYmax = Math.Max(mYmax, y);
                        } else {
                            errorMsg = "値不定か無限大が存在します";
                        }
                    } else {
                        errorMsg = calc.mErrorMsg;
                    }
                }
                if (0 < errorMsg.Length)
                    MessageBox.Show(errorMsg, "計算式エラー");
                if (0 < plotDataList.Count) {
                    mPlotDatas.Add(plotDataList);
                }
            }
        }

        /// <summary>
        /// 媒介変数 x=f(t),y=g(t)のグラフデータを作成
        /// </summary>
        /// <param name="function">関数リスト</param>
        private void makeParametricData(string function)
        {
            //  表示関数と変数リストに分ける
            string[] buf = function.Split(';');
            List<string> xFuncList = new List<string>();
            List<string> yFuncList = new List<string>();
            List<string> argList = new List<string>();
            foreach (string s in buf) {
                if (0 <= s.Replace(" ", "").IndexOf("[x]=")) {
                    xFuncList.Add(s);
                } else if (0 <= s.Replace(" ", "").IndexOf("[y]=")) {
                    yFuncList.Add(s);
                } else {
                    argList.Add(s);
                }
            }

            YCalc calcX = new YCalc();
            YCalc calcY = new YCalc();
            mPlotDatas = new List<List<Point>>();
            double tStep = (mTmax - mTmin) / mDivCount;
            Dictionary<string, string> argDic = calcX.getArgDic(argList);

            mXmin = double.MaxValue;
            mXmax = double.MinValue;
            mYmin = double.MaxValue;
            mYmax = double.MinValue;
            string errorMsg = "";

            for (int i = 0; i < xFuncList.Count && i < yFuncList.Count; i++) {
                List<Point> plotDataList = new List<Point>();
                string expressX = calcX.expressString(xFuncList[i], argDic);
                string expressY = calcX.expressString(yFuncList[i], argDic);
                expressX = expressX.Substring(expressX.IndexOf("=") + 1);
                expressY = expressY.Substring(expressY.IndexOf("=") + 1);
                calcX.setExpression(expressX);
                calcY.setExpression(expressY);

                double tmin = mTmin;
                double tmax = mTmax;
                for (double t = tmin; t < tmax + tStep ; t += tStep) {
                    if (tmax < t) t = tmax;
                    calcX.setArgValue("[t]", "(" + t + ")");
                    calcY.setArgValue("[t]", "(" + t + ")");
                    double x = calcX.calculate();
                    double y = calcY.calculate();
                    if (!calcX.mError && !calcY.mError) {
                        plotDataList.Add(new Point(x, y));
                        if (!double.IsInfinity(x) && !double.IsNaN(x) &&
                            !double.IsInfinity(y) && !double.IsNaN(y)) {
                            //  グラフ領域
                            mXmin = Math.Min(mXmin, x);
                            mXmax = Math.Max(mXmax, x);
                            mYmin = Math.Min(mYmin, y);
                            mYmax = Math.Max(mYmax, y);
                        } else {
                            errorMsg = "値不定か無限大が存在します";
                        }
                    } else {
                        if (calcX.mError)
                            errorMsg = calcX.mErrorMsg;
                        else if (calcY.mError)
                            errorMsg = calcY.mErrorMsg;
                    }
                }
                if (0 < errorMsg.Length)
                    MessageBox.Show(errorMsg, "計算式エラー");
                if (0 < plotDataList.Count) {
                    mPlotDatas.Add(plotDataList);
                }
            }
        }

        /// <summary>
        /// 極座標 r=f(t)のグラフデータを作成
        /// </summary>
        /// <param name="function">関数リスト</param>
        private void makePolarData(string function)
        {
            //  表示関数と変数リストに分ける
            string[] buf = function.Split(';');
            List<string> funcList = new List<string>();
            List<string> argList = new List<string>();
            foreach (string s in buf) {
                if (0 <= s.Replace(" ", "").IndexOf("[r]=")) {
                    funcList.Add(s);
                } else {
                    argList.Add(s);
                }
            }

            YCalc calc = new YCalc();
            mPlotDatas = new List<List<Point>>();
            double tStep = (mTmax - mTmin) / mDivCount;
            Dictionary<string, string> argDic = calc.getArgDic(argList);

            mXmin = double.MaxValue;
            mXmax = double.MinValue;
            mYmin = double.MaxValue;
            mYmax = double.MinValue;
            string errorMsg = "";

            for (int i = 0; i < funcList.Count; i++) {
                string express = calc.expressString(funcList[i], argDic);
                express = express.Substring(express.IndexOf("=") + 1);
                List<Point> plotDataList = new List<Point>();
                calc.setExpression(express);
                for (double t = mTmin; t < mTmax + tStep; t += tStep) {
                    if (mTmax < t) t = mTmax;
                    calc.setArgValue("[t]", "(" + t + ")");
                    double r = calc.calculate();
                    if (!calc.mError) {
                        if (!double.IsInfinity(r) && !double.IsNaN(r)) {
                            //  極座標から直交座標に変換
                            double x = r * Math.Cos(t);
                            double y = r * Math.Sin(t);
                            plotDataList.Add(new Point(x, y));
                            mXmin = Math.Min(mXmin, x);
                            mXmax = Math.Max(mXmax, x);
                            mYmin = Math.Min(mYmin, y);
                            mYmax = Math.Max(mYmax, y);
                        } else {
                            errorMsg = "値不定か無限大が存在します";
                        }
                    } else {
                        errorMsg = calc.mErrorMsg;
                    }
                }
                if (0 < errorMsg.Length)
                    MessageBox.Show(errorMsg, "計算式エラー");
                if (0 < plotDataList.Count) {
                    mPlotDatas.Add(plotDataList);
                }
            }
        }

        /// <summary>
        /// グラフの表示
        /// </summary>
        private void drawGraph()
        {
            if (mPlotDatas == null || mPlotDatas.Count < 1) {
                //MessageBox.Show("データが作成されていません", "描画エラー");
                return;
            }
            ydraw.setWindowSize(canvas.ActualWidth, canvas.ActualHeight);
            ydraw.setViewArea(0, 0, canvas.ActualWidth, canvas.ActualHeight);
            //  アスペクト比無効
            ydraw.setAspectFix(cbAspectFix.IsChecked == true);
            //  高さ自動
            double ymin = mYmin;
            double ymax = mYmax;
            if (cbAutoHeight.IsChecked != true) {
                YCalc calc = new YCalc();
                ymin = calc.expression(tbYmin.Text);
                ymax = calc.expression(tbYmax.Text);
            }
            double dx = (mXmax - mXmin);    //  グラフエリアの範囲
            double dy = (ymax - ymin);
            if (dx <= 0 || dy <= 0) {
                MessageBox.Show("領域が求められていません", "描画エラー");
                return;
            }

            //  グラフエリアの設定
            ydraw.setWorldWindow(mXmin - dx * 0.13, ymax + dy * 0.05, mXmax + dx * 0.05, ymin - dy * 0.1);
            ydraw.clear();
            ydraw.backColor(mBackColor);

            //  目盛り付き補助線の描画
            double x, y;
            ydraw.setScreenTextSize(mTextSize);
            ydraw.setTextColor(Brushes.Black);
            ydraw.setColor(Brushes.Aqua);
            ydraw.drawWRectangle(new Rect(mXmin, ymax, dx, dy), 0);
            double auxDx = ylib.graphStepSize(dx, 5);
            double auxDy = ylib.graphStepSize(dy, 5);
            for (x = Math.Floor(mXmin / auxDx) * auxDx; x <= mXmax; x += auxDx) {
                if (mXmin <= x && x <= mXmax) {
                    if (x == 0) {
                        //  原点を通る線
                        ydraw.setColor(Brushes.Red);
                        ydraw.setTextColor(Brushes.Red);
                    } else {
                        ydraw.setColor(Brushes.Aqua);
                        ydraw.setTextColor(Brushes.Black);
                    }
                    ydraw.drawWLine(new Point(x, ymin), new Point(x, ymax));
                    ydraw.drawWText(double2String(x, mXmax), new Point(x, ymin), 0, HorizontalAlignment.Center, VerticalAlignment.Top);
                }
            }
            for (y = Math.Floor(ymin / auxDy) * auxDy; y <= ymax; y += auxDy) {
                if (ymin <= y && y <= ymax) {
                    if (y == 0) {
                        //  原点を通る線
                        ydraw.setColor(Brushes.Red);
                        ydraw.setTextColor(Brushes.Red);
                    } else {
                        ydraw.setColor(Brushes.Aqua);
                        ydraw.setTextColor(Brushes.Black);
                    }
                    ydraw.drawWLine(new Point(mXmin, y), new Point(mXmax, y));
                    ydraw.drawWText(double2String(y, mYmax), new Point(mXmin, y), 0, HorizontalAlignment.Right, VerticalAlignment.Center);
                }
            }

            //  計算式のX,Yデータをプロット
            for (int i = 0; i < mPlotDatas.Count; i++) {
                ydraw.setColor(ydraw.getColor15(i * 2));
                List<Point> plotData = mPlotDatas[i];
                for (int j = 0; j < plotData.Count - 1; j++) {
                    if (!double.IsNaN(plotData[j].X) && !double.IsNaN(plotData[j].Y) &&
                        !double.IsInfinity(plotData[j].X) && !double.IsInfinity(plotData[j].Y) &&
                        !double.IsNaN(plotData[j + 1].X) && !double.IsNaN(plotData[j + 1].Y) &&
                        !double.IsInfinity(plotData[j + 1].X) && !double.IsInfinity(plotData[j + 1].Y))
                        clipingLine(plotData[j], plotData[j + 1], ymin, ymax);
                }

            }
        }

        /// <summary>
        /// 数値の書式を揃える F表示とE表示を併用
        /// </summary>
        /// <param name="val">数値</param>
        /// <param name="maxVal">最大数値</param>
        /// <returns>書式化数値文字列</returns>
        private string double2String(double val, double maxVal)
        {
            if (val == 0)
                return val.ToString();
            int magVal = (int)Math.Floor(Math.Log10(Math.Abs(val)));
            int magMax = (int)Math.Floor(Math.Log10(Math.Abs(maxVal)));
            if (3 < magMax)
                return string.Format("{0:F2}E{1}", val / Math.Pow(10, magVal), magVal);
            if (1 < magMax)
                return string.Format("{0:F1}", val);
            if (magMax < -2)
                return string.Format("{0:F2}E{1}", val / Math.Pow(10, magVal), magVal);
            return string.Format("{0:F3}", val);
        }

        /// <summary>
        /// 上下方向でクリッピングして線分を表示する
        /// </summary>
        /// <param name="ps">線分の始点</param>
        /// <param name="pe">線分の終点</param>
        /// <param name="ymin">クリッピングの下端</param>
        /// <param name="ymax">クリッピングの上端</param>
        private void clipingLine(Point ps, Point pe, double ymin, double ymax)
        {
            //  両端が領域内はそのまま表示
            if (ymin <= ps.Y && ps.Y <= ymax && ymin <= pe.Y && pe.Y <= ymax) {
                ydraw.drawWLine(ps, pe);
                return;
            }
            //  線分が領域を跨内場合は表示しない
            if ((ps.Y < ymin && pe.Y < ymin) || (ymax < ps.Y && ymax < pe.Y))
                return;
            //  領域をまたぐ線分をクリッピングする
            if (pe.Y < ps.Y)
                YLib.Swap(ref ps, ref pe);
            double a = (pe.Y - ps.Y) / (pe.X - ps.X);
            double b = ps.Y - a * ps.X;
            if (ps.Y < ymin) {
                ps.X = (ymin - b) / a;
                ps.Y = ymin;
            }
            if (ymax < pe.Y) {
                pe.X = (ymax - b) / a;
                pe.Y = ymax;
            }
            ydraw.drawWLine(ps, pe);
        }
    }
}

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Forms;
using Wpf3DLib;
using WpfLib;

namespace NoteApp
{
    /// <summary>
    /// FuncPlot3D.xaml の相互作用ロジック
    /// OenTK 3.3.3 OpenTK GLControl 3.3.3 (.NET Framework 4用)
    /// https://opentk.net/
    /// OpenGLを利用するにはNuGetより、OpenTKとOpenTK.GLControlのインストールが必要
    /// OpenTKの表示コンテナにはWindowsFormsHostを使用する
    /// WindowsFormsHostを使用するには参照でWindowsFormsIIntegrationを追加しておく
    /// GLControlでMouseEventArgsを使用するため、参照でSystem.Windows.Formsを追加しておく
    ///     glControl_Load などを作成する
    /// GL.ViewPortを使用するためには参照でSystem.Drawingを追加しておく
    /// </summary>
    public partial class FuncPlot3D : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private System.Windows.WindowState mWindowState = System.Windows.WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)

        public enum FUNCTYPE
        {                              //  関数の種類(一般、媒介変数,極方程式)
            Normal, Parametric, Polar
        };
        public FUNCTYPE mFuncType = FUNCTYPE.Normal;
        public List<string> mFuncList;                      //  計算式リスト

        public string mXminStr = "0";                       //  グラフ表示エリア
        public string mXmaxStr = "100";
        public string mYminStr = "0";
        public string mYmaxStr = "100";
        public string mZminStr = "0";
        public string mZmaxStr = "100";
        public string mDivCountStr = "50";                  //  関数グラフの分割数
        public double mXStart = 0;                            //  グラフ表示エリア
        public double mXEnd = 10;
        public double mYStart = 0;
        public double mYEnd = 100;
        public double mSStart = 0;
        public double mSEnd = 100;
        public double mTStart = 0;
        public double mTEnd = 100;
        public int mDivCount = 50;                          //  関数グラフの分割数
        public bool mAutoHeight = true;                     //  z軸値自動
        public bool mAspectFix = false;                     //  アスペクト比固定
        public bool mSurface = true;                        //  サーフェース表示
        public Color4 mBackColor = Color4.Black;            //  背景色

        private List<Vector3[,]> mPositionList;             //  座標データリスト
        private Vector3 mMin = new Vector3(-1, -1, -1);     //  表示領域の最小値
        private Vector3 mMax = new Vector3(1, 1, 1);        //  表示領域の最大値
        private Vector3 mManMin;
        private Vector3 mManMax;

        private bool mError = false;
        private string mErrorMsg = "";

        private GLControl glControl;                        //  OpenTK.GLcontrol
        private GL3DLib m3Dlib;                             //  三次元表示ライブラリ
        private YLib ylib = new YLib();                     //  単なるライブラリ

        public FuncPlot3D()
        {
            InitializeComponent();

            mWindowWidth = Width;
            mWindowHeight = Height;
            mPrevWindowWidth = mWindowWidth;

            WindowFormLoad();                               //  Windowの位置とサイズを復元

            glControl = new GLControl();
            m3Dlib = new GL3DLib(glControl);
            m3Dlib.initPosition(1.5f, -70f, 0f, 10f);
            m3Dlib.setArea(mMin, mMax);

            glControl.Load += glControl_Load;
            glControl.Paint += glControl_Paint;
            glControl.Resize += glControl_Resize;
            glControl.MouseDown += glControl_MouseDown;
            glControl.MouseUp += glControl_MouseUp;
            glControl.MouseMove += glControl_MosueMove;
            glControl.MouseWheel += glControl_MouseWheel;

            glGraph.Child = glControl;                      //  OpenGLをWindowsに接続

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            setControlData();
            execute();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowFormSave();       //  ウィンドの位置と大きさを保存
        }

        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
            if (WindowState != mWindowState &&
                 WindowState == System.Windows.WindowState.Maximized) {
                //  ウィンドウの最大化時
                mWindowWidth = SystemParameters.WorkArea.Width;
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
            //drawGraph();        //  グラフ表示
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.FuncPlot3DWidth < 100 ||
                Properties.Settings.Default.FuncPlot3DHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.FuncPlot3DHeight) {
                Properties.Settings.Default.FuncPlot3DWidth = mWindowWidth;
                Properties.Settings.Default.FuncPlot3DHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.FuncPlot3DTop;
                Left = Properties.Settings.Default.FuncPlot3DLeft;
                Width = Properties.Settings.Default.FuncPlot3DWidth;
                Height = Properties.Settings.Default.FuncPlot3DHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.FuncPlot3DTop = Top;
            Properties.Settings.Default.FuncPlot3DLeft = Left;
            Properties.Settings.Default.FuncPlot3DWidth = Width;
            Properties.Settings.Default.FuncPlot3DHeight = Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// OpenGLのLoad 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_Load(object sender, EventArgs e)
        {
            GL.Enable(EnableCap.DepthTest);
            //GL.Enable(EnableCap.Lighting);    //  光源の使用

            GL.PointSize(3.0f);                 //  点の大きさ
            GL.LineWidth(1.5f);                 //  線の太さ

            //setParameter();
            //makeFuncData(mFuncList);            //  座標データの作成

            //throw new NotImplementedException();
        }

        /// <summary>
        /// OpenGLの描画 都度呼ばれる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_Paint(object sender, PaintEventArgs e)
        {
            renderFrame();

            //throw new NotImplementedException();
        }

        /// <summary>
        /// Windowのサイズが変わった時、glControl_Paintも呼ばれる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_Resize(object sender, EventArgs e)
        {
            GL.Viewport(glControl.ClientRectangle);

            //throw new NotImplementedException();
        }

        /// <summary>
        /// マウスホイールによるzoom up/down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            float delta = (float)e.Delta / 1000f;// - wheelPrevious;
            m3Dlib.setZoom(delta);

            renderFrame();
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 視点(カメラ)の回転と移動
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_MosueMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (m3Dlib.moveObject(e.X, e.Y))
                renderFrame();
            //throw new NotImplementedException();
        }

        /// <summary>
        /// マウスダウン 視点(カメラ)の回転開始
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) {
                m3Dlib.setMoveStart(true, e.X, e.Y);
            } else if (e.Button == MouseButtons.Right) {
                m3Dlib.setMoveStart(false, e.X, e.Y);
            }
            //throw new NotImplementedException();
        }

        /// <summary>
        /// マウスアップ 視点(カメラ)の回転終了
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void glControl_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            m3Dlib.setMoveEnd();
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 三次元データ表示
        /// </summary>
        private void renderFrame()
        {
            if (mPositionList == null)
                return;
            m3Dlib.setBackColor(mBackColor);
            m3Dlib.renderFrameStart();
            foreach (Vector3[,] position in mPositionList) {
                //  3Dグラフの表示
                if (cbSurface.IsChecked == true)
                    m3Dlib.drawSurfaceShape(position);
                else
                    m3Dlib.drawWireShape(position);
            }
            m3Dlib.setAreaFrameDisp(cbFrame.IsChecked == true);
            m3Dlib.drawAxis();
            m3Dlib.rendeFrameEnd();
        }

        /// <summary>
        /// キー入力処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            bool ctrl = false;
            float translateStep = 0.1f;
            float rotateStep = 5f / 180f * (float)Math.PI;
            float scaleStep = 1/10f;
            if (e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Control) {
                ctrl = true;
            }
            if (e.Key == System.Windows.Input.Key.Left) {
                if (ctrl)       //  左へ移動
                    m3Dlib.translate(-translateStep, 0, 0);
                else            //  左に回転
                    m3Dlib.roatateY(rotateStep);
            } else if (e.Key == System.Windows.Input.Key.Right) {
                if (ctrl)       //  右へ移動
                    m3Dlib.translate(translateStep, 0, 0);
                else            //  右に回転
                    m3Dlib.roatateY(-rotateStep);
            } else if (e.Key == System.Windows.Input.Key.Up) {
                if (ctrl)       //  上に移動
                    m3Dlib.translate(0, translateStep, 0);
                else            //  上に回転
                    m3Dlib.roatateX(-rotateStep);
            } else if (e.Key == System.Windows.Input.Key.Down) {
                if (ctrl)       //  下に移動
                    m3Dlib.translate(0, -translateStep, 0);
                else            //  下に回転
                    m3Dlib.roatateX(rotateStep);
            } else if (e.Key == System.Windows.Input.Key.PageUp) {
                if (ctrl)       //  手前に移動
                    m3Dlib.translate(0, 0, translateStep);
                else            //  時計回りに回転
                    m3Dlib.roatateZ(-rotateStep);
            } else if (e.Key == System.Windows.Input.Key.PageDown) {
                if (ctrl)       //  奥へ移動
                    m3Dlib.translate(0, 0, -translateStep);
                else            //  反時計回りに回転
                    m3Dlib.roatateZ(rotateStep);
            } else if (e.Key == System.Windows.Input.Key.Home) {
                if (ctrl)       //  拡大
                    m3Dlib.setZoom(scaleStep);
                else {          //  初期状態
                    m3Dlib.initPosition(1.5f, -90f, 0f, 0f);
                }
            } else if (e.Key == System.Windows.Input.Key.End) {
                if (ctrl)       //  縮小
                    m3Dlib.setZoom(-scaleStep);
            } else {
                return;
            }
            renderFrame();
            e.Handled = true;
        }

        /// <summary>
        /// [実行]ボタン データの作成表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btExecute_Click(object sender, RoutedEventArgs e)
        {
            execute();
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
            if (mFuncType == FUNCTYPE.Normal) {
                lbXmin.Content = "範囲 x min";
                lbYmin.Content = "範囲 y min";
            } else {
                lbXmin.Content = "範囲 s min";
                lbYmin.Content = "範囲 t min";
            }
        }

        /// <summary>
        /// [Z方向自動]チェックボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbAutoHeight_Click(object sender, RoutedEventArgs e)
        {
            mAutoHeight = cbAutoHeight.IsChecked == true;
            tbZmin.IsEnabled = !mAutoHeight;
            tbZmax.IsEnabled = !mAutoHeight;
            setAreaParameter();
            renderFrame();
        }

        /// <summary>
        /// [サーフェース表示]チェックボックス
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbSurface_Click(object sender, RoutedEventArgs e)
        {
            renderFrame();
        }

        /// <summary>
        /// [アスペクト比固定]チェックボックス
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbAspectFix_Click(object sender, RoutedEventArgs e)
        {
            setAreaParameter();
            renderFrame();
        }

        /// <summary>
        /// [枠表示]チェックボックス
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbFrame_Click(object sender, RoutedEventArgs e)
        {
            renderFrame();
        }

        /// <summary>
        /// [背景色]コンボボックス選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbBackColor_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (0 <= cbBackColor.SelectedIndex) {
                mBackColor = GL3DLib.mColor4[cbBackColor.SelectedIndex];
                renderFrame();
            }
        }

        /// <summary>
        /// [コピー]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btCopy_Click(object sender, RoutedEventArgs e)
        {
            Bitmap bmp = ToBitmap();
            System.Windows.Clipboard.SetImage(ylib.bitmap2BitmapSource(bmp));
        }

        /// <summary>
        /// 三次元データ作成と表示
        /// </summary>
        private void execute()
        {
            //  パラメータの取得
            setParameter();
            if (mFuncType == FUNCTYPE.Normal)
                makeFuncData(mFuncList);                        //  直交座標のデータ作成
            else if (mFuncType == FUNCTYPE.Parametric)
                makeParametricFuncData(mFuncList);              //  パラメトリックのデータ作成
            setAreaParameter();
            if (0 < mPositionList.Count) {
                renderFrame();                                  //  座標データから三次元表示
            }
        }

        /// <summary>
        /// コントロールにデータを設定
        /// </summary>
        private void setControlData()
        {
            if (mFuncList != null)
                tbFunction.Text = string.Join(";", mFuncList);
            else
                tbFunction.Text = string.Empty;
            if (mFuncType == FUNCTYPE.Normal)
                rbNormal.IsChecked = true;
            else if (mFuncType == FUNCTYPE.Parametric)
                rbParametric.IsChecked = true;
            if (mFuncType == FUNCTYPE.Normal) {
                lbXmin.Content = "範囲 x min";
                lbYmin.Content = "範囲 y min";
            } else {
                lbXmin.Content = "範囲 s min";
                lbYmin.Content = "範囲 t min";
            }
            tbXmin.Text = mXminStr;
            tbXmax.Text = mXmaxStr;
            tbDivCount.Text = mDivCountStr;
            tbYmin.Text = mYminStr;
            tbYmax.Text = mYmaxStr;
            tbZmin.Text = mZminStr;
            tbZmax.Text = mZmaxStr;
            cbSurface.IsChecked = mSurface;
            cbAutoHeight.IsChecked = mAutoHeight;
            tbZmin.IsEnabled = !mAutoHeight;
            tbZmax.IsEnabled = !mAutoHeight;
            cbAspectFix.IsChecked = mAspectFix;
            cbBackColor.ItemsSource = GL3DLib.mColor4Title;
            cbBackColor.SelectedIndex = GL3DLib.mColor4.FindIndex(mBackColor);
        }

        /// <summary>
        /// 計算式やデータ範囲などをグローバル変数に設定
        /// 変数の初期値は計算式からも求められるようにした
        /// </summary>
        private void setParameter()
        {
            if (0 < tbFunction.Text.Length) {
                YCalc calc = new YCalc();
                mXStart = calc.expression(tbXmin.Text);                 //  X開始値
                mXEnd = calc.expression(tbXmax.Text);                   //  X終了値
                mDivCount = (int)calc.expression(tbDivCount.Text);      //  X,Y分割数
                mYStart = calc.expression(tbYmin.Text);                 //  Y開始値
                mYEnd = calc.expression(tbYmax.Text);                   //  Y終了値
                mFuncList = tbFunction.Text.Split(';').ToList();        //  数式、
            }
        }

        /// <summary>
        /// 直交座標の数式から座標データを作成
        /// </summary>
        /// <param name="function">数式リスト</param>
        private void makeFuncData(List<string> function)
        {
            //  表示関数と変数リストに分ける
            List<string> funcList = new List<string>();
            List<string> argList = new List<string>();
            foreach (string s in function) {
                if (0 <= s.Replace(" ", "").IndexOf("[z]=")) {
                    funcList.Add(s);
                } else {
                    argList.Add(s);
                }
            }
            YCalc calc = new YCalc();
            double xStep = (mXEnd - mXStart) / mDivCount;
            double yStep = (mYEnd - mYStart) / mDivCount;
            Dictionary<string, string> argDic = calc.getArgDic(argList);

            m3Dlib.setArea(new Vector3(float.MaxValue), new Vector3(float.MinValue));
            string errorMsg = "";
            if (mPositionList == null)
                mPositionList = new List<Vector3[,]>();
            else
                mPositionList.Clear();
            Vector3 tmpPos = new Vector3();
            for (int n = 0; n < funcList.Count; n++) {
                string express = calc.expressString(funcList[n], argDic);
                express = express.Substring(express.IndexOf("=") + 1);
                calc.setExpression(express);
                Vector3[,] position = new Vector3[mDivCount + 1, mDivCount + 1];
                double x = mXStart;
                for (int i = 0; i <= mDivCount; i++) {
                    if (mXEnd < x) x = mXEnd;
                    calc.setArgValue("[x]", "(" + x + ")");
                    double y = mYStart;
                    for (int j = 0; j <= mDivCount; j++) {
                        if (mYEnd < y) y = mYEnd;
                        calc.setArgValue("[y]", "(" + y + ")");
                        double z = calc.calculate();
                        if (!calc.mError) {
                            position[i,j] = new Vector3((float)x, (float)y, (float)z);
                            if (!double.IsInfinity(y) && !double.IsNaN(y) && !double.IsNaN(z)) {
                                m3Dlib.extendArea(position[i, j]);
                                tmpPos = position[i, j];
                            } else {
                                position[i, j] = tmpPos;
                                errorMsg = "値不定か無限大が存在します";
                            }
                        } else {
                            position[i, j] = tmpPos;
                            errorMsg = calc.mErrorMsg;
                        }
                        y += yStep;
                    }
                    x += xStep;
                }
                mPositionList.Add(position);
            }
            //  表示領域をチェックする
            m3Dlib.areaCheck();
            //  表示領域を取得
            mManMin = mMin = m3Dlib.getAreaMin();
            mManMax = mMax = m3Dlib.getAreaMax();
            //  カラーレベルの設定
            m3Dlib.setColorLevel(mMin.Z, mMax.Z);

            if (0 < errorMsg.Length) {
                MessageBoxEx dlg = new MessageBoxEx();
                dlg.Owner = this;                                               //  親Window
                dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;  //  センター表示
                dlg.mButton = MessageBoxButton.OK;                              //  ボタンタイプ
                dlg.mTitle = "計算式エラー";
                dlg.mMessage = errorMsg;
                dlg.ShowDialog();
            }
            //else {
            //    minmax.Text = "(" + mMin.Z + "," + mMax.Z + ")";
            //}
        }

        /// <summary>
        /// パラメトリックの数式から座標データを作成
        /// </summary>
        /// <param name="function">数式リスト</param>
        private void makeParametricFuncData(List<string> function)
        {
            //  表示関数と変数リストに分ける
            List<string> funcXList = new List<string>();
            List<string> funcYList = new List<string>();
            List<string> funcZList = new List<string>();
            List<string> argList = new List<string>();
            foreach (string s in function) {
                if (0 <= s.Replace(" ", "").IndexOf("[x]=")) {
                    funcXList.Add(s);
                } else if (0 <= s.Replace(" ", "").IndexOf("[y]=")) {
                    funcYList.Add(s);
                } else if (0 <= s.Replace(" ", "").IndexOf("[z]=")) {
                    funcZList.Add(s);
                } else {
                    argList.Add(s);
                }
            }
            YCalc xcalc = new YCalc();
            YCalc ycalc = new YCalc();
            YCalc zcalc = new YCalc();
            double sStep = (mXEnd - mXStart) / mDivCount;
            double tStep = (mYEnd - mYStart) / mDivCount;
            Dictionary<string, string> argDic = xcalc.getArgDic(argList);

            m3Dlib.setArea(new Vector3(float.MaxValue), new Vector3(float.MinValue));
            string errorMsg = "";
            if (mPositionList == null)
                mPositionList = new List<Vector3[,]>();
            else
                mPositionList.Clear();

            for (int n = 0; n < funcXList.Count && n < funcYList.Count && n < funcZList.Count; n++) {
                string xexpress = xcalc.expressString(funcXList[n], argDic);
                string yexpress = ycalc.expressString(funcYList[n], argDic);
                string zexpress = zcalc.expressString(funcZList[n], argDic);
                xexpress = xexpress.Substring(xexpress.IndexOf("=") + 1);
                yexpress = yexpress.Substring(yexpress.IndexOf("=") + 1);
                zexpress = zexpress.Substring(zexpress.IndexOf("=") + 1);
                xcalc.setExpression(xexpress);
                ycalc.setExpression(yexpress);
                zcalc.setExpression(zexpress);

                Vector3[,] position = new Vector3[mDivCount + 1, mDivCount + 1];
                double s = mXStart;
                for (int i = 0; i <= mDivCount; i++) {
                    if (mXEnd < s) s = mXEnd;
                    xcalc.setArgValue("[s]", "(" + s + ")");
                    ycalc.setArgValue("[s]", "(" + s + ")");
                    zcalc.setArgValue("[s]", "(" + s + ")");
                    double t = mYStart;
                    for (int j = 0; j <= mDivCount; j++) {
                        if (mYEnd < t) t = mYEnd;
                        xcalc.setArgValue("[t]", "(" + t + ")");
                        ycalc.setArgValue("[t]", "(" + t + ")");
                        zcalc.setArgValue("[t]", "(" + t + ")");
                        double x = xcalc.calculate();
                        double y = ycalc.calculate();
                        double z = zcalc.calculate();
                        if (!xcalc.mError && !ycalc.mError && !zcalc.mError) {
                            position[i, j] = new Vector3((float)x, (float)y, (float)z);
                            if (!double.IsInfinity(y) && !double.IsNaN(y) && !double.IsNaN(z)) {
                                m3Dlib.extendArea(position[i, j]);
                            } else {
                                errorMsg = "値不定か無限大が存在します";
                            }
                        } else {
                            errorMsg = xcalc.mError? xcalc.mErrorMsg : "";
                            errorMsg += " " + (ycalc.mError ? ycalc.mErrorMsg : "");
                            errorMsg += " " + (zcalc.mError ? zcalc.mErrorMsg : "");
                        }
                        t += tStep;
                    }
                    s += sStep;
                }
                mPositionList.Add(position);
            }
            //  表示領域をチェックする
            m3Dlib.areaCheck();
            //  表示領域を取得
            mManMin = mMin = m3Dlib.getAreaMin();
            mManMax = mMax = m3Dlib.getAreaMax();
            //  カラーレベルの設定
            m3Dlib.setColorLevel(mMin.Z, mMax.Z);

            if (0 < errorMsg.Length) {
                MessageBoxEx dlg = new MessageBoxEx();
                dlg.Owner = this;                                               //  親Window
                dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;  //  センター表示
                dlg.mButton = MessageBoxButton.OK;                              //  ボタンタイプ
                dlg.mTitle = "計算式エラー";
                dlg.mMessage = errorMsg;
                dlg.ShowDialog();
            }
            //else {
            //    minmax.Text = "(" + mMin.Z + "," + mMax.Z + ")";
            //}
        }

        /// <summary>
        /// Z方向の範囲の設定
        /// 高さ自動出ない場合は入力値を使用、入力値が設定されていない場合は
        /// 計算で求めたZ方向の最大最小値を入力値に設定
        /// </summary>
        private void setAreaParameter()
        {
            mManMin = mMin;
            mManMax = mMax;
            if (cbAutoHeight.IsChecked != true && 0 < tbZmin.Text.Length && 0 < tbZmax.Text.Length) {
                //  Z範囲に設定値を使う
                YCalc calc = new YCalc();
                mManMin.Z = (float)calc.expression(tbZmin.Text);
                mManMax.Z = (float)calc.expression(tbZmax.Text);
            } else {
                //  Z範囲を関数の最小最大範囲を設定
                if (tbZmin.Text.Length == 0 && mMin != null) {
                    tbZmin.Text = mMin.Z.ToString();
                    mManMin.Z = mMin.Z;
                }
                if (tbZmax.Text.Length == 0 && mMax != null) {
                    tbZmax.Text = mMax.Z.ToString();
                    mManMax.Z = mMax.Z;
                }
            }
            if (cbAspectFix.IsChecked == true) {
                float dx = mManMax.X - mManMin.X;
                float dy = mManMax.Y - mManMin.Y;
                float dz = mManMax.Z - mManMin.Z;
                float maxd = Math.Max(Math.Max(dx, dy), dz);
                float cx = (mManMax.X + mManMin.X) / 2;
                float cy = (mManMax.Y + mManMin.Y) / 2;
                float cz = (mManMax.Z + mManMin.Z) / 2;
                mManMin.X = cx - maxd / 2;
                mManMax.X = cx + maxd / 2;
                mManMin.Y = cy - maxd / 2;
                mManMax.Y = cy + maxd / 2;
                mManMin.Z = cz - maxd / 2;
                mManMax.Z = cz + maxd / 2;
            }

            m3Dlib.setArea(mManMin, mManMax);
        }

        /// <summary>
        /// glControl画面をBitmapに変換
        /// https://teratail.com/questions/283747
        /// </summary>
        /// <returns>Bitmap</returns>
        private Bitmap ToBitmap()
        {
            int WorldWidth = (int)glGraph.ActualWidth;
            int WorldHeight = (int)glGraph.ActualHeight;

            glControl.Refresh();
            Bitmap bmp = new Bitmap(WorldWidth, WorldHeight);
            var bmpData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.ReadPixels(0, 0, WorldWidth, WorldHeight, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0);

            bmp.UnlockBits(bmpData);
            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

            return bmp;
        }
    }
}

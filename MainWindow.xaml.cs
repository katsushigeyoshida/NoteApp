using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf3DLib;
using WpfLib;

namespace NoteApp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private WindowState mWindowState = WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)
        private WindowState mWinState;

        private const string mRootFolderName = "KNote";         //  データフォルダ名
        private const string mBackupFolderName = "KNoteBackup"; //  バックアップフォルダ名
        private string mRootFolder = ".\\" + mRootFolderName;   //  ルートパス(root)
        private string mBackupFolder = ".\\" + mBackupFolderName;   //  バックアップフォルダパス
        private string mInitGenre = "ノート";                   //  大分類(Genre)
        private string mInitCategory = "メモ";                  //  小分類(Category)
        private string mInitItem = "無題";                      //  項目名(Item,ファイル名)
        private string mCurItemPath;                            //  編集中(Item)のファイルパス
        private string mCurGenre = "";                          //  編集中の大分類
        private string mCurCategory = "";                       //  編集中の分類
        private string mCurItem = "";                           //  編集注の項目
        private string mTempPath = "";                          //  一時保存ファイルパス
        private List<string> mGenreList = new List<string>();   //  大分類リスト
        private List<string> mCategoryList = new List<string>();//  小分類リスト
        private List<string> mItemList = new List<string>();    //  項目リスト
        private int mFileFormatNo = 0;                          //  データ保存ファイルの種類
        private string[] mFileFormats = new string[] {
            DataFormats.XamlPackage, DataFormats.Xaml, DataFormats.Rtf, DataFormats.Text
        };
        private string[] mFileExts = new string[] {
            ".xaml", ".xaml", ".rtf", ".txt"
        };
        private string[] mFileFormatMenu = new string[] {
            "XamlPackageファイル(xaml)", "Xamlファイル(xaml)", "リッチテキストファイル(rtf)", "テキストファイル(txt)"
        };
        private string mFileFormat;                             //  保存ファイル形式
        private string mFileExt;                                //  保存ファイルの拡張子
        private string mLinkExt = ".nlnk";                     //  リンクファイルの拡張子
        private bool mEnableItemList = true;                    //  項目変更の有効性
        private bool mEnableCategoryList = true;                //  小分類変更の有効性
        private bool mEnableGenreList = true;                   //  大分類変更の有効性
        private TextPointer mCurTextPointer;                    //  検索した文字列の位置
        private bool mFontSizeEnabled = true;                   //  文字サイズSpinner有効可否
        private bool mFontFamilyEnabled = true;                 //  m文字ファミリーSpinner有効可否
        private string[] mSettingMenu = {                       //  設定コマンドのメニュー
            "データバックアップ", "バックアップの復元", "ルートフォルダの設定", "バックアップフォルダの設定",
            "データフォルダを初期値に戻す", "プロパティ"
        };
        private string[] mDateTimeMenu = {
            "今日の日付挿入 西暦(YYYY年MM月DD日)", "今日の日付挿入 西暦('YY年MM月DD日)", "今日の日付挿入 西暦付(YYYY/MM/DD)",
            "今日の日付挿入 和暦(令和YY年MM月DD日)",
            "現在時刻(HH時MM分SS秒)", "現在時刻(午前hh時MM分SS秒)", "現在時刻(HH:MM:SS)",
            "西暦→和暦変換", "和暦→西暦変換",
            "曜日の挿入(Sunday)","曜日の挿入(SUN)","曜日の挿入(日曜日)","曜日の挿入(日)"
        };
        private int mImageMaxWidth = 600;                       //  スクリーンキャプチャしたイメージを貼り付ける最大幅

        private YLib ylib = new YLib();

        public MainWindow()
        {
            InitializeComponent();

            WindowFormLoad();

            //  FontFamilyとSizeの設定
            cbFontFamily.ItemsSource = Fonts.SystemFontFamilies.OrderBy(f => f.Source);
            cbFontSize.ItemsSource = new List<double>() {
                 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72
            };
            setFontConbBox();

            int index;
            //  フォルダの設定
            mRootFolder = Path.GetFullPath(mRootFolder);
            Directory.CreateDirectory(mRootFolder);
            mBackupFolder = Path.GetFullPath(mBackupFolder);

            //  データファイルのフォーマットと各ちょしの設定
            mFileFormat = mFileFormats[mFileFormatNo];
            mFileExt = mFileExts[mFileFormatNo];

            getInitList();

            if (!ylib.createPathFolder(mCurItemPath)) {
                MessageBox.Show(mCurItemPath + " が作成できません");
            } else {
                loadFile(mCurItemPath, mFileFormat);
                setTitle();
            }

            //  前回の大分類、分類、項目を設定
            if (0 < mCurGenre.Length) {
                index = cbGenreList.Items.IndexOf(mCurGenre);
                if (0 <= index)
                    cbGenreList.SelectedIndex = index;
            }
            if (0 < mCurCategory.Length) {
                index = lbCategoryList.Items.IndexOf(mCurCategory);
                if (0 <= index)
                    lbCategoryList.SelectedIndex = index;
            }
            if (0 < mCurItem.Length) {
                index = lbItemList.Items.IndexOf(mCurItem);
                if (0 <= index)
                    lbItemList.SelectedIndex = index;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            saveCurFile();
            WindowFormSave();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.MainWindowWidth < 100 ||
                Properties.Settings.Default.MainWindowHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.MainWindowHeight) {
                Properties.Settings.Default.MainWindowWidth = mWindowWidth;
                Properties.Settings.Default.MainWindowHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.MainWindowTop;
                Left = Properties.Settings.Default.MainWindowLeft;
                Width = Properties.Settings.Default.MainWindowWidth;
                Height = Properties.Settings.Default.MainWindowHeight;
            }
            //if (0 < Properties.Settings.Default.CategoryListWidth)
            //    lbCategoryList.Width = Properties.Settings.Default.CategoryListWidth;
            //if (0 < Properties.Settings.Default.ItemListWidth)
            //    lbItemList.Width = Properties.Settings.Default.ItemListWidth;

            if (0 < Properties.Settings.Default.BackupFolder.Length)
                mBackupFolder = Properties.Settings.Default.BackupFolder;
            if (0 < Properties.Settings.Default.RootFolder.Length)
                mRootFolder = Properties.Settings.Default.RootFolder;

            if (0 < Properties.Settings.Default.GenreName.Length)
                mCurGenre = Properties.Settings.Default.GenreName;
            if (0 < Properties.Settings.Default.CategoryName.Length)
                mCurCategory = Properties.Settings.Default.CategoryName;
            if (0 < Properties.Settings.Default.ItemName.Length)
                mCurItem = Properties.Settings.Default.ItemName;
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            Properties.Settings.Default.BackupFolder = mBackupFolder;
            Properties.Settings.Default.RootFolder = mRootFolder;
            Properties.Settings.Default.ItemName = lbItemList.Items[lbItemList.SelectedIndex].ToString();
            Properties.Settings.Default.CategoryName = lbCategoryList.Items[lbCategoryList.SelectedIndex].ToString();
            Properties.Settings.Default.GenreName = cbGenreList.Items[cbGenreList.SelectedIndex].ToString();
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.MainWindowTop = Top;
            Properties.Settings.Default.MainWindowLeft = Left;
            Properties.Settings.Default.MainWindowWidth = Width;
            Properties.Settings.Default.MainWindowHeight = Height;
            //Properties.Settings.Default.CategoryListWidth = colDef0.ActualWidth;
            //Properties.Settings.Default.ItemListWidth = colDef20.ActualWidth;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// キー入力処理
        /// 画面上にButtonやComboBoxを追加すると矢印キーやタブキーがkeyDownでは
        /// 取得できなくなるのでPreviewKeyDownで取得する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            bool control = false;
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                control = true;

            if (e.Key == Key.F12) {
                //  スクリーンキャプチャ
                screenCapture();
            } else if (e.Key == Key.F11) {
                //  クリップボード画像の編集
                getClipbordImage();
            }
        }

        /// <summary>
        /// Fontの種類を選択変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbFontFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbFontFamily.SelectedItem != null && mFontFamilyEnabled) {
                rtTextEditor.Selection.ApplyPropertyValue(Inline.FontFamilyProperty, cbFontFamily.SelectedItem);
            }
        }

        /// <summary>
        /// FontSizeを選択変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mFontSizeEnabled) {
                rtTextEditor.Selection.ApplyPropertyValue(Inline.FontSizeProperty, cbFontSize.SelectedItem);
            }
        }

        /// <summary>
        /// ItemListのメニュー選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbItemMenu_Click(object sender, RoutedEventArgs e)
        {
            saveCurFile(true);
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("lbItemAddMenu") == 0) {
                addItem();
            }else if (menuItem.Name.CompareTo("lbItemRenameMenu") == 0) {
                renameItem();
            } else if (menuItem.Name.CompareTo("lbItemRemoveMenu") == 0) {
                removeItem();
            } else if (menuItem.Name.CompareTo("lbItemCopyMenu") == 0) {
                copyItem();
            } else if (menuItem.Name.CompareTo("lbItemMoveMenu") == 0) {
                copyItem(true);
            } else if (menuItem.Name.CompareTo("lbItemLinkMenu") == 0) {
                linkItem();
            } else if (menuItem.Name.CompareTo("lbItemOpenMenu") == 0) {
                openItem();
            } else if (menuItem.Name.CompareTo("lbItemReloadMenu") == 0) {
                reloadItem();
            } else if (menuItem.Name.CompareTo("lbItemImportMenu") == 0) {
                importItem();
            } else if (menuItem.Name.CompareTo("lbItemExprtMenu") == 0) {
                exportItem();
            } else if (menuItem.Name.CompareTo("lbItemPropertyMenu") == 0) {
                propertyItem();
            }
        }

        /// <summary>
        /// CategoryListのメニュー選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbCategoryMenu_Click(object sender, RoutedEventArgs e)
        {
            saveCurFile(true);
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("lbCategoryAddMenu") == 0) {
                addCategory();
            } else if (menuItem.Name.CompareTo("lbCategoryRenameMenu") == 0) {
                renameCategory();
            } else if (menuItem.Name.CompareTo("lbCategoryRemoveMenu") == 0) {
                removeCategory();
            } else if (menuItem.Name.CompareTo("lbCategoryCopyMenu") == 0) {
                copyCategory();
            } else if (menuItem.Name.CompareTo("lbCategoryMoveMenu") == 0) {
                copyCategory(true);
            }
        }

        /// <summary>
        /// Genre(大分類)のメニュー選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbGenreMenu_Click(object sender, RoutedEventArgs e)
        {
            saveCurFile(true);
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("cbGenreAddMenu") == 0) {
                addGenre();
            } else if (menuItem.Name.CompareTo("cbGenreRenameMenu") == 0) {
                renameGenre();
            } else if (menuItem.Name.CompareTo("cbGenreRemoveMenu") == 0) {
                removeGenre();
            }
        }

        /// <summary>
        /// RichTextBoxのコンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rtEditorMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            TextRange range = new TextRange(rtTextEditor.Selection.Start, rtTextEditor.Selection.End);
            if (menuItem.Name.CompareTo("rtEditorCalcMenu") == 0) {
                range.Text = textCalulate(range.Text);
            } else if (menuItem.Name.CompareTo("rtEditorDateTimeMenu") == 0) {
                range.Text = textDateTime(range.Text);
            } else if (menuItem.Name.CompareTo("rtEditorUrlCnvMenu") == 0) {
                range.Text = Uri.UnescapeDataString(range.Text);
            }
        }


        /// <summary>
        /// Item(ファイル)の選択変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbItemList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= lbItemList.SelectedIndex && mEnableItemList) {
                saveCurFile(true);
                mCurItemPath = getItemPath();
                if (loadFile(mCurItemPath, mFileFormat)) {
                    setTitle();
                } else {
                    mCurItemPath = "";
                }
            }
        }

        /// <summary>
        /// Category(小分類)の選択変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbCategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= lbCategoryList.SelectedIndex && mEnableCategoryList) {
                lbItemList.SelectedIndex = -1;
                getItemList();
                lbItemList.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Genre(大分類)の選択変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbGenreList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= cbGenreList.SelectedIndex && mEnableGenreList) {
                lbCategoryList.SelectedIndex = -1;
                getCategoryList();
                lbCategoryList.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 文字色の変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btFontColor_Click(object sender, RoutedEventArgs e)
        {
            fontColor(rtTextEditor);
        }

        /// <summary>
        /// 文字は背景色の設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btFontBackColor_Click(object sender, RoutedEventArgs e)
        {
            fontColor(rtTextEditor, true);
        }

        /// <summary>
        /// テキスト検索
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btSearch_Click(object sender, RoutedEventArgs e)
        {
            searchWord(rtTextEditor, tbSearchWord.Text);
        }

        /// <summary>
        /// 設定メニューの表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btSetting_Click(object sender, RoutedEventArgs e)
        {
            MenuDialog dlg = new MenuDialog();
            dlg.mMainWindow = this;
            dlg.mHorizontalAliment = 0;
            dlg.mVerticalAliment = 2;
            dlg.Title = "設定メニュー";
            dlg.mMenuList = mSettingMenu.ToList();
            dlg.mOneClick = true;
            dlg.ShowDialog();
            int index = mSettingMenu.FindIndex(dlg.mResultMenu);
            switch (index) {
                case 0: dataBackUp(); break;
                case 1: dataRestor(); break;
                case 2: setRootFolder(); break;
                case 3: setBackupFolder(); break;
                case 4: setInitFolder(); break;
                case 5: infoProperty(); break;
            }
        }

        /// <summary>
        /// スクリーンキャプチャ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btScreenCapture_Click(object sender, RoutedEventArgs e)
        {
            screenCapture();
        }

        /// <summary>
        /// クリップボードの画像データをサイズ指定で貼り付け
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btImagePaste_Click(object sender, RoutedEventArgs e)
        {
            getClipbordImage();
        }

        /// <summary>
        /// RichTextBoxでマウスダブルクリック
        /// 関連付けファイルの実行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rtTextEditor_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //  カーソルの位置の単語取得
            string word = getCursorPosWord(rtTextEditor);
            //  ファイルの実行(開く)
            if (0 < word.Length && (0 <= word.IndexOf("http") || File.Exists(word)) )
                ylib.openUrl(word);
            else {
                string str = getCursorPosData(rtTextEditor);
                if (2 < str.Length) {
                    List<string> listData = getWordList(str.Substring(1, str.Length - 2), true);
                    executeFunc(listData);
                }
            }
        }

        /// <summary>
        /// マウスの左ボタンダウン(Previewないと取れない)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rtTextEditor_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            setFontConbBox();
        }

        /// <summary>
        /// 数式から計算処理をおこなう
        /// [引数]がある場合は前文から[引数]を検索して計算する
        /// </summary>
        /// <param name="text">数式</param>
        /// <returns>数式+計算結果</returns>
        private string textCalulate(string text)
        {
            YCalc calc = new YCalc();

            //  数式文字以外を除く
            string express = ylib.stripControlCode(text);
            express = calc.stripExpressData(express);

            //  引数の抽出
            double result = 0;
            calc.setExpression(express);
            string[] arg = calc.getArgKey();
            if (0 < arg.Length) {
                TextRange argRange = new TextRange(rtTextEditor.Document.ContentStart, rtTextEditor.Selection.Start);
                string argText = argRange.Text;
                for (int i = 0; i < arg.Length; i++) {
                    int pos = argText.LastIndexOf(arg[i]);
                    //  内部引数は除外して引数を設定
                    if (0 <= pos && 0 > calc.mInnerParameter.FindIndex(arg[i])) {
                        string val = getArgVal(argText.Substring(pos));
                        calc.setArgValue(arg[i], val);
                    }
                }
                result = calc.calculate();
            } else {
                result = calc.expression(express);
            }
            if (calc.mError)
                text += " = " + calc.mErrorMsg;
            else
                text += " = " + result.ToString();
            return text;
        }

        /// <summary>
        /// RichTextBoxのカーソル位置の文字列を抽出
        /// スペース(' ')またはダブルクォーテーション(")で囲まれた文字列か、一行分の文字列
        /// </summary>
        /// <param name="richText">RichTextBox</param>
        /// <returns>文字列</returns>
        private string getCursorPosWord(RichTextBox richText)
        {
            TextPointer caretPos = richText.CaretPosition;
            string word = caretPos.GetTextInRun(LogicalDirection.Backward);
            int p = word.LastIndexOf("\"");
            if (p < 0)
                p = word.LastIndexOf(" ");
            if (0 <= p)
                word = word.Substring(p + 1);
            word += caretPos.GetTextInRun(LogicalDirection.Forward);
            p = word.IndexOf("\"");
            if (p < 0)
                p = word.IndexOf(" ");
            if (0 < p)
                word = word.Substring(0, p);

            return word;
        }

        /// <summary>
        /// カーソル位置で"[[""]]"で囲まれた文字列を抽出する
        /// </summary>
        /// <param name="richText">RichTextBox</param>
        /// <returns>文字列</returns>
        private string getCursorPosData(RichTextBox richText)
        {
            TextPointer caretPos = richText.CaretPosition;
            string word = caretPos.GetTextInRun(LogicalDirection.Backward);
            int p = word.LastIndexOf("[[");
            if (p < 0)
                return "";
            word = word.Substring(p);
            word += caretPos.GetTextInRun(LogicalDirection.Forward);
            word = ylib.getBracketInData(word, '[', ']', true);

            TextRange range = new TextRange(rtTextEditor.Document.ContentStart, rtTextEditor.Document.ContentEnd);
            string text = range.Text;
            p = text.IndexOf(word);
            if (0 <= p) {
                text = text.Substring(p);
                text = ylib.getBracketInData(text, '[', ']', true);
                return text;
            }
            return "";
        }

        /// <summary>
        /// 文字列を'['']'で分解してリストに変換
        /// </summary>
        /// <param name="text">文字列</param>
        /// <returns>分解リスト</returns>
        private List<string> getWordList(string text, bool removeComment = false)
        {
            List<string> strList = new List<string>();
            int bcount = 0;
            string buf = "";
            for (int i = 0; i < text.Length; i++) {
                if (text[i] == '[') {
                    if (0 < buf.Length && bcount == 0) {
                        strList.Add(buf);
                        buf = "";
                    }
                    bcount++;
                    buf += text[i];
                } else if (text[i]== ']') {
                    bcount--;
                    buf += text[i];
                    if (bcount == 0) {
                        strList.Add(buf);
                        buf = "";
                    }
                } else if (text[i] == '\n') {
                    buf += text[i];
                    if (0 < buf.Length && bcount == 0) {
                        strList.Add(buf);
                        buf = "";
                    }
                } else if (text[i] == '$' && removeComment) {
                    while (text[i] != '\n' && i < text.Length) {
                        i++;
                    }
                    i--;
                } else if (text[i] != '\t' && text[i] != ' ') {
                    buf += text[i];
                }
            }
            return strList;
        }

        /// <summary>
        /// 文字列リスト
        /// </summary>
        /// <param name="dataList"></param>
        private void executeFunc(List<string> dataList)
        {
            if (0 <= dataList[0].IndexOf("[関数グラフ]")) {
                functionGraph(dataList);
            } else if (0 <= dataList[0].IndexOf("[関数3Dグラフ]")) {
                function3DGraph(dataList);
            }
        }

        /// <summary>
        /// 関数のグラフ表示
        /// </summary>
        /// <param name="dataList">データリスト</param>
        private void functionGraph(List<string> dataList)
        {
            FuncPlot dlg = new FuncPlot();
            dlg.Title = ylib.getBracketInData(dataList[1]);
            for (int i = 2; i < dataList.Count; i++) {
                if (0 <= dataList[i].IndexOf("[方程式タイプ")) {
                    int p = dataList[i].IndexOf(':');
                    if (0 <= p) {
                        string buf = dataList[i].Substring(p + 1, dataList[i].Length - p - 2);
                        if (buf == "y=f(x)") dlg.mFuncType = FuncPlot.FUNCTYPE.Normal;
                        else if (buf == "x=f(t),y=g(t)") dlg.mFuncType = FuncPlot.FUNCTYPE.Parametric;
                        else if (buf == "r=f(t)") dlg.mFuncType = FuncPlot.FUNCTYPE.Polar;
                    }
                } else if (0 <= dataList[i].IndexOf("[x範囲") || 0 <= dataList[i].IndexOf("t範囲")) {
                    int p = dataList[i].IndexOf('=');
                    if (0 <= p) {
                        string buf = dataList[i].Substring(p + 1, dataList[i].Length - p - 2);
                        string[] data = buf.Split(',');
                        dlg.mXminStr = 0 < data.Length ? data[0] : "";
                        dlg.mXmaxStr = 1 < data.Length ? data[1] : "";
                        dlg.mDivCountStr = 2 < data.Length ? data[2] : "";
                    }
                } else if (0 <= dataList[i].IndexOf("[y範囲")) {
                    int p = dataList[i].IndexOf('=');
                    if (0 <= p) {
                        string buf = dataList[i].Substring(p + 1, dataList[i].Length - p - 2);
                        string[] data = buf.Split(',');
                        dlg.mYminStr = 0 < data.Length ? data[0] : "";
                        dlg.mYmaxStr = 1 < data.Length ? data[1] : "";
                        dlg.mAutoHeight = 2 < data.Length ? data[2] == "auto" : false;
                    } else {
                        dlg.mAutoHeight = true;
                    }
                } else if (0 <= dataList[i].IndexOf("[アスペクト比固定")) {
                    int p = dataList[i].IndexOf('=');
                    if (0 < p) {
                        dlg.mAspectFix = ylib.boolParse(dataList[i].Substring(p + 1, dataList[i].Length - p - 2));
                    }
                } else if (0 <= dataList[i].IndexOf("[背景色")) {
                    int p = dataList[i].IndexOf('=');
                    if (0 <p) {
                        string colorName = dataList[i].Substring(p + 1, dataList[i].Length - p - 2);
                        int colorNo = YDrawingShapes.mColorTitle.FindIndex(c => colorName == c);
                        if (0 <= colorNo)
                            dlg.mBackColor = YDrawingShapes.mColor[colorNo];
                    }
                } else if (0 <= dataList[i].IndexOf("[方程式:")) {
                    int p = dataList[i].IndexOf(':');
                    if (0 <= p) {
                        string buf = dataList[i].Substring(p + 1, dataList[i].Length - p - 2);
                        string[] data = buf.Split('\n');
                        List<string> listData = new List<string>();
                        for (int j = 0; j < data.Length; j++) {
                            int pc = data[j].IndexOf("$");
                            string func = ylib.stripControlCode(0 <= pc ? data[j].Substring(0, pc) : data[j]);
                            if (0 < func.Length)
                                listData.Add(func);
                        }
                        dlg.mFuncList = listData;
                    }
                }
            }

            dlg.Show();
        }

        /// <summary>
        /// 3次元関数のグラフ表示
        /// </summary>
        /// <param name="dataList">データリスト</param>
        private void function3DGraph(List<string> dataList)
        {
            FuncPlot3D dlg = new FuncPlot3D();
            dlg.Title = ylib.getBracketInData(dataList[1]);
            for (int i = 2; i < dataList.Count; i++) {
                if (0 <= dataList[i].IndexOf("[方程式タイプ")) {
                    int p = dataList[i].IndexOf(':');
                    if (0 <= p) {
                        string buf = dataList[i].Substring(p + 1, dataList[i].Length - p - 2);
                        if (buf == "z=f(x,y)") dlg.mFuncType = FuncPlot3D.FUNCTYPE.Normal;
                        else if (buf == "x=f(s,t),y=g(s,t),z=h(s,t)") dlg.mFuncType = FuncPlot3D.FUNCTYPE.Parametric;
                    }
                } else if (0 <= dataList[i].IndexOf("[x範囲") || 0 <= dataList[i].IndexOf("s範囲")) {
                    int p = dataList[i].IndexOf('=');
                    if (0 <= p) {
                        string buf = dataList[i].Substring(p + 1, dataList[i].Length - p - 2);
                        string[] data = buf.Split(',');
                        dlg.mXminStr = 0 < data.Length ? data[0] : "";
                        dlg.mXmaxStr = 1 < data.Length ? data[1] : "";
                        dlg.mDivCountStr = 2 < data.Length ? data[2] : "";
                    }
                } else if (0 <= dataList[i].IndexOf("[y範囲") || 0 <= dataList[i].IndexOf("t範囲")) {
                    int p = dataList[i].IndexOf('=');
                    if (0 <= p) {
                        string buf = dataList[i].Substring(p + 1, dataList[i].Length - p - 2);
                        string[] data = buf.Split(',');
                        dlg.mYminStr = 0 < data.Length ? data[0] : "";
                        dlg.mYmaxStr = 1 < data.Length ? data[1] : "";
                    }
                } else if (0 <= dataList[i].IndexOf("[z範囲")) {
                    int p = dataList[i].IndexOf('=');
                    if (0 <= p) {
                        string buf = dataList[i].Substring(p + 1, dataList[i].Length - p - 2);
                        string[] data = buf.Split(',');
                        dlg.mZminStr = 0 < data.Length ? data[0] : "";
                        dlg.mZmaxStr = 1 < data.Length ? data[1] : "";
                        dlg.mAutoHeight = 2 < data.Length ? data[2] == "auto" : false;
                    } else {
                        dlg.mAutoHeight = true;
                    }
                } else if (0 <= dataList[i].IndexOf("[表示形式")) {
                    int p = dataList[i].IndexOf('=');
                    if (0 < p) {
                        string buf = dataList[i].Substring(p + 1, dataList[i].Length - p - 2);
                        dlg.mSurface = buf == "Surface" ;
                    }
                } else if (0 <= dataList[i].IndexOf("[アスペクト比固定")) {
                    int p = dataList[i].IndexOf('=');
                    if (0 < p) {
                        dlg.mAspectFix = ylib.boolParse(dataList[i].Substring(p + 1, dataList[i].Length - p - 2));
                    }
                } else if (0 <= dataList[i].IndexOf("[背景色")) {
                    int p = dataList[i].IndexOf('=');
                    if (0 < p) {
                        string colorName = dataList[i].Substring(p + 1, dataList[i].Length - p - 2);
                        int colorNo = GL3DLib.mColor4Title.FindIndex(colorName);
                        if (0 <= colorNo)
                            dlg.mBackColor = GL3DLib.mColor4[colorNo];
                    }
                } else if (0 <= dataList[i].IndexOf("[方程式:")) {
                    int p = dataList[i].IndexOf(':');
                    if (0 <= p) {
                        string buf = dataList[i].Substring(p + 1, dataList[i].Length - p - 2);
                        string[] data = buf.Split('\n');
                        List<string> listData = new List<string>();
                        for (int j = 0; j < data.Length; j++) {
                            int pc = data[j].IndexOf("$");
                            string func = ylib.stripControlCode(0 <= pc ? data[j].Substring(0, pc) : data[j]);
                            if (0 < func.Length)
                                listData.Add(func);
                        }
                        dlg.mFuncList = listData;
                    }
                }
            }

            dlg.Show();
        }

        /// <summary>
        /// 日時挿入・変換のメニューダイヤログを表示し、挿入・変換を行う
        /// </summary>
        /// <param name="text">選択文字列</param>
        /// <returns>変換文字列</returns>
        private string textDateTime(string text)
        {
            MenuDialog dlg = new MenuDialog();
            dlg.mMainWindow = this;
            dlg.Title = "日時挿入・変換メニュー";
            dlg.mMenuList = mDateTimeMenu.ToList();
            dlg.mOneClick = true;
            dlg.ShowDialog();
            int index = mDateTimeMenu.FindIndex(dlg.mResultMenu);
            DateTime now = DateTime.Now;
            switch (index) {
                case 0: text = now.ToString("yyyy年M月d日"); break;
                case 1: text = now.ToString("\'yy年M月d日"); break;
                case 2: text = now.ToString("yyyy/MM/dd"); break;
                case 3: text = ylib.toWareki(); break;
                case 4: text = now.ToString("HH時mm分ss秒"); break;
                case 5: text = now.ToString("tth時m分s秒"); break;
                case 6: text = now.ToString("T"); break;
                case 7: text = convDateFormat(text); break;
                case 8: text = convDateFormat(text, false); break;
                case 9: text = convDate2Week(text, 0); break;
                case 10: text = convDate2Week(text, 1); break;
                case 11: text = convDate2Week(text, 2); break;
                case 12: text = convDate2Week(text, 3); break;
            }

            return text;
        }

        /// <summary>
        /// 西暦の日付を和暦に変換、和暦の日付を西暦に変換
        /// </summary>
        /// <param name="text">日付文字列</param>
        /// <param name="wareki">和暦/西暦</param>
        /// <returns>変換日付</returns>
        private string convDateFormat(string text, bool wareki = true)
        {
            (int index,string dateStr) = ylib.getDateMatch(text);
            if (0 < dateStr.Length) {
                string date = ylib.cnvDateFormat(dateStr);
                if (0 < date.Length) {
                    DateTime dt = DateTime.Parse(date);
                    if (wareki) {
                        text = text.Replace(dateStr, ylib.toWareki(dt.ToString("yyyy/MM/dd")));
                    } else {
                        text = text.Replace(dateStr, dt.ToString("yyyy年M月d日"));
                    }
                }
            }
            return text;
        }

        /// <summary>
        /// 選択した日付に曜日を追加
        /// 曜日のタイプ  0:Sunday 1:SUN 2:日曜日 3:日
        /// </summary>
        /// <param name="text">日付文字列</param>
        /// <param name="type">曜日のタイプ</param>
        /// <returns>曜日付き日付</returns>
        private string convDate2Week(string text, int type)
        {
            (int index, string dateStr) = ylib.getDateMatch(text);
            if (0 < dateStr.Length) {
                string date = ylib.cnvDateFormat(dateStr);
                if (0 < date.Length) {
                    text += " " + ylib.cnvDateWeekday(type, date);
                }
            }
            return text;

        }

        /// <summary>
        /// 引数の値を抽出する
        /// </summary>
        /// <param name="buf">引数文字列</param>
        /// <returns>値</returns>
        private string getArgVal(string buf)
        {
            string valbuf = "0";
            int eqPos = buf.IndexOf('=');
            int nextArgStart = buf.IndexOf('[', buf.IndexOf(']'));
            if (eqPos < 0 || (0 <= nextArgStart && nextArgStart < eqPos)) {
                buf = "";
            } else {
                valbuf = buf.Substring(eqPos + 1);
                nextArgStart = valbuf.IndexOf('[');
                if (0 <= nextArgStart) {
                    valbuf = valbuf.Substring(0, nextArgStart);
                }
                valbuf = ylib.string2StringNum(valbuf);
            }
            return valbuf;
        }

        /// <summary>
        /// 選択された文字の背景色を変更する
        /// </summary>
        /// <param name="richTextBox">RichTextBox</param>
        /// <param name="backColor">背景色</param>
        private void fontColor(RichTextBox richTextBox, bool backColor = false)
        {
            //  カラーダイヤログ [参照の追加][アセンブリ][System.Windows.Forms]の設定が必要
            //  ColorDialogの Color.A...を使うには [System.Drwing]の参照も追加
            var colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                var wpfcolor = Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B);
                TextRange range = new TextRange(richTextBox.Selection.Start, richTextBox.Selection.End);
                if (backColor)
                    range.ApplyPropertyValue(FlowDocument.BackgroundProperty, new SolidColorBrush(wpfcolor));
                else
                    range.ApplyPropertyValue(FlowDocument.ForegroundProperty, new SolidColorBrush(wpfcolor));
            }
        }


        /// <summary>
        /// 文字列検索 検索した文字列を選択状態にする
        /// 大文字と小文字は区別しない
        /// </summary>
        /// <param name="richTextBox">RichTextBox</param>
        /// <param name="searchText">検索文字列</param>
        private void searchWord(RichTextBox richTextBox, string searchText)
        {
            if (mCurTextPointer ==null)
                mCurTextPointer = richTextBox.Document.ContentStart;
            searchText = searchText.ToLower();
            while (mCurTextPointer != null) {
                TextPointer end = mCurTextPointer.GetPositionAtOffset(searchText.Length);
                if (end != null) {
                    TextRange foundText = new TextRange(mCurTextPointer, end);
                    // 見つかった
                    //if (searchText.Equals(foundText.Text, System.StringComparison.Ordinal)) { //  箇条書きなどの先頭にコントロールコードが入ると不可
                    if (0 <= foundText.Text.ToLower().IndexOf(searchText)) {
                        richTextBox.Focus();    // 次行で選択状態にするためにフォーカスする
                        richTextBox.Selection.Select(foundText.Start, foundText.End);
                        mCurTextPointer = mCurTextPointer.GetNextInsertionPosition(LogicalDirection.Forward);
                        break;
                    }
                }
                mCurTextPointer = mCurTextPointer.GetNextInsertionPosition(LogicalDirection.Forward);
            }
        }

        /// <summary>
        /// スクリーンキャプチャ
        /// </summary>
        private void screenCapture()
        {
            screenCapture(this);
            getClipbordImage(rtTextEditor);
            saveCurFile();
        }

        /// <summary>
        /// クリップボード画像の編集
        /// </summary>
        private void getClipbordImage()
        {
            getClipbordImage(rtTextEditor);
            saveCurFile();
        }

        /// <summary>
        /// 画面の一部を切り取ってクリップボードに貼り付ける
        /// </summary>
        /// <param name="window">親Window</param>
        private void screenCapture(Window window)
        {
            //  自アプリ退避
            mWinState = window.WindowState;
            window.WindowState = WindowState.Minimized;
            System.Threading.Thread.Sleep(500);
            //  全画面をキャプチャ
            BitmapSource bitmapSource = ylib.bitmap2BitmapSource(ylib.getFullScreenCapture()); ;
            //  自アプリを元に戻す
            window.WindowState = mWinState;
            window.Activate();
            //  キャプチャしたイメージを全画面表示し領域を切り取る
            FullView dlg = new FullView();
            dlg.mBitmapSource = bitmapSource;
            if (dlg.ShowDialog() == true) {
                System.Drawing.Bitmap bitmap = ylib.cnvBitmapSource2Bitmap(bitmapSource);
                bitmap = ylib.trimingBitmap(bitmap, dlg.mStartPoint, dlg.mEndPoint);
                if (bitmap != null) {
                    //  クリップボードに張り付ける
                    Clipboard.SetImage(ylib.bitmap2BitmapSource(bitmap));
                }
            }
        }

        /// <summary>
        /// クリップボードの画像を大きさを指定して貼り付ける
        /// </summary>
        /// <param name="rc">RichTextBox</param>
        private void getClipbordImage(RichTextBox rc)
        {
            ImagePaste dlg = new ImagePaste();
            dlg.mMainWindow = this;
            dlg.Title = "画像のサイズ設定";
            if (dlg.ShowDialog() == true) {
                Image image = new Image();
                image.Stretch = Stretch.Fill;
                image.Width = dlg.mWidth;
                image.Height = dlg.mHeight;
                image.Source = dlg.mBitmapSource;
                var tp = rc.CaretPosition.GetInsertionPosition(LogicalDirection.Forward);
                new InlineUIContainer(image, tp);
            }
        }

        /// <summary>
        /// Itemの追加
        /// </summary>
        private void addItem()
        {
            InputBox dlg = new InputBox();
            dlg.mMainWindow = this;
            dlg.Title = "項目の追加";
            dlg.mEditText = "";
            var result = dlg.ShowDialog();
            if (result == true && 0 < dlg.mEditText.Length) {
                string itemPath = getItemPath(dlg.mEditText + mFileExt);
                if (!mItemList.Contains(itemPath)) {
                    saveCurFile(true);
                    createFile(itemPath, mFileFormat);
                    getItemList();
                    int index = lbItemList.Items.IndexOf(Path.GetFileNameWithoutExtension(itemPath));
                    if (0 <= index)
                        lbItemList.SelectedIndex = index;
                }
            }
        }

        /// <summary>
        /// Itemの名前の変更
        /// </summary>
        private void renameItem()
        {
            if (lbItemList.SelectedIndex < 0)
                return;
            string oldItemName = Path.GetFileName(mItemList[lbItemList.SelectedIndex]);
            string oldItem = Path.GetFileNameWithoutExtension(oldItemName);
            InputBox dlg = new InputBox();
            dlg.mMainWindow = this;
            dlg.Title = "項目名の変更";
            dlg.mEditText = oldItem;
            var result = dlg.ShowDialog();
            if (result == true && 0 < dlg.mEditText.Length && oldItem != dlg.mEditText) {
                string oldItemPath = getItemPath(oldItemName);
                string newItemPath = getItemPath(dlg.mEditText + Path.GetExtension(oldItemName));
                if (!mItemList.Contains(newItemPath)) {
                    saveCurFile(true);
                    File.Move(oldItemPath, newItemPath);
                    getItemList();
                    int index = mItemList.IndexOf(newItemPath);
                    if (0 <= index)
                        lbItemList.SelectedIndex = index;
                } else {
                    MessageBox.Show("項目名が重複しています", "項目名の変更");
                }
            }
        }

        /// <summary>
        /// Itemの削除(ファイルの削除)
        /// </summary>
        private void removeItem()
        {
            if (lbItemList.SelectedIndex < 0)
                return;
            string itemPath = mItemList[lbItemList.SelectedIndex];
            string selectItemName = Path.GetFileNameWithoutExtension(itemPath);
            if (MessageBox.Show(selectItemName + " を削除します", "項目削除",MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                File.Delete(itemPath);
                mCurItemPath = "";
                getItemList();
                lbItemList.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Itemをコピーまたは移動
        /// </summary>
        /// <param name="move">移動フラグ</param>
        private void copyItem(bool move = false)
        {
            if (lbItemList.SelectedIndex < 0)
                return;
            string itemName = Path.GetFileName(mItemList[lbItemList.SelectedIndex]);

            SelectCategory dlg = new SelectCategory();
            dlg.mMainWindow = this;
            dlg.mRootFolder = mRootFolder;
            if (dlg.ShowDialog() == true) {
                string oldItemPath = getItemPath(itemName);
                string newItemPath = getItemPath(dlg.mSelectGenre, dlg.mSelectCategory, itemName);
                string item = Path.GetFileNameWithoutExtension(itemName);
                string ext = Path.GetExtension(itemName);
                int opt = 1;
                while (File.Exists(newItemPath)) {
                    newItemPath = getItemPath(dlg.mSelectGenre, dlg.mSelectCategory, item + "("+opt+")" + ext);
                    opt++;
                }
                if (move) {
                    File.Move(oldItemPath, newItemPath);
                    mCurItemPath = "";
                } else {
                    File.Copy(oldItemPath, newItemPath);
                }
                getItemList();
            }
        }

        /// <summary>
        /// Itemのリンクを作成
        /// </summary>
        private void linkItem()
        {
            if (lbItemList.SelectedIndex < 0)
                return;
            string itemName = Path.GetFileName(mItemList[lbItemList.SelectedIndex]);

            SelectCategory dlg = new SelectCategory();
            dlg.mMainWindow = this;
            dlg.mRootFolder = mRootFolder;
            if (dlg.ShowDialog() == true) {
                string oldItemPath = getItemPath(itemName);
                string item = Path.GetFileNameWithoutExtension(itemName);
                string ext = mLinkExt;
                string newItemPath = getItemPath(dlg.mSelectGenre, dlg.mSelectCategory, item + ext);
                int opt = 1;
                while (File.Exists(newItemPath)) {
                    newItemPath = getItemPath(dlg.mSelectGenre, dlg.mSelectCategory, item + "(" + opt + ")" + ext);
                    opt++;
                }
                createLinkFile(newItemPath, oldItemPath);
                getItemList();
            }
        }

        /// <summary>
        /// Itemのリンクファイルの作成
        /// </summary>
        /// <param name="itemPath"></param>
        /// <param name="linkPath"></param>
        private void createLinkFile(string itemPath, string linkPath)
        {
            string ext = Path.GetExtension(itemPath);
            if (0 < ext.Length) {
                itemPath = itemPath.Replace(ext, mLinkExt);
            } else {
                itemPath += mLinkExt;
            }
            linkPath = Path.GetFullPath(linkPath);
            linkPath = linkPath.Substring(mRootFolder.Length + 1);
            ylib.saveTextFile(itemPath, linkPath);
        }

        /// <summary>
        /// 表示中の項目をRTFファイルに保存して他のアプリで編集する   
        /// </summary>
        private void openItem()
        {
            if (lbItemList.SelectedIndex < 0)
                return;
            string itemPath = mItemList[lbItemList.SelectedIndex];
            string tmpPath = itemPath.Replace(mFileExt, mFileExts[2]);
            saveFile(tmpPath, mFileFormats[2]);       //  rtfで出力
            if (File.Exists(tmpPath)) {
                ylib.fileExecute(tmpPath);
            }
        }

        /// <summary>
        /// openItemで編集されたRTFファイルの再取り込み
        /// </summary>
        private void reloadItem()
        {
            if (lbItemList.SelectedIndex < 0)
                return;
            string itemPath = mItemList[lbItemList.SelectedIndex];
            string tmpPath = itemPath.Replace(mFileExt, mFileExts[2]);
            tmpPath = tmpPath.Replace(mFileExt, mFileExts[2]);
            if (File.Exists(tmpPath)) {
                if (loadFile(tmpPath, mFileFormats[2])) {
                    File.Delete(tmpPath);
                    mCurItemPath = itemPath;
                }
            }
        }

        /// <summary>
        /// ファイルを選択して取り込む
        /// </summary>
        private void importItem()
        {
            string fpath = ylib.fileSelect(".", "xaml,rtf,txt");
            if (0 < fpath.Length) {
                string ext = Path.GetExtension(fpath);
                int fileNo = mFileExts.FindIndex(ext.ToLower());
                if (0 <= fileNo) {
                    if (loadFile(fpath, mFileFormats[fileNo])) {
                        string item = Path.GetFileNameWithoutExtension(fpath);
                        string savePath = getItemPath(item + mFileExt);
                        int opt = 1;
                        while (File.Exists(savePath)) {
                            savePath = getItemPath(item + "(" + opt + ")" + mFileExt);
                            opt++;
                        }
                        saveFile(savePath, mFileFormats[mFileFormatNo]);
                        getItemList();
                        int index = lbItemList.Items.IndexOf(Path.GetFileNameWithoutExtension(savePath));
                        lbItemList.SelectedIndex = index < 0 ? 0 : index;
                    }
                }
            }
        }

        /// <summary>
        /// ファイル名を指定して保存する
        /// </summary>
        private void exportItem()
        {
            if (lbItemList.SelectedIndex < 0)
                return;
            string itemPath = mItemList[lbItemList.SelectedIndex];
            MenuDialog dlg = new MenuDialog();
            dlg.mMainWindow = this;
            dlg.Title = "ファイルの種類を選択";
            dlg.mMenuList = mFileFormatMenu.ToList();
            dlg.ShowDialog();
            int index = mFileFormatMenu.FindIndex(dlg.mResultMenu);
            if (0 <= index) {
                string fpath = ylib.saveFileSelect(".", mFileExts[index].Substring(1), Path.GetFileNameWithoutExtension(itemPath));
                if (0 < fpath.Length) {
                    saveFile(fpath, mFileFormats[index]);
                }
            }
        }

        /// <summary>
        /// 項目ファイルの属性表示
        /// </summary>
        private void propertyItem()
        {
            if (lbItemList.SelectedIndex < 0)
                return;
            string itemPath = mItemList[lbItemList.SelectedIndex];
            string itemName = Path.GetFileNameWithoutExtension(itemPath);
            string buf = "項目名: " + itemName + "\n";
            buf += "分類名: " + mCurCategory + "\n";
            buf += "大分類名: " + mCurGenre + "\n";
            buf += "パス: " + itemPath + "\n";
            if (Path.GetExtension(itemPath) == mLinkExt) {
                buf += "リンク先: " + getLinkPath(itemPath) + "\n";
            }
            FileInfo fileInfo = new FileInfo(itemPath);
            buf += "ファイルサイズ: " + fileInfo.Length.ToString("#,###") + "\n";
            buf += "作成日: " + fileInfo.CreationTime + "\n";
            buf += "更新日: " + fileInfo.LastWriteTime + "\n";
            MessageBox.Show(buf, "ファイルプロパティ");
        }

        /// <summary>
        /// Category(小分類)の追加
        /// </summary>
        private void addCategory()
        {
            InputBox dlg = new InputBox();
            dlg.mMainWindow = this;
            dlg.Title = "分類の追加";
            dlg.mEditText = "";
            var result = dlg.ShowDialog();
            if (result == true && 0 < dlg.mEditText.Length) {
                string category = dlg.mEditText;
                string categoryPath = getCategoryPath(category);
                if (!mItemList.Contains(categoryPath)) {
                    Directory.CreateDirectory(categoryPath);
                    lbCategoryList.SelectedIndex = 0;
                    getCategoryList();
                    int index = lbCategoryList.Items.IndexOf(category);
                    if (0 <= index)
                        lbCategoryList.SelectedIndex = index;
                }
            }
        }

        /// <summary>
        /// Category(小分類)名の変更
        /// </summary>
        private void renameCategory()
        {
            if (lbCategoryList.SelectedIndex < 0)
                return;
            string oldCategoryName = lbCategoryList.SelectedItem.ToString();
            InputBox dlg = new InputBox();
            dlg.mMainWindow = this;
            dlg.Title = "分類名の変更";
            dlg.mEditText = oldCategoryName;
            var result = dlg.ShowDialog();
            if (result == true && 0 < dlg.mEditText.Length && oldCategoryName != dlg.mEditText) {
                string newCategoryName = dlg.mEditText;
                if (!mCategoryList.Contains(newCategoryName)) {
                    saveCurFile(true);
                    string oldCategoryPath = getCategoryPath(oldCategoryName);
                    string newCategoryPath = getCategoryPath(newCategoryName);
                    Directory.Move(oldCategoryPath, newCategoryPath);
                    lbCategoryList.SelectedIndex = -1;
                    getCategoryList();
                    int index = lbCategoryList.Items.IndexOf(newCategoryName);
                    if (0 <= index) {
                        lbCategoryList.SelectedIndex = index;
                    }
                }
            }
        }

        /// <summary>
        /// 選択ategory(小分類)の削除
        /// </summary>
        private void removeCategory()
        {
            if (lbCategoryList.SelectedIndex < 0)
                return;
            string selectCategoryName = lbCategoryList.SelectedItem.ToString();
            string categoryPath = getCategoryPath(selectCategoryName);
            if (MessageBox.Show(selectCategoryName + " を削除します", "分類削除", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                try {
                    Directory.Delete(categoryPath, true);
                } catch (Exception e) {
                    MessageBox.Show(e.Message);
                }
                mCurItemPath = "";
                lbCategoryList.SelectedIndex = -1;
                getCategoryList();
                lbCategoryList.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 分類のコピーまたは移動
        /// </summary>
        /// <param name="move">移動の有無</param>
        private void copyCategory(bool move = false)
        {
            if (lbCategoryList.SelectedIndex < 0)
                return;
            string selectCategoryName = lbCategoryList.SelectedItem.ToString();

            SelectMenu dlg = new SelectMenu();
            dlg.mMainWindow = this;
            dlg.mMenuList = mGenreList.ConvertAll(x => Path.GetFileName(x)).ToArray();
            if (dlg.ShowDialog() == true) {
                string oldCategoryPath = getCategoryPath(selectCategoryName);
                string newCategoryPath = getCategoryPath(dlg.mSelectItem, selectCategoryName);
                int opt = 1;
                while (Directory.Exists(newCategoryPath)) {
                    newCategoryPath = getCategoryPath(dlg.mSelectItem, selectCategoryName + "(" + opt + ")");
                    opt++;
                }
                if (move) {
                    Directory.Move(oldCategoryPath, newCategoryPath);
                    mCurItemPath = "";
                } else {
                    ylib.copyDrectory(oldCategoryPath, newCategoryPath);
                }
                getCategoryList();
            }
        }

        /// <summary>
        /// Genre(大分類)の追加
        /// </summary>
        private void addGenre()
        {
            InputBox dlg = new InputBox();
            dlg.mMainWindow = this;
            dlg.Title = "大分類の追加";
            dlg.mEditText = "";
            var result = dlg.ShowDialog();
            if (result == true && 0 < dlg.mEditText.Length) {
                string genre = dlg.mEditText;
                string genrePath = getGenrePath(genre);
                if (!mItemList.Contains(genrePath)) {
                    Directory.CreateDirectory(genrePath);
                    getGenreList();
                    int index = cbGenreList.Items.IndexOf(genre);
                    if (0 <= index)
                        cbGenreList.SelectedIndex = index;
                }
            }
        }

        /// <summary>
        /// Genre(大分類)名の変更
        /// </summary>
        private void renameGenre()
        {
            if (cbGenreList.SelectedIndex < 0)
                return;
            string oldGenreName = cbGenreList.SelectedItem.ToString();
            InputBox dlg = new InputBox();
            dlg.mMainWindow = this;
            dlg.Title = "大分類名の変更";
            dlg.mEditText = oldGenreName;
            var result = dlg.ShowDialog();
            if (result == true && 0 < dlg.mEditText.Length && oldGenreName != dlg.mEditText) {
                string newGenreName = dlg.mEditText;
                if (!mGenreList.Contains(newGenreName)) {
                    saveCurFile(true);
                    string oldGenrePath = getGenrePath(oldGenreName);
                    string newGenrePath = getGenrePath(newGenreName);
                    Directory.Move(oldGenrePath, newGenrePath);
                    cbGenreList.SelectedIndex = -1;
                    getGenreList();
                    int index = cbGenreList.Items.IndexOf(newGenreName);
                    if (0 <= index)
                        cbGenreList.SelectedIndex = index;
                }
            }
        }

        /// <summary>
        /// Genre(大分類)の削除
        /// </summary>
        private void removeGenre()
        {
            if (cbGenreList.SelectedIndex < 0)
                return;
            string selectGenreName = cbGenreList.SelectedItem.ToString();
            string genrePath = getGenrePath(selectGenreName);
            if (MessageBox.Show(selectGenreName + " を削除します", "大分類削除", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                try {
                    Directory.Delete(genrePath, true);
                } catch (Exception e) {
                    MessageBox.Show(e.Message);
                }
                mCurItemPath = "";
                cbGenreList.SelectedIndex = -1;
                getGenreList();
                cbGenreList.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// データをバックアップする
        /// </summary>
        private void dataBackUp()
        {
            DirectoryDiff directoryDiff = new DirectoryDiff(mRootFolder, mBackupFolder);
            int count = directoryDiff.syncFolder();
            MessageBox.Show($"{count} ファイルのバックアップを更新しました。");
        }

        /// <summary>
        /// バックアップデータを元に戻す
        /// </summary>
        private void dataRestor()
        {
            DiffFolder dlg = new DiffFolder();
            dlg.Owner = this;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.mSrcTitle  = "比較元(データフォルダ)";
            dlg.mDestTitle = "比較先(バックアップ先)";
            dlg.mSrcFolder  = mRootFolder;
            dlg.mDestFolder = mBackupFolder;
            dlg.ShowDialog();
            //if (MessageBox.Show("バックアップデータを元に戻します\n既存のデータが上書きされます","確認", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
            //    ylib.copyDrectory(mBackupFolder, mRootFolder);
            //    getInitList();
            //}
        }

        /// <summary>
        /// ルートフォルダの設定変更
        /// </summary>
        private void setRootFolder()
        {
            string rootFolder = ylib.folderSelect(mRootFolder);
            if (0 < rootFolder.Length) {
                rootFolder = Path.Combine(rootFolder, mRootFolderName);
                if (!Directory.Exists(rootFolder)) {
                    Directory.CreateDirectory(rootFolder);
                }
                mRootFolder = rootFolder;
                getInitList();
            }
        }

        /// <summary>
        /// バックアップフォルダの設定
        /// </summary>
        private void setBackupFolder()
        {
            string backupFolder = ylib.folderSelect(mBackupFolder);
            if (0 < backupFolder.Length) {
                backupFolder = Path.Combine(backupFolder, mBackupFolderName);
                if (Directory.Exists(backupFolder)) {
                    Directory.CreateDirectory(backupFolder);
                }
                mBackupFolder = backupFolder;
            }
        }

        /// <summary>
        /// 初期値に戻す
        /// </summary>
        private void setInitFolder()
        {
            //  データフォルダの初期値
            mRootFolder = ".\\" + mRootFolderName;
            mRootFolder = Path.GetFullPath(mRootFolder);
            if (!Directory.Exists(mRootFolder))
                Directory.CreateDirectory(mRootFolder);
            getInitList();
            //  バックアップフォルダの初期値
            mBackupFolder = ".\\" + mBackupFolderName;
            mBackupFolder = Path.GetFullPath(mBackupFolder);
            if (Directory.Exists(mBackupFolder))
                Directory.CreateDirectory(mBackupFolder);
        }

        /// <summary>
        /// プロパティ表示
        /// </summary>
        private void infoProperty()
        {
            string mes = "データフォルダ\n" + mRootFolder;
            mes += "\n" + getDirectoryInfo(mRootFolder, "データ", mFileExt);
            mes += "\n" + getDirectoryInfo(mRootFolder, "リンク", mLinkExt);
            mes += "\n\nバックアップフォルダ\n" + mBackupFolder;
            mes += "\n" + getDirectoryInfo(mBackupFolder, "データ", mFileExt);
            mes += "\n" + getDirectoryInfo(mBackupFolder, "リンク", mLinkExt);
            MessageBox.Show(mes, "プロパティ");
        }

        /// <summary>
        /// フォルダ検索情報
        /// </summary>
        /// <param name="folder">検索フォルダ</param>
        /// <param name="title">タイトル</param>
        /// <param name="ext">拡張子</param>
        /// <returns></returns>
        private string getDirectoryInfo(string folder, string title, string ext)
        {
            string buf = "";
            List<FileInfo> fi = ylib.getDirectoriesInfo(folder, "*" + ext);
            long size = fi.Sum(x => x.Length);
            return $"{title}ファイル数: {fi.Count}  データサイズ: {size.ToString("#,###")} byte";
        }

        /// <summary>
        /// リストの初期化(既存データの更新))
        /// </summary>
        private void getInitList()
        {
            mEnableItemList = false;
            mEnableCategoryList = false;
            mEnableGenreList = false;

            getGenreList();
            getCategoryList();
            getItemList();
            mCurItemPath = getItemPath();

            mEnableItemList = true;
            mEnableCategoryList = true;
            mEnableGenreList = true;
        }

        /// <summary>
        /// Genre(大分類ディレクトリ)リストの取得
        /// </summary>
        private void getGenreList()
        {
            mGenreList = ylib.getDirectories(mRootFolder);
            if (mGenreList.Count == 0) {
                string genreDir = Path.Combine(mRootFolder, mInitGenre);
                Directory.CreateDirectory(genreDir);
                mGenreList = ylib.getDirectories(mRootFolder);
            }
            cbGenreList.ItemsSource = mGenreList.ConvertAll(x => Path.GetFileName(x));
            if (0 < cbGenreList.Items.Count)
                cbGenreList.SelectedIndex = 0;
        }

        /// <summary>
        /// 選択されているGenreのパスを求める
        /// </summary>
        /// <returns>Genreパス</returns>
        private string getGenrePath()
        {
            int genreIndex = cbGenreList.SelectedIndex < 0 ? 0 : cbGenreList.SelectedIndex;
            string genre = cbGenreList.Items[genreIndex].ToString();
            return Path.Combine(mRootFolder, genre);
        }

        /// <summary>
        /// Genreのパスを求める
        /// </summary>
        /// <param name="genre">パス</param>
        /// <returns>Genreパス</returns>
        private string getGenrePath(string genre)
        {
            return Path.Combine(mRootFolder, genre);
        }


        /// <summary>
        /// Category(小分類ディレクトリ)リストの取得
        /// </summary>
        private void getCategoryList()
        {
            int genreIndex= cbGenreList.SelectedIndex < 0 ? 0 : cbGenreList.SelectedIndex;
            string genre = cbGenreList.Items[genreIndex].ToString();
            string categroryPath = Path.Combine(mRootFolder, genre);
            mCategoryList = ylib.getDirectories(categroryPath);
            if (mCategoryList.Count == 0) {
                string categoryDir = Path.Combine(categroryPath, mInitCategory);
                Directory.CreateDirectory(categoryDir);
                mCategoryList = ylib.getDirectories(categroryPath);
            }
            lbCategoryList.ItemsSource = mCategoryList.ConvertAll(x => Path.GetFileName(x));
        }

        /// <summary>
        /// Categoryパスの取得
        /// </summary>
        /// <returns>パス</returns>
        private string getCategoryPath()
        {
            string genrePath = getGenrePath();
            int categoryIndex = lbCategoryList.SelectedIndex < 0 ? 0 : lbCategoryList.SelectedIndex;
            string category = lbCategoryList.Items[categoryIndex].ToString();
            string categroryPath = Path.Combine(genrePath, category);
            return categroryPath;
        }

        /// <summary>
        /// Categoryパスの取得
        /// </summary>
        /// <param name="category">Category名</param>
        /// <returns>パス</returns>
        private string getCategoryPath(string category)
        {
            string genrePath = getGenrePath();
            string categroryPath = Path.Combine(genrePath, category);
            return categroryPath;
        }

        /// <summary>
        /// Categoryパスの取得
        /// </summary>
        /// <param name="genre">Genre名</param>
        /// <param name="category">Category名</param>
        /// <returns>パス</returns>
        private string getCategoryPath(string genre, string category)
        {
            string categroryPath = Path.Combine(mRootFolder, genre);
            categroryPath = Path.Combine(categroryPath, category);
            return categroryPath;
        }

        /// <summary>
        /// 選択されている項目リストの取得
        /// </summary>
        private void getItemList()
        {
            int genreIndex = cbGenreList.SelectedIndex < 0 ? 0 : cbGenreList.SelectedIndex;
            string genre = cbGenreList.Items[genreIndex].ToString();
            int categoryIndex = lbCategoryList.SelectedIndex < 0 ? 0 : lbCategoryList.SelectedIndex;
            string category = lbCategoryList.Items[categoryIndex].ToString();
            getItemList(genre, category);
        }

        /// <summary>
        /// Item(ファイル)リストの作成しListBoxに登録
        /// </summary>
        /// <param name="genre">大分類</param>
        /// <param name="category">小分類</param>
        private void getItemList(string genre, string category)
        {
            string itemFolder = Path.Combine(mRootFolder, genre);
            itemFolder = Path.Combine(itemFolder, category);
            string itemPath = Path.Combine(itemFolder, "*" + mFileExt);
            string itemPath2 = Path.Combine(itemFolder, "*" + mLinkExt);
            mItemList = ylib.getFiles(itemPath).ToList();
            mItemList.Sort();
            mItemList.AddRange(ylib.getFiles(itemPath2).ToList());
            if (mItemList.Count == 0) {
                //  ファイルが存在しない場合、からファイルをつくる
                string itemFile = Path.Combine(mRootFolder, genre);
                itemFile = Path.Combine(itemFile, category);
                itemFile = Path.Combine(itemFile, mInitItem + mFileExt);
                createFile(itemFile, mFileFormat);
                mItemList = ylib.getFiles(itemPath).ToList();
            }
            lbItemList.ItemsSource = mItemList.ConvertAll(x => Path.GetFileNameWithoutExtension(x));
        }

        /// <summary>
        /// 編集中のItemのパスを求める
        /// </summary>
        /// <returns>パス</returns>
        private string getItemPath()
        {
            int itemIndex = lbItemList.SelectedIndex < 0 ? 0 : lbItemList.SelectedIndex;
            return mItemList[itemIndex];
        }

        /// <summary>
        /// Item(ファイル)のパスを求める
        /// </summary>
        /// <param name="itemName">Item名(拡張子あり)</param>
        /// <param name="write">ファイル作成時</param>
        /// <returns>パス</returns>
        private string getItemPath(string itemName)
        {
            itemName = Path.GetFileName(itemName);
            string categoryPath = getCategoryPath();
            string itemPath = Path.Combine(categoryPath, itemName);
            return itemPath;
        }

        /// <summary>
        /// Item(ファイル)のパスを求める
        /// </summary>
        /// <param name="category">Category名</param>
        /// <param name="itemName">Item名</param>
        /// <param name="write">ファイル作成時</param>
        /// <returns>パス</returns>
        private string getItemPath(string category, string itemName)
        {
            itemName = Path.GetFileName(itemName);
            string categoryPath = getCategoryPath(category);
            string itemPath = Path.Combine(categoryPath, itemName);
            return itemPath;
        }

        /// <summary>
        /// Item(ファイル)のパスを求める
        /// </summary>
        /// <param name="genre">Genre名</param>
        /// <param name="category">Category名</param>
        /// <param name="itemName">Item名(拡張子付き)</param>
        /// <param name="write">ファイル作成時</param>
        /// <returns>パス</returns>
        private string getItemPath(string genre, string category, string itemName)
        {
            itemName = Path.GetFileName(itemName);
            string categoryPath = getCategoryPath(genre, category);
            string itemPath = Path.Combine(categoryPath, itemName);
            return itemPath;
        }

        /// <summary>
        /// カーソル位置のフォントファミリーとサイズを取得してコンボボックスに設定
        /// </summary>
        private void setFontConbBox()
        {
            mFontSizeEnabled = false;
            mFontFamilyEnabled = false;
            object fontSize = rtTextEditor.Selection.GetPropertyValue(Inline.FontSizeProperty);
            int index = cbFontSize.Items.IndexOf(Math.Round((double)fontSize));
            cbFontSize.SelectedIndex = index;
            object fontFamily = rtTextEditor.Selection.GetPropertyValue(Inline.FontFamilyProperty);
            index = cbFontFamily.Items.IndexOf(fontFamily); 
            cbFontFamily.SelectedIndex = index;
            mFontSizeEnabled = true;
            mFontFamilyEnabled = true;
        }

        /// <summary>
        /// タイトルを設定
        /// </summary>
        private void setTitle()
        {
            Title = "ノートもどき [" + Path.GetFileNameWithoutExtension(mCurItemPath) + "]";
        }

        /// <summary>
        /// 編集中のファイルを保存する
        /// </summary>
        /// <param name="curPathClear">カレントファイル名クリア</param>
        private void saveCurFile(bool curPathClear = false)
        {
            if (0 < mCurItemPath.Length)
                saveFile(mCurItemPath, mFileFormat);
            if (curPathClear)
                mCurItemPath = "";
        }

        /// <summary>
        /// ドキュメントの文字数と行数を表示
        /// </summary>
        private void setInfoData()
        {
            TextRange range = new TextRange(rtTextEditor.Document.ContentStart, rtTextEditor.Document.ContentEnd);
            string text = range.Text;
            int crCount = text.Count(f => f == '\n');
            int lfCount = text.Count(f => f == '\r');
            tbStatusbar.Text = $"文字数[{text.Length - crCount - lfCount}] 行数[{crCount}]";
        }

        /// <summary>
        /// ファイルを保存
        /// </summary>
        /// <param name="path">ファイルパス</param>
        /// <param fileFormat="path">ファイルフォーマット</param>
        private void saveFile(string path, string fileFormat)
        {
            if (path.Length == 0)
                return;
            path = getLinkPath(path);
            try {
                TextRange range;
                FileStream fStream;
                range = new TextRange(rtTextEditor.Document.ContentStart, rtTextEditor.Document.ContentEnd);
                fStream = new FileStream(path, FileMode.Create);
                range.Save(fStream, fileFormat);
                fStream.Close();
            } catch (Exception e) {
                MessageBox.Show(e.Message);
            }
        }

        /// <summary>
        /// RichTextBoxに読み込む
        /// </summary>
        /// <param name="path">ファイルパス</param>
        /// <param fileFormat="path">ファイルフォーマット</param>
        private bool loadFile(string fpath, string fileFormat)
        {
            string path = getLinkPath(fpath);
            TextRange range;
            FileStream fStream;
            if (File.Exists(path)) {
                try {
                    //  ファイルが存在する時は読み込む
                    range = new TextRange(rtTextEditor.Document.ContentStart, rtTextEditor.Document.ContentEnd);
                    fStream = new FileStream(path, FileMode.OpenOrCreate);
                    range.Load(fStream, fileFormat);
                    fStream.Close();
                    setInfoData();
                    return true;
                } catch (Exception e) {
                    MessageBox.Show(e.Message);
                }
            } else {
                if (Path.GetExtension(fpath) == mLinkExt) {
                    MessageBox.Show("リンク先のファイルが存在しません。削除されたかファイル名が変更された可能性があります。");
                } else {
                    MessageBox.Show("ファイルが存在しません。");
                }
            }
            return false;
        }

        /// <summary>
        /// リンクファイルのリンク先パスの取得
        /// </summary>
        /// <param name="path">リンクファイルパス</param>
        /// <returns></returns>
        private string getLinkPath(string path)
        {
            if (Path.GetExtension(path) == mLinkExt) {
                string buf = ylib.loadTextFile(path);
                if (0 < buf.Length) {
                    path = Path.Combine(mRootFolder, buf);
                }
            }
            return path;
        }

        /// <summary>
        /// 空のファイルを作成
        /// </summary>
        /// <param name="path">ファイルパス</param>
        /// <param fileFormat="path">ファイルフォーマット</param>
        private void createFile(string path, string fileFormat)
        {
            TextRange range;
            FileStream fStream;
            if (!File.Exists(path)) {
                try {
                    //  ファイルがない時は空のファイルを作成
                    range = new TextRange(rtTextEditor.Document.ContentStart, rtTextEditor.Document.ContentStart);
                    fStream = new FileStream(path, FileMode.Create);
                    range.Save(fStream, fileFormat);
                    fStream.Close();
                } catch (Exception e) {
                    MessageBox.Show(e.Message);
                }
            }
        }
    }
}

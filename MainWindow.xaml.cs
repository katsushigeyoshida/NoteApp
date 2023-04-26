using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
                screenCapture(rtTextEditor);
            } else if (e.Key == Key.F11) {
                getClipbordImage(rtTextEditor);
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
            saveCurFile();
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
            } else if (menuItem.Name.CompareTo("lbItemOpenMenu") == 0) {
                openItem();
            } else if (menuItem.Name.CompareTo("lbItemReloadMenu") == 0) {
                reloadItem();
            } else if (menuItem.Name.CompareTo("lbItemImportMenu") == 0) {
                importItem();
            } else if (menuItem.Name.CompareTo("lbItemExprtMenu") == 0) {
                exportItem();
            }
        }

        /// <summary>
        /// CategoryListのメニュー選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbCategoryMenu_Click(object sender, RoutedEventArgs e)
        {
            saveCurFile();
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
            saveCurFile();
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
            if (menuItem.Name.CompareTo("rtEditorCalcMenu") == 0) {
                TextRange range = new TextRange(rtTextEditor.Selection.Start, rtTextEditor.Selection.End);
                range.Text = textCalulate(range.Text);
            } else if (menuItem.Name.CompareTo("rtEditorDateTimeMenu") == 0) {
                TextRange range = new TextRange(rtTextEditor.Selection.Start, rtTextEditor.Selection.End);
                range.Text = textDateTime(range.Text);
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
                saveCurFile();
                mCurItemPath = getItemPath();
                loadFile(mCurItemPath, mFileFormat);
                setTitle();
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
            screenCapture(rtTextEditor);
        }

        /// <summary>
        /// クリップボードの画像データをサイズ指定で貼り付け
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btImagePaste_Click(object sender, RoutedEventArgs e)
        {
            getClipbordImage(rtTextEditor);
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
            TextPointer caretPos = rtTextEditor.CaretPosition;
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
            //  ファイルの実行(開く)
            if (0 < word.Length && (0 <= word.IndexOf("http") || File.Exists(word)) )
                ylib.openUrl(word);
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
        /// 画面の一部を切り取ってカーソル位置に貼り付ける
        /// </summary>
        /// <param name="rc">RichTextBox</param>
        private void screenCapture(RichTextBox rc)
        {
            //  自アプリ退避
            mWinState = this.WindowState;
            this.WindowState = WindowState.Minimized;
            System.Threading.Thread.Sleep(500);
            //  全画面をキャプチャ
            BitmapSource bitmapSource = ylib.bitmap2BitmapSource(ylib.getFullScreenCapture()); ;
            //  自アプリを元に戻す
            this.WindowState = mWinState;
            this.Activate();
            //  キャプチャしたイメージを全画面表示し領域を切り取る
            FullView dlg = new FullView();
            dlg.mBitmapSource = bitmapSource;
            if (dlg.ShowDialog() == true) {
                System.Drawing.Bitmap bitmap = ylib.cnvBitmapSource2Bitmap(bitmapSource);
                bitmap = ylib.trimingBitmap(bitmap, dlg.mStartPoint, dlg.mEndPoint);

                //  切り取った領域を貼り付ける
                Image image = new Image();
                image.Stretch = Stretch.Fill;
                if (mImageMaxWidth < bitmap.Width) {
                    image.Width = mImageMaxWidth;
                    image.Height = image.Width * bitmap.Height / bitmap.Width;
                } else {
                    image.Width = bitmap.Width;
                    image.Height = bitmap.Height;
                }
                image.Source = ylib.bitmap2BitmapSource(bitmap);
                var tp = rc.CaretPosition.GetInsertionPosition(LogicalDirection.Forward);
                new InlineUIContainer(image, tp);
            }
        }

        /// <summary>
        /// クリップボードの画像を大きさを指定して貼り付ける
        /// </summary>
        /// <param name="rc"></param>
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
                string itemPath = getItemPath(dlg.mEditText);
                if (!mItemList.Contains(itemPath)) {
                    saveCurFile();
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
            string oldItem = lbItemList.SelectedItem.ToString();
            InputBox dlg = new InputBox();
            dlg.mMainWindow = this;
            dlg.Title = "項目名の変更";
            dlg.mEditText = oldItem;
            var result = dlg.ShowDialog();
            if (result == true && 0 < dlg.mEditText.Length && oldItem != dlg.mEditText) {
                string oldItemPath = getItemPath(oldItem);
                string newItemPath = getItemPath(dlg.mEditText);
                if (!mItemList.Contains(newItemPath)) {
                    saveCurFile();
                    File.Move(oldItemPath, newItemPath);
                    getItemList();
                    int index = lbItemList.Items.IndexOf(Path.GetFileNameWithoutExtension(dlg.mEditText));
                    if (0 <= index)
                        lbItemList.SelectedIndex = index;
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
            string selectItemName = lbItemList.SelectedItem.ToString();
            string itemPath = getItemPath(selectItemName);
            File.Delete(itemPath);
            mCurItemPath = "";
            getItemList();
            lbItemList.SelectedIndex = 0;
        }

        /// <summary>
        /// Itemをコピーまたは移動
        /// </summary>
        /// <param name="move">移動フラグ</param>
        private void copyItem(bool move = false)
        {
            if (lbItemList.SelectedIndex < 0)
                return;
            string itemName = lbItemList.SelectedItem.ToString();

            SelectCategory dlg = new SelectCategory();
            dlg.mMainWindow = this;
            dlg.mRootFolder = mRootFolder;
            if (dlg.ShowDialog() == true) {
                string oldItemPath = getItemPath(itemName);
                string newItemPath = getItemPath(dlg.mSelectGenre, dlg.mSelectCategory, itemName);
                int opt = 1;
                while (File.Exists(newItemPath)) {
                    newItemPath = getItemPath(dlg.mSelectGenre, dlg.mSelectCategory, itemName + "("+opt+")");
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
        /// 表示中の項目をRTFファイルに保存して他のアプリで編集する   
        /// </summary>
        private void openItem()
        {
            if (lbItemList.SelectedIndex < 0)
                return;
            string itemName = lbItemList.SelectedItem.ToString();
            string tmpPath = getItemPath(itemName);
            tmpPath = tmpPath.Replace(mFileExt, mFileExts[2]);
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
            string itemName = lbItemList.SelectedItem.ToString();
            string tmpPath = getItemPath(itemName);
            tmpPath = tmpPath.Replace(mFileExt, mFileExts[2]);
            if (itemName == Path.GetFileNameWithoutExtension(tmpPath)) {
                loadFile(tmpPath, mFileFormats[2]);
                File.Delete(tmpPath);
                mCurItemPath = getItemPath(itemName);
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
                    loadFile(fpath, mFileFormats[fileNo]);
                    string savePath = getItemPath(Path.GetFileNameWithoutExtension(fpath));
                    int opt = 1;
                    while (File.Exists(savePath)) {
                        savePath = getItemPath(Path.GetFileNameWithoutExtension(fpath) + "(" + opt + ")");
                        opt++;
                    }
                    saveFile(savePath, mFileFormats[mFileFormatNo]);
                    getItemList();
                    int index = lbItemList.Items.IndexOf(Path.GetFileNameWithoutExtension(savePath));
                    lbItemList.SelectedIndex = index < 0 ? 0 : index;
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
            string itemName = lbItemList.SelectedItem.ToString();
            MenuDialog dlg = new MenuDialog();
            dlg.mMainWindow = this;
            dlg.Title = "ファイルの種類を選択";
            dlg.mMenuList = mFileFormatMenu.ToList();
            dlg.ShowDialog();
            int index = mFileFormatMenu.FindIndex(dlg.mResultMenu);
            if (0 <= index) {
                string fpath = ylib.saveFileSelect(".", mFileExts[index].Substring(1), itemName);
                if (0 < fpath.Length) {
                    saveFile(fpath, mFileFormats[index]);
                }
            }
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
                    saveCurFile();
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
            Directory.Delete(categoryPath, true);
            mCurItemPath = "";
            lbCategoryList.SelectedIndex = -1;
            getCategoryList();
            lbCategoryList.SelectedIndex = 0;
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
                    saveCurFile();
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
            Directory.Delete(genrePath, true);
            mCurItemPath = "";
            cbGenreList.SelectedIndex = -1;
            getGenreList();
            cbGenreList.SelectedIndex = 0;
        }

        /// <summary>
        /// データをバックアップする
        /// </summary>
        private void dataBackUp()
        {
            ylib.copyDrectory(mRootFolder, mBackupFolder);
        }

        /// <summary>
        /// バックアップデータを元に戻す
        /// </summary>
        private void dataRestor()
        {
            if (MessageBox.Show("バックアップデータを元に戻します\n既存のデータが上書きされます","確認", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                ylib.copyDrectory(mBackupFolder, mRootFolder);
                getInitList();
            }
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
            mes += "\n\nバックアップフォルダ\n" + mBackupFolder;
            MessageBox.Show(mes, "プロパティ");
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
            string itemPath = Path.Combine(mRootFolder, genre);
            itemPath = Path.Combine(itemPath, category);
            itemPath = Path.Combine(itemPath, "*" + mFileExt);
            mItemList = ylib.getFiles(itemPath).ToList();
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
            return getItemPath(lbItemList.Items[itemIndex].ToString());
        }

        /// <summary>
        /// Item(ファイル)のパスを求める
        /// </summary>
        /// <param name="itemName">Item名(拡張子なし)</param>
        /// <returns>パス</returns>
        private string getItemPath(string itemName)
        {
            string categoryPath = getCategoryPath();
            string itemPath = Path.Combine(categoryPath, itemName + mFileExt);
            return itemPath;
        }

        /// <summary>
        /// Item(ファイル)のパスを求める
        /// </summary>
        /// <param name="category">Category名</param>
        /// <param name="itemName">Item名</param>
        /// <returns>パス</returns>
        private string getItemPath(string category, string itemName)
        {
            string categoryPath = getCategoryPath(category);
            string itemPath = Path.Combine(categoryPath, itemName + mFileExt);
            return itemPath;
        }

        /// <summary>
        /// Item(ファイル)のパスを求める
        /// </summary>
        /// <param name="genre">Genre名</param>
        /// <param name="category">Category名</param>
        /// <param name="itemName">Item名</param>
        /// <returns>パス</returns>
        private string getItemPath(string genre, string category, string itemName)
        {
            string categoryPath = getCategoryPath(genre, category);
            string itemPath = Path.Combine(categoryPath, itemName + mFileExt);
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
        private void saveCurFile()
        {
            if (0 < mCurItemPath.Length)
                saveFile(mCurItemPath, mFileFormat);
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
        private void loadFile(string path, string fileFormat)
        {
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
                } catch (Exception e) {
                    MessageBox.Show(e.Message);
                }
            }
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

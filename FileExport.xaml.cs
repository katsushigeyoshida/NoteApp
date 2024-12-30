using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using WpfLib;

namespace NoteApp
{
    /// <summary>
    /// FileExport.xaml の相互作用ロジック
    /// </summary>
    public partial class FileExport : Window
    {
        private string[] mFileFormats = new string[] {
            DataFormats.Text, DataFormats.Rtf, DataFormats.XamlPackage, DataFormats.Xaml
        };
        private string[] mFileExts = new string[] {
            ".txt", ".rtf", ".xaml", ".xaml"
        };
        private string[] mFileFormatMenu = new string[] {
            "テキストファイル(txt)", "リッチテキストファイル(rtf)", "XamlPackageファイル(xaml)", "Xamlファイル(xaml)"
        };
        private List<FileInfo> mFileList;
        private string mExportFolderListPath = "ExportList.csv";
        private List<string> mExportFolderList;

        public string mCategoryName;
        public string mSrcFolder;
        public bool mCategory = true;

        private YLib ylib = new YLib();

        public FileExport()
        {
            InitializeComponent();

            cbExportType.ItemsSource = mFileFormatMenu;
            cbExportType.SelectedIndex = 0;
            mExportFolderList = ylib.loadListData(mExportFolderListPath);
            cbExportFolder.ItemsSource = mExportFolderList;
        }

        /// <summary>
        /// 変換前処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lbCategory.Content = mCategoryName;
            mFileList = ylib.getDirectoriesInfo(mSrcFolder, "*.xaml");
        }

        /// <summary>
        /// 終了処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //  変換先フォルダリストの保存
            List<string> exportFolderList = mExportFolderList.GetRange(0, Math.Min(30, mExportFolderList.Count));
            ylib.saveListData(mExportFolderListPath, exportFolderList);
        }

        /// <summary>
        /// 変換開始ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btStart_Click(object sender, RoutedEventArgs e)
        {
            //  出力先フォルダの設定
            setOutFolder(cbExportFolder.Text);
            //  変換処理
            exportFiles(mFileList);
        }

        /// <summary>
        /// 終了ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btEnd_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// マウスダブルクリック
        /// 変換先フォルダの選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbExportFolder_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //  出力先フォルダの選択
            string outFolder = ylib.folderSelect(null);
            setOutFolder(outFolder);
        }

        /// <summary>
        /// 出力先フォルダの登録
        /// </summary>
        /// <param name="outFolder">出力先フォルダ</param>
        private void setOutFolder(string outFolder)
        {
            if (mExportFolderList == null)
                mExportFolderList = new List<string>();
            if (mExportFolderList.Contains(outFolder))
                mExportFolderList.Remove(outFolder);
            mExportFolderList.Insert(0, outFolder);
            cbExportFolder.ItemsSource = mExportFolderList;
            cbExportFolder.SelectedIndex = 0;
        }

        /// <summary>
        /// ファイルの変換
        /// </summary>
        /// <param name="fileList">ファイルリスト</param>
        private void exportFiles(List<FileInfo> fileList)
        {
            int formatNo = cbExportType.SelectedIndex;
            string outFolder = Path.Combine(cbExportFolder.Text, mCategoryName);
            pbExport.Maximum = fileList.Count;
            pbExport.Minimum = 0;
            pbExport.Value = 0;
            foreach (FileInfo file in fileList) {
                if (mSrcFolder.Length < file.FullName.Length) {
                    string relaPath = file.FullName.Substring(mSrcFolder.Length + 1);
                    string destPath = Path.Combine(outFolder, relaPath);
                    destPath = destPath.Replace(".xaml", mFileExts[formatNo]);
                    lbExportFile.Content = destPath;
                    string folder = Path.GetDirectoryName(destPath);
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);
                    exportFile(file.FullName, destPath, mFileFormats[formatNo]);
                    pbExport.Value++;
                    ylib.DoEvents();
                }
            }
        }

        /// <summary>
        /// XamlPackageファイルを変換する
        /// </summary>
        /// <param name="srcPath">変換元ファイルパス</param>
        /// <param name="destPath">変換先ァイルパス</param>
        /// <param name="format">保存フォーマット</param>
        private void exportFile(string srcPath, string destPath, string format)
        {
            TextRange range;
            FileStream fStream;
            FlowDocument document = new FlowDocument();
            TextPointer start = document.ContentStart;
            TextPointer end = document.ContentEnd;
            if (File.Exists(srcPath)) {
                try {
                    //  ファイルを読み込む
                    range = new TextRange(start, end);
                    fStream = new FileStream(srcPath, FileMode.OpenOrCreate);
                    range.Load(fStream, mFileFormats[2]);
                    fStream.Close();
                    //  ファイルを保存する
                    fStream = new FileStream(destPath, FileMode.Create);
                    range.Save(fStream, format);
                    fStream.Close();
                } catch (Exception e) {
                    MessageBox.Show(e.Message);
                }
            } else {
                MessageBox.Show("ファイルが存在しません。");
            }

        }
    }
}

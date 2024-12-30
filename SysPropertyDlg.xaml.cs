using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WpfLib;

namespace NoteApp
{
    /// <summary>
    /// SysPropertyDlg.xaml の相互作用ロジック
    /// </summary>
    public partial class SysPropertyDlg : Window
    {
        public int mScreenCaptureTimeLag = 0;
        public string mDataFolder = "";
        public string mBackupFolder = "";
        public string mFileExt;
        public string mLinkExt;

        private YLib ylib = new YLib();

        /// <summary>
        /// システム設定ダイヤログ
        /// </summary>
        public SysPropertyDlg()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tbScreenCaptureTimelag.Text = mScreenCaptureTimeLag.ToString();
            tbDataFolder.Text = mDataFolder.ToString();
            tbBackupFolder.Text = mBackupFolder.ToString();
        }

        /// <summary>
        /// データフォルダの選択設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbDataFolder_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string dataFolder = ylib.folderSelect(mDataFolder);
            if (0 < dataFolder.Length) {
                if (Directory.Exists(dataFolder)) {
                    Directory.CreateDirectory(dataFolder);
                }
                tbDataFolder.Text = dataFolder;
            }
        }

        /// <summary>
        /// バックアップフォルダの選択設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbBackupFolder_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string backupFolder = ylib.folderSelect(mBackupFolder);
            if (0 < backupFolder.Length) {
                if (Directory.Exists(backupFolder)) {
                    Directory.CreateDirectory(backupFolder);
                }
                tbBackupFolder.Text = backupFolder;
            }
        }

        /// <summary>
        /// バックアップ処理の実行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btBackupExec_Click(object sender, RoutedEventArgs e)
        {
            DirectoryDiff directoryDiff = new DirectoryDiff(mDataFolder, mBackupFolder);
            int count = directoryDiff.syncFolder();
            MessageBox.Show($"{count} ファイルのバックアップを更新しました。");
        }

        /// <summary>
        /// バックアップの復元
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btBackupRestor_Click(object sender, RoutedEventArgs e)
        {
            DiffFolder dlg = new DiffFolder();
            dlg.Owner = this;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.mSrcTitle = "比較元(データフォルダ)";
            dlg.mDestTitle = "比較先(バックアップ先)";
            dlg.mSrcFolder = mDataFolder;
            dlg.mDestFolder = mBackupFolder;
            dlg.ShowDialog();
        }

        /// <summary>
        /// データフォルダのファイル情報
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btProperty_Click(object sender, RoutedEventArgs e)
        {
            string mes = "データフォルダ\n" + mDataFolder;
            mes += "\n" + getDirectoryInfo(mDataFolder, "データ", mFileExt);
            mes += "\n" + getDirectoryInfo(mDataFolder, "リンク", mLinkExt);
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
        /// [OK]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btOK_Click(object sender, RoutedEventArgs e)
        {
            mScreenCaptureTimeLag = ylib.intParse(tbScreenCaptureTimelag.Text);
            mDataFolder = tbDataFolder.Text;
            mBackupFolder = tbBackupFolder.Text;
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// [Cancel]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();

        }
    }
}

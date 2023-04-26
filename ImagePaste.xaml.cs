using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfLib;

namespace NoteApp
{
    /// <summary>
    /// ImagePaste.xaml の相互作用ロジック
    /// クリップボードの画像を取得して大きさを設定する
    /// </summary>
    public partial class ImagePaste : Window
    {
        public int mBitmapWidth = 0;            //  画像の幅
        public int mBitmapHeight = 0;           //  画像の高さ
        public int mWidth = 0;                  //  画像の指定幅
        public int mHeight = 0;                 //  画像の指定高さ
        public BitmapSource mBitmapSource;      //  画像データ

        public Window mMainWindow = null;       //  親ウィンドウの設定

        private YLib ylib = new YLib();

        public ImagePaste()
        {
            InitializeComponent();

            cbAspect.IsChecked = true;
            if (cbAspect.IsChecked == true)
                tbHeight.IsReadOnly = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (mMainWindow != null) {
                //  親ウィンドウの中心に表示
                Left = mMainWindow.Left + (mMainWindow.Width - Width) / 2;
                Top = mMainWindow.Top + (mMainWindow.Height - Height) / 2;
            }

            if (Clipboard.ContainsImage()) {
                //  クリップボードに画像データがある
                mBitmapSource = Clipboard.GetImage();
                if (mBitmapSource != null) {
                    Bitmap bitmap = ylib.cnvBitmapSource2Bitmap(mBitmapSource);
                    mBitmapWidth = bitmap.Width;
                    mBitmapHeight = bitmap.Height;
                    imImageView.Stretch = Stretch.Fill;
                    lbImageSize.Content = $"{bitmap.Width} x {bitmap.Height}";      //  画像の大きさ
                    tbWidth.Text = bitmap.Width.ToString();
                    tbHeight.Text = bitmap.Height.ToString();
                    imImageView.Source = ylib.bitmap2BitmapSource(bitmap);
                }
            } else {
                Close();
            }
        }

        /// <summary>
        /// アスペクト比固定のチェックボックス
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbAspect_Click(object sender, RoutedEventArgs e)
        {
            if (cbAspect.IsChecked == true) {
                tbHeight.IsReadOnly = true;
                int width = ylib.intParse(tbWidth.Text, 0);
                tbHeight.Text = (width * mBitmapHeight / mBitmapWidth).ToString();
            } else {
                tbHeight.IsReadOnly = false;
            }
        }

        /// <summary>
        /// 画像幅を入力した時、高さを入れる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbWidth_KeyUp(object sender, KeyEventArgs e)
        {
            if (cbAspect.IsChecked == true) {
                int width = ylib.intParse(tbWidth.Text, 0);
                tbHeight.Text = (width * mBitmapHeight / mBitmapWidth).ToString();
            }
        }

        /// <summary>
        /// [OK]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btOK_Click(object sender, RoutedEventArgs e)
        {
            mWidth = ylib.intParse(tbWidth.Text);
            mHeight = ylib.intParse(tbHeight.Text);
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

        /// <summary>
        /// キャプチャしたイメージを表示しトリミングする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btTriming_Click(object sender, RoutedEventArgs e)
        {
            //  キャプチャしたイメージを表示し領域を切り取る
            FullView dlg = new FullView();
            dlg.mBitmapSource = mBitmapSource;
            dlg.mFullScreen = false;
            if (dlg.ShowDialog() == true) {
                Bitmap bitmap = ylib.cnvBitmapSource2Bitmap(mBitmapSource);
                bitmap = ylib.trimingBitmap(bitmap, dlg.mStartPoint, dlg.mEndPoint);
                //  切り取った領域を貼り付ける
                mBitmapSource = ylib.bitmap2BitmapSource(bitmap);
                imImageView.Source = mBitmapSource;
                //  画像の大きさ
                mBitmapWidth = bitmap.Width;
                mBitmapHeight = bitmap.Height;
                tbWidth.Text = bitmap.Width.ToString();
                tbHeight.Text = bitmap.Height.ToString();
                lbImageSize.Content = $"{bitmap.Width} x {bitmap.Height}";
            }
        }
    }
}

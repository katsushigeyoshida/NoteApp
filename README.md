# NoteApp
## メモや備忘録のように手軽にドキュメントを整理するツール
### WPFのRichTextBoxの機能を使って作成

<br>

![MainWindow画像](Image/NoteApp_MainWindow.png)


実行方法は[NoteApp.zip](NoteApp.zip)をダウンロードし適当なフォルダーに展開してNoteApp.exeを実行する。  
起動後にサンプルデータが表示されない時は画面左上の大分類のコンボボックスでマウスの右ボタンを押しコンテキストメニューを出し「初期値に戻す」を選択する。
  
#### ■おもな機能

ドキュメントの管理
1.	データを分類して管理する
2.	データの分類は、大分類(Genre)、小分類(Category)、項目(Item)の3段階で分類する
3.	データはRichTextを利用してテキスト、図を使えるようにする
4.	データはファイルとディレクトリ構造で管理する
5.	ファイルは Xaml Packegeの形式で保存(拡張子は xaml)
6.	テキスト、RTF 形式でのエクスポートやインポートを可能にする
7.	編集機能は最小限とし、他のアプリ(ワードパッド、ワード、LibreOfficeWriter)でも編集可とする
8.	当面はバグもあるのでバックアップ機能を持つ
9.	データやバックアップの保存場所は設定可能とする(初期値は実行ファイルの保存フォルダ)


エディタの機能
1.	文字フォント、文字サイズの変更ができる
2.	文字の色、文字の背景色の設定ができる
3.	図の挿入は段落単位でできるが、現状大きさの変更ができない。
大きさを変更したい場合には、項目リストのコンテキストメニューで他のアプリで開いて編集する
4.	ドキュメント内に数式がある場合には数式を選択してコンテキストメニューの計算で計算処理ができる
5.	ドキュメント内にURLやフルパスのファイル名がある場合にカーソルをあててマウスのダブルクリックを行うと開くことができる
  


### ■履歴  
2023/04/07 初回登録  

### ■実行環境
Windows10で動作の確認
ソフトの実行方法は NoteApp.zip をダウンロードして適当なフォルダに展開し、フォルダ内の NoteApp.exe をダブルクリックして実行する。  
起動後にサンプルデータが表示されない時は画面左上の大分類のコンボボックスでマウスの右ボタンを押してコンテキストメニューを出し「初期値に戻す」を選択する。

### ■開発環境  
開発ソフト : Microsoft Visual Studio 2022  
開発言語　 : C# 7.3 Windows アプリケーション  
フレームワーク : .NET framework 4.8  
自作ライブラリ  : WpfLib
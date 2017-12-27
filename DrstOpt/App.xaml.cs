using DrstOpt.Models;
using DrstOpt.ViewModels;
using DrstOpt.Views;
using System.Windows;

namespace DrstOpt
{
	/// <summary>
	/// App.xaml の相互作用ロジック
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);
			// データベースを初期化
			if (!DataStore.Initialize()) {
				MessageBox.Show("エラー：データベースの初期化に失敗しました", "デレステ編成最適化", MessageBoxButton.OK, MessageBoxImage.Error);
				
			}
			// メイン画面を作成して表示する
			var mv = new MainView();
			var mvm = new MainViewModel();
			mv.DataContext = mvm;
			mv.Show();
		}
	}
}

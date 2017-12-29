using DrstOpt.Models;
using Reactive.Bindings;
using System.ComponentModel.DataAnnotations;
using System.Windows;

namespace DrstOpt.ViewModels
{
	class MainViewModel
	{
		private MainModel mainModel = new MainModel();

		// 読み込み先フォルダのパス設定
		[Required(ErrorMessage = "Error!!")]
		public ReactiveProperty<string> SoftwareFolderPath { get; private set; } = new ReactiveProperty<string>("");
		// 最適化したい曲の属性
		public ReactiveProperty<int> MusicAttributeIndex { get; private set; } = new ReactiveProperty<int>(0);
		// データを読み込めたか？
		public ReactiveProperty<bool> ReadDataFlg { get; private set; } = new ReactiveProperty<bool>(false);
		// 参照ボタン
		public ReactiveCommand BrowseSoftwareFolderPathCommand { get; private set; } = new ReactiveCommand();
		// データ読み込みボタン
		public ReactiveCommand ReadDataCommand { get; private set; } = new ReactiveCommand();
		// 最適化ボタン
		public ReactiveCommand OptimizeCommand { get; private set; }

		public MainViewModel() {
			// コマンドを設定
			BrowseSoftwareFolderPathCommand.Subscribe(() => {
				SoftwareFolderPath.Value = mainModel.BrowseSoftwareFolderPath(SoftwareFolderPath.Value);
			});
			ReadDataCommand.Subscribe(() => {
				ReadDataFlg.Value = DataStore.Initialize(SoftwareFolderPath.Value);
				if (!ReadDataFlg.Value) {
					MessageBox.Show("エラー：データベースの初期化に失敗しました", "デレステ編成最適化", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			});
			OptimizeCommand = ReadDataFlg.ToReactiveCommand();
			OptimizeCommand.Subscribe(() => {
				mainModel.OptimizeIdolUnit((Attribute)MusicAttributeIndex.Value);
			});
		}
	}
}

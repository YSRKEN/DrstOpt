using DrstOpt.Models;
using Reactive.Bindings;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;

namespace DrstOpt.ViewModels
{
	class MainViewModel
	{
		private MainModel mainModel = new MainModel();

		// 読み込み先フォルダのパス設定
		public ReactiveProperty<string> SoftwareFolderPath { get; private set; } = new ReactiveProperty<string>("");
		// 最適化したい曲の属性
		public ReactiveProperty<int> MusicAttributeIndex { get; private set; } = new ReactiveProperty<int>(0);
		// 属性一覧
		public List<string> MusicAttributeList { get; } = new List<string> { "全体曲", "キュート", "クール", "パッション" };
		// データを読み込めたか？
		public ReactiveProperty<bool> ReadDataFlg { get; private set; } = new ReactiveProperty<bool>(false);
		// 実行ログ
		public ReactiveProperty<string> LoggingText { get; private set; } = new ReactiveProperty<string>("");
		// 参照ボタン
		public ReactiveCommand BrowseSoftwareFolderPathCommand { get; private set; } = new ReactiveCommand();
		// データ読み込みボタン
		public ReactiveCommand ReadDataCommand { get; private set; } = new ReactiveCommand();
		// 最適化ボタン
		public ReactiveCommand OptimizeCommand { get; private set; }

		private void AddLogText(string text) {
			LoggingText.Value += text + "\n";
		}

		public MainViewModel() {
			// コマンドを設定
			//MusicAttributeIndex.Subscribe(() => { AddLogText($"属性変更：{MusicAttributeList[MusicAttributeIndex.Value]}"); });
			BrowseSoftwareFolderPathCommand.Subscribe(() => {
				SoftwareFolderPath.Value = mainModel.BrowseSoftwareFolderPath(SoftwareFolderPath.Value);
				AddLogText($"フォルダパス：{SoftwareFolderPath.Value}");
			});
			ReadDataCommand.Subscribe(() => {
				ReadDataFlg.Value = DataStore.Initialize(SoftwareFolderPath.Value);
				if (!ReadDataFlg.Value) {
					MessageBox.Show("エラー：データベースの初期化に失敗しました", "デレステ編成最適化", MessageBoxButton.OK, MessageBoxImage.Error);
					AddLogText("エラー：データベースの初期化に失敗しました");
				} else {
					AddLogText("データベースを初期化できました");
				}
			});
			OptimizeCommand = ReadDataFlg.ToReactiveCommand();
			OptimizeCommand.Subscribe(async () => {
				AddLogText("最適化開始...");
				string optimizedLog = await mainModel.OptimizeIdolUnitAsync((Attribute)MusicAttributeIndex.Value);
				AddLogText("最適化完了");
				AddLogText($"属性：{MusicAttributeList[MusicAttributeIndex.Value]}");
				AddLogText(optimizedLog);
			});
		}
	}
}

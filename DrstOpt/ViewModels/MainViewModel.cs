using DrstOpt.Models;
using Reactive.Bindings;
using System.Windows;
using System.Collections.Generic;
using System;
using DrstOpt.Properties;

namespace DrstOpt.ViewModels
{
	class MainViewModel
	{
		private MainModel mainModel = new MainModel();

		// 属性一覧
		public List<string> MusicAttributeList { get; } = new List<string> { "全体曲", "キュート", "クール", "パッション" };
		// 設定一覧
		private Config config { get {
			return new Config {
				Attribute = (Models.Attribute)MusicAttributeIndex.Value,
				IncludeLifeRecoveryFlg = IncludeLifeRecoveryFlg.Value,
				IncludeDamageGuardFlg = IncludeDamageGuardFlg.Value,
				ExcludeConcentrationFlg = ExcludeConcentrationFlg.Value,
				ExcludeOverloadFlg = ExcludeOverloadFlg.Value
			};
		}}

		// 読み込み先フォルダのパス設定
		public ReactiveProperty<string> SoftwareFolderPath { get; }
			= new ReactiveProperty<string>(Settings.Default.SoftwareFolderPath);
		// 最適化したい曲の属性
		public ReactiveProperty<int> MusicAttributeIndex { get; }
			= new ReactiveProperty<int>(Settings.Default.MusicAttributeIndex);
		// 回復を積むか？
		public ReactiveProperty<bool> IncludeLifeRecoveryFlg { get; }
			= new ReactiveProperty<bool>(Settings.Default.IncludeLifeRecoveryFlg);
		// ダメガを積むか？
		public ReactiveProperty<bool> IncludeDamageGuardFlg { get; }
			= new ReactiveProperty<bool>(Settings.Default.IncludeDamageGuardFlg);
		// コンセを禁止するか？
		public ReactiveProperty<bool> ExcludeConcentrationFlg { get; }
			= new ReactiveProperty<bool>(Settings.Default.ExcludeConcentrationFlg);
		// オバロを禁止するか？
		public ReactiveProperty<bool> ExcludeOverloadFlg { get; }
			= new ReactiveProperty<bool>(Settings.Default.ExcludeOverloadFlg);
		// ソフト起動時に自動でDBを読み込むか？
		public ReactiveProperty<bool> ReadDataOnLoadFlg { get; }
			= new ReactiveProperty<bool>(Settings.Default.ReadDataOnLoadFlg);
		// データを読み込めたか？
		public ReactiveProperty<bool> ReadDataFlg { get; } = new ReactiveProperty<bool>(false);
		// 実行ログ
		public ReactiveProperty<string> LoggingText { get; } = new ReactiveProperty<string>("");
		// 参照ボタン
		public ReactiveCommand BrowseSoftwareFolderPathCommand { get; } = new ReactiveCommand();
		// データ読み込みボタン
		public ReactiveCommand ReadDataCommand { get; } = new ReactiveCommand();
		// 最適化ボタン
		public ReactiveCommand OptimizeCommand { get; }

		private void AddLogText(string text) {
			LoggingText.Value += text + "\n";
		}

		public MainViewModel() {
			// コマンドを設定
			IncludeLifeRecoveryFlg.Subscribe(_ => {
				AddLogText($"回復を積むか？：{IncludeLifeRecoveryFlg.Value}");
				Settings.Default.IncludeLifeRecoveryFlg = IncludeLifeRecoveryFlg.Value;
				Settings.Default.Save();
			});
			IncludeDamageGuardFlg.Subscribe(_ => {
				AddLogText($"ダメガを積むか？：{IncludeDamageGuardFlg.Value}");
				Settings.Default.IncludeDamageGuardFlg = IncludeDamageGuardFlg.Value;
				Settings.Default.Save();
			});
			ExcludeConcentrationFlg.Subscribe(_ => {
				AddLogText($"コンセを禁止するか？：{ExcludeConcentrationFlg.Value}");
				Settings.Default.ExcludeConcentrationFlg = ExcludeConcentrationFlg.Value;
				Settings.Default.Save();
			});
			ExcludeOverloadFlg.Subscribe(_ => {
				AddLogText($"オバロを禁止するか？：{ExcludeOverloadFlg.Value}");
				Settings.Default.ExcludeOverloadFlg = ExcludeOverloadFlg.Value;
				Settings.Default.Save();
			});
			ReadDataOnLoadFlg.Subscribe(_ => {
				AddLogText($"ソフト起動時にDBを読み込むか？：{ReadDataOnLoadFlg.Value}");
				Settings.Default.ReadDataOnLoadFlg = ReadDataOnLoadFlg.Value;
				Settings.Default.Save();
			});
			MusicAttributeIndex.Subscribe(_ => {
				AddLogText($"属性：{MusicAttributeList[MusicAttributeIndex.Value]}");
				Settings.Default.MusicAttributeIndex = MusicAttributeIndex.Value;
				Settings.Default.Save();
			});
			SoftwareFolderPath.Subscribe(_ => {
				Settings.Default.SoftwareFolderPath = SoftwareFolderPath.Value;
				Settings.Default.Save();
			});
			BrowseSoftwareFolderPathCommand.Subscribe(() => {
				SoftwareFolderPath.Value = mainModel.BrowseSoftwareFolderPath(SoftwareFolderPath.Value);
				if(SoftwareFolderPath.Value != "")
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
				string optimizedLog = await mainModel.OptimizeIdolUnitAsync(config);
				AddLogText("最適化完了");
				AddLogText($"属性：{MusicAttributeList[MusicAttributeIndex.Value]}");
				AddLogText(optimizedLog);
			});
			// ソフト起動時にDBを読み込む
			if (ReadDataOnLoadFlg.Value) {
				ReadDataCommand.Execute();
			}
		}
	}
}

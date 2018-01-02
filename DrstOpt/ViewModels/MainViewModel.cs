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
		// Grooveイベントにおける属性・タイプ一覧
		// 属性一覧
		public List<string> GrooveAttributeList { get; } = new List<string> { "キュート", "クール", "パッション" };
		// 属性一覧
		public List<string> GrooveAppealList { get; } = new List<string> { "全体アピール", "ボーカル", "ダンス", "ビジュアル" };


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
		// Grooveイベントにおける最適化か？
		public ReactiveProperty<bool> GrooveFlg { get; }
			= new ReactiveProperty<bool>(Settings.Default.GrooveFlg);
		// Grooveイベントにおける属性
		public ReactiveProperty<int> GrooveAttributeIndex { get; }
			= new ReactiveProperty<int>(Settings.Default.GrooveAttributeIndex);
		// Grooveイベントにおけるアピール
		public ReactiveProperty<int> GrooveAppealIndex { get; }
			= new ReactiveProperty<int>(Settings.Default.GrooveAppealIndex);
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
			//DBの読み込みについて
			SoftwareFolderPath.Subscribe(x => {
				Settings.Default.SoftwareFolderPath = x;
				Settings.Default.Save();
			});
			BrowseSoftwareFolderPathCommand.Subscribe(() => {
				SoftwareFolderPath.Value = mainModel.BrowseSoftwareFolderPath(SoftwareFolderPath.Value);
				if (SoftwareFolderPath.Value != "")
					AddLogText($"フォルダパス：{SoftwareFolderPath.Value}");
			});
			ReadDataOnLoadFlg.Subscribe(x => {
				AddLogText($"ソフト起動時にDBを読み込むか？：{x}");
				Settings.Default.ReadDataOnLoadFlg = x;
				Settings.Default.Save();
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
			//選択メンバーについて
			IncludeLifeRecoveryFlg.Subscribe(x => {
				AddLogText($"回復を積むか？：{x}");
				Settings.Default.IncludeLifeRecoveryFlg = x;
				Settings.Default.Save();
			});
			IncludeDamageGuardFlg.Subscribe(x => {
				AddLogText($"ダメガを積むか？：{x}");
				Settings.Default.IncludeDamageGuardFlg = x;
				Settings.Default.Save();
			});
			ExcludeConcentrationFlg.Subscribe(x => {
				AddLogText($"コンセを禁止するか？：{x}");
				Settings.Default.ExcludeConcentrationFlg = x;
				Settings.Default.Save();
			});
			ExcludeOverloadFlg.Subscribe(x => {
				AddLogText($"オバロを禁止するか？：{x}");
				Settings.Default.ExcludeOverloadFlg = x;
				Settings.Default.Save();
			});
			// 最適化設定について
			MusicAttributeIndex.Subscribe(x => {
				AddLogText($"属性：{MusicAttributeList[x]}");
				Settings.Default.MusicAttributeIndex = x;
				Settings.Default.Save();
			});
			// Grooveイベントについて
			GrooveFlg.Subscribe(x => {
				AddLogText($"Grooveイベントにおける最適化か？：{x}");
				Settings.Default.GrooveFlg = x;
				Settings.Default.Save();
			});
			GrooveAttributeIndex.Subscribe(x => {
				AddLogText($"Groove属性：{GrooveAttributeList[x]}");
				Settings.Default.GrooveAttributeIndex = x;
				Settings.Default.Save();
			});
			GrooveAppealIndex.Subscribe(x => {
				AddLogText($"Grooveアピールタイプ：{GrooveAppealList[x]}");
				Settings.Default.GrooveAppealIndex = x;
				Settings.Default.Save();
			});
			// 最適化コマンドについて
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

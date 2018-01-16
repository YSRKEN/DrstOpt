using Prism.Mvvm;
using System;
using System.Windows;
using Google.OrTools.LinearSolver;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace DrstOpt.Models
{
	class MainModel : BindableBase
	{
		// 最適化処理を行い、結果をダイアログで表示する
		public Task<string> OptimizeIdolUnitAsync(Config config) {
			return Task.Run(() => OptimizeIdolUnit(config));
		}
		public string OptimizeIdolUnit(Config config) {
			// 最適化処理を行い、結果をダイアログで表示する
			// 解きたい数式の概要：
			// 1. 各アイドルカードの種類数をNとする
			// 2. 配置箇所を1～5(M)番とすると、各アイドルカードの使用可否は
			//    0/1変数x_n_mで表現できる
			// 3. 各n=1～Nについて、∑(m=1～M, x_n_m) <= アイドルカードnの所持枚数(0 or 1)
			// 4. 各m=1～Mについて、∑(n=1～N, x_n_m) = 1
			//    各m=1～M-1, 各n=1～Nについて、x_n_m-x_n_{m+1} >= 0とすれば、重複解を排除できる
			// 5. アイドルnの補正後Vo・Da・Viをa_n, b_n, c_nとすると、編成の合計アピール値は
			//    ∑(m=1～M n=1～N, x_n_m * (a_n + b_n + c_n))となるのでこれを最大化する
			// 6. センター効果は非線形処理なので、各ケースについてのアピール値を計算し、
			//    最大となる編成を返すようにする(つまり力技)
			var csAttributeList = new Attribute[] {
				Attribute.All, Attribute.Cute, Attribute.Cool,
				Attribute.Passion, Attribute.CuteP, Attribute.CoolP,
				Attribute.PassionP, Attribute.Tricolore, Attribute.None };
			var csTypeList = new CSType[] {
				CSType.All, CSType.Vocal, CSType.Dance, CSType.Visual,
				CSType.Skill, CSType.Life, CSType.None
			};
			// まずは何の縛りを入れない状態で解く
			var haveIdolCardList = DataStore.HaveIdolCardList;
			long allWallTime = 0;
			double bestAppealValue = double.NegativeInfinity;
			var bestSelectedIdolIndex = new List<int>();
			OptimizeIdolUnitImpl(config, ref allWallTime, ref bestAppealValue, ref bestSelectedIdolIndex);
			Console.WriteLine($"{bestAppealValue}");
			if (double.IsNegativeInfinity(bestAppealValue)) {
				MessageBox.Show("エラー：問題に解が存在しませんでした", "デレステ編成最適化", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return "エラー：問題に解が存在しませんでした";
			}
			// 次に、センターを指定して解く(この際能力は必ず有効になる配置だとする)
			for(int i = 0; i < DataStore.IdolCardList.Count; ++i) {
				// 持ってないアイドルの場合は飛ばす
				if (!DataStore.HaveCardFlg[i])
					continue;
				// 計算を行う
				long wallTime = 0;
				double appealValue = double.NegativeInfinity;
				var selectedIdolIndex = new List<int>();
				OptimizeIdolUnitImpl(config, ref wallTime, ref appealValue, ref selectedIdolIndex, i);
				allWallTime += wallTime;
				// 最良解を更新
				if (bestAppealValue < appealValue) {
					Console.WriteLine($"{appealValue} {DataStore.IdolCardList[i].CardName}");
					bestAppealValue = appealValue;
					bestSelectedIdolIndex = selectedIdolIndex;
				}
			}
			string message = $"計算時間：{allWallTime}[ms]";
			message += $"\n合計アピール値：{bestAppealValue}";
			message += $"\nセンター：{DataStore.IdolCardList[bestSelectedIdolIndex[0]].CardInfo}";
			message += $"\n二番手：{DataStore.IdolCardList[bestSelectedIdolIndex[1]].CardInfo}";
			message += $"\n三番手：{DataStore.IdolCardList[bestSelectedIdolIndex[2]].CardInfo}";
			message += $"\n四番手：{DataStore.IdolCardList[bestSelectedIdolIndex[3]].CardInfo}";
			message += $"\n五番手：{DataStore.IdolCardList[bestSelectedIdolIndex[4]].CardInfo}";
			MessageBox.Show(message, "デレステ編成最適化", MessageBoxButton.OK, MessageBoxImage.Information);
			return message;
		}
		void OptimizeIdolUnitImpl(Config config, ref long wallTime, ref double appealValue, ref List<int> selectedIdolIndex, int idolIndex = -1) {
			using (var solver = Solver.CreateSolver("IntegerProgramming", "CBC_MIXED_INTEGER_PROGRAMMING")) {
				// よく使う定数値
				int N = DataStore.IdolCardList.Count;
				int M = 5;
				var centerCard = (idolIndex >= 0 ? DataStore.IdolCardList[idolIndex] : new IdolCard());
				var csAttribute = centerCard.CenterSkillAttribute;
				var csType = centerCard.CenterSkillType;
				decimal csPower = centerCard.CenterSkillPower;

				// 初期化できてない場合はnullが返る
				if (solver == null) {
					return;
				}

				// 最適化の方向
				var objective = solver.Objective();
				objective.SetMaximization();

				// 変数の数・名前・範囲
				//x_n_mが1なら、n番目のアイドルカードがm番目の配置で使用される
				var x = new Variable[N, M];
				for (int n = 0; n < N; ++n) {
					for (int m = 0; m < M; ++m) {
						x[n, m] = solver.MakeBoolVar($"x_{n + 1}_{m + 1}");
					}
				}
				var v_CuCount = solver.MakeIntVar(0.0, double.PositiveInfinity, "CuCount");
				var v_CoCount = solver.MakeIntVar(0.0, double.PositiveInfinity, "CoCount");
				var v_PaCount = solver.MakeIntVar(0.0, double.PositiveInfinity, "PaCount");

				// 制約式の数・範囲
				//そのカードを使用できるか？
				var e_useflg = new Constraint[N];
				for (int n = 0; n < N; ++n) {
					e_useflg[n] = solver.MakeConstraint(0.0, (DataStore.HaveCardFlg[n] ? 1.0 : 0.0), $"e_useflg_{n+1}");
				}
				// あるポジションには1枚だけ存在しうる
				var e_posflg = new Constraint[M];
				for (int m = 0; m < M; ++m) {
					e_posflg[m] = solver.MakeConstraint(1.0, 1.0, $"e_posflg_{m + 1}");
				}
				// センターに指定したカードを指定するための制約
				var e_center = solver.MakeConstraint((idolIndex >= 0? 1.0 : 0.0), 1.0, "e_center");
				// ○○プリンセス系スキルに関する制約
				var e_princess = solver.MakeConstraint(0.0, 0.0, "e_princess");
				// トリコロール系スキルに関する制約
				var e_cu = solver.MakeConstraint((centerCard.CenterSkillAttribute == Attribute.Tricolore ? 1.0 : 0.0), double.PositiveInfinity, "e_cu");
				var e_co = solver.MakeConstraint((centerCard.CenterSkillAttribute == Attribute.Tricolore ? 1.0 : 0.0), double.PositiveInfinity, "e_co");
				var e_pa = solver.MakeConstraint((centerCard.CenterSkillAttribute == Attribute.Tricolore ? 1.0 : 0.0), double.PositiveInfinity, "e_pa");
				// 回復・ダメガ・コンセ・オバロに関する制約
				var e_member_life = solver.MakeConstraint((config.IncludeLifeRecoveryFlg ? 1.0 : 0.0), double.PositiveInfinity, "e_member_life");
				var e_member_dmg  = solver.MakeConstraint((config.IncludeDamageGuardFlg ? 1.0 : 0.0), double.PositiveInfinity, "e_member_dmg");
				var e_member_cons = solver.MakeConstraint(0.0, (config.ExcludeConcentrationFlg ? 0.0 : double.PositiveInfinity), "e_member_cons");
				var e_member_over = solver.MakeConstraint(0.0, (config.ExcludeOverloadFlg ? 0.0 : double.PositiveInfinity), "e_member_over");

				// 目的関数の係数
				for (int n = 0; n < N; ++n) {
					// カードを選択
					var idolCard = DataStore.IdolCardList[n];
					// カードに掛かる効果を計算する
					int vocalPump = 100, dancePump = 100, visualPump = 100;
					//プレイする楽曲に掛かる効果
					if(config.Attribute == Attribute.All || config.Attribute == idolCard.Attribute) {
						vocalPump += 30;
						dancePump += 30;
						visualPump += 30;
					}
					//センター効果
					if(idolIndex >= 0) {
						if (csAttribute == Attribute.All || csAttribute == Attribute.Tricolore
						|| ((csAttribute == Attribute.Cute || csAttribute == Attribute.CuteP) && idolCard.Attribute == Attribute.Cute)
						|| ((csAttribute == Attribute.Cool || csAttribute == Attribute.CoolP) && idolCard.Attribute == Attribute.Cool)
						|| ((csAttribute == Attribute.Passion || csAttribute == Attribute.PassionP) && idolCard.Attribute == Attribute.Passion)) {
							if (csType == CSType.All || csType == CSType.Vocal)
								vocalPump += (int)csPower;
							if (csType == CSType.All || csType == CSType.Dance)
								dancePump += (int)csPower;
							if (csType == CSType.All || csType == CSType.Visual)
								visualPump += (int)csPower;
						}
					}
					//Groove効果
					if (config.GrooveFlg && config.GrooveAttribute == idolCard.Attribute) {
						switch (config.GrooveAppeal) {
						case Appeal.All:
							vocalPump += 50;
							dancePump += 50;
							visualPump += 50;
							break;
						case Appeal.Vocal:
							vocalPump += 50;
							break;
						case Appeal.Dance:
							dancePump += 50;
							break;
						case Appeal.Visual:
							visualPump += 50;
							break;
						}
					}
					// 最終的なカードのアピール値を計算する
					double idolVocal = Math.Ceiling((double)idolCard.Vocal * vocalPump / 100);
					double idolDance = Math.Ceiling((double)idolCard.Dance * dancePump / 100);
					double idolVisual = Math.Ceiling((double)idolCard.Visual * visualPump / 100);
					double idolPower = idolVocal + idolDance + idolVisual;
					// 係数をセットする
					for (int m = 0; m < M; ++m) {
						objective.SetCoefficient(x[n, m], idolPower);
					}
				}

				// 制約式の係数
				for (int n = 0; n < N; ++n) {
					for (int m = 0; m < M; ++m) {
						e_useflg[n].SetCoefficient(x[n, m], 1);
					}
				}
				for (int m = 0; m < M; ++m) {
					for (int n = 0; n < N; ++n) {
						e_posflg[m].SetCoefficient(x[n, m], 1);
					}
				}
				if(idolIndex >= 0.0) {
					e_center.SetCoefficient(x[idolIndex, 0], 1.0);
					for (int n = 0; n < N; ++n) {
						if ((csAttribute == Attribute.CuteP && DataStore.IdolCardList[n].Attribute != Attribute.Cute)
						|| (csAttribute == Attribute.CoolP && DataStore.IdolCardList[n].Attribute != Attribute.Cool)
						|| (csAttribute == Attribute.PassionP && DataStore.IdolCardList[n].Attribute != Attribute.Passion)) {
							for (int m = 0; m < M; ++m) {
								e_princess.SetCoefficient(x[n, m], 1);
							}
						}
					}
					for (int m = 0; m < M; ++m) {
						for (int n = 0; n < N; ++n) {
							switch (DataStore.IdolCardList[n].Attribute) {
							case Attribute.Cute:
								e_cu.SetCoefficient(x[n, m], 1.0);
								break;
							case Attribute.Cool:
								e_co.SetCoefficient(x[n, m], 1.0);
								break;
							case Attribute.Passion:
								e_pa.SetCoefficient(x[n, m], 1.0);
								break;
							}
						}
					}
				}
				for (int n = 0; n < N; ++n) {
					var idolCard = DataStore.IdolCardList[n];
					for (int m = 0; m < M; ++m) {
						switch (idolCard.Ability) {
						case Ability.Life:
						case Ability.All:
							e_member_life.SetCoefficient(x[n, m], 1.0);
							break;
						case Ability.Damage:
							e_member_dmg.SetCoefficient(x[n, m], 1.0);
							break;
						case Ability.Cons:
							e_member_cons.SetCoefficient(x[n, m], 1.0);
							break;
						case Ability.Over:
							e_member_over.SetCoefficient(x[n, m], 1.0);
							break;
						}
					}
				}

				// 最適化
				int resultStatus = solver.Solve();

				// 結果を取得
				if (resultStatus != Solver.OPTIMAL) {
					wallTime = solver.WallTime();
					return;
				}
				selectedIdolIndex = new List<int>();
				for (int m = 0; m < M; ++m) {
					for (int n = 0; n < N; ++n) {
						if(x[n,m].SolutionValue() > 0.5) {
							selectedIdolIndex.Add(n);
							break;
						}
					}
				}
				wallTime = solver.WallTime();
				appealValue = solver.Objective().Value();
				return;
			}
		}
		// データ集計
		public string CountData() {
			var haveIdolCardList = DataStore.HaveIdolCardList;
			var idolCardCount = new Dictionary<string, int>();
			foreach(var idol in haveIdolCardList) {
				if (idolCardCount.ContainsKey(idol.IdolName)) {
					++idolCardCount[idol.IdolName];
				} else {
					idolCardCount[idol.IdolName] = 1;
				}
			}
			var idolCardCount2 = new List<KeyValuePair<string, int>>();
			foreach(var pair in idolCardCount) {
				idolCardCount2.Add(pair);
			}
			idolCardCount2 = idolCardCount2.OrderByDescending(x => x.Value).ToList();
			string output = "アイドル集計結果：\n";
			foreach(var pair in idolCardCount2) {
				output += $"{pair.Key}\t{pair.Value}\n";
			}
			return output;
		}
		// フォルダパスを参照する
		public string BrowseSoftwareFolderPath(string startPath) {
			var fbd = new System.Windows.Forms.FolderBrowserDialog {
				Description = "「デレステ計算機」のフォルダを指定",
				SelectedPath = startPath
			};
			if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
				return fbd.SelectedPath;
			} else {
				return startPath;
			}
		}
	}
}

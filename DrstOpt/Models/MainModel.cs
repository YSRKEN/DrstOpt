using Prism.Mvvm;
using System;
using System.Windows;
using Google.OrTools.LinearSolver;
using System.Collections.Generic;

namespace DrstOpt.Models
{
	class MainModel : BindableBase
	{
		public void OptimizeIdolUnit(Attribute attribute) {
			// 最適化処理を行い、結果をダイアログで表示する
			// 解きたい数式の概要：
			// 1. 各アイドルカードの種類数をNとする
			// 2. 配置箇所を1～5(M)番とすると、各アイドルカードの使用可否は
			//    0/1変数x_n_mで表現できる
			// 3. 各n=1～Nについて、∑(m=1～M, x_n_m) <= アイドルカードnの所持枚数(0 or 1)
			// 4. 各m=1～Mについて、∑(n=1～N, x_n_m) = 1
			//    各m=1～M-1, 各n=1～Nについて、x_n_m-x_n_{m+1} >= 0とすれば、重複解を排除できる
			// 6. アイドルnのVo・Da・Viをa_n, b_n, c_nとすると、編成の合計スコアは
			//    ∑(m=1～M n=1～N, x_n_m * (a_n + b_n + c_n))となるのでこれを最大化する
			using (var solver = Solver.CreateSolver("IntegerProgramming", "CBC_MIXED_INTEGER_PROGRAMMING")) {
				// よく使う定数値
				int N = DataStore.IdolCardList.Count;
				int M = 5;
				
				// 初期化できてない場合はnullが返る
				if (solver == null) {
					MessageBox.Show("エラー：ソルバーを初期化できませんでした。", "デレステ編成最適化", MessageBoxButton.OK, MessageBoxImage.Exclamation);
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

				// 制約式の数・範囲
				var e_useflg = new Constraint[N];
				for (int n = 0; n < N; ++n) {
					e_useflg[n] = solver.MakeConstraint(0.0, (DataStore.HaveCardFlg[n] ? 1.0 : 0.0), $"e_useflg_{n+1}");
				}
				var e_posflg = new Constraint[M];
				for (int m = 0; m < M; ++m) {
					e_posflg[m] = solver.MakeConstraint(1.0, 1.0, $"e_posflg_{m + 1}");
				}

				// 目的関数の係数
				for (int n = 0; n < N; ++n) {
					var idolCard = DataStore.IdolCardList[n];
					double idolPower = (double)(idolCard.Vocal + idolCard.Dance + idolCard.Visual);
					double idolPower2 = Math.Round((double)idolCard.Vocal * 1.3)
						+ Math.Round((double)idolCard.Dance * 1.3)
						+ Math.Round((double)idolCard.Visual * 1.3);
					for (int m = 0; m < M; ++m) {
						switch (attribute) {
						case Attribute.All:
							objective.SetCoefficient(x[n, m], idolPower2);
							break;
						case Attribute.Cute:
							if (idolCard.Attribute == Attribute.Cute) {
								objective.SetCoefficient(x[n, m], idolPower2);
							} else {
								objective.SetCoefficient(x[n, m], idolPower);
							}
							break;
						case Attribute.Cool:
							if (idolCard.Attribute == Attribute.Cool) {
								objective.SetCoefficient(x[n, m], idolPower2);
							} else {
								objective.SetCoefficient(x[n, m], idolPower);
							}
							break;
						case Attribute.Passion:
							if (idolCard.Attribute == Attribute.Passion) {
								objective.SetCoefficient(x[n, m], idolPower2);
							} else {
								objective.SetCoefficient(x[n, m], idolPower);
							}
							break;
						}
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

				// 最適化
				int resultStatus = solver.Solve();

				// 結果を取得
				if (resultStatus != Solver.OPTIMAL) {
					var xxx = Solver.INFEASIBLE;
					MessageBox.Show("エラー：ソルバーで解けませんでした。", "デレステ編成最適化", MessageBoxButton.OK, MessageBoxImage.Exclamation);
					return;
				}
				var SelectedIdolIndex = new List<int>();
				for (int n = 0; n < N; ++n) {
					for (int m = 0; m < M; ++m) {
						if(x[n,m].SolutionValue() > 0.5) {
							SelectedIdolIndex.Add(n);
							break;
						}
					}
				}
				string message = $"計算時間：{solver.WallTime()}[ms]";
				message += $"\n合計アピール値：{solver.Objective().Value()}";
				message += $"\nセンター：{DataStore.IdolCardList[SelectedIdolIndex[0]].CardName}";
				message += $"\n二番手：{DataStore.IdolCardList[SelectedIdolIndex[1]].CardName}";
				message += $"\n三番手：{DataStore.IdolCardList[SelectedIdolIndex[2]].CardName}";
				message += $"\n四番手：{DataStore.IdolCardList[SelectedIdolIndex[3]].CardName}";
				message += $"\n五番手：{DataStore.IdolCardList[SelectedIdolIndex[4]].CardName}";
				MessageBox.Show(message, "デレステ編成最適化", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}
	}
}

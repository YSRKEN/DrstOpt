using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;

namespace DrstOpt.Models
{
	// データベース
	static class DataStore
	{
		public static List<IdolCard> IdolCardList { get; private set; }
		// 初期化
		public static bool Initialize() {
			const string folderPath = @"F:\ソフトウェア\パズル・ゲーム\PC\デレステ計算機\Ver3系";
			// アイドル名の一覧を読み込む
			try {
				// SQLを唱えて結果を取得する
				string connectionString = $"Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};Dbq={folderPath}\\IdolDB.accdb;Uid=admin;Pwd=;";
				Console.WriteLine(connectionString);
				string queryString = "SELECT IdolName,NickName,Rarity FROM IdolList";
				var dt = new DataTable();
				using (var connection = new OdbcConnection(connectionString)) {
					connection.Open();
					var adapter = new OdbcDataAdapter(queryString, connection);
					adapter.Fill(dt);
				}
				// アイドルのカード一覧として解釈する
				IdolCardList = dt.Select().Select(r => {
					string idolName = r.Field<string>("IdolName");
					string situation = (r.Field<string>("NickName") ?? "").Replace("[", "").Replace("]", "");
					string reality = r.Field<string>("Rarity");
					switch (reality) {
						case "N":
							return new IdolCard { IdolName = idolName, Situation = situation, Reality = Reality.N };
						case "R":
							return new IdolCard { IdolName = idolName, Situation = situation, Reality = Reality.R };
						case "SR":
							return new IdolCard { IdolName = idolName, Situation = situation, Reality = Reality.SR };
						case "SSR":
							return new IdolCard { IdolName = idolName, Situation = situation, Reality = Reality.SSR };
						default:
							throw new Exception("エラー：データベース内のカードにおけるレアリティに異常があります");
					}
				}).ToList();
			} catch(Exception ex) {
				Console.WriteLine(ex.ToString());
				return false;
			}
			return true;
		}
	}
	// アイドルカード
	struct IdolCard {
		// アイドルの名前
		public string IdolName;
		// カードのシチュエーション
		public string Situation;
		// カードのレアリティ
		public Reality Reality;

		// カードの名前
		public string CardName {
			get {
				if (Situation != "") {
					return $"({RealityString[(int)Reality]})［{Situation}］{IdolName}";
				} else {
					return $"({RealityString[(int)Reality]}){IdolName}";
				}
			}
		}
		// レアリティ表示用文字列
		private static string[] RealityString = { "N+", "R+", "SR+", "SSR+" };
	}
	// アイドルカードのレアリティ
	// 暗黙の仮定として、全て特訓済みのカード(N+、SSR+など)だとする
	enum Reality { N, R, SR, SSR }
}

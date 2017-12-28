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
				string queryString = "SELECT Rarity,NickName,IdolName,IdolType,Vo,Da,Vi FROM IdolList";
				var dt = new DataTable();
				using (var connection = new OdbcConnection(connectionString)) {
					connection.Open();
					var adapter = new OdbcDataAdapter(queryString, connection);
					adapter.Fill(dt);
				}
				// アイドルのカード一覧として解釈する
				var realityTable = new Dictionary<string, Reality> {
					{ "N", Reality.N },
					{ "R", Reality.R },
					{ "SR", Reality.SR },
					{ "SSR", Reality.SSR },
				};
				var attributeTable = new Dictionary<string, Attribute> {
					{ "キュート", Attribute.Cute },
					{ "クール", Attribute.Cool },
					{ "パッション", Attribute.Passion },
				};
				IdolCardList = dt.Select().Select(r => {
					string idolName = r.Field<string>("IdolName");
					string situation = (r.Field<string>("NickName") ?? "").Replace("[", "").Replace("]", "");
					Reality reality = realityTable[r.Field<string>("Rarity")];
					Attribute attribute = attributeTable[r.Field<string>("IdolType")];
					decimal vocal = r.Field<decimal>("Vo");
					decimal dance = r.Field<decimal>("Da");
					decimal visual = r.Field<decimal>("Vi");
					return new IdolCard {
						IdolName = idolName, Situation = situation, Reality = reality,
						Attribute = attribute, Vocal = vocal, Dance = dance, Visual = visual };
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
		// カードの属性
		public Attribute Attribute;
		// カードのVocal値
		public decimal Vocal;
		// カードのDance値
		public decimal Dance;
		// カードのVisual値
		public decimal Visual;

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
		// 属性表示用文字列
		private static string[] AttributeString = { "Cute", "Cool", "Passion" };
	}
	// アイドルカードのレアリティ
	// 暗黙の仮定として、全て特訓済みのカード(N+、SSR+など)だとする
	enum Reality { N, R, SR, SSR }
	// アイドル/楽曲の属性
	enum Attribute { Cute, Cool, Passion, All }
}

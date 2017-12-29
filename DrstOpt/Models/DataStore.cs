using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Text;

namespace DrstOpt.Models
{
	// データベース
	static class DataStore
	{
		// アイドル名の一覧
		public static List<IdolCard> IdolCardList { get; private set; }
		// アイドル名→インデックスへの変換
		public static Dictionary<string, int> IdolCardIndex { get; private set; }
		// アイドルを所持しているかのフラグ
		public static List<bool> HaveCardFlg { get; private set; }

		// 初期化
		public static bool Initialize(string folderPath) {
			// ファイル読み込み
			try {
				// アイドル名の一覧を読み込む
				IdolCardList = ReadIdolCardList(folderPath, out var idolCardIndex);
				IdolCardIndex = idolCardIndex;
				// 所持しているアイドルの一覧を読み込む
				HaveCardFlg = ReadHaveCardList(folderPath, out var extCardList);
				foreach (var idolCard in extCardList) {
					IdolCardIndex[idolCard.CardName2] = IdolCardList.Count;
					IdolCardList.Add(idolCard);
				}
			} catch(Exception ex) {
				Console.WriteLine(ex.ToString());
				return false;
			}
			return true;
		}
		// アイドル名の一覧を読み込む
		// refには、アイドル名→インデックスへの変換操作を行わせる
		private static List<IdolCard> ReadIdolCardList(string folderPath, out Dictionary<string, int> idolCardIndex) {
			// SQLを唱えて結果を取得する
			string connectionString = $"Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};Dbq={folderPath}\\IdolDB.accdb;Uid=admin;Pwd=;";
			Console.WriteLine(connectionString);
			string queryString = "SELECT * FROM IdolList";
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
					{ "全タイプ", Attribute.All },
					{ "Cuプリンセス", Attribute.CuteP },
					{ "Coプリンセス", Attribute.CoolP },
					{ "Paプリンセス", Attribute.PassionP },
					{ "トリコロール", Attribute.Tricolore },
					{ "", Attribute.None },
				};
			var csTable = new Dictionary<string, CSType> {
					{ "全アピール", CSType.All },
					{ "ボーカル", CSType.Vocal },
					{ "ダンス", CSType.Dance },
					{ "ビジュアル", CSType.Visual },
					{ "特技発動率", CSType.Skill },
					{ "ライフ", CSType.Life },
					{ "", CSType.None },
				};
			idolCardIndex = new Dictionary<string, int>();
			var idolCardList = dt.Select().Select(r => {
				// レコードから各項目を読み取る
				string idolName = r.Field<string>("IdolName");
				string situation = (r.Field<string>("NickName") ?? "").Replace("[", "").Replace("]", "").Replace("［", "").Replace("］", "");
				Reality reality = realityTable[r.Field<string>("Rarity")];
				Attribute attribute = attributeTable[r.Field<string>("IdolType")];
				decimal vocal = r.Field<decimal>("Vo");
				decimal dance = r.Field<decimal>("Da");
				decimal visual = r.Field<decimal>("Vi");
				Attribute centerSkillAttribute = attributeTable[(r.Field<string>("SklType") ?? "")];
				CSType centerSkillType = csTable[(r.Field<string>("SklTgt") ?? "")];
				decimal centerSkillPower = (r.Field<decimal?>("SklEff") ?? 0);
				// 結果を構造体に包んで返す
				return new IdolCard {
					IdolName = idolName, Situation = situation, Reality = reality,
					Attribute = attribute, Vocal = vocal, Dance = dance, Visual = visual,
					CenterSkillAttribute = centerSkillAttribute,
					CenterSkillType = centerSkillType,
					CenterSkillPower = centerSkillPower
				};
			}).ToList();
			for(int i = 0; i < idolCardList.Count; ++i) {
				idolCardIndex[idolCardList[i].CardName2] = i;
				idolCardIndex[idolCardList[i].CardName3] = i;
			}
			return idolCardList;
		}
		// 所持しているアイドルの一覧を読み込む
		private static List<bool> ReadHaveCardList(string folderPath, out List<IdolCard> extCardList) {
			// メモリ割り当て
			var haveCardFlg = new List<bool>();
			extCardList = new List<IdolCard>();
			// ファイル読み込み
			var textData = new List<List<string>>();
			using (var sr = new StreamReader($"{folderPath}\\IdolData.txt", Encoding.GetEncoding("shift_jis"))) {
				while (sr.Peek() > -1) {
					// 冒頭の文字列・カンマで区切った数によって分岐
					string getLine = sr.ReadLine();
					var splitedData = getLine.Split(",".ToCharArray()).ToList();
					if (splitedData.Count == 4) {
						// 1列目が「Master」ならば、マスターデータから引っ張れるはず
						if(splitedData[0] == "Master") {
							textData.Add(splitedData);
							haveCardFlg.Add(false);
						}
					} else {
						// 1列目が「User」ならば、ユーザーが作成したアイドルカードデータなはず
						if (splitedData[0] == "User") {
							textData.Add(splitedData);
							haveCardFlg.Add(false);
						}
					}
				}
			}
			// 読み込んだデータを分析する
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
			int masterCardListCount = textData.Count(r => r.Count == 4);
			foreach (var splitedData in textData) {
				if (splitedData.Count == 4) {
					// マスターデータ
					int cardCount = int.Parse(splitedData[2]);
					if (cardCount > 0)
						haveCardFlg[IdolCardIndex[splitedData[1]]] = true;
				} else {
					// ユーザーデータ
					string idolName = splitedData[6];
					string situation = splitedData[5];
					Reality reality = realityTable[splitedData[4]];
					Attribute attribute = attributeTable[splitedData[7]];
					decimal vocal = int.Parse(splitedData[24]);
					decimal dance = int.Parse(splitedData[25]);
					decimal visual = int.Parse(splitedData[26]);
					// 結果を構造体に包んで返す
					var card = new IdolCard {
						IdolName = idolName, Situation = situation, Reality = reality,
						Attribute = attribute, Vocal = vocal, Dance = dance, Visual = visual
					};
					int cardCount = int.Parse(splitedData[8]);
					if (cardCount > 0)
						haveCardFlg[masterCardListCount + extCardList.Count] = true;
					extCardList.Add(card);
				}
			}
			return haveCardFlg;
		}
		// 所持しているアイドルの一覧
		public static List<IdolCard> HaveIdolCardList {
			get {
				var haveIdolCardList = new List<IdolCard>();
				for(int i = 0; i < IdolCardList.Count; ++i) {
					if (HaveCardFlg[i])
						haveIdolCardList.Add(IdolCardList[i]);
				}
				return haveIdolCardList;
			}
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
		// センター効果の属性
		public Attribute CenterSkillAttribute;
		// センター効果の種類
		public CSType CenterSkillType;
		// センター効果の性能
		public decimal CenterSkillPower;

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
		// カードの名前(IdolData.txt用)
		public string CardName2 {
			get {
				if (Situation != "") {
					return $"{RealityString[(int)Reality]}[{Situation}]{IdolName}";
				} else {
					return $"{RealityString[(int)Reality]}{IdolName}";
				}
			}
		}
		public string CardName3 {
			// どういうわけか、［Trinity Field］神谷奈緒と［Trinity Field］北条加蓮だけ
			// マスターデータの大カッコが全角なので、その対策
			get {
				if (Situation != "") {
					return $"{RealityString[(int)Reality]}［{Situation}］{IdolName}";
				} else {
					return $"{RealityString[(int)Reality]}{IdolName}";
				}
			}
		}
		// カード情報
		public string CardInfo {
			get {
				return $"{CardName} ({Vocal},{Dance},{Visual}) <{CenterSkillAttribute} {CenterSkillType} {CenterSkillPower}>";
			}
		}
		// レアリティ表示用文字列
		private static string[] RealityString = { "N", "R", "SR", "SSR" };
		// 属性表示用文字列
		private static string[] AttributeString = { "Cute", "Cool", "Passion" };
	}
	// アイドルカードのレアリティ
	// 暗黙の仮定として、全て特訓済みのカード(N+、SSR+など)だとする
	enum Reality { N, R, SR, SSR }
	// アイドル/楽曲/センター効果の属性
	enum Attribute { All, Cute, Cool, Passion, CuteP, CoolP, PassionP, Tricolore, None }
	// センター効果の種類
	enum CSType { All, Vocal, Dance, Visual, Skill, Life, None }
}

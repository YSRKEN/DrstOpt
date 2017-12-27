using System;
using System.Data;
using System.Data.Odbc;
using System.Linq;

namespace DrstOpt.Models
{
	static class DataStore
	{
		// 初期化
		public static bool Initialize() {
			const string folderPath = @"hoge";
			// アイドル名の一覧を読み込む
			string connectionString = $"Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};Dbq={folderPath};Uid=admin;Pwd=;";
			Console.WriteLine(connectionString);
			string queryString = "SELECT IdolName FROM IdolList GROUP BY IdolName";
			var dt = new DataTable();
			try {
				using (var connection = new OdbcConnection(connectionString)) {
					connection.Open();
					var adapter = new OdbcDataAdapter(queryString, connection);
					adapter.Fill(dt);
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
				return false;
			}
			var idolNameList = dt.Select().Select(r => (string)r[0]).ToList();
			return true;
		}
	}
}

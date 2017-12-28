using Prism.Mvvm;
using System;
using System.Windows;

namespace DrstOpt.Models
{
	class MainModel : BindableBase
	{
		public void OptimizeIdolUnit(Attribute attribute) {
			//MessageBox.Show($"{attribute}", "デレステ編成最適化", MessageBoxButton.OK);
			for(int i = 0; i < DataStore.IdolCardList.Count; ++i) {
				if (DataStore.HaveCardFlg[i]) {
					Console.WriteLine($"{DataStore.IdolCardList[i].CardName}");
				}
			}
		}
	}
}

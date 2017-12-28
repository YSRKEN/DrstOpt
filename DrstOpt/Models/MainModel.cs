using Prism.Mvvm;
using System.Windows;

namespace DrstOpt.Models
{
	class MainModel : BindableBase
	{
		public void OptimizeIdolUnit(Attribute attribute) {
			MessageBox.Show($"{attribute}", "デレステ編成最適化", MessageBoxButton.OK);
		}
	}
}

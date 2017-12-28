using DrstOpt.Models;
using Reactive.Bindings;
using DrstOpt.Models;

namespace DrstOpt.ViewModels
{
	class MainViewModel
	{
		private MainModel mainModel;

		// 最適化したい曲の属性
		public ReactiveProperty<int> MusicAttributeIndex { get; private set; } = new ReactiveProperty<int>(0);
		// 最適化ボタン
		public ReactiveCommand OptimizeCommand { get; private set; } = new ReactiveCommand();

		public MainViewModel() {
			mainModel = new MainModel();
			OptimizeCommand.Subscribe(() => { mainModel.OptimizeIdolUnit((Attribute)MusicAttributeIndex.Value); });
		}
	}
}

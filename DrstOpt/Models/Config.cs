using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrstOpt.Models
{
	struct Config
	{
		// 曲の属性
		public Attribute Attribute;
		// 回復を積むか？
		public bool IncludeLifeRecoveryFlg;
		// ダメガを積むか？
		public bool IncludeDamageGuardFlg;
		// コンセを禁止するか？
		public bool ExcludeConcentrationFlg;
		// オバロを禁止するか？
		public bool ExcludeOverloadFlg;
		// Grooveイベントにおける最適化か？
		public bool GrooveFlg;
		// Grooveイベントにおける属性
		public Attribute GrooveAttribute;
		// Grooveイベントにおけるアピール
		public Appeal GrooveAppeal;
	}
}

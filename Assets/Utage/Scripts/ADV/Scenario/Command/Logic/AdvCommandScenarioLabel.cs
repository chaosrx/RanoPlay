//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：シナリオラベル
	/// </summary>
	internal class AdvCommandScenarioLabel : AdvCommand
	{
		public AdvCommandScenarioLabel(StringGridRow row)
			: base(row)
		{
			this.scenarioLabel = ParseScenarioLabel(AdvColumnName.Command);
			this.Type = ParseCellOptional<ScenarioLabelType>(AdvColumnName.Arg1, ScenarioLabelType.None);
		}


		public override void DoCommand(AdvEngine engine)
		{
		}

		//シナリオラベルか
		public override string GetScenarioLabel() { return scenarioLabel; }
		string scenarioLabel;

		public enum ScenarioLabelType
		{
			None,
			SavePoint,
		};

		public ScenarioLabelType Type { get; protected set; }
	}
}
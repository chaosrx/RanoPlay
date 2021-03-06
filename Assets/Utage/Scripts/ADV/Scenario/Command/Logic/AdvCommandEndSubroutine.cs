//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：サブルーチンの終了
	/// </summary>
	internal class AdvCommandEndSubroutine : AdvCommand
	{
		public AdvCommandEndSubroutine(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
		}

		public override void DoCommand(AdvEngine engine)
		{
			engine.ScenarioPlayer.JumpManager.EndSubroutine();
		}
	}
}
//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{

	/// <summary>
	/// コマンド：シェイク表示
	/// </summary>
	internal class AdvCommandShake : AdvCommand
	{
		public AdvCommandShake(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			this.effectData = new AdvEffectDataShake(this);
		}

		public override void DoCommand(AdvEngine engine)
		{
			engine.EffectManager.Play(effectData);
		}

		//コマンド終了待ち
		public override bool Wait(AdvEngine engine)
		{
			return engine.EffectManager.IsCommandWaiting(effectData);
		}

		AdvEffectDataShake effectData;
	}
}

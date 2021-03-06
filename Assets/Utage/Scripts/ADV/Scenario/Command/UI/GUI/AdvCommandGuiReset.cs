//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------

using UnityEngine;


namespace Utage
{

	/// <summary>
	/// コマンド：GUI操作　Reset
	/// </summary>
	internal class AdvCommandGuiReset : AdvCommand
	{
		public AdvCommandGuiReset(StringGridRow row)
			: base(row)
		{
			this.name = this.ParseCell<string>(AdvColumnName.Arg1);
		}

		public override void DoCommand(AdvEngine engine)
		{
			AdvGuiBase gui;
			if (!engine.UiManager.GuiManager.TryGet(this.name, out gui))
			{
				 Debug.LogError( this.ToErrorString(name + " is not found in GuiManager"));
			}
			else
			{
				gui.Reset();
			}
		}

		string name;
	}
}

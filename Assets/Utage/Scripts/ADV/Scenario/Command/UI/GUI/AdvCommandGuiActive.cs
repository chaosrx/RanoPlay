//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------

using UnityEngine;


namespace Utage
{

	/// <summary>
	/// コマンド：GUI操作　Active
	/// </summary>
	internal class AdvCommandGuiActive : AdvCommand
	{
		public AdvCommandGuiActive(StringGridRow row)
			: base(row)
		{
			this.name = this.ParseCell<string>(AdvColumnName.Arg1);
			this.isActive = this.ParseCell<bool>(AdvColumnName.Arg2);
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
				gui.SetActive(this.isActive);
			}
		}

		string name;
		bool isActive;
	}
}

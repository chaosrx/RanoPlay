//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

namespace Utage
{
	/// <summary>
	/// ローカライズの設定
	/// </summary>
	public class AdvLocalizeSetting : AdvSettingDataBase
	{
		protected override void OnParseGrid(StringGrid grid, AdvBootSetting bootSetting)
		{
			LanguageManagerBase languageManager = LanguageManagerBase.Instance;
			if(languageManager!=null)
			{
				languageManager.OverwriteData(grid);
			}
		}
	}
}

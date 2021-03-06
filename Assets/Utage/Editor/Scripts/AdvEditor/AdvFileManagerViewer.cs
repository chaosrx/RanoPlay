//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Utage
{
	//宴のビューワー表示ウィンドウ
	public class AdvFileManagerViewer : CustomEditorWindow
	{
		void OnEnable()
		{
			//シーン変更で描画をアップデートする
			this.autoRepaintOnSceneChange = true;
			//スクロールを有効にする
			this.isEnableScroll = true;
		}

		protected override void OnGUISub()
		{
			AssetFileManager.OnGuiViwer();
		}
	}
}

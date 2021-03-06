//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------


namespace Utage
{

	/// <summary>
	/// コマンド：キャラクター＆台詞表示
	/// </summary>
	internal class AdvCommandCharacterOff : AdvCommand
	{

		public AdvCommandCharacterOff(StringGridRow row)
			: base(row)
		{
			this.name = ParseCellOptional<string>(AdvColumnName.Arg1, "");
			//フェード時間
			this.fadeTime = ParseCellOptional<float>(AdvColumnName.Arg6, 0.2f);
		}

		public override void DoCommand(AdvEngine engine)
		{
			if (string.IsNullOrEmpty(name))
			{
				engine.GraphicManager.CharacterManager.FadeOutAll(fadeTime );
			}
			else
			{
				engine.GraphicManager.CharacterManager.FadeOut(name, fadeTime);
			}
		}

		string name;
		float fadeTime;
	}

}
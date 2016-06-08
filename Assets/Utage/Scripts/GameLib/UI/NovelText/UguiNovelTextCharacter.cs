//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{
	//ノベル用のテキスト描画文字情報
	public class UguiNovelTextCharacter
	{
		public bool isAutoLineBreak;			//自動改行
		public bool isKinsokuBurasage;			//禁則処理によるぶら下げ文字

		public CharData charData;						//文字情報
		public CharacterInfo charInfo;					//フォントから取得した描画用の文字情報

		//表示の最大幅
		public CharData.CustomCharaInfo CustomInfo
		{
			get { return charData.CustomInfo; }
		}

		//幅
		float width;
		public float Width
		{
			get { return width; }
			set { width = value; }
		}

		//フォントサイズ
		public int FontSize
		{
			get { return fontSize; }
		}
		int fontSize;

		//基本のフォントサイズ（上付き処理とかでスケールをかける前）
		public int DefaultFontSize { get { return defaultFontSize; } }
		int defaultFontSize;

		//フォントスタイル
		public FontStyle FontStyle
		{
			get { return fontStyle; }
		}
		FontStyle fontStyle;

		UIVertex[] verts;			//描画に使う頂点情報

		public UIVertex[] Verts
		{
			get { return verts; }
		}


		//描画位置（座標は中央ではなく、文字の左下基準位置になるので注意）
		Vector2 position;
		public float PositionX { get { return position.x; } set { position.x = value; }}
		public float PositionY { get { return position.y; } set { position.y = value; } }

		bool isError;								//何らかの理由で文字が取得できない

		//文字
		public char Char { get { return charData.Char; } }

		//改行文字か
		public bool IsBr { get { return charData.IsBr; } }

		//改行文字・または自動改行されているか
		public bool IsBrOrAutoBr { get { return (isAutoLineBreak || charData.IsBr); } }


		//改行文字または空白か
		public bool IsBlank
		{
			get
			{
				return isError || char.IsWhiteSpace(charData.Char) || charData.IsBr;
			}
		}

		//スプライトを使うか
		public bool IsSprite {
			get{ return isSprite; }
			set { isSprite = value; }
		}
		bool isSprite;

		//フォントデータがないか（改行文字または空白、絵文字など）
		public bool IsNoFontData
		{
			get
			{
				return IsBlank || IsSprite;
			}
		}

		//ルビによるスペースサイズ
		public float RubySpaceSize { get; set; }

		//自動改行が無効か
		public bool IsDisableAutoLineBreak
		{
			get
			{
				//ルビは先頭以外では改行できない
				if (CustomInfo.IsRuby && !CustomInfo.IsRubyTop)
				{
					return true;
				}
				//グループ文字は先頭行以外は改行できない
				if (CustomInfo.IsGroup && !CustomInfo.IsGroupTop)
				{
					return true;
				}
				return false;
			}
		}

		//コンストラクタ
		public UguiNovelTextCharacter(CharData charData, UguiNovelTextGenerator generator )
		{
			if (charData.CustomInfo.IsDash)
			{
				charData.Char = generator.DashChar;
			}

			int bmpFontSize = generator.BmpFontSize;
			Init(charData, generator.NovelText.font, generator.NovelText.fontSize, bmpFontSize, generator.NovelText.fontStyle, generator.Space);

			//上つき文字、下つき文字
			if (charData.CustomInfo.IsSuperOrSubScript)
			{
				this.fontSize = Mathf.FloorToInt(generator.SupOrSubSizeScale*this.fontSize);
				if (!generator.NovelText.font.dynamic)
				{
					BmpFontScale = 1.0f * fontSize / bmpFontSize;
				}

			}

			//スペースの指定
			if (charData.CustomInfo.IsSpace)
			{
				width = charData.CustomInfo.SpaceSize;
			}

			//絵文字
			if (generator.EmojiData)
			{
				//スプライトを使うか
				if (CustomInfo.IsEmoji || generator.EmojiData.Contains(Char))
				{
					IsSprite = true;
				}

				//絵文字の制御
				if (IsSprite)
				{
					Sprite sprite = FindSpirte(generator);
					if (sprite)
					{
						float scale = sprite.rect.width / generator.EmojiData.Size;
						width = scale * fontSize;
					}
					else
					{
						Debug.LogError("Not found Emoji[" + Char + "]" + ":" + (int)Char);
					}
				}
			}
		}
		
		//コンストラクタ
		public UguiNovelTextCharacter(CharData charData, Font font, int fontSize, int bmpFontSize, FontStyle fontStyle)
		{
			Init(charData, font, fontSize, bmpFontSize, fontStyle, 0);
		}

		//初期化
		void Init(CharData charData, Font font, int fontSize, int bmpFontSize, FontStyle fontStyle, float spacing  )
		{
			this.charData = charData;

			//フォントサイズの設定
			this.fontSize = this.defaultFontSize = charData.CustomInfo.GetCustomedSize(fontSize);			
			//フォントスタイルの設定
			this.fontStyle = charData.CustomInfo.GetCustomedStyle(fontStyle);

			if (charData.IsBr)
			{
				//改行文字の場合は、
				//'\'などの文字である場合があるので、幅0にして非表示
				width = 0;
			}
			else if (char.IsWhiteSpace(charData.Char))
			{
				//スペースの場合は、幅を固定する
				if (spacing >= 0)
				{
					width = spacing;
				}
				else
				{
					width = fontSize / 3;
				}
			}

			if (font.dynamic)
			{
				BmpFontScale = 1;
			}
			else
			{
				BmpFontScale = 1.0f * this.fontSize / bmpFontSize;
			}
		}

		//CharacterInfo（描画用の文字情報）の設定を試行
		public bool TrySetCharacterInfo( Font font )
		{
			if (IsNoFontData) return true;

			if (!font.dynamic)
			{
				if (!font.GetCharacterInfo(charData.Char, out this.charInfo))
				{
					return false;
				}
			}
			else if (!font.GetCharacterInfo(charData.Char, out this.charInfo, FontSize, FontStyle))
			{
				return false;
			}

			this.width = WrapperUnityVersion.GetCharacterInfoWidth(ref charInfo);
			this.width *= BmpFontScale;
			//ダッシュ
			if (CustomInfo.IsDash)
			{
				width *= CustomInfo.DashSize;
			}
			return true;
		}

		//CharacterInfo（描画用の文字情報）の設定
		public void SetCharacterInfo(Font font)
		{
			if (!TrySetCharacterInfo(font))
			{
//				Debug.LogError(string.Format("Missing CharaInfo :{0}", charData.Char));
				isError = true;
				width = fontSize;
			}
		}


		internal void InitPositionX(float x, float currentSpace)
		{
			PositionX = x;
		}

		public void ApplyOffsetX(float offsetX)
		{
			PositionX += offsetX;
		}
		public void ApplyOffsetY(float offsetY)
		{
			PositionY += offsetY;
		}


		//描画頂点作成
		public void MakeVerts(Color defaultColor, UguiNovelTextGenerator generator )
		{
			if (IsNoFontData) return;

			verts = new UIVertex[4];
			Color color = charData.CustomInfo.GetCustomedColor(defaultColor);
			color *= effectColor;

			for (int i = 0; i < 4; ++i)
			{
				verts[i] = UIVertex.simpleVert;
				verts[i].color = color;
			}

			WrapperUnityVersion.SetCharacterInfoToVertex(verts, this, generator.NovelText.font );
            if (!generator.NovelText.font.dynamic && !generator.IsUnicodeFont)
            {
                float offset = this.fontSize;
                for (int i = 0; i < 4; ++i)
                {
                    verts[i].position.y += offset;
                }
            }

            //上付き文字の処理
            if (CustomInfo.IsSuperScript)
			{
				float offset = (1.0f - generator.SupOrSubSizeScale) * this.DefaultFontSize;
				for (int i = 0; i < 4; ++i)
				{
					verts[i].position.y += offset;
				}
			}

			//ダッシュの場合
			if (CustomInfo.IsDash)
			{
				//中央の位置に表示
				float h = Mathf.Abs(verts[2].position.y - verts[0].position.y);
				float y0 = PositionY + this.FontSize/2;
				verts[0].position.y = verts[1].position.y = y0 - h / 2;
				verts[2].position.y = verts[3].position.y = y0 + h / 2;

				//横に伸ばす処理を入れる
				//Slice処理のために頂点増やす
				UIVertex[] sliceVerts = new UIVertex[12];
				for( int i = 0;i < 12; ++i)
				{
					sliceVerts[i] = verts[i % 4];
				}
				float x0 = verts[0].position.x;
				float w0 = verts[1].position.x - verts[0].position.x;

				float x3 = x0 + Width;
				float x1 = x0 + w0 / 3;
				float x2 = x3 - w0 / 3;
				SetVertexX(sliceVerts, 0, x0, x1);
				SetVertexX(sliceVerts, 4, x1, x2);
				SetVertexX(sliceVerts, 8, x2, x3);
				
				//UV座標はFilpされてる可能性があるので注意
				Vector2 uvBottomLeft, uvBottomRight, uvTopRight, uvTopLeft;
				Vector2 uvBottomLeft2, uvBottomRight2, uvTopRight2, uvTopLeft2;

				uvBottomLeft = verts[0].uv0;
				uvBottomRight = verts[1].uv0;
				uvTopRight = verts[2].uv0;
				uvTopLeft = verts[3].uv0;

				uvBottomLeft2 = (uvBottomRight - uvBottomLeft) * 1 / 3 + uvBottomLeft;
				uvBottomRight2 = (uvBottomRight - uvBottomLeft) * 2 / 3 + uvBottomLeft;
				uvTopRight2 = (uvTopRight - uvTopLeft) * 2 / 3 + uvTopLeft;
				uvTopLeft2 = (uvTopRight - uvTopLeft) * 1 / 3 + uvTopLeft;

				SetVertexUV(sliceVerts, 0, uvBottomLeft, uvBottomLeft2, uvTopLeft2, uvTopLeft);
				SetVertexUV(sliceVerts, 4, uvBottomLeft2, uvBottomRight2, uvTopRight2, uvTopLeft2);
				SetVertexUV(sliceVerts, 8, uvBottomRight2, uvBottomRight, uvTopRight, uvTopRight2);

				verts = sliceVerts;
			}
		}

		void SetVertexX(UIVertex[] vertex, int index, float xMin, float xMax)
		{
			vertex[index + 0].position.x = vertex[index + 3].position.x = xMin;
			vertex[index + 1].position.x = vertex[index + 2].position.x = xMax;
		}

		void SetVertexUV(UIVertex[] vertex, int index, Vector2 uvBottomLeft, Vector2 uvBottomRight, Vector2 uvTopRight, Vector2 uvTopLeft)
		{
			vertex[index + 0].uv0 = uvBottomLeft;
			vertex[index + 1].uv0 = uvBottomRight;
			vertex[index + 2].uv0 = uvTopRight;
			vertex[index + 3].uv0 = uvTopLeft;
		}

		//左端の座標
		public float BeginPositionX
		{
			get
			{
				return PositionX - RubySpaceSize;
			}
		}
		
		//右端の座標
		public float EndPositionX
		{
			get
			{
				return PositionX + Width + RubySpaceSize;
			}
		}

		//絵文字などのグラフィックオブジェクトを作成
		internal RectTransform AddGraphicObject(RectTransform parent, UguiNovelTextGenerator generator)
		{
			if (!IsSprite) return null;

			Sprite sprite = FindSpirte(generator);
			if (sprite)
			{
				RectTransform child = UtageToolKit.AddChildGameObjectComponent<RectTransform>(parent, sprite.name);
				child.gameObject.hideFlags = HideFlags.DontSave;
				Image image = child.gameObject.AddComponent<Image>();
				image.sprite = sprite;

				float scaleX, scaleY = 1.0f;
				scaleX = scaleY = 1.0f * FontSize / generator.EmojiData.Size;
				float w = sprite.rect.width * scaleX;
				float h = sprite.rect.height * scaleY;
				child.sizeDelta = new Vector2(w, h);
				child.localPosition = new Vector3(PositionX + w / 2, PositionY + h / 2, 0);
				return child;
			}
			else
			{
				return null;
			}
		}

		//使用するスプライトを検索
		Sprite FindSpirte(UguiNovelTextGenerator generator)
		{
			//絵文字の取得
			if (generator.EmojiData == null) return null;
			Sprite sprite = generator.EmojiData.GetSprite(Char);
			if (sprite == null)
			{
				if (CustomInfo.IsEmoji)
				{
					sprite = generator.EmojiData.GetSprite(charData.CustomInfo.EmojiKey);
				}
			}
			return sprite;
		}

		public bool IsVisible { get; set; }

		Color effectColor = Color.white;

		//クリック可能なことを示すエフェクトカラーを設定
		internal void ChangeEffectColor(Color effectColor)
		{
			this.effectColor = effectColor;
		}

		//クリック可能なことを示すエフェクトカラーをリセット
		internal void ResetEffectColor()
		{
			this.effectColor = Color.white;
		}

		public float BmpFontScale { get; private set; }
	};
}

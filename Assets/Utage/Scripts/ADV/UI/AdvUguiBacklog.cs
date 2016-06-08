//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using System.Collections;

namespace Utage
{
	/// <summary>
	/// バックログ用UI
	/// </summary>
	[AddComponentMenu("Utage/ADV/UiBacklog")]
	[RequireComponent(typeof(Button))]
	public class AdvUguiBacklog : MonoBehaviour
	{
		/// <summary>テキスト</summary>
		public UguiNovelText text;

		/// <summary>キャラ名</summary>
		public Text characterName;

		/// <summary>ボイス再生アイコン</summary>
		public GameObject soundIcon;

		public Button Button { get { return button ?? (button = GetComponent<Button>()); } }
		Button button;

		/// <summary>ページ内に複数行あるか（ログの長さにあわせて変えるたりする）</summary>
		public bool isMultiTextInPage;

		public AdvBacklog Data { get { return data; } }
		AdvBacklog data;

		/// <summary>
		/// 初期化
		/// </summary>
		/// <param name="data">バックログのデータ</param>
		public void Init(Utage.AdvBacklog data)
		{
			this.data = data;

			if (isMultiTextInPage)
			{
				float defaltHeight = this.text.rectTransform.rect.height;
				this.text.text = data.Text;
				float height = this.text.preferredHeight;
				(this.text.transform as RectTransform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

				float baseH = (this.transform as RectTransform).rect.height;
				float scale = this.text.transform.lossyScale.y / this.transform.lossyScale.y;
				baseH += (height - defaltHeight) * scale;
				(this.transform as RectTransform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, baseH);
			}
			else
			{
				this.text.text = data.Text;
			}

			characterName.text = data.MainCharacterNameText;

			int countVoice = data.CountVoice;
			if (countVoice <= 0)
			{
				soundIcon.SetActive(false);
				Button.interactable = false;
			}
			else
			{
				Button.onClick.AddListener(() => OnClicked(data.MainVoiceFileName));
				if (countVoice > 2 || isMultiTextInPage)
				{
					UguiNovelTextEventTrigger trigger = UtageToolKit.GetComponentCreateIfMissing<UguiNovelTextEventTrigger>(text.gameObject);
					trigger.OnClick.AddListener((x) => OnClickHitArea(x, OnClicked));
				}
				else
				{
					//				Button.onClick.AddListener(() => ButtonClickedEvent(this, ""));
				}
			}
		}

		void OnClickHitArea(UguiNovelTextHitArea hitGroup, Action<string> OnClicked)
		{
			switch (hitGroup.HitEventType)
			{
				case CharData.HitEventType.Sound:
					OnClicked(hitGroup.Arg);
					break;
			}
		}


		/// <summary>
		/// 音声再生ボタンが押された
		/// </summary>
		/// <param name="button">押されたボタン</param>
		void OnClicked(string voiceFileName)
		{
			if (!string.IsNullOrEmpty(voiceFileName))
			{
				StartCoroutine(CoPlayVoice(voiceFileName, Data.FindCharacerLabel(voiceFileName)));
			}
		}

		//ボイスの再生
		IEnumerator CoPlayVoice(string voiceFileName, string charcterLabel)
		{
			AssetFile file = AssetFileManager.Load(voiceFileName, this);
			if (file == null)
			{
				Debug.LogError("Backlog voiceFile is NULL");
				yield break;
			}
			while (!file.IsLoadEnd)
			{
				yield return 0;
			}
			SoundManager manager = SoundManager.GetInstance();
			if (manager)
			{
				manager.PlayVoice(charcterLabel, file);
			}
			file.Unuse(this);
		}

	}
}


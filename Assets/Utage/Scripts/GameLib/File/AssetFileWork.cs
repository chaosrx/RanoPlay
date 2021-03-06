//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------
#if false
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Utage
{


	/// <summary>
	/// ファイルの実態。システムから使う想定なので、外からは使用しないこと
	/// </summary>
	public class AssetFileWork : AssetFile
	{
		/// <summary>
		/// ファイルパス
		/// </summary>
		public string FileName { get; set; }


		/// <summary>
		/// ファイルの情報
		/// </summary>
		AssetFileInfo FileInfo { get; set; }

		/// <summary>
		/// ファイルタイプ
		/// </summary>
		public AssetFileType FileType { get { return FileInfo.FileType; } }

		/// <summary>
		/// ロードの優先順
		/// </summary>
		public AssetFileLoadPriority Priority { get; protected  set;}

		//状態
		enum STAUS
		{
			LOAD_WAIT,	//待機中
			LOADING,	//ロード中
			LOAD_END,	//ロード終了
			USING,		//アセットは使用中
			UNUSED,		//アセットは未使用
		};
		STAUS status;

		//ストリーミングの状態
		enum LOAD_STREAMING_STAUS
		{
			NONE,		//ストリーミングしない
			LOADING,	//ロード中
			READY,		//ストリーミング再生可能状態
			LOADEND,	//ストリーミングロード終了
			DONE,		//wwwのロードが完全に終わった（ストリーミングできない小さい素材なので通常ロードする）
		};
		LOAD_STREAMING_STAUS streamingStatus;

		/// <summary>
		/// メモリサイズ（正確な値ではなく目安）
		/// </summary>
		public int MemSize { get { return this.memSize; } }
		int memSize = 0;

		/// <summary>
		/// ロード終了したか
		/// </summary>
		public bool IsLoadEnd
		{
			get
			{
				bool ret = (status != STAUS.LOAD_WAIT) && (status != STAUS.LOADING);
				foreach( var subFile in SubFiles.Values )
				{
					ret &= subFile.IsLoadEnd;
				}
				return ret;
			}
		}

		/// <summary>
		/// 再度ロード（エラー時のリトライとは別）
		/// </summary>
		public bool IsLoadRetry { get { return this.isLoadRetry; } }
		bool isLoadRetry = false;

		/// <summary>
		/// ロードエラーしたか
		/// </summary>
		public bool IsLoadError { get { return this.isLoadError; } }
		bool isLoadError = false;

		/// <summary>
		/// ロードエラー時のエラーメッセージ
		/// </summary>
		public string LoadErrorMsg { get { return this.loadErrorMsg; } }
		string loadErrorMsg = "";

		/// <summary>
		/// ロードエラー時のリトライ回数を加算
		/// </summary>
		/// <returns>加算後のリトライ回数</returns>
		public int IncLoadErrorRetryCount()
		{
			return ++loadErrorRetryCount;
		}

		/// <summary>
		/// ロードエラー時のリトライ回数をリセット
		/// </summary>
		public void ResetLoadErrorRetryCount()
		{
			loadErrorRetryCount = 0; ;
		}
		int loadErrorRetryCount = 0;

		/// <summary>
		/// ストリーム再生ができるか
		/// </summary>
		public bool IsReadyStreaming { get { return (streamingStatus == LOAD_STREAMING_STAUS.READY) || IsLoadEnd; } }

		/// <summary>
		/// 新たなキャッシュファイルを書き込むときのバイナリ
		/// </summary>
		public byte[] CacheWriteBytes { get { return this.cacheWriteBytes; } }
		byte[] cacheWriteBytes;

		///ダウンロードのみか
		bool IsDownloadOnly { get { return (Priority == AssetFileLoadPriority.DownloadOnly); } }

		//ファイルIOシステム
		FileIOManagerBase fileIO;

		//参照オブジェクト
		HashSet<System.Object> referenceSet = new HashSet<object>();

		//未使用リソースのソートID
		static int sCommonUnusedSortID = 0;
		int unusedSortID;
		public int UnusedSortID { get { return unusedSortID; } }

		float elapsedTime = 0.0f;	//DL経過時間
		float lastProgress = 0;		//前回のDL進行度

		/// <summary>
		/// 実際にロード中のパス
		/// </summary>
		public string LoadingPath { get { return this.loadingPath; } }
		string loadingPath;

		/// <summary>
		/// ロードしたテキスト
		/// </summary>
		public string Text { get { return IsReadyToUse() ? this.text : null; } }
		string text;

		/// <summary>
		/// ロードしたバイナリ
		/// </summary>
		public byte[] Bytes { get { return IsReadyToUse() ? this.bytes : null; } }
		byte[] bytes;

		/// <summary>
		/// ロードしたテクスチャ
		/// </summary>
		public Texture2D Texture { get { return IsReadyToUse() ? this.texture : null; } }
		Texture2D texture;

		/// <summary>
		/// ロードしたサウンド
		/// </summary>
		public AudioClip Sound { get { return IsReadyToUse() ? this.sound : null; } }
		AudioClip sound;
		public AudioClip WriteCacheFileSound { get { return this.sound; } }

		/// <summary>
		/// ロードしたCSV
		/// </summary>
		public StringGrid Csv { get { return IsReadyToUse() ? this.csv : null; } }
		StringGrid csv;

		/// <summary>
		/// ロードしたUnityObject
		/// </summary>
		public UnityEngine.Object UnityObject { get { return IsReadyToUse() ? this.unityObject : null; } }
		UnityEngine.Object unityObject;

		//ロードしたアセットバンドル
		public AssetBundle AssetBundle { get; set; }

		/// <summary>
		/// ロード終了時のコールバック
		/// </summary>
		public AssetFileLoadComplete OnLoadComplete { get; set; }

		/// <summary>
		/// ロードしたテクスチャから作ったスプライト
		/// </summary>
		/// <param name="pixelsToUnits">スプライトを作成する際の、座標1.0単位辺りのピクセル数</param>
		/// <returns>作成したスプライト</returns>
		public Sprite GetSprite( GraphicInfo graphic, float pixelsToUnits)
		{
			Sprite sprite;
			if (!spriteTbl.TryGetValue(graphic, out sprite))
			{
				if (graphic != null)
				{
					sprite = UtageToolKit.CreateSprite(this.Texture, pixelsToUnits, graphic.Pivot);
				}
				else
				{
					sprite = UtageToolKit.CreateSprite(this.Texture, pixelsToUnits );
				}
			}
			return sprite;
		}
		Dictionary<GraphicInfo,Sprite> spriteTbl = new Dictionary<GraphicInfo,Sprite>();

		/// <summary>
		/// バージョン
		/// </summary>
		public int Version
		{
			get { return FileInfo.Version; }
			set
			{
				if (FileInfo.Version != value)
				{
					if (status != STAUS.LOAD_WAIT)
					{
						Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.DisableChangeFileVersion) + "  " +  FileInfo.FilePath);
					}
					else
					{
						FileInfo.Version = value;
					}
				}
			}
		}

		/// <summary>
		/// キャッシュファイルのバージョン
		/// </summary>
		public int CacheVersion
		{
			get { return FileInfo.CacheVersion; }
		}

		/// <summary>
		/// ロードフラグ
		/// </summary>
		public AssetFileLoadFlags LoadFlags
		{
			get { return FileInfo.LoadFlags; }
		}
		/// <summary>
		/// ロードフラグを追加
		/// </summary>
		public void AddLoadFlag(AssetFileLoadFlags flags)
		{
			if ((FileInfo.LoadFlags & flags) == flags) return;

#if UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0
			if ((flags & AssetFileLoadFlags.Streaming) == AssetFileLoadFlags.Streaming)
			{
				//oggはUnityのバグがあるのでストリーム読み込みを無効にする
				//バグの内容は、曲の長さがとれず一度鳴らすと終了しなくなるというもの。
				if (ExtensionUtil.CheckExtention(this.FileInfo.FilePath, ExtensionUtil.Ogg))
				{
					Debug.LogWarning("Not support ogg streaming :" + this.FileInfo.FilePath);
//					return;
				}
			}
#endif

			//ロードフラグの反映
			if (status != STAUS.LOAD_WAIT)
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.DisableChangeFileLoadFlag));
			}
			else
			{
				FileInfo.AddLoadFlag( flags );
			}
		}

		/// <summary>
		/// 派生ファイル群
		/// 設定ファイルなどを読んだときに
		/// その中で設定されているファイルもロードする必要があるときに使う
		/// </summary>
		public Dictionary<string,AssetFile> SubFiles { get; protected set; }

		public bool IsWriteNewCache { get { return FileInfo.IsWriteNewCache; } }

//		int UnusedSortID { get; }

//		void Unload();

//		int IncLoadErrorRetryCount();

//		IEnumerator CoLoadAsync(float timeOutDownload);

		public bool IsCaching { get { return FileInfo.IsCaching; } }

//		void ResetLoadErrorRetryCount();

//		bool IsLoadRetry { get; }

//		void LoadComplete();

		public void DeleteOldCacheFile()
		{
			this.FileInfo.DeleteOldCacheFile();
		}

//		bool CheckUnuse();

		public AssetFileEncodeType EncodeType { get { return FileInfo.EncodeType; } }

//		public AssetFileType FileType { get { return FileInfo.FileType; } }

		public void ReadyToWriteCache(int id, string cacheRootDir, bool isDebugFileName)
		{
			this.FileInfo.ReadyToWriteCache(id, cacheRootDir, isDebugFileName);
		}

		public string CachePath { get { return FileInfo.CachePath; } }

//		AudioClip WriteCacheFileSound { get; }

//		byte[] CacheWriteBytes { get; }

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="info">ファイル情報</param>
		/// <param name="fileIO">ファイルのIO管理クラス</param>
		public AssetFileWork(AssetFileInfo info, FileIOManagerBase fileIO)
		{
			this.FileName = info.Key;
			this.FileInfo = info;
			this.fileIO = fileIO;
			this.status = STAUS.LOAD_WAIT;
			this.Priority = AssetFileLoadPriority.DownloadOnly;
			this.SubFiles = new Dictionary<string, AssetFile>();
		}

		/// <summary>
		/// ロードの準備開始
		/// </summary>
		/// <param name="loadPriority">ロードの優先順</param>
		/// <param name="referenceObj">ファイルを参照するオブジェクト</param>
		/// <returns></returns>
		public bool ReadyToLoad(AssetFileLoadPriority loadPriority, System.Object referenceObj)
		{

			//ロードプライオリティの反映
			if (loadPriority < this.Priority)
			{
				this.Priority = loadPriority;
			}

			Use(referenceObj);
			unusedSortID = Int32.MaxValue;

			//通常ロード
			switch (status)
			{
				case STAUS.LOAD_WAIT:
					status = STAUS.LOADING;
					return false;
				case STAUS.LOADING:
				case STAUS.LOAD_END:
					return true;
				case STAUS.USING:
				case STAUS.UNUSED:
				default:
					status = STAUS.USING;
					return true;
			}
		}

		/// <summary>
		/// ロード処理
		/// </summary>
		/// <param name="timeOutDownload">ダウンロードのタイムアウトエラー時間</param>
		/// <returns></returns>
		public IEnumerator CoLoadAsync(float timeOutDownload)
		{
			this.status = STAUS.LOADING;
			this.isLoadRetry = false;
			this.isLoadError = false;
			this.streamingStatus = LOAD_STREAMING_STAUS.NONE;
			this.loadingPath = FileInfo.LoadWWWPath;

			if (FileInfo.StrageType == AssetFileStrageType.Resources)
			{
				LoadResource(loadingPath);
			}
			else
			{
				elapsedTime = 0.0f;
				lastProgress = 0;

				WWW www;
//				if (FileInfo.IsAssetBundle && FileInfo.StrageType != AssetFileStrageType.StreamingAssets  )
				if (FileInfo.IsAssetBundle)
				{
					//アセットバンドルのDL
					www = WWW.LoadFromCacheOrDownload(loadingPath,Version);
				}
				else
				{
					//その他
					www = new WWW(loadingPath);
				}
				if (www == null)
				{
					SetLoadError("Not Found");
					yield break;
				}

				//WWWでダウンロード
				using (www)
				{
					//ストリーミング再生でキャッシュへの書き込みが必要ない場合は、先にストリーミング用のサウンドを作成
					if (FileInfo.IsStreamingType && !FileInfo.IsWriteNewCache )
					{
						streamingStatus = LOAD_STREAMING_STAUS.LOADING;
						sound = www.GetAudioClip(FileInfo.IsAudio3D, true, FileInfo.AudioType );
						sound.name = FileInfo.FilePath;
					}

					//ロード待ち
					while (!www.isDone && string.IsNullOrEmpty(www.error)
						&& !CheckSoundStreamReady(www)
						&& !CheckDownloadTimeout(www, timeOutDownload))
					{
						UpdateLoadPirority(www);
						yield return 0;
					}

					if (!string.IsNullOrEmpty(www.error))
					{
						Debug.Log(loadingPath + " is " + File.Exists(loadingPath) );
						//ロードエラー
						SetLoadError(www.error);
					}
					else if (CheckSoundStreamReady(www))
					{
						//ストリーミングのみ独自処理
						streamingStatus = LOAD_STREAMING_STAUS.READY;

						//その後のロード待ち
						while (!www.isDone && string.IsNullOrEmpty(www.error)
							&& !CheckDownloadTimeout(www, timeOutDownload))
						{
							UpdateLoadPirority(www);
							yield return 0;
						}
						if (!string.IsNullOrEmpty(www.error))
						{
							//ロードエラー
							SetLoadError(www.error);
						}
						else if (!www.isDone)
						{
							//ロードエラー（タイムアウト）
							SetLoadError("DownLoad TimeOut " + elapsedTime + "sec");
						}
						else
						{

						}
					}
					else if (!www.isDone)
					{
						//ロードエラー（タイムアウト）
						SetLoadError("DownLoad TimeOut " + elapsedTime + "sec");
					}
					else
					{
						//ロード終了

						//ストリーミングでも、完全にロードした場合はこっち来る
						if (streamingStatus != LOAD_STREAMING_STAUS.NONE) streamingStatus = LOAD_STREAMING_STAUS.DONE;

						if (!IsDownloadOnly)
						{
							try
							{
								switch (FileInfo.EncodeType)
								{
									case AssetFileEncodeType.None:
										LoadWWWNormal(www);
										break;
									case AssetFileEncodeType.AlreadyEncoded:
										LoadWWWCriptFile(www);
										break;
									case AssetFileEncodeType.EncodeOnCache:
										if (FileInfo.IsWriteNewCache)
										{
											LoadWWWNormal(www);
										}
										else
										{
											LoadWWWCriptFile(www);
										}
										break;
									case AssetFileEncodeType.AssetBundle:
										LoadAssetBundle(www);
										break;
									default:
										SetLoadError("Load Error " + FileInfo.CachePath);
										break;
								}
							}
							catch( Exception e )
							{
								SetLoadError("Load Error " + e.Message + "\n" + e.StackTrace);
							}
						}
						//新たにキャッシュファイルとして書き込む必要がある場合は、バイナリを取得しておく
						if (FileInfo.IsWriteNewCache)
						{
							cacheWriteBytes = www.bytes;
						}
					}
				}
			}
			yield break;
		}

		//ロードエラーを設定
		void SetLoadError(string errorMsg)
		{
			loadErrorMsg = errorMsg + " : load from " + this.loadingPath;
			isLoadError = true;
		}

		//ロードのタイムアウトをチェック
		bool CheckDownloadTimeout(WWW www, float timeOutDownload)
		{
			if (lastProgress == www.progress)
			{
				elapsedTime += Time.deltaTime;
				if (elapsedTime >= timeOutDownload)
				{
					return true;
				}
			}
			else
			{
				elapsedTime = 0;
				lastProgress = www.progress;
			}
			return false;
		}

		void UpdateLoadPirority(WWW www)
		{
#if !UNITY_WEBGL
			switch (Priority)
			{
				case AssetFileLoadPriority.Default:
					www.threadPriority = ThreadPriority.High;
					break;
				case AssetFileLoadPriority.Preload:
					www.threadPriority = ThreadPriority.Normal;
					break;
				case AssetFileLoadPriority.BackGround:
					www.threadPriority = ThreadPriority.BelowNormal;
					break;
				case AssetFileLoadPriority.DownloadOnly:
				default:
					www.threadPriority = ThreadPriority.Low;
					break;
			}
#endif
		}

		//ストリーミングタイプのサウンドだった場合、利用可能になったか
		bool CheckSoundStreamReady(WWW www)
		{
			if (null == sound)
			{
				return false;
			}
			else
			{
				return WrapperUnityVersion.IsReadyPlayAudioClip(sound);
			}
		}

		//ロード処理（通常）
		void LoadWWWNormal(WWW www)
		{
			if (FileInfo.IsStreamingType && IsWriteNewCache )
			{
				//ストリーミング再生かつ、キャッシュに書き込む必要がある場合は、まだリソースを読まない
				//書き込み後の再読み込みを発行する
				isLoadRetry = true;
			}
			else
			{
				switch (FileType)
				{
					case AssetFileType.Text:		//テキスト
						text = www.text;
						break;
					case AssetFileType.Bytes:		//バイナリ
						bytes = www.bytes;
						break;
					case AssetFileType.Texture:		//テクスチャ
						if (FileInfo.IsTextureMipmap)
						{
							//サイズとTextureFormatはLoadImage後無視される。ミップマップの反映のみ
							texture = new Texture2D(1, 1, TextureFormat.ARGB32, FileInfo.IsTextureMipmap);
							texture.LoadImage(www.bytes);
							texture.Apply(false,false);
						}
						else
						{
							texture = www.texture;
						}
						texture.name = FileInfo.FilePath;
						texture.wrapMode = TextureWrapMode.Clamp;
						break;
					case AssetFileType.Sound:				//サウンド
						if (sound != null)
						{
							//ロードできていないストリーミング用のサウンドを消す
							UnityEngine.Object.Destroy(sound);
						}
						//非ストリーミングでオンメモリでロード
						sound = www.GetAudioClip(FileInfo.IsAudio3D, false, FileInfo.AudioType);
						sound.name = FileInfo.FilePath;
						if (!WrapperUnityVersion.IsReadyPlayAudioClip(sound))
						{
							Debug.LogError(sound.name + ":" + sound.loadState);
						}
						break;
					case AssetFileType.Csv:			//CSV
						csv = new StringGrid(this.FileName, FileInfo.IsTsv ? CsvType.Tsv : CsvType.Csv, www.text);
						break;
					case AssetFileType.UnityObject:	//UnityObject
						Debug.LogError("AssetBundle not support load from normal WWW");
						break;
					default:
						break;
				}
			}
		}

		//ロード処理（暗号化済みファイルから）
		void LoadWWWCriptFile(WWW www)
		{
			byte[] readBytes = www.bytes;
			switch (FileType)
			{
				case AssetFileType.Text:		//テキスト
					text = System.Text.Encoding.UTF8.GetString(fileIO.Decode(readBytes));
					break;
				case AssetFileType.Bytes:		//バイナリ
					bytes = fileIO.Decode(readBytes);
					break;
				case AssetFileType.Texture:	//テクスチャ
					fileIO.DecodeNoCompress(readBytes);			//圧縮なしでデコード
					//サイズとTextureFormatはLoadImage後無視される。ミップマップの反映のみ
					texture = new Texture2D(1, 1, TextureFormat.ARGB32, FileInfo.IsTextureMipmap);
					if (texture.LoadImage(readBytes))
					{
						texture.name = FileInfo.FilePath;
						texture.wrapMode = TextureWrapMode.Clamp;
						texture.Apply(false, false);
					}
					else
					{
						Debug.LogError(" Filed load image " + FileInfo.FilePath);
					}

					break;
				case AssetFileType.Sound:		//サウンド
					fileIO.DecodeNoCompress(readBytes);			//圧縮なしでデコード
					sound = FileIOManagerBase.ReadAudioFromMem(this.FileName, readBytes);
					sound.name = FileInfo.FilePath;
					break;
				case AssetFileType.Csv:			//CSV
					csv = new StringGrid(this.FileName, FileInfo.IsTsv ? CsvType.Tsv : CsvType.Csv, System.Text.Encoding.UTF8.GetString(fileIO.Decode(readBytes)));
					break;
				case AssetFileType.UnityObject:
					Debug.LogError("AssetBundle not support load from utage cript cache file");
					break;
				default:
					break;
			}
		}

		//ロード処理（Resourcesから）
		void LoadResource(string loadPath)
		{
			loadPath = FilePathUtil.GetPathWithoutExtension(loadPath);
			TextAsset textAsset;
			switch (FileType)
			{
				case AssetFileType.Text:		//テキスト
					textAsset = Resources.Load(loadPath, typeof(TextAsset)) as TextAsset;
					if (null != textAsset)
					{
						text = textAsset.text;
						Resources.UnloadAsset(textAsset);
					}
					else
					{
						SetLoadError("LoadResource Error");
					}
					break;
				case AssetFileType.Bytes:		//バイナリ
					textAsset = Resources.Load(loadPath, typeof(TextAsset)) as TextAsset;
					if (null != textAsset)
					{
						bytes = textAsset.bytes;
						Resources.UnloadAsset(textAsset);
					}
					else
					{
						SetLoadError("LoadResource Error");
					}
					break;
				case AssetFileType.Texture:		//テクスチャ
					texture = Resources.Load(loadPath, typeof(Texture2D)) as Texture2D;
					if (null == texture)
					{
						SetLoadError("LoadResource Error");
					}
					break;
				case AssetFileType.Sound:		//サウンド
					sound = Resources.Load(loadPath, typeof(AudioClip)) as AudioClip;
					if (null == sound)
					{
						SetLoadError("LoadResource Error");
					}
					break;
				case AssetFileType.Csv:			//CSV
					textAsset = Resources.Load(loadPath, typeof(TextAsset)) as TextAsset;
					if (null != textAsset)
					{
						csv = new StringGrid(loadPath, FileInfo.IsTsv ? CsvType.Tsv : CsvType.Csv, textAsset.text);
						Resources.UnloadAsset(textAsset);
					}
					else
					{
						SetLoadError("LoadResource Error");
					}
					break;
				case AssetFileType.UnityObject:		//Unityオブジェクト（プレハブとか）
					unityObject = Resources.Load(loadPath);
					if (null == unityObject)
					{
						SetLoadError("LoadResource Error");
					}
					break;
				default:
					break;
			}
		}

		//ロードアセットバンドル（通常）
		void LoadAssetBundle(WWW www)
		{
			UnityEngine.Object[] assets;
			this.AssetBundle = www.assetBundle;
			if (AssetBundle == null)
			{
				Debug.LogError("Failed to load AssetBundle:" + this.FileInfo.FilePath);
				return;
			}
			else
			{
				assets = AssetBundle.LoadAllAssets();
				AssetBundle.Unload(false);
			}
			UnityEngine.Object asset = assets[0];

			switch (FileType)
			{
				case AssetFileType.Text:		//テキスト
				case AssetFileType.Bytes:		//バイナリ
					{
						TextAsset textAsset = asset as TextAsset;
						if (null != textAsset)
						{
							text = textAsset.text;
							bytes = textAsset.bytes;
						}
						else
						{
							Debug.LogError("FileType Error");
						}
						break;
					}
				case AssetFileType.Texture:		//テクスチャ
					texture = asset as Texture2D;
					if (null == texture)
					{
						Debug.LogError("FileType Error");
					}
					break;
				case AssetFileType.Sound:		//サウンド
					sound = asset as AudioClip;
					if (null == sound)
					{
						Debug.LogError("FileType Error");
					}
					break;
				case AssetFileType.Csv:			//CSV
					{
						TextAsset textAsset = asset as TextAsset;
						if (null != textAsset)
						{
							bool isTsv = (LoadFlags & AssetFileLoadFlags.Tsv) != AssetFileLoadFlags.None;
							csv = new StringGrid(FileName, isTsv ? CsvType.Tsv : CsvType.Csv, textAsset.text);
						}
						else
						{
							Debug.LogError("FileType Error");
						}
					}
					break;
				case AssetFileType.UnityObject:		//Unityオブジェクト（プレハブとか）
					unityObject = asset;
					break;
				default:
					break;
			}
		}


		/// <summary>
		/// リソースをアンロードして、メモリを解放(Resourcesでロードしたもの)
		/// </summary>
		void UnloadResources()
		{
			switch (FileType)
			{
				case AssetFileType.Text:		//テキスト
					text = null;
					break;
				case AssetFileType.Bytes:		//バイナリ
					bytes = null;
					break;
				case AssetFileType.Texture:		//テクスチャ
					Resources.UnloadAsset(texture);
					texture = null;
					break;
				case AssetFileType.Sound:		//サウンド
					Resources.UnloadAsset(sound);
					sound = null;
					break;
				case AssetFileType.Csv:		//CSV
					csv = null;
					break;
				case AssetFileType.UnityObject:		//Unityオブジェクト
//					Resources.UnloadAsset(unityObject);
					unityObject = null;
					break;
				default:
					break;
			}
		}


		/// <summary>
		/// リソースをアンロードして、メモリを解放(WWWでロードしたもの)
		/// </summary>
		void UnloadWWW()
		{
			switch (FileType)
			{
				case AssetFileType.Text:		//テキスト
					text = null;
					break;
				case AssetFileType.Bytes:		//バイナリ
					bytes = null;
					break;
				case AssetFileType.Texture:		//テクスチャ
					if (texture != null)
					{
						UnityEngine.Object.Destroy(texture);
						texture = null;
					}
					if (spriteTbl != null)
					{
						foreach (Sprite sprite in spriteTbl.Values)
						{
							UnityEngine.Object.Destroy(sprite);
						}
						spriteTbl.Clear();
					}
					break;
				case AssetFileType.Sound:		//サウンド
					if (sound != null)
					{
						UnityEngine.Object.Destroy(sound);
						sound = null;
					}
					break;
				case AssetFileType.Csv:		//CSV
					csv = null;
					break;
				case AssetFileType.UnityObject:		//Unityオブジェクト（AssetBundle）
					Debug.LogError("AssetBundle not support UnloadWWW");
					unityObject = null;
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// アセットバンドルのアンロード
		/// </summary>
		void UnloadAssetBundle()
		{
			switch (FileType)
			{
				case AssetFileType.Text:		//テキスト
					text = null;
					break;
				case AssetFileType.Bytes:		//バイナリ
					bytes = null;
					break;
				case AssetFileType.Texture:		//テクスチャ
					texture = null;
					if (spriteTbl != null)
					{
						foreach (Sprite sprite in spriteTbl.Values)
						{
							UnityEngine.Object.Destroy(sprite);
						}
						spriteTbl.Clear();
					}
					break;
				case AssetFileType.Sound:		//サウンド
					sound = null;
					break;
				case AssetFileType.Csv:		//CSV
					csv = null;
					break;
				case AssetFileType.UnityObject:		//Unityオブジェクト（AssetBundle）
					unityObject = null;
					break;
				default:
					break;
			}
			if (FileInfo.IsAssetBundle)
			{
				try
				{
					//アセットバンドルのアンロード
					//					AssetBundle.Unload(true);
				}
				catch (Exception e)
				{
					Debug.LogError(e.Message);
				}
			}
		}

		
		//メモリサイズを計算
		void InitMemsSize()
		{
			switch (FileType)
			{
				case AssetFileType.Text:		//テキスト
					memSize = text.Length * 2;
					break;
				case AssetFileType.Bytes:		//バイナリ
					memSize = bytes.Length;
					break;
				case AssetFileType.Texture:		//テクスチャ
					memSize = Mathf.NextPowerOfTwo(texture.width) * Mathf.NextPowerOfTwo(texture.height)*4;
					break;
				case AssetFileType.Sound:		//サウンド
					if (streamingStatus == LOAD_STREAMING_STAUS.READY)
					{
						memSize = 1024 * 128;	//適当なある程度の値
					}
					else
					{
						memSize = sound.samples * sound.channels * 4;
					}
					break;
				case AssetFileType.Csv:			//CSV
					memSize = csv.TextLength * 2;
					break;
				case AssetFileType.UnityObject:	//Unityオブジェクト（プレハブとか）
					memSize = 1024*1024;		//ファイルサイズ不明なので適当なある程度の値					
					break;
				default:
					break;
			}
		}



		/// <summary>
		/// ロードの完了処理
		/// </summary>
		public void LoadComplete()
		{
			cacheWriteBytes = null;
			if (IsDownloadOnly)
			{
				status =  FileInfo.IsAssetBundle ?  STAUS.LOAD_END : STAUS.LOAD_WAIT;
			}
			else
			{
				//			status = STAUS.LOAD_END;
				status = STAUS.USING;
				//メモリサイズを計算
				if (!IsLoadRetry)
				{
					InitMemsSize();
					if (OnLoadComplete != null)
					{
						OnLoadComplete(this);
					}
				}
				streamingStatus = LOAD_STREAMING_STAUS.LOADEND;
			}
		}

		/// <summary>
		/// そのオブジェクトで使用する（参照を設定する）
		/// </summary>
		/// <param name="referenceObj">ファイルを参照するオブジェクト</param>
		public void Use(System.Object referenceObj)
		{
			if (null != referenceObj)
			{
				referenceSet.Add(referenceObj);
			}
		}

		/// <summary>
		/// そのオブジェクトから未使用にする（参照を解放する）
		/// </summary>
		/// <param name="referenceObj">ファイルの参照を解除するオブジェクト</param>
		public void Unuse(System.Object referenceObj)
		{
			if (null != referenceObj)
			{
				referenceSet.Remove(referenceObj);
			}
		}

		/// <summary>
		/// 参照用コンポーネントの追加
		/// </summary>
		/// <param name="go">参照コンポーネントを追加するGameObject</param>
		public void AddReferenceComponet(GameObject go)
		{
			AssetFileReference fileReference = go.AddComponent<AssetFileReference>();
			fileReference.Init(this);
		}


		bool IsReadyToUse()
		{
			return (status == STAUS.USING);
		}

		void ErrorCheckReadyToUse()
		{
			if (!IsReadyToUse())
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.FileIsNotReady));
			}
		}

		/// <summary>
		/// 使っていないか（アンロード可能か）チェック
		/// </summary>
		/// <returns>使っていないならtrue。まだ使っているならfalse</returns>
		public bool CheckUnuse()
		{

			if (referenceSet.RemoveWhere(s => s == null) > 0)
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.FileReferecedIsNull));
			}
			if (status == STAUS.USING)
			{
				if (referenceSet.Count <= 0)
				{
					status = STAUS.UNUSED;
					unusedSortID = sCommonUnusedSortID;
					++sCommonUnusedSortID;
				}
			}
			else if (status == STAUS.UNUSED)
			{
				if (referenceSet.Count > 0)
				{
					status = STAUS.USING;
					unusedSortID = 0;
					Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.MemoryLeak));
				}
			}
			return (status == STAUS.UNUSED);
		}

		/// <summary>
		/// リソースをアンロードして、メモリを解放
		/// </summary>
		public void Unload()
		{
			if (FileInfo.StrageType == AssetFileStrageType.Resources)
			{
				UnloadResources();
			}
			else
			{
				if( FileInfo.IsAssetBundle )
				{
					UnloadAssetBundle();
				}
				else
				{
					UnloadWWW();
				}
			}
			foreach (AssetFile subFile in SubFiles.Values)
			{
				subFile.Unuse(this);
			}
			SubFiles.Clear();
			memSize = 0;
			Priority = AssetFileLoadPriority.DownloadOnly;
			status = STAUS.LOAD_WAIT;
		}
		/// <summary>
		/// 派生ファイルを追加してロードを開始させる
		/// </summary>
		public void LoadAndAddSubFile(string path)
		{
			if(!SubFiles.ContainsKey(path))
			{
				AssetFile file = AssetFileManager.Load(path, this.Version, this);
				SubFiles.Add(path, file);
			}
		}
	}
}
#endif

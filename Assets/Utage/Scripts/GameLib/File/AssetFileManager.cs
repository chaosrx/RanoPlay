//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// ファイル管理
	/// </summary>
	[AddComponentMenu("Utage/Lib/File/AssetFileManager")]
	[RequireComponent(typeof(StaticAssetManager))]
	[RequireComponent(typeof(ConvertFileListManager))]
	public partial class AssetFileManager : MonoBehaviour
	{
		public FileIOManager FileIOManger
		{
			get { return fileIOManger ?? ( fileIOManger = this.GetComponent<FileIOManager>()); }
			set { fileIOManger = value; }
		}
		[SerializeField]
		FileIOManager fileIOManger;


		[SerializeField]
		float timeOutDownload = 10;					//タイムアウト時間
		[SerializeField]
		int autoRetryCountOnDonwloadError = 5;		//ダウンロードエラー時に、自動でリトライする回数

		[SerializeField]
		int loadFileMax = 5;					//同時にロードするファイルの最大数
		[SerializeField]
		float maxMemSizeMB = 64;				//最大メモリサイズ
		[SerializeField]
		float optimizedMemSizeMB = 32;			//最適化後メモリサイズ

		//キャッシュファイルを使用しない
		//起動後にチェックのオン、オフをしてはいけない
		public bool DontUseCache { get { return this.dontUseCache; } set { this.dontUseCache = value; } }
		[SerializeField]
		bool dontUseCache = false;
		[SerializeField]
		internal string cacheDirectoryName = "Cache";	//DLファイルをキャッシュするディレクトリ名
		[SerializeField]
		internal string cacheTblFileName = "CacheTbl";	//キャッシュしたファイル名一覧のファイル名

		//アンロード時の処理タイプ
		enum UnloadType
		{
			None,						//特に何もしない
			UnloadUnusedAsset,			//アセットバンドルがある場合はUnloadUnusedAsset
			UnloadUnusedAssetAlways,	//常にUnloadUnusedAsset
		};
		[SerializeField]
		UnloadType unloadType = UnloadType.UnloadUnusedAsset;

		[SerializeField]
		internal bool isOutPutDebugLog = false;								//ダウンロードのログをコンソールに出力する
		[SerializeField]
		internal bool isDebugCacheFileName = false;							//キャッシュファイルパスをデバッグモードにする（隠蔽せずに公開する）
		[SerializeField]
		internal bool isDebugBootDeleteChacheTextAndBinary = false;					//起動時に、テキストやバイナリのキャッシュを削除する
		[SerializeField]
		internal bool isDebugBootDeleteChacheAll = false;							//起動時に、キャッシュファイルを全て消す

		public AssetFileManagerSettings Settings
		{
			get { return settings; }
			set{ settings = value; }
		}
		[SerializeField]
		AssetFileManagerSettings settings;

		int totalMemSize = 0;											//ロード済みファイル（使用中とプール中両方）の総メモリサイズ
		int totalMemSizeUsing = 0;										//使用中ファイルの総メモリサイズ
		List<AssetFile> loadingFileList = new List<AssetFile>();		//ロード中ファイルリスト
		List<AssetFile> loadWaitFileList = new List<AssetFile>();		//ロード待ちファイルリスト
		List<AssetFile> usingFileList = new List<AssetFile>();			//使用中ファイルリスト
		List<AssetFile> unuesdFileList = new List<AssetFile>();			//未使用（ロード済みでオンメモリ）ファイルリスト
		Dictionary<string, AssetFile> fileTbl= new Dictionary<string, AssetFile>();	//管理中のファイルリスト

		AssetFileUtageManager AssetFileUtageManager
		{
			get
			{
				if (assetFileUtageManager == null)
				{
					assetFileUtageManager = new AssetFileUtageManager(this);
				}
				return assetFileUtageManager;
			}
		}
		AssetFileUtageManager assetFileUtageManager;


		Action<AssetFile> callbackError;

		public Action<AssetFile> CallbackError
		{
			get { return callbackError ??( callbackError = CallbackFileLoadError); }
			set { callbackError = value; }
		}

		AssetFile errorFile;		// ロードエラーしたファイル

		// ロードエラー時のデフォルトコールバック
		void CallbackFileLoadError(AssetFile file)
		{
			errorFile = file;
			string errorMsg = file.LoadErrorMsg + "\n" + file.FileName;
			Debug.LogError(errorMsg);

			if (SystemUi.GetInstance() != null)
			{
				//リロードを促すダイアログを表示
				SystemUi.GetInstance().OpenDialog1Button(
					errorMsg, LanguageSystemText.LocalizeText(SystemText.Retry),
					OnCloseFileLoadErrorDialog);
				this.gameObject.SetActive(false);
			}
			else
			{
				ReloadFileSub(errorFile);
			}
		}

		// ロードエラーダイアログが閉じられたとき
		void OnCloseFileLoadErrorDialog()
		{
			this.gameObject.SetActive(true);
			ReloadFileSub(errorFile);
		}



		void Awake()
		{
			if (null == instance)
			{
				instance = this;
			}
			BootInit();
		}

		void BootInit()
		{
			AssetFileUtageManager.BootInit(Settings, ConvertFileListManager);
		}


		/// <summary>
		/// ファイルリストをロードして初期化する
		/// </summary>
		void LoadInitFileListSub(List<string> pathList, int version )
		{
			ConvertFileListManager.LoadStart(pathList, version, OnLoadCompleteFileList);
		}

		void OnLoadCompleteFileList()
		{
			AssetFileUtageManager.SetConvertFileInfo(ConvertFileListManager);
		}

		void Update()
		{
			MemoryOptimize();
		}

		void LastUpdate()
		{
			RefleshUnuseList();
		}

		void OnDestroy()
		{
			ClearUnuseFile();
		}

		// 管理ファイルを追加
		AssetFile AddSub(string path, StringGridRow rowData = null )
		{
			AssetFile file;
			//管理テーブルにあるなら、そこから
			if (!fileTbl.TryGetValue(path, out file))
			{
				if (path.Contains(" "))
				{
					Debug.LogWarning("[" + path + "] contains white space");
				}

				//staticなアセットにあるなら、そこから
				file = StaticAssetManager.FindAssetFile(path, this.Settings, rowData );
				if (file == null)
				{
					//カスタムロードなアセットにあるなら、そこから
					file = CustomLoadManager.Find(path, this.Settings, rowData);
					if (file == null)
					{
						//宴形式の通常ファイルロード
						file = AssetFileUtageManager.CreateFile(path, Settings, rowData, ConvertFileListManager);
					}
				}
				fileTbl.Add(path, file);
			}
			return file;
		}

		//カスタムロードのマネージャー
		CustomLoadManager CustomLoadManager
		{
			get
			{
				if (customLoadManager == null)
				{
					customLoadManager = UtageToolKit.GetComponentCreateIfMissing<CustomLoadManager>(this.gameObject);
				}
				return customLoadManager;
			}
		}
		CustomLoadManager customLoadManager;

		//動的にロードしないリソースのリスト
		StaticAssetManager StaticAssetManager
		{
			get
			{
				if( staticAssetManager == null )
				{
					staticAssetManager = GetComponent<StaticAssetManager>();
					if (staticAssetManager == null)
					{
						staticAssetManager = this.gameObject.AddComponent<StaticAssetManager>();
					}
				}
				return staticAssetManager;
			}
		}
		StaticAssetManager staticAssetManager;

		//コンバートファイル情報のマネージャー
		ConvertFileListManager ConvertFileListManager
		{
			get
			{
				if (convertFileListManager == null)
				{
					convertFileListManager = GetComponent<ConvertFileListManager>();
					if (convertFileListManager == null)
					{
						convertFileListManager = this.gameObject.AddComponent<ConvertFileListManager>();
					}
				}
				return convertFileListManager;
			}
		}
		ConvertFileListManager convertFileListManager;

		// ダウンロード
		void DownloadSub(AssetFile file)
		{
			if (file == null)
			{
				return;
			}
			if (file.NeedsToCache || file.OnLoadSubFiles !=null)
			{
				file.ReadyToLoad(AssetFileLoadPriority.DownloadOnly, null);
				AddLoadFile(file);
			}
		}

		// プリロード
		void PreloadSub(AssetFile file, System.Object referenceObj)
		{
			MoveToUseList(file);
			file.ReadyToLoad(AssetFileLoadPriority.Preload, referenceObj);
			AddLoadFile(file);
		}
		// ロード
		AssetFile LoadSub(AssetFile file, System.Object referenceObj)
		{
			MoveToUseList(file);
			file.ReadyToLoad(AssetFileLoadPriority.Default, referenceObj);
			AddLoadFile(file);
			return file;
		}
		//	ファイルのバックグランドロード
		AssetFile BackGroundLoadSub(AssetFile file, System.Object referenceObj)
		{
			MoveToUseList(file);
			file.ReadyToLoad(AssetFileLoadPriority.BackGround, referenceObj);
			AddLoadFile(file);
			return file;
		}

		//ロード待ちリストを追加
		void AddLoadFile(AssetFile file)
		{
			if (!file.IsLoadEndOriginal)
			{
				if (loadingFileList.Count < loadFileMax && !loadingFileList.Contains(file))
				{
					loadingFileList.Add(file);
					if (isOutPutDebugLog) Debug.Log("Load Start :" + file.FileName + " ver:" + file.Version + " cache:" + file.CacheVersion);
					StartCoroutine(CoLoadFile(file));
				}
				else if (!loadWaitFileList.Contains(file))
				{
					loadWaitFileList.Add(file);
				}
			}
			RefleshMemSize();
		}

		IEnumerator CoLoadFile(AssetFile file)
		{
			yield return StartCoroutine(file.CoLoadAsync(timeOutDownload));

			if (!file.IsLoadError)
			{
				//ロード終了処理
				file.LoadComplete();

				//再ロード必要
				if (file.IsLoadRetry)
				{
					StartCoroutine(CoLoadFile(file));
				}
				else
				{
					//ロード成功
					if (isOutPutDebugLog) Debug.Log("Load End :" + file.FileName + " ver:" + file.Version);
					loadingFileList.Remove(file);
					LoadNextFile();
					MemoryOptimize();
				}
			}
			else
			{
				//ロード失敗
				Debug.LogError("Load Failed :" + file.FileName + " ver:" + file.Version + "\n" + file.LoadErrorMsg);

				//リトライ
				if (file.CountLoadErrorRetry++ < autoRetryCountOnDonwloadError)
				{
					if (isOutPutDebugLog) Debug.Log( string.Format( "Load Retry({0}) :{1} ver:{2}", file.CountLoadErrorRetry, file.FileName,file.Version));
					StartCoroutine(CoLoadFile(file));
				}
				else
				{
					//ロード失敗処理
					file.LoadFailed(CallbackError, ReloadFileSub);
				}
			}
		}

		//ファイルリロード
		void ReloadFileSub(AssetFile file)
		{
			StartCoroutine(CoLoadFile(file));
		}



		void LoadNextFile()
		{
			AssetFile next = null;
			foreach (AssetFile file in loadWaitFileList)
			{
				if (next == null)
				{
					next = file;
				}
				else
				{
					if (file.Priority < next.Priority)
					{
						next = file;
					}
				}
			}
			if (next != null)
			{
				loadWaitFileList.Remove(next);
				AddLoadFile(next);
			}
		}

		//システムメモリにキャッシュされてるファイルをいったんアンロードして、メモリを解放
		void UnloadMemChaceFile(int unloadSize)
		{
			//未使用ファイルの消去優先順でソート
			unuesdFileList.Sort((a, b) => a.UnusedSortID - b.UnusedSortID);

			//指定サイズだけアンロード
			int count = 0;
			int size = 0;
			int assetBundleCount = 0;
			foreach (AssetFile file in unuesdFileList)
			{
				if (isOutPutDebugLog) Debug.Log("Unload " + file.FileName + " ver:" + file.Version);
				if (file.FileType == AssetFileType.UnityObject)
				{
					++assetBundleCount;
				}
				file.Unload();
				++count;
				size += file.MemSize;
				if (size >= unloadSize)
				{
					break;
				}
			}
			if (unloadType == UnloadType.UnloadUnusedAssetAlways || (assetBundleCount > 0 && unloadType == UnloadType.UnloadUnusedAsset) )
			{
				if (isOutPutDebugLog) Debug.Log("UnloadUnusedAssets");
				Resources.UnloadUnusedAssets();
			}
			unuesdFileList.RemoveRange(0, count);
		}

		//未使用ファイルをクリア
		void ClearUnuseFile()
		{
			RefleshUnuseList();
			UnloadMemChaceFile(Int32.MaxValue);
			RefleshMemSize();
		}

		//ファイルを使用中リストに
		void MoveToUseList(AssetFile file)
		{
			if (!usingFileList.Contains(file))
			{
				usingFileList.Add(file);
			}
			if (unuesdFileList.Contains(file))
			{
				unuesdFileList.Remove(file);
			}
			RefleshMemSize();
		}
		//ファイルの使用・未使用リストを更新
		List<AssetFile> tmpList = new List<AssetFile>();
		void RefleshUnuseList()
		{
			tmpList.Clear();
			foreach (AssetFile file in usingFileList)
			{
				if (file.CheckUnuse())
				{
					tmpList.Add(file);
				}
			}
			if (tmpList.Count > 0)
			{
				foreach (AssetFile file in tmpList)
				{
					usingFileList.Remove(file);
					unuesdFileList.Add(file);
				}
				tmpList.Clear();
				RefleshMemSize();
			}
		}

		//確保メモリ数を再計算
		void RefleshMemSize()
		{
			totalMemSize = 0;
			totalMemSizeUsing = 0;
			foreach (AssetFile file in usingFileList)
			{
				totalMemSizeUsing += file.MemSize;
				totalMemSize += file.MemSize;
			}
			foreach (AssetFile file in unuesdFileList)
			{
				totalMemSize += file.MemSize;
			}
		}

		//メモリの最適化
		void MemoryOptimize()
		{
			RefleshUnuseList();
			//確保メモリが上限を超えていたら、キャッシュメモリを消去
			if (totalMemSize > MaxMemSize)
			{
				UnloadMemChaceFile(totalMemSize - OptimizedMemSize);
				RefleshMemSize();
			}
		}

		//ロード終了チェック
		bool IsLoadEnd( AssetFileLoadPriority priority )
		{
			foreach (var file in loadingFileList)
			{
				if( file.Priority <= priority )
				{
					if (!file.IsLoadEnd) return false;
				}
			}
			foreach (var file in loadWaitFileList)
			{
				if (file.Priority <= priority)
				{
					if (!file.IsLoadEnd) return false;
				}
			}
			return true;
		}

		//ロード中のファイル数
		int CountLoading(AssetFileLoadPriority priority)
		{
			int count = 0;
			foreach (var file in loadingFileList)
			{
				if (file.Priority <= priority)
				{
					if (!file.IsLoadEnd)
					{
						++count;
					}
				}
			}
			foreach (var file in loadWaitFileList)
			{
				if (file.Priority <= priority)
				{
					if (!file.IsLoadEnd)
					{
						++count;
					}
				}
			}
			return count;
		}
	}
}
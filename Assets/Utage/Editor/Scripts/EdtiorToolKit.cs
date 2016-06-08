//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Utage
{

	/// <summary>
	/// 便利クラス
	/// </summary>
	public class UtageEditorToolKit
	{
		public static void BeginGroup(string label)
		{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Space(4f);
			GruopLabel(label);
			GUILayout.Space(4f);
			GUILayout.BeginHorizontal();
			GUILayout.Space(12f);
			EditorGUILayout.BeginVertical();
		}
		public static void BeginGroup(string label, SerializedObject serializedObject,  string disablePropertyName )
		{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Space(4f);
			GUILayout.BeginHorizontal();
			GruopLabel(label);
			PropertyField (serializedObject, disablePropertyName);
			GUILayout.EndHorizontal();
			GUILayout.Space(4f);
			GUILayout.BeginHorizontal();
			GUILayout.Space(12f);
			EditorGUILayout.BeginVertical();
		}

		public static void EndGroup()
		{
			EditorGUILayout.EndVertical();
			GUILayout.Space(4f);
			GUILayout.EndHorizontal();
			GUILayout.Space(4f);
			EditorGUILayout.EndVertical();
		}

		public static void GruopLabel(string label)
		{
			BoldLabel(label);
		}

		//太字のラベル表示
		public static void BoldLabel(string label, params GUILayoutOption[] options)
		{
			GUIStyle style = new GUIStyle("label");
			style.richText = true;
			GUILayout.Label("<b>" + label + "</b>", style, options);
		}

		public static void PropertyField(SerializedObject serializedObject, string propertyPath, string label, params GUILayoutOption[] options)
		{
			SerializedProperty property = serializedObject.FindProperty(propertyPath);
			if (property == null)
			{
				Debug.LogError(propertyPath + " is Not Found");
			}
			else
			{
				EditorGUILayout.PropertyField(property, new GUIContent(label), options);
			}
		}

		public static void PropertyField(SerializedObject serializedObject, string propertyPath, params GUILayoutOption[] options)
		{
			SerializedProperty property = serializedObject.FindProperty(propertyPath);
			if (property == null)
			{
				Debug.LogError(propertyPath + " is Not Found");
			}
			else
			{
				EditorGUILayout.PropertyField(property, GUIContent.none, options );
			}
		}

		public static void PropertyFieldArray(SerializedObject serializedObject, string propertyPath, string label, params GUILayoutOption[] options)
		{
			SerializedProperty property = serializedObject.FindProperty(propertyPath);
			if (property == null)
			{
				Debug.LogError(propertyPath + " is Not Found");
			}
			else
			{
				EditorGUILayout.PropertyField(property, new GUIContent(label), true, options);
			}
		}

		public static T PrefabField<T>(string title, T currentPrefab) where T : Component
		{
			GameObject asset = (currentPrefab != null) ? currentPrefab.gameObject : null;
			EditorGUILayout.BeginHorizontal();

			GUILayout.Label(title);
			asset = EditorGUILayout.ObjectField(asset, typeof(GameObject), false) as GameObject;

			EditorGUILayout.EndHorizontal();

			T prefabComponent = (asset != null) ? asset.GetComponent<T>() : null;
			return prefabComponent;
		}

		//折りたたみ機能つきの描画
		public static void FoldoutGroup(ref bool foldOunt, string name, System.Action OnGui)
		{
			if (foldOunt = EditorGUILayout.Foldout(foldOunt, name))
			{
				EditorGUI.indentLevel++;
				OnGui();
				EditorGUI.indentLevel--;
			}
		}


		//インポート後のアセット（ScriptableObject）を取得。
		//既にあったらロード。なかったらCreate
		static public T GetImportedAssetCreateIfMissing<T>(string path) where T : ScriptableObject
		{
			var asset = AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
			if (asset == null)
			{
				asset = ScriptableObject.CreateInstance<T>() as T;
				AssetDatabase.CreateAsset(asset, path);
			}
			return asset;
		}

		//インポート後のアセット（Object）を取得。
		//既にあったらロード。なかったらCreate
		static public T GetImportedAssetObjectCreateIfMissing<T>(string path) where T : Object, new()
		{
			var asset = AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
			if (asset == null)
			{
				asset = new T();
				AssetDatabase.CreateAsset(asset, path);
			}
			return asset;
		}


		static public T CreateNewUniqueAsset<T>() where T : ScriptableObject
		{
			string path = GetSelectedDirectory();
			string typeName = typeof(T).ToString();

			//ネームスペース対策
			if( typeName.Contains(".") )
			{
				int index = typeName.LastIndexOf('.') + 1;
				typeName = typeName.Substring( index, typeName.Length -index );
			}
			path += "/New " + typeName + ".asset";
			return CreateNewUniqueAsset<T>(path);
		}

		static public T CreateNewUniqueAsset<T>(string path) where T : ScriptableObject
		{
			path = AssetDatabase.GenerateUniqueAssetPath(path);
			T asset = ScriptableObject.CreateInstance<T>() as T;
			AssetDatabase.CreateAsset(asset, path);
			EditorUtility.SetDirty(asset);
			return asset;
		}
/*
		static public void CreateDirectory(string path)
		{
			string dir = System.IO.Path.GetDirectoryName(path);
			if (System.IO.Directory.Exists(dir)) return;

			CreateDirectorySub(dir);
		}

		static void CreateDirectorySub(string dir)
		{
			if (System.IO.Directory.Exists(dir)) return;

			System.IO.DirectoryInfo paretInfo = System.IO.Directory.GetParent(dir);
			CreateDirectorySub(paretInfo.FullName);
			string parentFolder = FileUtil.GetProjectRelativePath(paretInfo.FullName);
			parentFolder = "Assets" + ( string.IsNullOrEmpty(parentFolder) ?  "" : "/" + parentFolder );
			string newFolderName = System.IO.Path.GetFileName(dir);
			AssetDatabase.CreateFolder(parentFolder,newFolderName);
		}
		static public string GenerateUniqueDirectoryName(string dir)
		{
			string path = Application.dataPath + "/" + dir;
			path = GenerateUniqueDirectoryPathSub(path);
			return path.Replace(Application.dataPath+"/","");
		}

		static string GenerateUniqueDirectoryPathSub(string path)
		{
			string uniquePath = path;
			int count = 0;
			while (System.IO.Directory.Exists(uniquePath))
			{
				uniquePath = path + (++count);
			}
			return uniquePath;
		}
*/

		//選択中のディレクトリ名
		static public string GetSelectedDirectory()
		{
			string path = "";
			foreach (var obj in Selection.objects)
			{
				path = AssetDatabase.GetAssetPath(obj);
				if (!string.IsNullOrEmpty(path) && !System.IO.Directory.Exists(path))
				{
					path = System.IO.Path.GetDirectoryName(path);
				}

				break;
			}

			if (string.IsNullOrEmpty(path))
			{
				return "Assets";
			}

			return path;
		}

		/// <summary>
		/// アセットリストからファイルパスのリストを取得
		/// </summary>
		/// <param name="assets">アセットのリスト</param>
		/// <returns>ファイルパスのリスト</returns>
		static public List<string> AssetsToPathList( List<Object> assets )
		{
			List<string> pathList = new List<string>();
			foreach (var asset in assets)
			{
				pathList.Add(AssetDatabase.GetAssetPath(asset));
			}
			return pathList;
		}

		/// <summary>
		/// アセットの拡張子をチェック
		/// </summary>
		/// <param name="asset">アセット</param>
		/// <param name="extensions">チェックする拡張子</param>
		/// <returns>指定の拡張子があればtrue。なければfalse</returns>
		static public bool CheckAssetExtensiton(Object asset, params string[] extensions )
		{
			string path = AssetDatabase.GetAssetPath(asset);
			string ext = System.IO.Path.GetExtension(path).ToLower();
			foreach( var extexsion in extensions )
			{
				if( ext == extexsion.ToLower() )
				{
					return true;
				}
			}
			return false;
		}

		static public T LoadAssetAtPath<T>(string path) where T : Object
		{
			return AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
		}

		//AssetDatabaseなどで使うAssets以下の相対パスを、System.IO系でも使えるフルパスに変換する
		static public string AssetPathToSystemIOFullPath(string assetPath)
		{
			return Application.dataPath.Remove( Application.dataPath.LastIndexOf("Assets")) + assetPath;
		}

		//System.IO系などで使うフルパスを、AssetDatabaseなどで使うAssets以下の相対パスに直す。
		static public string SystemIOFullPathToAssetPath(string fullPath)
		{
			return FileUtil.GetProjectRelativePath(fullPath.Replace(@"\", @"/"));
		}

		//すべてのウィンドウを取得
		public static List<EditorWindow> GetAllEditorWindow()
		{
			List<EditorWindow> allWindows = new List<EditorWindow>();
			foreach (EditorWindow window in Resources.FindObjectsOfTypeAll(typeof(EditorWindow)) as EditorWindow[])
			{
//				Debug.Log( window.title );
				allWindows.Add(window);
			}
			return allWindows;
		}
		
		//シーン内のすべてのオブジェクトを取得
		public static List<GameObject> GetAllObjectsInScene()
		{
			List<GameObject> objectsInScene = new List<GameObject>();
			
			foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
			{
				if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave)
					continue;
       			
				//プレハブの排除
				string assetPath = AssetDatabase.GetAssetPath(go.transform.root.gameObject);
				if (!string.IsNullOrEmpty(assetPath))
					continue;
        
				objectsInScene.Add(go);
			}
			return objectsInScene;
		}
		
		
		//シーン内の指定の型のコンポーネントをすべて取得
		public static T[] FindComponentsAllInTheScene<T>() where T : Component
		{
			return FindComponents<T>(GetAllObjectsInScene());
		}
	
		//シーン内の指定の型のコンポーネントを取得
		public static T FindComponentAllInTheScene<T>() where T : Component
		{
			T[] components = FindComponentsAllInTheScene<T>();
			return components.Length <= 0 ? null : components[0];
		}

		//シーン内の指定の型のコンポーネントを取得
		public static T[] FindComponents<T>(List<GameObject> goes) where T : Component
		{
			List<T> components = new List<T>();
			foreach (GameObject go in goes)
			{
				if (go.transform.parent == null)
				{
					components.AddRange(go.GetComponentsInChildren<T>(true));
				}
			}
			return components.ToArray();
		}

		//全てのSerializedPropertyを取得
		static public List<SerializedProperty> GetAllSerializedProperties(SerializedObject obj)
		{
			List<SerializedProperty> serializedProperties = new List<SerializedProperty>();
			SerializedProperty it = obj.GetIterator();
			while (it.Next(true))
			{
				Debug.Log (it.name);
				serializedProperties.Add(it);
			}
			return serializedProperties;
		}

		//シーン内の全てのコンポーネントのうち、指定のアセットを参照しているものを取得
		internal static List<Component> FindReferencesCopomnetsInScene(Object srcAsset)
		{
			List<Component> components = new List<Component>();
			foreach (Component component in FindComponentsAllInTheScene<Component>())
			{
				if (ContainsReferenceObject( new SerializedObject(component), srcAsset) )
				{
					components.Add(component);
				}
			}
			return components;
		}

		//プロジェクト内の全てのコンポーネント（プレハブとScriptableObject）のうち、指定のアセットを参照しているものを取得
		internal static List<Object> FindReferencesAssetsInProject(string dir, Object srcAsset)
		{
			List<Object> refereces = new List<Object>();

			List<string> pathList = UtageEditorToolKit.GetAllAssetPathList(dir);
			foreach (string assetpath in pathList)
			{
				if (Path.GetExtension(assetpath) == ".unity") continue;
				foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(assetpath))
				{
					if (PrefabUtility.GetPrefabType(obj) == PrefabType.Prefab)
					{
						//プレハブの場合
						GameObject go = obj as GameObject;
						if (go == null)
						{
							continue;
						}
						foreach (Component component in go.GetComponentsInChildren<Component>(true))
						{
							if (ContainsReferenceObject(new SerializedObject(component), srcAsset))
							{
								refereces.Add(component);
							}
						}
					}
					else if (UtageEditorToolKit.IsScriptableObject(obj))
					{
						//ScriptableObjectの場合
						if (ContainsReferenceObject(new SerializedObject(obj), srcAsset))
						{
							refereces.Add(obj);
						}
					}
				}
			}
			return refereces;
		}
		
		//指定のオブジェクトを参照しているものを取得
		static public bool ContainsReferenceObject(SerializedObject obj, UnityEngine.Object referenceObject)
		{
			SerializedProperty it = obj.GetIterator();
			while (it.Next(true))
			{
				if (it.propertyType == SerializedPropertyType.ObjectReference)
				{
					if (it.objectReferenceValue == referenceObject)
					{
						return true;
					}
				}
			}
			return false;
		}
		//全てのSerializedPropertyのobjectReferenceValueを入れ替える
		static public bool ReplaceSerializedProperties(SerializedObject obj, UnityEngine.Object srcObjet, UnityEngine.Object dstObjet)
		{
			bool isReplaced = false;
			SerializedProperty it = obj.GetIterator();
			while (it.Next(true))
			{
				if (it.propertyType == SerializedPropertyType.ObjectReference)
				{
					if (it.objectReferenceValue != null && it.objectReferenceValue == srcObjet)
					{
						it.objectReferenceValue = dstObjet;
						isReplaced = true;
					}
				}
			}
			if (isReplaced)
			{
				obj.ApplyModifiedProperties();
			}
			return isReplaced;
		}

		//シーン内の全コンポーネントから参照されているアセットを全て入れ替える
		static public bool ReplaceSerializedPropertiesAllComponentsInSene(Dictionary<Object, Object> replaceAssetPair)
		{
			bool isReplaced = false;
			foreach (GameObject go in GetAllObjectsInScene())
			{
				isReplaced |= ReplaceSerializedPropertiesAllComponents(go, replaceAssetPair);
			}
			return isReplaced;
		}

		//オブジェクトの全コンポーネントから参照されているアセットを全て入れ替える
		static public bool ReplaceSerializedPropertiesAllComponents( GameObject go, Dictionary<Object, Object> replaceAssetPair)
		{
			bool isReplaced = false;
			foreach (Component component in go.GetComponents<Component>())
			{
				if (component as Transform) continue;
				isReplaced |= ReplaceSerializedProperties(new SerializedObject(component), replaceAssetPair);
			}
			return isReplaced;
		}

		//全てのSerializedPropertyのobjectReferenceValueを入れ替える
		static public bool ReplaceSerializedProperties(SerializedObject obj, Dictionary<Object, Object> replaceAssetPair)
		{
			bool isReplaced = false;
			SerializedProperty it = obj.GetIterator();
			while (it.Next(true))
			{
				if (it.propertyType == SerializedPropertyType.ObjectReference)
				{
					Object reference = it.objectReferenceValue;
					if (reference != null)
					{
						Object asset;
						if (replaceAssetPair.TryGetValue(reference, out asset))
						{
							it.objectReferenceValue = asset;
							isReplaced = true;
						}
					}
				}
			}
			if (isReplaced)
			{
				obj.ApplyModifiedProperties();
			}
			return isReplaced;
		}


		//ScriptableObjectか
		internal static bool IsScriptableObject(Object obj)
		{
			return (obj as ScriptableObject) != null;			
		}

		//指定ディレクトリ以下の全てのアセットをロード
		internal static List<string> GetAllAssetPathList(string dir)
		{
			List<string> pathList = new List<string>();
			foreach (string filePath in System.IO.Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
			{
				if (Path.GetExtension(filePath) != ".meta")
				{
					pathList.Add(SystemIOFullPathToAssetPath(filePath));
				}
			}
			return pathList;
		}

		internal static bool TryAddLayerName(string layerName)
		{
			if (string.IsNullOrEmpty(layerName)) return false;

			SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
 			SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers == null || !layers.isArray)
            {
                Debug.LogError("Layers is not found. Maybe Unity version error");
				return false;
            }

			//すでにレイヤー名があったら何もしない
			for (int i = 0; i < layers.arraySize; i++)
			{
				SerializedProperty it = layers.GetArrayElementAtIndex(i);
				if (it.stringValue == layerName)
				{
					return false;
				}
			}

			//ユーザー設定のレイヤー名が空欄だったら、そこに追加
			const int userLayerIndex = 8;
			for (int i = userLayerIndex; i < layers.arraySize; i++)
			{
				SerializedProperty it = layers.GetArrayElementAtIndex(i);
				if (string.IsNullOrEmpty(it.stringValue))
				{
					it.stringValue = layerName;
					break;
				}
			}
            tagManager.ApplyModifiedProperties();
			return true;
        }

		static public Font LoadArialFont()
		{
			return Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
		}
	}
}
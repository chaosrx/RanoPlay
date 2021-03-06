//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Utage
{
	/// <summary>
	/// [FilePathDialogAttribute]アトリビュートのあるプロパティ描画
	/// ファイルのパス文字列を、選択ダイアログ用のボタンつきで表示する
	/// </summary>
	[CustomPropertyDrawer(typeof(PathDialogAttribute))]
	public class PathDialogDrawer : PropertyDrawer
	{
		PathDialogAttribute Attribute { get { return (this.attribute as PathDialogAttribute); } }

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(property, new GUIContent(property.displayName));
			if (GUILayout.Button("Select", GUILayout.Width(100)))
			{
				string path = property.stringValue;
				string dir = string.IsNullOrEmpty(path) ? "" : Path.GetDirectoryName(path);
				string name = string.IsNullOrEmpty(path) ? "" : Path.GetFileName(path);
				switch( Attribute.Type )
				{
					case PathDialogAttribute.DialogType.Directory:
						path = EditorUtility.OpenFolderPanel("Select Directory", dir, name);
						break;
					case PathDialogAttribute.DialogType.File:
						path = EditorUtility.OpenFilePanel("Select File", dir, Attribute.Extention);
						break;
					default:
						Debug.LogError("Unkonwn Type");
						break;
				}
				if (!string.IsNullOrEmpty(path))
				{
					property.stringValue = path;
				}
			}
			EditorGUILayout.EndHorizontal();
			//			property.stringValue = EditorGUI.MaskField(position, label, property.stringValue, property.enumNames);
		}
	}
}

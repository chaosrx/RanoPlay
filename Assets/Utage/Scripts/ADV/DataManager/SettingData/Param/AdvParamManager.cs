//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System; 
using System.Text.RegularExpressions; 
using UnityEngine;

namespace Utage
{
	[System.Serializable]
	public class AdvParamManager : AdvSettingDataBase
	{

		public const string SheetNameParam = "Param";

		const string SheetNamePattern = @"(.+)\{\}";
		static readonly Regex SheetNameRegix = new Regex(SheetNamePattern, RegexOptions.IgnorePatternWhitespace);
		//パラメーターシート名か
		public static bool IsParamSheetName(string sheetName)
		{
			if (sheetName == SheetNameParam) return true;
			Match match = SheetNameRegix.Match(sheetName);
			return match.Success;
		}

		const string KeyPattern = @"(.+)\[(.+)\]\.(.+)";
		static readonly Regex KeyRegix = new Regex(KeyPattern, RegexOptions.IgnorePatternWhitespace);
		internal static bool ParseKey(string key, out string structName, out string indexKey, out string valueKey)
		{
			structName = indexKey = valueKey = "";
			if (!key.Contains("[")) return false;
			Match match = KeyRegix.Match(key);
			if (!match.Success) return false;

			structName = match.Groups[1].Value + "{}";
			indexKey = match.Groups[2].Value;
			valueKey = match.Groups[3].Value;
			return true;
		}
		public bool IsInit { get; protected set; }

		public Dictionary<string,AdvParamStructTbl> StructTbl{ get{ return structTbl; } }
		Dictionary<string,AdvParamStructTbl> structTbl = new Dictionary<string,AdvParamStructTbl>(); 

		//システム系のパラメーターが変化したか（セーブに使う）
		public bool HasChangedSystemParam { get; set; }

		/// <summary>
		/// キーからパラメータを取得
		/// </summary>
		bool TryGetParamData(string key, out AdvParamData data)
		{
			data = null;
			string structName,indexKey,valueKey;
			if (!ParseKey(key, out structName, out indexKey, out valueKey))
			{
				return GetDefault().Tbl.TryGetValue(key,out data);
			}
			else
			{
				if(!StructTbl.ContainsKey(structName)) return false;

				if(!StructTbl[structName].Tbl.ContainsKey(indexKey)) return false;

				return StructTbl[structName].Tbl[indexKey].Tbl.TryGetValue(valueKey,out data);
			}
		}

		public AdvParamStruct GetDefault()
		{
			if (!StructTbl.ContainsKey(SheetNameParam))
			{
				return null;
			}
			return StructTbl[SheetNameParam].Tbl[""];
		}

		protected override void OnParseGrid(StringGrid grid, AdvBootSetting bootSetting)
		{
			if (GridList.Count == 0)
			{
				Debug.LogError("Old Version Reimport Excel Scenario Data");
				return;
			}
			
			string sheetName = grid.SheetName;
			AdvParamStructTbl data;
			if (!StructTbl.TryGetValue(sheetName, out data))
			{
				data = new AdvParamStructTbl();
				StructTbl.Add(sheetName, data);
			}

			if (sheetName == SheetNameParam)
			{
				data.AddSingle(grid);
			}
			else
			{
				data.AddTbl(grid);
			}
		}

		/// <summary>
		/// システムデータ含めてデフォルト値で初期化
		/// </summary>
		internal void InitDefaultAll(AdvParamManager src)
		{
			this.StructTbl.Clear();
			foreach ( var keyValue in src.StructTbl)
			{
				StructTbl.Add(keyValue.Key, keyValue.Value.Clone() );
			}
			IsInit = true;
		}

		/// <summary>
		/// システムデータ以外の値をデフォルト値で初期化
		/// </summary>
		/// <param name="advParamSetting"></param>
		internal void InitDefaultNormal(AdvParamManager src)
		{
			foreach (var keyValue in src.StructTbl)
			{
				AdvParamStructTbl data;
				if (StructTbl.TryGetValue(keyValue.Key, out data))
				{
					data.InitDefaultNormal(keyValue.Value);
				}
				else
				{
					Debug.LogError("Param: " + keyValue.Key + "  is not found in default param");
				}
			}
		}


		/// <summary>
		/// 値の代入を試みる
		/// </summary>
		/// <param name="key">値の名前</param>
		/// <param name="parameter">値</param>
		/// <returns>代入に成功したらtrue。指定の名前の数値なかったらfalse</returns>
		public bool TrySetParameter(string key, object parameter)
		{
			AdvParamData data;
			if (!TryGetParamData(key, out data))
			{
				return false;
			}

			bool ret = CheckSetParameterSub(data, parameter);
			if (ret)
			{
				data.Parameter = parameter;
				if (data.SaveFileType == AdvParamData.FileType.System)
				{
					HasChangedSystemParam = true;
				}
			}
			return ret;
		}

		/// <summary>
		/// 値の取得を試みる
		/// </summary>
		/// <param name="key">値の名前</param>
		/// <param name="parameter">値</param>
		/// <returns>成功したらtrue。指定の名前の数値なかったらfalse</returns>
		public bool TryGetParameter(string key, out object parameter)
		{
			AdvParamData data;
			if (TryGetParamData(key, out data))
			{
				parameter = data.Parameter;
				return true;
			}
			parameter = null;
			return false;
		}

		/// <summary>
		/// 値の代入が可能かチェックする。実際には代入しない
		/// </summary>
		/// <param name="key">値の名前</param>
		/// <param name="parameter">値</param>
		/// <returns>代入に成功したらtrue。指定の名前の数値なかったらfalse</returns>
		public bool CheckSetParameter(string key, object parameter)
		{
			AdvParamData data;
			if (!TryGetParamData(key, out data))
			{
				return false;
			}
			return CheckSetParameterSub(data, parameter);
		}

		/// <summary>
		/// 値の取得
		/// </summary>
		/// <param name="key">値の名前</param>
		/// <returns>数値</returns>
		public object GetParameter(string key)
		{
			object parameter;
			if (TryGetParameter(key, out parameter))
			{
				return parameter;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// 文字列で書かれた数式から数式を作成
		/// </summary>
		/// <param name="exp">文字列で書かれた数式</param>
		/// <returns>数式</returns>
		public ExpressionParser CreateExpression(string exp)
		{
			return new ExpressionParser(exp, GetParameter, CheckSetParameter);
		}


		/// <summary>
		/// 文字列で書かれた数式を計算して結果を返す
		/// ただし、パラメーターに代入はしない
		/// </summary>
		/// <param name="exp">文字列で書かれた数式</param>
		/// <returns>計算結果</returns>
		public object CalcExpressionNotSetParam(string exp)
		{
			ExpressionParser expression = CreateExpression(exp);
			if (string.IsNullOrEmpty(expression.ErrorMsg))
			{
				return expression.CalcExp(GetParameter, CheckSetParameter);
			}
			else
			{
				throw new System.Exception(expression.ErrorMsg);
			}
		}

		/// <summary>
		/// 数式を計算して結果を返す
		/// </summary>
		/// <param name="exp">数式</param>
		/// <returns>計算結果</returns>
		public object CalcExpression(ExpressionParser exp)
		{
			return exp.CalcExp(GetParameter, TrySetParameter);
		}


		/// <summary>
		/// 数式を計算してfloatを返す
		/// </summary>
		/// <param name="exp">数式</param>
		/// <returns>計算結果</returns>
		public float CalcExpressionFloat(ExpressionParser exp)
		{
			object obj = exp.CalcExp(GetParameter, TrySetParameter);
			if (obj.GetType() == typeof(int))
			{
				return (float)(int)obj;
			}
			else if (obj.GetType() == typeof(float))
			{
				return (float)obj;
			}
			else
			{
				Debug.LogError("Float Cast error : " + exp.Exp);
				return 0;
			}
		}

		/// <summary>
		/// 数式を計算してintを返す
		/// </summary>
		/// <param name="exp">数式</param>
		/// <returns>計算結果</returns>
		public int CalcExpressionInt(ExpressionParser exp)
		{
			object obj = exp.CalcExp(GetParameter, TrySetParameter);
			if (obj.GetType() == typeof(int))
			{
				return (int)obj;
			}
			else if (obj.GetType() == typeof(float))
			{
				return (int)(float)obj;
			}
			else
			{
				Debug.LogError("Int Cast error : " + exp.Exp);
				return 0;
			}
		}

		/// <summary>
		/// 文字列で書かれた論理式から数式を作成
		/// </summary>
		/// <param name="exp">文字列で書かれた論理式</param>
		/// <returns>数式</returns>
		public ExpressionParser CreateExpressionBoolean(string exp)
		{
			return new ExpressionParser(exp, GetParameter, CheckSetParameter, true);
		}

		/// <summary>
		/// 論理式を計算して結果を返す
		/// </summary>
		/// <param name="exp">数式</param>
		/// <returns>計算結果</returns>
		public bool CalcExpressionBoolean(ExpressionParser exp)
		{
			bool ret = exp.CalcExpBoolean(GetParameter, TrySetParameter);
			if (!string.IsNullOrEmpty(exp.ErrorMsg))
			{
				Debug.LogError(exp.ErrorMsg);
			}
			return ret;
		}

		/// <summary>
		/// 文字列で書かれた論理式を計算して結果を返す
		/// </summary>
		/// <param name="exp">文字列で書かれた論理式</param>
		/// <returns>計算結果</returns>
		public bool CalcExpressionBoolean(string exp)
		{
			return CalcExpressionBoolean(CreateExpressionBoolean(exp));
		}


		/// <summary>
		/// 値の代入を試みる
		/// </summary>
		bool CheckSetParameterSub(AdvParamData data, object parameter)
		{
			///bool値のキャストは気をつける
			if (data.Type == AdvParamData.ParamType.Bool || parameter is bool)
			{
				if (data.Type != AdvParamData.ParamType.Bool || !(parameter is bool))
				{
					return false;
				}
			}
			///string値のキャストは気をつける
			if (parameter is string)
			{
				if (data.Type != AdvParamData.ParamType.String)
				{
					return false;
				}
			}
			if (data.Type == AdvParamData.ParamType.String)
			{
				if (parameter is bool)
				{
					return false;
				}
			}
			return true;
		}

		const int Version = 2;
		const int OldVersion = 1;

		internal void ReadSaveDataBuffer(byte[] buffer)
		{
			BinaryUtil.BinaryRead(buffer, Read);
		}
		internal void ReadSystemData(BinaryReader reader)
		{
			Read(reader);
		}

		void Read(BinaryReader reader)
		{
			long position = reader.BaseStream.Position;
			int version = reader.ReadInt32();
			if (version <= Version)
			{
				if (version <= OldVersion)
				{
					reader.BaseStream.Position = position;
					ReadOld(reader);
				}
				else
				{
					int count = reader.ReadInt32();
					for (int i = 0; i < count; i++)
					{
						string key = reader.ReadString();
						byte[] buffer = BinaryUtil.ReadBytes(reader);
						if (StructTbl.ContainsKey(key))
						{
							BinaryUtil.BinaryRead(buffer, StructTbl[key].Read);
						}
						else
						{
							//セーブされていたが、パラメーター設定から消えているので読み込まない
						}
					}
				}
			}
			else
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}

		void ReadOld(BinaryReader reader)
		{
			GetDefault().Read(reader);
		}


		internal void WriteSystemData(BinaryWriter writer)
		{
			Write(writer, AdvParamData.FileType.System);
		}

		internal byte[] ToSaveDataBuffer()
		{
			return BinaryUtil.BinaryWrite((writer)=>Write(writer, AdvParamData.FileType.Default));
		}

		void Write(BinaryWriter writer, AdvParamData.FileType fileType)
		{
			writer.Write(Version);
			writer.Write(StructTbl.Count);
			foreach (var keyValue in StructTbl)
			{
				writer.Write(keyValue.Key);
				BinaryUtil.WriteBytes(writer, BinaryUtil.BinaryWrite((x) => keyValue.Value.Write(x, fileType)));
			}
		}
	}
}
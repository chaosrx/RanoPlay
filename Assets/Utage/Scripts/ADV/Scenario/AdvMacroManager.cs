//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;


namespace Utage
{

	/// <summary>
	/// マクロ定義の管理クラス
	/// </summary>
	public class AdvMacroManager
	{
		Dictionary<string, AdvMacroData> macroDataTbl = new Dictionary<string,AdvMacroData>();

		public bool TryAddMacroData(string name, StringGrid grid)
		{
			if (!IsMacroName(name)) return false;

			int index = 0;
			while(index<grid.Rows.Count)
			{
				StringGridRow row = grid.Rows[index];
				++index;
				if (row.RowIndex < grid.DataTopRow) continue;			//データの行じゃない
				if (row.IsEmptyOrCommantOut) continue;								//データがない
				
				string 	macroName;
				if( TryParseMacoBegin(row, out macroName) )
				{
					List<StringGridRow> rowList = new List<StringGridRow>();
					while (index < grid.Rows.Count)
					{
						StringGridRow macroRow = grid.Rows[index];
						++index;
						if (macroRow.IsEmptyOrCommantOut) continue;						//データがない
						if (AdvParser.ParseCellOptional<string>(macroRow, AdvColumnName.Command, "") == "EndMacro")
						{
							break;
						}

						rowList.Add(macroRow);
					}

					if (macroDataTbl.ContainsKey(macroName))
					{
						Debug.LogError(row.ToErrorString(macroName + " is already contains "));
					}
					else
					{
						macroDataTbl.Add(macroName, new AdvMacroData(macroName, row, rowList));
					}
				}
			}

			return true;
		}

		public bool TryParseMacro(StringGridRow row, AdvSettingDataManager dataManager, out List<AdvCommand> commnadList)
		{
			commnadList = null;
			string commandName = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Command,"");
			AdvMacroData data;
			if( macroDataTbl.TryGetValue(commandName, out data ) )
			{
				commnadList = new List<AdvCommand>();
				List<StringGridRow> macroRows = data.MakeMacroRows(row);
				foreach( StringGridRow macroRow in macroRows )
				{
					List<AdvCommand> list;
					if( TryParseMacro( macroRow, dataManager, out list ) )
					{
						commnadList.AddRange(list);
					}
					else
					{
						commnadList.Add(AdvCommandParser.CreateCommand(macroRow, dataManager));
					}
				}
				return true;
			}
			else
			{
				return false;
			}
		}

		const string SheetNamePattern = "Macro[0-9]";
		static readonly Regex SheetNameRegix = new Regex(SheetNamePattern, RegexOptions.IgnorePatternWhitespace);

		bool IsMacroName(string sheetName)
		{
			if (sheetName == "Macro") return true;
			Match match = SheetNameRegix.Match(sheetName);
			return match.Success;
		}

		bool TryParseMacoBegin(StringGridRow row, out string macroName)
		{
			return AdvCommandParser.TryParseScenarioLabel(row, AdvColumnName.Command, out macroName);
		}
	}
}
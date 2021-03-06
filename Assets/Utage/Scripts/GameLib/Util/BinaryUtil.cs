//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

namespace Utage
{

	/// <summary>
	/// バイナリへの読み書き用の便利クラス 
	/// </summary>
	public class BinaryUtil
	{
		/// <summary>
		/// Stringを変換してバイナリ読み込み
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public static void BinaryReadFromString(string str, Action<BinaryReader> Read)
		{
			BinaryRead( System.Convert.FromBase64String(str), Read);
		}

		/// <summary>
		/// バイナリ読み込み
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public static void BinaryRead(byte[] bytes, Action<BinaryReader> Read)
		{
			using (MemoryStream stream = new MemoryStream(bytes))
			{
				//バイナリ化
				using (BinaryReader reader = new BinaryReader(stream))
				{
					Read(reader);
				}
			}
		}


		/// <summary>
		/// バイナリ書き込みをStringに変換
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public static string BinaryWriteToString(Action<BinaryWriter> Write)
		{
			return System.Convert.ToBase64String(BinaryWrite(Write));
		}

		/// <summary>
		/// バイナリ書き込み
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public static byte[] BinaryWrite(Action<BinaryWriter> Write)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				//バイナリ化
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					Write(writer);
				}
				return stream.ToArray();
			}
		}

		/// <summary>
		/// バイト配列書き込み
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="bytes"></param>
		internal static void WriteBytes(BinaryWriter writer, byte[] bytes)
		{
			writer.Write(bytes.Length);
			writer.Write(bytes);
		}

		/// <summary>
		/// バイト配列書き込み
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="bytes"></param>
		internal static byte[] ReadBytes(BinaryReader reader)
		{
			return reader.ReadBytes( reader.ReadInt32() );
		}

		/// <summary>
		/// Vector2をバイナリ書き込み
		/// </summary>
		/// <param name="vector">書き込むVector値</param>
		/// <param name="writer">バイナリライター</param>
		public static void WriteVector2(Vector2 vector, BinaryWriter writer)
		{
			writer.Write(vector.x);
			writer.Write(vector.y);
		}

		/// <summary>
		/// Vector2をバイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		/// <returns>読み込んだVector値</returns>
		public static Vector2 ReadVector2(BinaryReader reader)
		{
			return new Vector2(reader.ReadSingle(),reader.ReadSingle());
		}

		/// <summary>
		/// Vector3をバイナリ書き込み
		/// </summary>
		/// <param name="vector">書き込むVector値</param>
		/// <param name="writer">バイナリライター</param>
		public static void WriteVector3(Vector3 vector, BinaryWriter writer)
		{
			writer.Write(vector.x);
			writer.Write(vector.y);
			writer.Write(vector.z);
		}

		/// <summary>
		/// Vector3をバイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		/// <returns>読み込んだVector値</returns>
		public static Vector3 ReadVector3(BinaryReader reader)
		{
			return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
		}

		/// <summary>
		/// Vector4をバイナリ書き込み
		/// </summary>
		/// <param name="vector">書き込むVector値</param>
		/// <param name="writer">バイナリライター</param>
		public static void WriteVector4(Vector4 vector, BinaryWriter writer)
		{
			writer.Write(vector.x);
			writer.Write(vector.y);
			writer.Write(vector.z);
			writer.Write(vector.w);
		}

		/// <summary>
		/// Vector4をバイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		/// <returns>読み込んだVector値</returns>
		public static Vector4 ReadVector4(BinaryReader reader)
		{
			return new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
		}

		/// <summary>
		/// Quaternionをバイナリ書き込み
		/// </summary>
		/// <param name="quaternion">書き込むVector値</param>
		/// <param name="writer">バイナリライター</param>
		public static void WriteQuaternion(Quaternion quaternion, BinaryWriter writer)
		{
			writer.Write(quaternion.x);
			writer.Write(quaternion.y);
			writer.Write(quaternion.z);
			writer.Write(quaternion.w);
		}

		/// <summary>
		/// Quaternionをバイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		/// <returns>読み込んだQuaternion値</returns>
		public static Quaternion ReadQuaternion(BinaryReader reader)
		{
			return new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
		}

		/// <summary>
		/// Transformのローカル情報をバイナリ書き込み
		/// </summary>
		/// <param name="transform">書き込むTransform</param>
		/// <param name="writer">バイナリライター</param>
		public static void WriteLocalTransform(Transform transform, BinaryWriter writer)
		{
			WriteVector3(transform.localPosition, writer);
			WriteVector3(transform.localEulerAngles, writer);
			WriteVector3(transform.localScale, writer);
		}

		/// <summary>
		/// Transformのローカル情報をバイナリ読み込み
		/// </summary>
		/// <param name="transform">読み込むTransform</param>
		/// <param name="reader">バイナリリーダー/param>
		public static void ReadLocalTransform(Transform transform, BinaryReader reader)
		{
			transform.localPosition = ReadVector3(reader);
			transform.localEulerAngles = ReadVector3(reader);
			transform.localScale = ReadVector3(reader);
		}
		
		/// <summary>
		/// Colorをバイナリ書き込み
		/// </summary>
		/// <param name="color">書き込むカラー</param>
		/// <param name="writer">バイナリライター</param>
		public static void WriteColor(Color color, BinaryWriter writer)
		{
			writer.Write(color.r);
			writer.Write(color.g);
			writer.Write(color.b);
			writer.Write(color.a);
		}
		
		/// <summary>
		/// Colorをバイナリ書き込み読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		/// <returns>読み込んだカラー値</returns>
		public static Color ReadColor(BinaryReader reader)
		{
			return new Color(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
		}

		/// <summary>
		/// Rectをバイナリ書き込み
		/// </summary>
		/// <param name="rect">書き込むRect</param>
		/// <param name="writer">バイナリライター</param>
		public static void WriteRect(Rect rect, BinaryWriter writer)
		{
			writer.Write(rect.xMin);
			writer.Write(rect.yMin);
			writer.Write(rect.width);
			writer.Write(rect.height);
		}

		/// <summary>
		/// Rectをバイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		/// <returns>読み込んだRect値</returns>
		public static Rect ReadRect(BinaryReader reader)
		{
			return new Rect(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
		}

		/// <summary>
		/// Boundsをバイナリ書き込み
		/// </summary>
		/// <param name="bounds">書き込むBounds</param>
		/// <param name="writer">バイナリライター</param>
		public static void WriteBounds(Bounds bounds, BinaryWriter writer)
		{
			WriteVector3(bounds.center,writer);
			WriteVector3(bounds.size, writer);
		}

		/// <summary>
		/// Boundsをバイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		/// <returns>読み込んだRect値</returns>
		public static Bounds ReadBounds(BinaryReader reader)
		{
			return new Bounds(ReadVector3(reader), ReadVector3(reader));
		}

		/// <summary>
		/// AnimationCurveをバイナリ書き込み
		/// </summary>
		/// <param name="animationCurve">書き込むAnimationCurve</param>
		/// <param name="writer">バイナリライター</param>
		public static void WriteAnimationCurve(AnimationCurve animationCurve, BinaryWriter writer)
		{
			throw new NotImplementedException();
		}


		/// <summary>
		/// RectTransfomの情報をバイナリ書き込み
		/// </summary>
		/// <param name="rectTransform">書き込むRectTransfom</param>
		/// <param name="writer">バイナリライター</param>
		public static void WriteRectTransfom(RectTransform rectTransform, BinaryWriter writer)
		{
			WriteLocalTransform(rectTransform, writer);
			WriteVector3(rectTransform.anchoredPosition3D, writer);
			WriteVector2(rectTransform.anchorMin, writer);
			WriteVector2(rectTransform.anchorMax, writer);
			WriteVector2(rectTransform.sizeDelta, writer);
			WriteVector2(rectTransform.pivot, writer);
		}


		/// <summary>
		/// RectTransfomの情報をバイナリ読み込み
		/// </summary>
		/// <param name="rectTransform">読み込むRectTransfom</param>
		/// <param name="reader">バイナリリーダー/param>
		internal static void ReadRectTransfom(RectTransform rectTransform, BinaryReader reader)
		{
			ReadLocalTransform(rectTransform, reader);
			rectTransform.anchoredPosition3D = ReadVector3(reader);
			rectTransform.anchorMin = ReadVector2(reader);
			rectTransform.anchorMax = ReadVector2(reader);
			rectTransform.sizeDelta = ReadVector2(reader);
			rectTransform.pivot = ReadVector2(reader);
		}
	}
}
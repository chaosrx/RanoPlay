//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{
	/// <summary>
	///  子オブジェクトを横に並べる
	/// </summary>
	[ExecuteInEditMode]
	[AddComponentMenu("Utage/Lib/UI/HolizontalAlignGroupScaleEffect")]
	public class UguiHorizontalAlignGroupScaleEffect : UguiHorizontalAlignGroup
	{
		public float scaleRangeLeft = -100f;
		public float scaleRangeWidth = 200f;
		public bool ignoreLocalPositionToScaleEffectRage = true;
		
		public float minScale = 0.5f;
		public float maxScale = 1f;

		protected override void CustomChild(RectTransform child, float offset )
		{
			tracker.Add(this, child,DrivenTransformProperties.Scale);

			float scale = minScale;
			float w = child.rect.width*scale;
			float left = ScaleEffectChildLocalPointLeft;
			float right = ScaleEffectChildLocalPointRight;
			if (direction == AlignDirection.LeftToRight)
			{
				left -= w;
				if (left < offset && offset < right)
				{
					float t = (offset -left)/(right-left);
					if(t>0.5f) t = 1.0f-t;
					scale =  Mathf.Lerp( minScale, maxScale, t );
				}
			}
			else
			{
				right += w;
				if (left < offset && offset < right)
				{
					float t = Mathf.Sin( Mathf.PI*(offset -left)/(right-left) );
					scale =  Mathf.Lerp( minScale, maxScale, t );
				}
			}
			child.localScale = Vector3.one*scale;
		}

		protected override void CustomLayoutRectTransform()
		{
			DrivenTransformProperties properties = DrivenTransformProperties.None;
			properties |= DrivenTransformProperties.AnchorMinX
				| DrivenTransformProperties.AnchorMaxX
					| DrivenTransformProperties.PivotX;
			tracker.Add(this, CachedRectTransform, properties);

			if (direction == AlignDirection.LeftToRight)
			{
				CachedRectTransform.anchorMin = new Vector2(0, CachedRectTransform.anchorMin.y);
				CachedRectTransform.anchorMax = new Vector2(0, CachedRectTransform.anchorMax.y);
				CachedRectTransform.pivot = new Vector2(0, CachedRectTransform.pivot.y);
			}
			else
			{
				CachedRectTransform.anchorMin = new Vector2(1, CachedRectTransform.anchorMin.y);
				CachedRectTransform.anchorMax = new Vector2(1, CachedRectTransform.anchorMax.y);
				CachedRectTransform.pivot = new Vector2(1, CachedRectTransform.pivot.y);
			}
		}

		
		void OnDrawGizmos ()
		{
			Vector3 left = ScaleEffectWolrdPointLeft;
			Vector3 right = ScaleEffectWolrdPointRight;
			Gizmos.DrawLine(left, right);
		}
		
		Vector3 ScaleEffectWolrdPointLeft
		{
			get
			{
				Vector3 pos = new Vector3(scaleRangeLeft,0,0);
				if( ignoreLocalPositionToScaleEffectRage )
				{
					pos -= CachedRectTransform.localPosition;
				}
				return CachedRectTransform.TransformPoint(pos);
			}
		}
		
		Vector3 ScaleEffectWolrdPointRight
		{
			get
			{
				Vector3 pos = new Vector3(scaleRangeLeft + scaleRangeWidth,0,0);
				if( ignoreLocalPositionToScaleEffectRage )
				{
					pos -= CachedRectTransform.localPosition;
				}
				return CachedRectTransform.TransformPoint(pos);
			}
		}
		
		float ScaleEffectChildLocalPointLeft
		{
			get
			{
				Vector3 left = ScaleEffectWolrdPointLeft;
				return CachedRectTransform.InverseTransformPoint(left).x;
			}
		}
		
		float ScaleEffectChildLocalPointRight
		{
			get
			{
				Vector3 right = ScaleEffectWolrdPointRight;
				return CachedRectTransform.InverseTransformPoint(right).x;
			}
		}
	}
}

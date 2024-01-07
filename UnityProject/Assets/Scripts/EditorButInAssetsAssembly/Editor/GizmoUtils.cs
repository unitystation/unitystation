#if UNITY_EDITOR
using System.Collections.Generic;
using Logs;
using UnityEngine;
using UnityEditor;

namespace Shared.Editor
{
	public static class GizmoUtils
	{
		public static readonly Vector3 HalfOne = new(0.5f, 0.5f, 0);

		public static void DrawGizmos<S>(S source, List<Check<S>> checks, bool local = true) where S : MonoBehaviour
		{
			var (camDistance, bounds) = InitDrawingArea(source, local);

			if (camDistance <= 100f)
			{
				DrawCheck(source, bounds, checks, camDistance <= 25f);
			}
		}

		private static (float camDistance, BoundsInt bounds) InitDrawingArea<S>(S source, bool local)
			where S : MonoBehaviour
		{
			if (local)
			{
				Gizmos.matrix = source.transform.localToWorldMatrix;
			}

			const float margin = 256;
			var currentCam = Camera.current;

			var screenBegin = Vector3.one * -margin;
			var screenEnd = new Vector3(currentCam.pixelWidth + margin, currentCam.pixelHeight + margin);

			var worldPointBegin = currentCam.ScreenToWorldPoint(screenBegin);
			var worldPointEnd = currentCam.ScreenToWorldPoint(screenEnd);

			var sourcePosition = source.transform.position;
			var localStart = GetPosition(worldPointBegin, sourcePosition, 0);
			var localEnd = GetPosition(worldPointEnd, sourcePosition, 1);

			var camDistance = localEnd.y - localStart.y;
			var bounds = new BoundsInt(localStart, localEnd - localStart);

			return (camDistance, bounds);

			Vector3Int GetPosition(Vector3 worldPoint, Vector3 pos, float z)
			{
				if (local) worldPoint -= pos;
				worldPoint.z = z;
				return worldPoint.RoundToInt();
			}
		}

		private static void DrawCheck<S>(S source, BoundsInt bounds, IEnumerable<Check<S>> checks, bool drawLabel)
		{
			foreach (var check in checks)
			{
				if (check.Active == false) continue;

				foreach (var position in bounds.allPositionsWithin)
				{
					check.DrawGizmo(source, position);
					if (drawLabel) check.DrawLabel(source, position);
				}
			}
		}

		public static void DrawCube(Vector3 position, Color color, bool local = true, float alpha = 0.5f,
			float size = 1)
		{
			color.a = alpha;
			Gizmos.color = color;
			Gizmos.DrawCube(position + (local ? HalfOne : Vector3.zero), Vector3.one * size);
		}

		public static void DrawWireCube(Vector3 position, Color color, bool local = true, float alpha = 0.5f,
			float size = 1)
		{
			color.a = alpha;
			Gizmos.color = color;
			Gizmos.DrawWireCube(position + (local ? HalfOne : Vector3.zero), Vector3.one * size);
		}

		public static void DrawRay(Vector3 pos, Vector3 direction, bool local = true)
		{
			if (direction == Vector3.zero)
			{
				return;
			}

			if (local)
			{
				pos += HalfOne;
			}

			Gizmos.DrawRay(pos, direction);
		}

		public static void DrawArrow(Vector3 pos, Vector3 direction, bool local = true, float arrowHeadLength = 0.25f,
			float arrowHeadAngle = 20.0f)
		{
			if (direction == Vector3.zero)
			{
				return;
			}

			if (local)
			{
				pos += HalfOne;
			}

			Gizmos.DrawRay(pos, direction);

			Quaternion lookRotation = Quaternion.LookRotation(direction);
			Vector3 right = lookRotation * Quaternion.Euler(arrowHeadAngle, 0, 0) * Vector3.back;
			Vector3 left = lookRotation * Quaternion.Euler(-arrowHeadAngle, 0, 0) * Vector3.back;
			Vector3 up = lookRotation * Quaternion.Euler(0, arrowHeadAngle, 0) * Vector3.back;
			Vector3 down = lookRotation * Quaternion.Euler(0, -arrowHeadAngle, 0) * Vector3.back;
			var rayFrom = pos + direction;
			Gizmos.DrawRay(rayFrom, right * arrowHeadLength);
			Gizmos.DrawRay(rayFrom, left * arrowHeadLength);
			Gizmos.DrawRay(rayFrom, up * arrowHeadLength);
			Gizmos.DrawRay(rayFrom, down * arrowHeadLength);
		}

		public static void DrawText(string text, Vector3 position, bool local = true, int fontSize = 0,
			float yOffset = 0)
		{
			DrawText(text, position, Color.white, local, fontSize, yOffset);
		}

		public static void DrawText(string text, Vector3 position, Color color, bool local = true, int fontSize = 0,
			float yOffset = 0)
		{
			GUISkin guiSkin = GUI.skin;
			if (guiSkin == null)
			{
				Loggy.LogWarning("editor warning: guiSkin parameter is null", Category.UI);
				return;
			}

			GUIContent textContent = new GUIContent(text);

			GUIStyle style = new GUIStyle(guiSkin.GetStyle("Label")) { normal = { textColor = color } };

			if (fontSize > 0)
			{
				style.fontSize = fontSize;
				style.fontStyle = FontStyle.Bold;
			}

			Vector2 textSize = style.CalcSize(textContent);

			if (local)
			{
				position += Vector3.one;
			}

			var currentCam = Camera.current;
			Vector3 screenPoint = currentCam.WorldToScreenPoint(position);

			float x = screenPoint.x - textSize.x * 0.5f;
			float y = screenPoint.y + textSize.y * 0.5f + yOffset;
			float z = screenPoint.z;

			Vector3 worldPosition = currentCam.ScreenToWorldPoint(new Vector3(x, y, z));
			Handles.Label(worldPosition, textContent, style);
		}
	}
}
#endif
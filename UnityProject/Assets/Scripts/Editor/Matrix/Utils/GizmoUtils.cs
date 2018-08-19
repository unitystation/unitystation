
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class GizmoUtils
{
	private static Vector3 localOffset = new Vector3(0.5f, 0.5f, 0);

	public static void DrawGizmos<S>(S source, List<Check<S>> checks, bool local=true) where S : MonoBehaviour
	{
		float camDistance;
		BoundsInt bounds;
		InitDrawingArea(source, out camDistance, out bounds, local);

		if (camDistance <= 100f)
		{
			DrawGizmos(source, bounds, checks);
		}

		if (camDistance <= 10f)
		{
			DrawLabels(source, bounds, checks);
		}
	}

	private static void InitDrawingArea<S>(S source, out float camDistance, out BoundsInt bounds, bool local) where S : MonoBehaviour
	{
		if (local)
		{
			Gizmos.matrix = source.transform.localToWorldMatrix;
		}

		Vector3 screenBegin = Vector3.one * -32;
		Vector3 screenEnd = new Vector3(Camera.current.pixelWidth + 32, Camera.current.pixelHeight + 32);

		Vector3 worldPointBegin = Camera.current.ScreenToWorldPoint(screenBegin);
		Vector3 worldPointEnd = Camera.current.ScreenToWorldPoint(screenEnd);

		Vector3Int localStart;
		Vector3Int localEnd;

		if (local)
		{
			localStart = (worldPointBegin - source.transform.position).RoundToInt();
			localEnd = (worldPointEnd - source.transform.position).RoundToInt();
		}
		else
		{
			localStart = worldPointBegin.RoundToInt();
			localEnd = worldPointEnd.RoundToInt();
		}

		localStart.z = 0;
		localEnd.z = 1;

		camDistance = localEnd.y - localStart.y;

		bounds = new BoundsInt(localStart, localEnd - localStart);
	}

	private static void DrawGizmos<S>(S source, BoundsInt bounds, IReadOnlyCollection<Check<S>> checks)
	{
		foreach (Vector3Int position in bounds.allPositionsWithin)
		{
			foreach (Check<S> check in checks)
			{
				if (check.Active)
				{
					check.DrawGizmo(source, position);
				}
			}
		}
	}

	private static void DrawLabels<S>(S source, BoundsInt bounds, IReadOnlyCollection<Check<S>> checks)
	{
		foreach (Vector3Int position in bounds.allPositionsWithin)
		{
			foreach (Check<S> check in checks)
			{
				if (check.Active)
				{
					check.DrawLabel(source, position + Vector3Int.up);
				}
			}
		}
	}

	public static void DrawCube(Vector3 position, Color color, bool local, float alpha = 0.5f)
	{
		Gizmos.color = color.WithAlpha(alpha);
		Gizmos.DrawCube(position + (local ? localOffset: Vector3.zero), Vector3.one);
	}

	public static void DrawCube(Vector3 position, Color color, float alpha = 0.5f)
	{
		DrawCube(position, color, true, alpha);
	}

	public static void DrawWireCube(Vector3 position, Color color, bool local, float alpha = 0.5f)
	{
		Gizmos.color = color.WithAlpha(alpha);
		Gizmos.DrawWireCube(position + (local ? localOffset: Vector3.zero), Vector3.one);
	}

	public static void DrawWireCube(Vector3 position, Color color, float alpha = 0.5f)
	{
		DrawWireCube(position, color, true, alpha);
	}

	public static void DrawText(string text, Vector3 position, int fontSize = 0, float yOffset = 0)
	{
		DrawText(text, position, Color.white, fontSize, yOffset);
	}

	public static void DrawText(string text, Vector3 position, Color color, int fontSize = 0, float yOffset = 0)
	{
		GUISkin guiSkin = GUI.skin;
		if (guiSkin == null)
		{
			Debug.LogWarning("editor warning: guiSkin parameter is null");
			return;
		}

		GUIContent textContent = new GUIContent(text);

		GUIStyle style = new GUIStyle(guiSkin.GetStyle("Label")) {normal = {textColor = color}};

		if (fontSize > 0)
		{
			style.fontSize = fontSize;
			style.fontStyle = FontStyle.Bold;
		}

		Vector2 textSize = style.CalcSize(textContent);
		Vector3 screenPoint = Camera.current.WorldToScreenPoint(position + Vector3.one);

		float x = screenPoint.x - textSize.x * 0.5f;
		float y = screenPoint.y + textSize.y * 0.5f + yOffset;
		float z = screenPoint.z;

		Vector3 worldPosition = Camera.current.ScreenToWorldPoint(
			new Vector3(x, y, z));
		Handles.Label(worldPosition, textContent, style);
	}
}
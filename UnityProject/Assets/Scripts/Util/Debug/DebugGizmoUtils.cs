using Logs;
using UnityEngine;

public static class DebugGizmoUtils
{
	public static void DrawText(string text, Vector3 position, int fontSize = 0, float yOffset = 0)
	{
#if UNITY_EDITOR
		GUISkin guiSkin = GUI.skin;
		if (guiSkin == null)
		{
			Loggy.LogWarning("editor warning: guiSkin parameter is null", Category.DebugConsole);
			return;
		}

		GUIContent textContent = new GUIContent(text);

		GUIStyle style = new GUIStyle(guiSkin.GetStyle("Label")) {normal = {textColor = Gizmos.color}};
		if (fontSize > 0)
		{
			style.fontSize = fontSize;
			style.fontStyle = FontStyle.Bold;
		}

		Vector2 textSize = style.CalcSize(textContent);
		Vector3 screenPoint = Camera.current.WorldToScreenPoint(position);

		Vector3 worldPosition = Camera.current.ScreenToWorldPoint(
			new Vector3(screenPoint.x - textSize.x * 0.5f, screenPoint.y + textSize.y * 0.5f + yOffset, screenPoint.z));
		UnityEditor.Handles.Label(worldPosition, textContent, style);
#endif
	}

	public static void DrawArrow(Vector3 pos, Vector3 direction, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
	{
		if (direction == Vector3.zero)
		{
			return;
		}

		Gizmos.DrawRay(pos, direction);

		Quaternion lookRotation = Quaternion.LookRotation(direction);
		Vector3 right = lookRotation * Quaternion.Euler(arrowHeadAngle, 0, 0) * Vector3.back;
		Vector3 left = lookRotation * Quaternion.Euler(-arrowHeadAngle, 0, 0) * Vector3.back;
		Vector3 up = lookRotation * Quaternion.Euler(0, arrowHeadAngle, 0) * Vector3.back;
		Vector3 down = lookRotation * Quaternion.Euler(0, -arrowHeadAngle, 0) * Vector3.back;
		Gizmos.color = Gizmos.color;
		Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
		Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
		Gizmos.DrawRay(pos + direction, up * arrowHeadLength);
		Gizmos.DrawRay(pos + direction, down * arrowHeadLength);
	}

	public static void DrawRect( Bounds bounds )
	{
		DrawRect( bounds.min, bounds.max );
	}
	public static void DrawRect( BoundsInt bounds )
	{
		DrawRect( bounds.min, bounds.max );
	}
	public static void DrawRect( Rect bounds )
	{
		DrawRect( bounds.min, bounds.max );
	}
	public static void DrawRect( RectInt bounds )
	{
		DrawRect( (Vector2)bounds.min, (Vector2)bounds.max );
	}

	public static void DrawRect(Vector3 min, Vector3 max)
	{
		//bottom
		Gizmos.DrawLine( min, new Vector2(max.x,min.y) );
		//top
		Gizmos.DrawLine( new Vector2(min.x,max.y), max );
		//left
		Gizmos.DrawLine( min, new Vector2(min.x,max.y) );
		//right
		Gizmos.DrawLine( new Vector2(max.x,min.y), max );
	}
}
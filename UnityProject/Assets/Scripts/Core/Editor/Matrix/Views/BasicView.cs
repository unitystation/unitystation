using UnityEngine;

public abstract class BasicView
{
	private Vector2 scrollPosition = Vector2.zero;
	
	public void OnGUI()
	{
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);
		
		DrawContent();
		
		GUILayout.EndScrollView();
	}

	public abstract void DrawContent();
}
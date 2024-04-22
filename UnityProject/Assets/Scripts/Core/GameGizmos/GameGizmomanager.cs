using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shared.Managers;
using UnityEngine;

public class GameGizmomanager : SingletonManager<GameGizmomanager>
{
	public GameGizmoLine PrefabLineRenderer;

	public GameGizmoSprite PrefabSpriteRenderer;

	public GameGizmoText PrefabText;

	public GameGizmoSquare PrefabSquare;

	public GameGizmoBox PrefabBox;

	public List<GameGizmo> ActiveGizmos = new List<GameGizmo>();

	public List<ISelectionGizmo> ActiveSelection = new List<ISelectionGizmo>();

	public GameObject SelectedObject;


	public void OnEnable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}


	public void OnDisable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	public void UpdateMe()
	{
		foreach (var ActiveSelection in ActiveSelection)
		{
			ActiveSelection.UpdateGizmos();
		}
	}

	public static GameGizmoLine AddNewLineStaticClient(GameObject TrackingFrom, Vector3 From,   GameObject TrackingTo, Vector3 To, Color color, float LineThickness =  0.03125f)
	{
		return Instance.AddNewLine(TrackingFrom, From,TrackingTo, To, color, LineThickness);
	}

	public static GameGizmoSprite AddNewSpriteStaticClient(GameObject Tracking, Vector3 position,  Color color, SpriteDataSO Sprite)
	{
		return Instance.AddNewSprite(Tracking, position,color,Sprite);
	}

	public static GameGizmoText AddNewTextStaticClient(GameObject Tracking, Vector3 position, string Text,Color Colour, float TextSize = 3f)
	{
		return Instance.AddNewText(Tracking, position,Text,Colour, TextSize);
	}

	public static GameGizmoSquare AddNewSquareStaticClient(GameObject TrackingFrom, Vector3 Position, Color Colour, float LineThickness  =  0.03125f, Vector2? BoxSize = null)
	{
		BoxSize ??= Vector2.one;
		return Instance.AddNewSquare(TrackingFrom, Position,Colour,LineThickness, BoxSize.Value);
	}

	public static GameGizmoBox AddNewBoxStaticClient(GameObject TrackingFrom, Vector3 Position, Color Colour , float BoxSize = 1)
	{
		return Instance.AddNewBox(TrackingFrom, Position,Colour, BoxSize);
	}


	public GameGizmoLine AddNewLine(GameObject TrackingFrom, Vector3 From,   GameObject TrackingTo, Vector3 To, Color color, float LineThickness)
	{
		var Line =  Instantiate(PrefabLineRenderer, Instance.transform);
		ActiveGizmos.Add(Line);
		Line.SetUp(TrackingFrom,From,TrackingTo,  To,color, LineThickness );
		return Line;
	}


	public GameGizmoSprite AddNewSprite(GameObject Tracking, Vector3 position,  Color color, SpriteDataSO Sprite)
	{
		var SpriteGizmo =  Instantiate(PrefabSpriteRenderer, Instance.transform);
		ActiveGizmos.Add(SpriteGizmo);
		SpriteGizmo.SetUp(Tracking,position,color, Sprite );
		return SpriteGizmo;
	}

	public GameGizmoText AddNewText(GameObject Tracking, Vector3 position, string Text,Color Colour, float TextSize = 3f)
	{
		var GizmoText =  Instantiate(PrefabText, Instance.transform);
		ActiveGizmos.Add(GizmoText);
		GizmoText.SetUp(Tracking,position,Text,Colour , TextSize );
		return GizmoText;
	}

	public GameGizmoSquare AddNewSquare(GameObject TrackingFrom, Vector3 Position, Color Colour, float LineThickness , Vector2 BoxSize)
	{
		var GizmoSquare =  Instantiate(PrefabSquare, Instance.transform);
		ActiveGizmos.Add(GizmoSquare);
		GizmoSquare.SetUp(TrackingFrom, Position,Colour,LineThickness, BoxSize);
		return GizmoSquare;
	}

	public GameGizmoBox AddNewBox(GameObject TrackingFrom, Vector3 Position, Color Colour , float BoxSize = 1)
	{
		var GizmoBox =  Instantiate(PrefabBox, Instance.transform);
		ActiveGizmos.Add(GizmoBox);
		GizmoBox.SetUp(TrackingFrom, Position,Colour, BoxSize);
		return GizmoBox;
	}


	public static void SelectObject(GameObject Object)
	{
		if (Instance.SelectedObject != null)
		{
			UnSelectObject(Object);
		}

		Instance.SelectedObject = Object;

		var Gizmos = Object.GetComponents<ISelectionGizmo>().ToList();
		foreach (var Gizmo in Gizmos)
		{
			Gizmo.OnSelected();
		}

		Instance.ActiveSelection.AddRange(Gizmos);
	}
	public static void UnSelectObject(GameObject Object)
	{
		if (Object == null)
		{
			foreach (var Gizmo in Instance.ActiveSelection)
			{
				Gizmo.OnDeselect();
			}
			Instance.ActiveSelection.Clear();
		}
		else
		{
			var Gizmos = Object.GetComponents<ISelectionGizmo>().ToList();
			foreach (var Gizmo in Gizmos)
			{
				Gizmo.OnDeselect();
				Instance.ActiveSelection.Remove(Gizmo);
			}
		}
	}
}

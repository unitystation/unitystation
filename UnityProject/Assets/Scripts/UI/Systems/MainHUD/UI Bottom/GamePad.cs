using System.Collections;
using System.Collections.Generic;
using Logs;
using UnityEngine;

public class GamePad : MonoBehaviour
{
	public List<GameKey> Keys = new List<GameKey>();
	private HashSet<KeyCode> pressedKeys = new HashSet<KeyCode>();
	/// <summary>
	/// pressed this frame
	/// </summary>
	private HashSet<KeyCode> justPressed = new HashSet<KeyCode>();
	/// <summary>
	/// released this frame
	/// </summary>
	private HashSet<KeyCode> justReleased = new HashSet<KeyCode>();

	private void OnEnable()
	{
		if ( Keys.Count == 0 )
		{
			foreach ( var gameKey in GetComponentsInChildren<GameKey>(true) )
			{
				Keys.Add( gameKey );
			}
		}

		foreach ( var key in Keys )
		{
			key.OnKeyPress.AddListener( () => PressKey( key ) );
			key.OnKeyRelease.AddListener( () => ReleaseKey( key ) );
		}
	}

	private void PressKey( GameKey gameKey )
	{
		Loggy.LogTrace($"Pressed key {gameKey}", Category.UserInput);
		foreach ( var key in gameKey.Keys )
		{
			pressedKeys.Add( key );
			justPressed.Add( key );
		}

		StartCoroutine( ClearPressed() );
	}

	private void ReleaseKey( GameKey gameKey )
	{
		Loggy.LogTrace($"Released key {gameKey}", Category.UserInput);
		foreach ( var key in gameKey.Keys )
		{
			pressedKeys.Remove( key );
			justReleased.Add( key );
		}

		StartCoroutine( ClearReleased() );
	}

	private IEnumerator ClearPressed()
	{
		yield return WaitFor.EndOfFrame;
		justPressed.Clear();
	}
	private IEnumerator ClearReleased()
	{
		yield return WaitFor.EndOfFrame;
		justReleased.Clear();
	}

	private void OnDisable()
	{
		foreach ( var gameKey in Keys )
		{
			gameKey.OnKeyPress.RemoveAllListeners();
			gameKey.OnKeyRelease.RemoveAllListeners();
		}
		pressedKeys.Clear();
	}

	public bool GetKey( KeyCode key )
	{
		return pressedKeys.Contains( key );
	}
	public bool GetKeyDown( KeyCode key )
	{
		return justPressed.Contains( key );
	}

	public bool GetKeyUp( KeyCode key )
	{
		return justReleased.Contains( key );

	}
}
using System.Collections.Generic;
using UnityEngine;

public class GamePad : MonoBehaviour
{
	public List<GameKey> Keys = new List<GameKey>();

	private void OnEnable()
	{
		if ( Keys.Count == 0 )
		{
			foreach ( var gameKey in GetComponentsInChildren<GameKey>() )
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
		Logger.LogTrace( "Pressed key " + gameKey, Category.UI );
		//todo: make it functional
	}

	private void ReleaseKey( GameKey gameKey )
	{
		Logger.LogTrace( "Released key " + gameKey, Category.UI );
		//todo: make it functional
	}

	private void OnDisable()
	{
		foreach ( var gameKey in Keys )
		{
			gameKey.OnKeyPress.RemoveAllListeners();
			gameKey.OnKeyRelease.RemoveAllListeners();
		}
	}
}
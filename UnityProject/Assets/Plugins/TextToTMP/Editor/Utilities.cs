using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TextToTMPNamespace
{
	[Serializable]
	internal class ObjectsToUpgradeList : IEnumerable<string>
	{
		private class Enumerator : IEnumerator<string>
		{
			public string Current { get { return list.paths[index]; } }
			object IEnumerator.Current { get { return list.paths[index]; } }

			private readonly ObjectsToUpgradeList list;
			private int index;

			public Enumerator( ObjectsToUpgradeList list )
			{
				this.list = list;
				Reset();
			}

			public void Dispose() { }

			public bool MoveNext()
			{
				while( ++index < list.m_length )
				{
					if( list.enabled[index] )
						return true;
				}

				return false;
			}

			public void Reset()
			{
				index = -1;
			}
		}

		[SerializeField]
		private string[] paths;
		[SerializeField]
		private bool[] enabled;

		[SerializeField]
		private int m_length = 0;
		public int Length { get { return m_length; } }

		[SerializeField]
		private int m_enabledCount = 0;
		public int EnabledCount { get { return m_enabledCount; } }

		public void Add( string path )
		{
			if( paths == null )
			{
				paths = new string[32];
				enabled = new bool[32];
			}

			for( int i = 0; i < m_length; i++ )
			{
				if( paths[i] == path )
					return;
			}

			if( m_length >= paths.Length )
			{
				int newSize = paths.Length > 0 ? paths.Length * 2 : 2;
				Array.Resize( ref paths, newSize );
				Array.Resize( ref enabled, newSize );
			}

			paths[m_length] = path;
			enabled[m_length] = true;

			m_length++;
			m_enabledCount++;
		}

		public bool Contains( string path )
		{
			for( int i = 0; i < m_length; i++ )
			{
				if( paths[i] == path )
					return true;
			}

			return false;
		}

		public void Clear()
		{
			if( paths != null )
			{
				for( int i = 0; i < m_length; i++ )
					paths[i] = null;
			}

			m_length = 0;
			m_enabledCount = 0;
		}

		public void DrawOnGUI()
		{
			// Show "Toggle All" toggle
			if( m_length > 1 )
			{
				EditorGUI.showMixedValue = m_enabledCount > 0 && m_enabledCount < m_length;

				EditorGUI.BeginChangeCheck();
				bool _enabled = TextToTMPWindow.WordWrappingToggleLeft( "- Toggle All -", m_enabledCount > 0 );
				if( EditorGUI.EndChangeCheck() )
				{
					for( int i = 0; i < m_length; i++ )
						enabled[i] = _enabled;

					m_enabledCount = _enabled ? m_length : 0;
				}

				EditorGUI.showMixedValue = false;
			}

			for( int i = 0; i < m_length; i++ )
			{
				bool _enabled = TextToTMPWindow.WordWrappingToggleLeft( paths[i], enabled[i] );
				if( _enabled != enabled[i] )
				{
					enabled[i] = _enabled;

					if( _enabled )
						m_enabledCount++;
					else
						m_enabledCount--;
				}
			}
		}

		public IEnumerator<string> GetEnumerator()
		{
			return new Enumerator( this );
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator( this );
		}
	}

	public partial class TextToTMPWindow
	{
		private T GetFirstAssetOfType<T>() where T : Object
		{
			string[] assetsOfType = AssetDatabase.FindAssets( "t:" + typeof( T ).Name );
			if( assetsOfType != null && assetsOfType.Length > 0 )
				return AssetDatabase.LoadAssetAtPath<T>( AssetDatabase.GUIDToAssetPath( assetsOfType[0] ) );

			return null;
		}

		private bool ComponentHasAnyPrefabInstanceModifications( Object component )
		{
#if UNITY_2018_3_OR_NEWER
			if( PrefabUtility.IsPartOfPrefabInstance( component ) )
#else
			if( PrefabUtility.GetPrefabType( component ) == PrefabType.PrefabInstance )
#endif
			{
				SerializedProperty iterator = new SerializedObject( component ).GetIterator();
				while( iterator.Next( true ) )
				{
					if( iterator.prefabOverride )
						return true;
				}
			}

			return false;
		}

#if !UNITY_2018_3_OR_NEWER
		private RemovedComponentLegacy[] GetRemovedComponentsFromPrefabInstance( Transform instance )
		{
			Component[] instanceComponents = instance.GetComponents<Component>();
			List<Component> prefabComponents = new List<Component>( instanceComponents.Length );
			( (Transform) PrefabUtility.GetPrefabParent( instance ) ).GetComponents( prefabComponents );

			for( int i = 0; i < instanceComponents.Length; i++ )
				prefabComponents.Remove( (Component) PrefabUtility.GetPrefabParent( instanceComponents[i] ) );

			RemovedComponentLegacy[] result = new RemovedComponentLegacy[prefabComponents.Count];
			for( int i = 0; i < prefabComponents.Count; i++ )
				result[i] = new RemovedComponentLegacy( prefabComponents[i], instance.gameObject );

			return result;
		}
#endif

		private string GetPathOfObject( Transform obj )
		{
			string result = obj.name;
			while( obj.parent )
			{
				obj = obj.parent;
				result = obj.name + "/" + result;
			}

			return result;
		}

		private bool AreScenesSaved()
		{
			for( int i = 0; i < EditorSceneManager.loadedSceneCount; i++ )
			{
				Scene scene = EditorSceneManager.GetSceneAt( i );
				if( scene.isDirty || string.IsNullOrEmpty( scene.path ) )
					return false;
			}

			return true;
		}
	}
}
#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Serialization;
using US13.Clothing;
using US13.Core.Lifecycle;
using US13.Core.Physics;
using US13.Systems.Lobby;

namespace US13.UI.Systems.Character
{
	public class PlayerBlueprint : MonoBehaviour, IServerSpawn
	{
		public CharacterSheet CharacterSheet = new CharacterSheet();

		[FormerlySerializedAs("NonImportantMind")] [Tooltip("Should the mind be destroyed If the body is destroyed and a player is not possessing it")]
		public bool nonImportantMind = false;

		[NaughtyAttributes.Button()]
		public void Spawn()
		{
			SpawnObject();
		}

		public GameObject SpawnObject()
		{
			var Mind =  PlayerSpawn.NewSpawnCharacterV2(null, CharacterSheet, nonImportantMind);
			Mind.Body.GetComponent<UniversalObjectPhysics>().AppearAtWorldPositionServer(this.transform.position);
			return Mind.Body.gameObject;
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			Spawn();
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(PlayerBlueprint))]
	public class PlayerBlueprintEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			var blueprint = (PlayerBlueprint)target;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Species");
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.TextField(blueprint.CharacterSheet.Species);
			EditorGUI.EndDisabledGroup();

			if (GUILayout.Button("Select", GUILayout.Width(60)))
			{
				SpeciesSelectorWindow.ShowWindow(blueprint);
			}

			EditorGUILayout.EndHorizontal();

			// Draw rest of inspector
			DrawDefaultInspector();
		}
	}


	public class SpeciesSelectorWindow : EditorWindow
	{
		private PlayerBlueprint targetBlueprint;
		private Vector2 scrollPosition;
		private const float ICON_SIZE = 64f;
		private const float PADDING = 5f;
		private const int ITEMS_PER_ROW = 4;

		public static void ShowWindow(PlayerBlueprint blueprint)
		{
			var window = GetWindow<SpeciesSelectorWindow>("Select Species");
			window.targetBlueprint = blueprint;
			window.minSize = new Vector2(ICON_SIZE * ITEMS_PER_ROW + PADDING * (ITEMS_PER_ROW + 1), 200);
		}

		private void OnGUI()
		{
			if (targetBlueprint == null) return;

			var species = RaceSOSingleton.GetAllSpecies();
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			float x = PADDING;
			float y = PADDING;
			float buttonHeight = ICON_SIZE + 20f;

			for (int i = 0; i < species.Count; i++)
			{
				// Reset x and increment y when reaching end of row
				if (i % ITEMS_PER_ROW == 0 && i != 0)
				{
					x = PADDING;
					y += buttonHeight + PADDING;
				}

				var buttonRect = new Rect(x, y, ICON_SIZE, buttonHeight);
				var preview = species[i].Base.PreviewSprite;
				var texture = preview.Variance[0].Frames[0].sprite.texture;

				var style = new GUIStyle();
				style.alignment = TextAnchor.LowerCenter;
				style.normal.textColor = Color.white;
				style.wordWrap = true;

				if (GUI.Button(buttonRect, ""))
				{
					Undo.RecordObject(targetBlueprint, "Change Species");
					targetBlueprint.CharacterSheet.Species = species[i].name;
					EditorUtility.SetDirty(targetBlueprint);
					Close();
				}

				GUI.DrawTexture(new Rect(buttonRect.x, buttonRect.y, ICON_SIZE, ICON_SIZE), texture);
				GUI.Label(buttonRect, species[i].name, style);

				x += ICON_SIZE + PADDING;
			}

			EditorGUILayout.EndScrollView();
		}
	}
#endif
}
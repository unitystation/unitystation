using System;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using UnityEngine;

/// <summary>
/// allows clothing to add its armor values to the creature wearing it
/// </summary>
[RequireComponent(typeof(Integrity))]
public class WearableArmor : MonoBehaviour, IServerInventoryMove
{
	[SerializeField] [Tooltip("When wore in this slot, the armor values will be applied to player.")]
	private NamedSlot slot = NamedSlot.outerwear;

	[SerializeField] [Tooltip("What body parts does this item protect and how well does it protect.")]
	private List<ProtectedBodyPart> armoredBodyParts = new List<ProtectedBodyPart>();

	private PlayerHealthV2 playerHealthV2;


	[Serializable]
	public class ProtectedBodyPart
	{
		[SerializeField]
		private BodyPartType armoringBodyPartType;

		internal BodyPartType ArmoringBodyPartType => armoringBodyPartType;

		[SerializeField]
		private Armor armor;

		internal Armor Armor => armor;

		internal BodyPart bodyPartScript;
	}

	public void OnInventoryMoveServer(InventoryMove info)
	{
		//Wearing
		if (info.ToSlot != null & info.ToSlot?.NamedSlot != null)
		{
			playerHealthV2 = info.ToRootPlayer?.PlayerScript.playerHealth;

			if (playerHealthV2 != null && info.ToSlot.NamedSlot == slot)
			{
				UpdateBodyPartsArmor();
			}
		}

		//taking off
		if (info.FromSlot != null & info.FromSlot?.NamedSlot != null)
		{
			playerHealthV2 = info.FromRootPlayer?.PlayerScript.playerHealth;

			if (playerHealthV2 != null && info.FromSlot.NamedSlot == slot)
			{
				UpdateBodyPartsArmor(currentlyRemovingArmor: true);
			}
		}
	}

/// <summary>
		/// Adds or removes armor per body part depending on the characteristics of this armor.
		/// </summary>
		/// <param name="currentlyRemovingArmor">Are we taking off our armor or putting it on?</param>
		private void UpdateBodyPartsArmor(bool currentlyRemovingArmor = false)
		{
			foreach (ProtectedBodyPart protectedBodyPart in armoredBodyParts)
			{
				foreach (RootBodyPartContainer rootBodyPartContainer in playerHealthV2.RootBodyPartContainers)
				{
					foreach (BodyPart bodyPart in rootBodyPartContainer.ContainsLimbs)
					{
						DeepUpdateBodyPartArmor(bodyPart, protectedBodyPart, currentlyRemovingArmor);
					}
				}
			}
		}

		/// <summary>
		/// Adds or removes armor per body part depending on the characteristics of this armor.
		/// Checks not only the bodyPart, but also all other body parts nested in bodyPart.
		/// </summary>
		/// <param name="bodyPart">body part to update</param>
		/// <param name="protectedBodyPart">a tuple of the body part associated with the body part</param>
		/// <param name="currentlyRemovingArmor">Are we taking off our armor or putting it on?</param>
		/// <returns>true if the bodyPart was updated, false otherwise</returns>
		private static bool DeepUpdateBodyPartArmor(
			BodyPart bodyPart,
			ProtectedBodyPart protectedBodyPart,
			bool currentlyRemovingArmor
		)
		{
			if (bodyPart.BodyPartType == protectedBodyPart.ArmoringBodyPartType)
			{
				if (currentlyRemovingArmor)
				{
					bodyPart.ClothingArmor.Remove(protectedBodyPart.Armor);
					protectedBodyPart.bodyPartScript = null;
				}
				else
				{
					bodyPart.ClothingArmor.AddFirst(protectedBodyPart.Armor);
					protectedBodyPart.bodyPartScript = bodyPart;
				}

				return true;
			}

			if (protectedBodyPart.bodyPartScript.ContainBodyParts.Count == 0)
			{
				return false;
			}

			foreach (BodyPart innerBodyPart in protectedBodyPart.bodyPartScript.ContainBodyParts)
			{
				if (DeepUpdateBodyPartArmor(bodyPart, protectedBodyPart, currentlyRemovingArmor))
				{
					return true;
				}
			}

			return false;
		}

		/*
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Clothing.WearableArmor;
	using HealthV2;
	using UnityEngine;
	using BodyPart = HealthV2.BodyPart;
	#if UNITY_EDITOR
	using UnityEditor;
	using UnityEditorInternal;

	#endif

	namespace Clothing.WearableArmor
	{
		/// <summary>
		/// allows clothing to add its armor values to the creature wearing it
		/// </summary>
		[RequireComponent(typeof(Integrity))]
		[CreateAssetMenu]
		public class WearableArmor : MonoBehaviour, IServerInventoryMove
		{
			[SerializeField] [Tooltip("When wore in this slot, the armor values will be applied to player.")]
			private NamedSlot slot = NamedSlot.outerwear;

			[SerializeField] [Tooltip("What body parts does this item protect and how well does it protect.")]
			private List<ProtectedBodyPart> armoredBodyParts = new List<ProtectedBodyPart>();

			private PlayerHealthV2 playerHealthV2;

			public string[] ArmorableBodyParts;
			public List<WearableArmor.ProtectedBodyPart> ArmoredBodyParts;

			[Serializable]
			public class ProtectedBodyPart
			{
				[SerializeField] public int bodyPartId;
				[SerializeField] public Armor armor;

				internal BodyPart BodyPartScript = null;
			}

			public void OnInventoryMoveServer(InventoryMove info)
			{
				//Wearing
				if (info.ToSlot != null & info.ToSlot?.NamedSlot != null)
				{
					playerHealthV2 = info.ToRootPlayer?.PlayerScript.playerHealth;

					if (playerHealthV2 != null && info.ToSlot.NamedSlot == slot)
					{
						UpdateBodyPartsArmor();
					}
				}

				//taking off
				if (info.FromSlot != null & info.FromSlot?.NamedSlot != null)
				{
					playerHealthV2 = info.FromRootPlayer?.PlayerScript.playerHealth;

					if (playerHealthV2 != null && info.FromSlot.NamedSlot == slot)
					{
						UpdateBodyPartsArmor(currentlyRemovingArmor: true);
					}
				}
			}

			/// <summary>
			/// Adds or removes armor per body part depending on the characteristics of this armor.
			/// </summary>
			/// <param name="currentlyRemovingArmor">Are we taking off our armor or putting it on?</param>
			private void UpdateBodyPartsArmor(bool currentlyRemovingArmor = false)
			{
				foreach (ProtectedBodyPart protectedBodyPart in armoredBodyParts)
				{
					foreach (RootBodyPartContainer rootBodyPartContainer in playerHealthV2.RootBodyPartContainers)
					{
						foreach (BodyPart bodyPart in rootBodyPartContainer.ContainsLimbs)
						{
							DeepUpdateBodyPartArmor(bodyPart, protectedBodyPart, currentlyRemovingArmor);
						}
					}
				}
			}

			/// <summary>
			/// Adds or removes armor per body part depending on the characteristics of this armor.
			/// Checks not only the bodyPart, but also all other body parts nested in bodyPart.
			/// </summary>
			/// <param name="bodyPart">body part to update</param>
			/// <param name="protectedBodyPart">a tuple of the body part associated with the body part</param>
			/// <param name="currentlyRemovingArmor">Are we taking off our armor or putting it on?</param>
			/// <returns>true if the bodyPart was updated, false otherwise</returns>
			private static bool DeepUpdateBodyPartArmor(
				BodyPart bodyPart,
				ProtectedBodyPart protectedBodyPart,
				bool currentlyRemovingArmor
			)
			{
				if (true)
				{
					if (currentlyRemovingArmor)
					{
						bodyPart.ClothingArmor.Remove(protectedBodyPart.armor);
						protectedBodyPart.BodyPartScript = null;
					}
					else
					{
						bodyPart.ClothingArmor.AddFirst(protectedBodyPart.armor);
						protectedBodyPart.BodyPartScript = bodyPart;
					}

					return true;
				}

				if (protectedBodyPart.BodyPartScript.ContainBodyParts.Count == 0)
				{
					return false;
				}

				foreach (BodyPart innerBodyPart in protectedBodyPart.BodyPartScript.ContainBodyParts)
				{
					if (DeepUpdateBodyPartArmor(bodyPart, protectedBodyPart, currentlyRemovingArmor))
					{
						return true;
					}
				}

				return false;
			}
		}

	#if UNITY_EDITOR
		[CustomEditor(typeof(WearableArmor))]
		public class BPaADEditor : Editor
		{
			// This will be the serialized clone property of Dialogue.CharacterList
			private SerializedProperty SPArmorableBodyPartTypes;

			// This will be the serialized clone property of Dialogue.DialogueItems
			private SerializedProperty SPArmoredBodyParts;

			// This is a little bonus from my side!
			// These Lists are extremely more powerful then the default presentation of lists!
			// you can/have to implement completely custom behavior of how to display and edit
			// the list elements
			private ReorderableList armorableBodyPartTypes;
			private ReorderableList armoredBodyParts;

			// Reference to the actual Dialogue instance this Inspector belongs to
			private WearableArmor wearableArmor;

			// class field for storing available options
			private GUIContent[] availableBodyPartTypes;

			// Called when the Inspector is opened / ScriptableObject is selected
			private void OnEnable()
			{
				// Get the target as the type you are actually using
				wearableArmor = (WearableArmor) target;

				// Link in serialized fields to their according SerializedProperties
				SPArmorableBodyPartTypes =
					serializedObject.FindProperty(nameof(WearableArmor.ArmorableBodyParts));
				SPArmoredBodyParts = serializedObject.FindProperty(nameof(WearableArmor.ArmoredBodyParts));

				// Setup and configure the charactersList we will use to display the content of the CharactersList
				// in a nicer way
				armorableBodyPartTypes = new ReorderableList(serializedObject, SPArmorableBodyPartTypes)
				{
					displayAdd = true,
					displayRemove = true,
					draggable = false, // for now disable reorder feature since we later go by index!

					// As the header we simply want to see the usual display name of the CharactersList
					drawHeaderCallback = rect => EditorGUI.LabelField(rect, SPArmorableBodyPartTypes.displayName),

					// How shall elements be displayed
					drawElementCallback = (rect, index, focused, active) =>
					{
						// get the current element's SerializedProperty
						SerializedProperty SPElement = SPArmorableBodyPartTypes.GetArrayElementAtIndex(index);

						// Get all characters as string[]
						string[] availableIDs = wearableArmor.ArmorableBodyParts;

						// store the original GUI.color
						Color originalGUIColor = GUI.color;
						// Tint the field in red for invalid values
						// either because it is empty or a duplicate
						if (string.IsNullOrWhiteSpace(SPElement.stringValue) ||
						    availableIDs.Count(id => string.Equals(id, SPElement.stringValue)) > 1)
						{
							GUI.color = Color.red;
						}

						// Draw the property which automatically will select the correct drawer -> a single line text field
						EditorGUI.PropertyField(
							new Rect(rect.x, rect.y, rect.width, EditorGUI.GetPropertyHeight(SPElement)),
							SPElement
						);

						// reset to the default color
						GUI.color = originalGUIColor;

						// If the value is invalid draw a HelpBox to explain why it is invalid
						if (string.IsNullOrWhiteSpace(SPElement.stringValue))
						{
							rect.y += EditorGUI.GetPropertyHeight(SPElement);
							EditorGUI.HelpBox(
								new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
								"ID may not be empty!",
								MessageType.Error
							);
						}
						else if (availableIDs.Count(id => string.Equals(id, SPElement.stringValue)) > 1)
						{
							rect.y += EditorGUI.GetPropertyHeight(SPElement);
							EditorGUI.HelpBox(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
								"Duplicate! ID has to be unique!", MessageType.Error);
						}
					},

					// Get the correct display height of elements in the list
					// according to their values
					// in this case e.g. dependent whether a HelpBox is displayed or not
					elementHeightCallback = index =>
					{
						SerializedProperty SPElement = SPArmorableBodyPartTypes.GetArrayElementAtIndex(index);
						string[] availableIDs = wearableArmor.ArmorableBodyParts;

						float height = EditorGUI.GetPropertyHeight(SPElement);

						if (string.IsNullOrWhiteSpace(SPElement.stringValue) ||
						    availableIDs.Count(id => string.Equals(id, SPElement.stringValue)) > 1)
						{
							height += EditorGUIUtility.singleLineHeight;
						}

						return height;
					},

					// Overwrite what shall be done when an element is added via the +
					// Reset all values to the defaults for new added elements
					// By default Unity would clone the values from the last or selected element otherwise
					onAddCallback = list =>
					{
						// This adds the new element but copies all values of the select or last element in the list
						list.serializedProperty.arraySize++;

						SerializedProperty SPNewElement = list.serializedProperty.GetArrayElementAtIndex(
							list.serializedProperty.arraySize - 1
						);
						SPNewElement.stringValue = "";
					}
				};

				// Setup and configure the dialogItemsList we will use to display the content of the DialogueItems
				// in a nicer way
				armoredBodyParts = new ReorderableList(serializedObject, SPArmoredBodyParts)
				{
					displayAdd = true,
					displayRemove = true,
					draggable = true, // for the dialogue items we can allow re-ordering

					// As the header we simply want to see the usual display name of the DialogueItems
					drawHeaderCallback = rect => EditorGUI.LabelField(rect, SPArmoredBodyParts.displayName),

					// How shall elements be displayed
					drawElementCallback = (rect, index, focused, active) =>
					{
						// get the current element's SerializedProperty
						SerializedProperty SPElement = SPArmoredBodyParts.GetArrayElementAtIndex(index);

						// Get the nested property fields of the DialogueElement class
						SerializedProperty SPBodyPartID = SPElement.FindPropertyRelative(
							nameof(WearableArmor.ProtectedBodyPart.bodyPartId)
						);
						SerializedProperty SPArmor =
							SPElement.FindPropertyRelative(nameof(WearableArmor.ProtectedBodyPart.armor));

						float popUpHeight = EditorGUI.GetPropertyHeight(SPBodyPartID);

						// store the original GUI.color
						Color originalGUIColor = GUI.color;

						// if the value is invalid tint the next field red
						if (SPBodyPartID.intValue < 0)
						{
							GUI.color = Color.red;
						}

						// Draw the Popup so you can select from the existing character names
						SPBodyPartID.intValue = EditorGUI.Popup(
							new Rect(rect.x, rect.y, rect.width, popUpHeight),
							new GUIContent(SPBodyPartID.displayName),
							SPBodyPartID.intValue,
							availableBodyPartTypes
						);

						// reset the GUI.color
						GUI.color = originalGUIColor;
						rect.y += popUpHeight;

						// Draw the text field
						// since we use a PropertyField it will automatically recognize that this field is tagged [TextArea]
						// and will choose the correct drawer accordingly
						float armorHeight = EditorGUI.GetPropertyHeight(SPArmor);
						EditorGUI.PropertyField(
							new Rect(rect.x, rect.y, rect.width, armorHeight),
							SPArmor,
							true
						);
					},

					// Get the correct display height of elements in the list
					// according to their values
					// in this case e.g. we add an additional line as a little spacing between elements
					elementHeightCallback = index =>
					{
						SerializedProperty SPElement = SPArmoredBodyParts.GetArrayElementAtIndex(index);

						SerializedProperty SPBodyPartID = SPElement.FindPropertyRelative(
							nameof(WearableArmor.ProtectedBodyPart.bodyPartId)
						);
						SerializedProperty SPArmor =
							SPElement.FindPropertyRelative(nameof(WearableArmor.ProtectedBodyPart.armor));

						return EditorGUI.GetPropertyHeight(SPBodyPartID)
						       + EditorGUI.GetPropertyHeight(SPArmor)
						       + EditorGUIUtility.singleLineHeight;
					},

					// Overwrite what shall be done when an element is added via the +
					// Reset all values to the defaults for new added elements
					// By default Unity would clone the values from the last or selected element otherwise
					onAddCallback = list =>
					{
						// This adds the new element but copies all values of the select or last element in the list
						list.serializedProperty.arraySize++;

						SerializedProperty SPNewElement = list.serializedProperty.GetArrayElementAtIndex(
							list.serializedProperty.arraySize - 1
						);
						SerializedProperty SPBodyPartID = SPNewElement.FindPropertyRelative(
							nameof(WearableArmor.ProtectedBodyPart.bodyPartId)
						);
						SPBodyPartID.intValue = -1;
						SerializedProperty SPArmor =
							SPNewElement.FindPropertyRelative(nameof(WearableArmor.ProtectedBodyPart.armor));
						WearableArmor bodyPartAndArmorDialogue =
							(WearableArmor) SPArmor.serializedObject.targetObject;
					}
				};

				// Get the existing character names ONCE as GuiContent[]
				// Later only update this if the charcterList was changed
				availableBodyPartTypes = wearableArmor.ArmorableBodyParts.Select(
					item => new GUIContent(item)
				).ToArray();
			}

			public override void OnInspectorGUI()
			{
				DrawScriptField();

				// load real target values into SerializedProperties
				serializedObject.Update();

				EditorGUI.BeginChangeCheck();
				armorableBodyPartTypes.DoLayoutList();
				if (EditorGUI.EndChangeCheck())
				{
					// Write back changed values into the real target
					serializedObject.ApplyModifiedProperties();

					// Update the existing character names as GuiContent[]
					availableBodyPartTypes = wearableArmor.ArmorableBodyParts
						.Select(item => new GUIContent(item)).ToArray();
				}

				armoredBodyParts.DoLayoutList();

				// Write back changed values into the real target
				serializedObject.ApplyModifiedProperties();
			}

			private void DrawScriptField()
			{
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((WearableArmor) target),
					typeof(WearableArmor),
					false);
				EditorGUI.EndDisabledGroup();

				EditorGUILayout.Space();
			}
		}
	#endif
	}
		 */
}
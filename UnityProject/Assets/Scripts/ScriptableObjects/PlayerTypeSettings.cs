using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "PlayerStateSettings", menuName = "ScriptableObjects/Player/PlayerStateSettings")]
	public class PlayerTypeSettings : ScriptableObject
	{
		[FormerlySerializedAs("playerState")]
		[Header("Player Type, only choose one!")]
		[SerializeField]
		private PlayerTypes playerType = PlayerTypes.Normal;
		public PlayerTypes PlayerType => playerType;

		[HorizontalLine]
		[SerializeField]
		private bool canBeCuffed = false;
		public bool CanBeCuffed => canBeCuffed;

		[SerializeField]
		private bool canCraft = false;
		public bool CanCraft => canCraft;

		[SerializeField]
		private bool canBuckleOthers = false;
		public bool CanBuckleOthers => canBuckleOthers;

		[SerializeField]
		private bool canPull = false;
		public bool CanPull => canPull;

		[HorizontalLine]
		[Header("Door Interaction")]
		[SerializeField]
		private bool canInteractWithDoors = false;
		public bool CanInteractWithDoors => canInteractWithDoors;

		[SerializeField]
		private bool canPryDoorsWithHands = false;
		public bool CanPryDoorsWithHands => canPryDoorsWithHands;

		[SerializeField]
		//TODO this should really be from body part instead
		private string pryHandName = "";
		public string PryHandName => pryHandName;

		[HorizontalLine]
		[Header("UI Actions buttons")]
		[SerializeField]
		private bool canDropItems = false;
		public bool CanDropItems => canDropItems;

		[SerializeField]
		private bool canThrowItems = false;
		public bool CanThrowItems => canThrowItems;

		[SerializeField]
		private bool canResist = false;
		public bool CanResist => canResist;

		[SerializeField]
		private bool canRest = false;
		public bool CanRest => canRest;

		[HorizontalLine]
		[Header("Examine")]
		[SerializeField]
		private ExamineType canBeExamined = ExamineType.None;
		public ExamineType CanBeExamined => canBeExamined;

		[SerializeField]
		private ExamineType canExamineOthers = ExamineType.None;
		public ExamineType CanExamineOthers => canExamineOthers;

		[HorizontalLine]
		[Header("Chat")]
		[SerializeField]
		private ChatChannel transmitChannels = ChatChannel.None;
		public ChatChannel TransmitChannels => transmitChannels;

		[SerializeField]
		private ChatChannel receiveChannels = ChatChannel.None;
		public ChatChannel ReceiveChannels => receiveChannels;

		[SerializeField]
		private ChatChannel defaultChannel = ChatChannel.None;
		public ChatChannel DefaultChannel => defaultChannel;

		[SerializeField]
		private bool checkForRadios = false;
		public bool CheckForRadios => checkForRadios;

		[SerializeField]
		private PlayerTypes sendSpeechBubbleTo = PlayerTypes.Ghost;
		public PlayerTypes SendSpeechBubbleTo => sendSpeechBubbleTo;

		[SerializeField]
		private PlayerTypes receiveSpeechBubbleFrom = PlayerTypes.Normal;
		public PlayerTypes ReceiveSpeechBubbleFrom => receiveSpeechBubbleFrom;

		[HorizontalLine]
		[Header("Melee")]
		[SerializeField]
		private bool canMelee = false;
		public bool CanMelee => canMelee;

		[SerializeField]
		private List<WeaponNetworkActions.MeleeData> emptyMeleeAttackData = new List<WeaponNetworkActions.MeleeData>();
		public List<WeaponNetworkActions.MeleeData> EmptyMeleeAttackData => emptyMeleeAttackData;
	}
}
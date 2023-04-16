using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using UnityEngine;
using Mirror;
using Communications;
using Managers;
using Systems.Explosions;
using Scripts.Core.Transform;
using UI.Items;
using UnityEngine.Events;
using Chemistry;
using Random = UnityEngine.Random;

namespace Items.Weapons
{
	/// <summary>
	/// The base script for holding all universal data and functions between explosives.
	/// Interactions are written separately.
	/// </summary>
	public class ExplosiveBase : SignalReceiver
	{
		[Header("Explosive settings")]
		[SerializeField] protected ExplosiveType explosiveType;
		[SerializeField] protected bool detonateImmediatelyOnSignal;
		[SerializeField] protected int timeToDetonate = 10;
		[SerializeField] protected int minimumTimeToDetonate = 10;
		[SerializeField] protected float explosiveStrength = 150f;
		[SerializeField, Range(0,150)] protected int explosiveRadius = 150;
		[SerializeField] protected SpriteDataSO activeSpriteSO;
		[SerializeField] protected AddressableAudioSource beepSound;
		[SerializeField] protected float progressTime = 3f;
		[Header("Explosive Components")]
		[SerializeField] protected SpriteHandler spriteHandler;
		[SerializeField] protected ScaleSync scaleSync;
		protected RegisterItem registerItem;
		protected UniversalObjectPhysics objectBehaviour;
		protected Pickupable pickupable;
		protected HasNetworkTabItem explosiveGUI;
		[HideInInspector] public GUI_Explosive GUI;
		[SyncVar(hook=nameof(OnArmStateChange))] protected bool isArmed;
		[SyncVar] protected bool countDownActive = false;
		protected List<SignalEmitter> emitters = new List<SignalEmitter>();

		public int MinimumTimeToDetonate => minimumTimeToDetonate;
		public bool DetonateImmediatelyOnSignal => detonateImmediatelyOnSignal;
		public bool CountDownActive => countDownActive;
		public ExplosiveType ExplosiveType => explosiveType;

		public int TimeToDetonate
		{
			get => timeToDetonate;
			set => timeToDetonate = value;
		}

		public bool IsArmed
		{
			get => isArmed;
			set => isArmed = value;
		}

		private void Awake()
		{
			if(spriteHandler == null) spriteHandler = GetComponentInChildren<SpriteHandler>();
			if(scaleSync == null) scaleSync = GetComponent<ScaleSync>();
			registerItem = GetComponent<RegisterItem>();
			objectBehaviour = GetComponent<UniversalObjectPhysics>();
			pickupable = GetComponent<Pickupable>();
			explosiveGUI = GetComponent<HasNetworkTabItem>();
			RandomizeFreqAndCode();
		}

		[Server]
		public virtual IEnumerator Countdown()
		{
			Chat.AddActionMsgToChat(gameObject, $"The {gameObject.ExpensiveName()} beeps and lights up as it starts counting down..");
			countDownActive = true;
			spriteHandler.SetSpriteSO(activeSpriteSO);
			if (GUI != null) GUI.StartCoroutine(GUI.UpdateTimer());
			yield return WaitFor.Seconds(timeToDetonate); //Delay is in milliseconds
			Detonate();
		}

		public static UnityEvent<Vector3Int, BlastData> ExplosionEvent { get; set; } = new UnityEvent<Vector3Int, BlastData>();

		protected virtual void Detonate()
		{
			if(gameObject == null) return;

			// Get data before despawning
			var worldPos = objectBehaviour.registerTile.WorldPosition;
			// Despawn the explosive
			RemoveSelfFromManager();
			_ = Despawn.ServerSingle(gameObject);

			BlastData blastData = new BlastData();
			blastData.BlastYield = explosiveStrength;

			ExplosionEvent.Invoke(worldPos, blastData);
			Explosion.StartExplosion(worldPos, explosiveStrength, null, explosiveRadius);
		}

		/// <summary>
		/// Toggle the detention mode of the explosive, true means it will only work with a signaling device.
		/// </summary>
		/// <param name="mode"></param>
		public void ToggleMode(bool mode)
		{
			detonateImmediatelyOnSignal = mode;
		}

		public override void ReceiveSignal(SignalStrength strength, SignalEmitter responsibleEmitter, ISignalMessage message = null)
		{
			if(gameObject == null || countDownActive == true) return;
			if(ValidSignal(responsibleEmitter) == false) return;
			if (detonateImmediatelyOnSignal)
			{
				Detonate();
				return;
			}
			StartCoroutine(Countdown());
		}

		private bool ValidSignal(SignalEmitter responsibleEmitter)
		{
			if(PassCode == 0) return true; //0 means that this explosive will accept any signal it passes through it even if it's not on the emitter list.
			return emitters.Contains(responsibleEmitter) && responsibleEmitter.Passcode == PassCode;
		}

		protected bool HackEmitter(TargetedInteraction interaction)
		{
			if (interaction.UsedObject == null || interaction.UsedObject.TryGetComponent<SignalEmitter>(out var emitter) == false) return false;
			void Hack()
			{
				emitters.Add(emitter);
				Frequency = emitter.Frequency;
				PassCode = emitter.Passcode;
				Chat.AddActionMsgToChat(interaction.Performer, $"The {gameObject.ExpensiveName()} copies {emitter.gameObject.ExpensiveName()}'s " +
																$"codes from {interaction.PerformerPlayerScript.visibleName}'s hands!");
			}
			var bar = StandardProgressAction.Create(
				new StandardProgressActionConfig(StandardProgressActionType.CPR, false, false), Hack);
			bar.ServerStartProgress(interaction.Performer.RegisterTile(), progressTime, interaction.Performer);
			SparkUtil.TrySpark(interaction.Performer);
			Chat.AddActionMsgToChat(interaction.Performer, $"{interaction.PerformerPlayerScript.visibleName} hovers a " +
															$"{emitter.gameObject.ExpensiveName()} over the {gameObject.ExpensiveName()}.");
			return true;
		}

		protected virtual void OnArmStateChange(bool oldState, bool newState) { }
	}

	public enum ExplosiveType
	{
		C4,
		X4,
		SyndicateBomb,
	}

	public struct BlastData
	{
		public float BlastYield { get; set; }
		public ReagentMix ReagentMix { get; set; }
	}
}

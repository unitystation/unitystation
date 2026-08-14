using Logs;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using US13.Core.Lifecycle;
using US13.Core.Physics;
using US13.Core.Sprite_Handler;
using US13.HealthV2.Living;
using US13.Items.Weapons;
using US13.Objects;
using US13.Objects.Traps;
using US13.Player;
using US13.Projectiles;
using US13.Systems.Explosions;
using US13.Systems.Spells.Wizard;
using US13.Tilemaps.Behaviours.Objects;
using Util;

public class MagicTrap : EnterTileBase
{
    public enum TrapType
    {
	    Fire,
	    Shock,
	    Earth
    }

    public TrapType trapType;


    [SerializeField] private float pressDuration = 3f;


    private RegisterObject registerObject;
    private const int IDLE_VARIANT_INDEX = 0;
    private const int PRESSED_VARIANT_INDEX = 1;

    public GameObject ToIgnore;

    public UnityEvent OnPlayerStepEvent = new UnityEvent();

    [SerializeField, BoxGroup("References")]
    private GameObject smallRockPrefab = default;

    protected override void Awake()
    {
	    objectPhysics = GetComponent<UniversalObjectPhysics>();
	    registerObject = GetComponent<RegisterObject>();
    }

    protected override void OnDisable()
    {

	    objectPhysics.OnLocalTileReached.RemoveListener(OnLocalPositionChangedServer);
    }

    public override void OnPlayerStep(PlayerScript playerScript)
    {
	    ObjectIn(playerScript.gameObject);
    }

    public void ObjectIn(GameObject eventData)
    {
	    if (eventData == null) return;
	    if (eventData.gameObject == null) return;
	    if (eventData.gameObject == this.gameObject) return;
	    if (eventData.gameObject == ToIgnore) return;
	    switch (trapType)
	    {
		    case TrapType.Shock:
			    eventData.GetComponent<LivingHealthMasterBase>()?.playerScript?.RegisterPlayer?.ServerStun(4, true, true);
			    break;
		    case TrapType.Earth:
			    var smallRocks = Spawn.ServerPrefab(smallRockPrefab, this.gameObject.AssumedWorldPosServer());
			    OnRockLanded(smallRocks.GameObject, 30);
			    break;
		    case TrapType.Fire:
			    eventData?.GetComponent<LivingHealthMasterBase>()?.ChangeFireStacks (4);
			    break;
	    }

	    _ = Despawn.ServerSingle(this.gameObject);
    }

    public override void OnObjectEnter(GameObject eventData)
    {
	    ObjectIn(eventData);
    }

    private void OnRockLanded(GameObject rock, float damage)
    {
	    var landingPosition = rock.RegisterTile().WorldPositionServer;
	    Explosion.StartExplosion(landingPosition, damage);
	    ExplosionUtils.PlaySoundAndShake(landingPosition, 16, 4);
    }

}

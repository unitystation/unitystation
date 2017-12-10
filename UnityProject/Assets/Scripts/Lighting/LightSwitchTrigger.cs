using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;
using InputControl;
using PlayGroups.Input;

namespace Lighting
{
    public class LightSwitchTrigger : InputTrigger
    {
        const int MAX_TARGETS = 44;
        public float radius = 10f;

        int lightingMask;
        int obstacleMask;

        readonly Collider2D[] lightSpriteColliders = new Collider2D[MAX_TARGETS];

        [SyncVar(hook = "SyncLightSwitch")]
        public bool isOn = true;
        private SpriteRenderer spriteRenderer;
        public Sprite lightOn;
        public Sprite lightOff;
        private bool switchCoolDown = false;
        private AudioSource clickSFX;
        private bool soundAllowed = false;

        void Awake()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            clickSFX = GetComponent<AudioSource>();
        }

        void Start()
        {
            //This is needed because you can no longer apply lightSwitch prefabs (it will move all of the child sprite positions)
            gameObject.layer = LayerMask.NameToLayer("WallMounts");
            //and the rest of the mask caches:
            lightingMask = LayerMask.GetMask("Lighting");
            obstacleMask = LayerMask.GetMask("Walls", "Door Open", "Door Closed");

        }

        public override void OnStartClient()
        {
            StartCoroutine(WaitForLoad());
        }

        IEnumerator WaitForLoad()
        {
            yield return new WaitForSeconds(3f);
            SyncLightSwitch(isOn);
        }

        public override void Interact(GameObject originator, Vector3 position, string hand)
        {
            if (!PlayerManager.LocalPlayerScript.IsInReach(position))
                return;

            if (switchCoolDown)
                return;

            StartCoroutine(CoolDown());
            PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleLightSwitch(gameObject);
        }

        IEnumerator CoolDown()
        {
            switchCoolDown = true;
            yield return new WaitForSeconds(0.2f);
            switchCoolDown = false;
        }

        void DetectLightsAndAction(bool state)
        {
            var startPos = GetCastPos();
            var length = Physics2D.OverlapCircleNonAlloc(startPos, radius, lightSpriteColliders, lightingMask);
            for (int i = 0; i < length; i++)
            {
                var localCollider = lightSpriteColliders[i];
                var localObject = localCollider.gameObject;
                var localObjectPos = (Vector2)localObject.transform.position;
                var distance = Vector3.Distance(startPos, localObjectPos);
                if (IsWithinReach(startPos, localObjectPos, distance))
                {
                    localObject.SendMessage("Trigger", state, SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        private bool IsWithinReach(Vector2 pos, Vector2 targetPos, float distance)
        {
            return distance <= radius
            &&
            Physics2D.Raycast(pos, targetPos - pos, distance, obstacleMask).collider == null;
        }

        Vector2 GetCastPos()
        {
            Vector2 newPos = transform.position + ((transform.position - spriteRenderer.transform.position));
            return newPos;
        }

        void SyncLightSwitch(bool state)
        {
            DetectLightsAndAction(state);

            if (clickSFX != null && soundAllowed)
            {
                clickSFX.Play();
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = state ? lightOn : lightOff;
            }
            soundAllowed = true;
        }
    }
}

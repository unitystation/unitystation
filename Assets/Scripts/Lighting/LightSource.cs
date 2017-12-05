using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputControl;
using Events;
using UnityEngine.Events;
using Sprites;

namespace Lighting
{
    enum LightState
    {
        On,
        Off,
        Broken
    }

    public class LightSource : ObjectTrigger
    {
        /// <summary>
        /// The SpriteRenderer for this light
        /// </summary>
        private SpriteRenderer Renderer;

        /// <summary>
        /// The state of this light
        /// </summary>
        private LightState LightState;

        /// <summary>
        /// The actual light effect that the light source represents
        /// </summary>
        public GameObject Light;

        /// <summary>
        /// The sprite to show when this light is turned on
        /// </summary>
        public Sprite SpriteLightOn;

        /// <summary>
        /// The sprite to show when this light is turned off
        /// </summary>
        public Sprite SpriteLightOff;

        //For network sync reliability
        private bool waitToCheckState = false;
        private bool tempStateCache;

        const int MAX_TARGETS = 400;
        public float radius = 6f;

        int ambientMask;
        int obstacleMask;

        readonly Collider2D[] lightSpriteColliders = new Collider2D[MAX_TARGETS];

        void Awake()
        {
            Renderer = GetComponentInChildren<SpriteRenderer>();
        }

        void Start()
        {
            ambientMask = LayerMask.GetMask("LightingAmbience");
            obstacleMask = LayerMask.GetMask("Walls", "Door Open", "Door Closed");
            InitLightSprites();
        }

        void SetLocalAmbientTiles(bool state)
        {
            var length = Physics2D.OverlapCircleNonAlloc(transform.position, radius, lightSpriteColliders, ambientMask);
            for (int i = 0; i < length; i++)
            {
                var localCollider = lightSpriteColliders[i];
                var localObject = localCollider.gameObject;
                var localObjectPos = (Vector2)localObject.transform.position;
                var distance = Vector3.Distance(transform.position, localObjectPos);
                if (IsWithinReach(transform.position, localObjectPos, distance))
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

        public override void Trigger(bool state)
        {
            tempStateCache = state;

            if (waitToCheckState)
                return;

            if (Renderer == null)
            {
                waitToCheckState = true;
                StartCoroutine(WaitToTryAgain());
                return;
            }
            Renderer.sprite = state ? SpriteLightOn : SpriteLightOff;
            if (Light != null)
            {
                Light.SetActive(state);
            }
            SetLocalAmbientTiles(state);
        }

        private void InitLightSprites()
        {
            LightState = LightState.On;

            //set the ON sprite to whatever the spriterenderer child has?
            var lightSprites = SpriteManager.LightSprites["lights"];
            SpriteLightOn = Renderer.sprite;

            //find the OFF light?
            string[] split = SpriteLightOn.name.Split('_');
            int onPos;
            int.TryParse(split[1], out onPos);
            SpriteLightOff = lightSprites[onPos + 4];
        }

        //Handle sync failure
        IEnumerator WaitToTryAgain()
        {
            yield return new WaitForSeconds(0.2f);
            if (Renderer == null)
            {
                Renderer = GetComponentInChildren<SpriteRenderer>();
                if (Renderer != null)
                {
                    Renderer.sprite = tempStateCache ? SpriteLightOn : SpriteLightOff;
                    if (Light != null)
                    {
                        Light.SetActive(tempStateCache);
                    }
                }
                else
                {
                    Debug.LogWarning("LightSource still failing Renderer sync");
                }
            }
            else
            {
                Renderer.sprite = tempStateCache ? SpriteLightOn : SpriteLightOff;
                if (Light != null)
                {
                    Light.SetActive(tempStateCache);
                }
            }
            waitToCheckState = false;
        }
    }
}
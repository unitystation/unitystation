using System.Linq;
using UnityEngine;
using System.Collections;

namespace Light2D.Examples
{
    public class Spacecraft : MonoBehaviour
    {
        public bool ReleaseLandingGear = false;
        public RocketEngine BottomLeftEngine;
        public RocketEngine BottomRightEngine;
        public RocketEngine SideLeftEngine;
        public RocketEngine SideRightEngine;
        public RocketEngine ReverseLeftEngine;
        public RocketEngine ReverseRightEngine;
        public Rigidbody2D MainRigidbody;
        public GameObject FlaresPrefab;
        public Vector2 RightFlareSpawnPos = new Vector3(1.87f, -0.28f, 0);
        public Vector2 RightFlareVelocity;
        public float FlareAngularVelocity;
        private LandingLeg[] _landingLegs;

        private void Awake()
        {
            _landingLegs = GetComponentsInChildren<LandingLeg>(true);
        }

        private void Start()
        {
            BalanceCenterOfMass();
            FixCollision();
        }

        private void FixCollision()
        {
            var colliders = GetComponentsInChildren<Collider2D>();
            foreach (var coll1 in colliders)
            {
                foreach (var coll2 in colliders)
                {
                    if (coll1 != coll2)
                        Physics2D.IgnoreCollision(coll1, coll2);
                }
            }
        }

        private void BalanceCenterOfMass()
        {
            var rigidbodies = GetComponentsInChildren<Rigidbody2D>();
            var groups = rigidbodies
                .GroupBy(rb => rb.name.Replace("Left", "").Replace("Right", ""))
                .ToArray();
            foreach (var group in groups)
            {
                var mainCenterOfMass = transform.InverseTransformPoint(group.First().worldCenterOfMass);
                foreach (var rb in group)
                {
                    var cm = transform.InverseTransformPoint(rb.worldCenterOfMass);
                    if (Mathf.Abs(mainCenterOfMass.x + cm.x) < 0.02f && Mathf.Abs(cm.y - mainCenterOfMass.y) < 0.02f)
                    {
                        cm.x = -mainCenterOfMass.x;
                        cm.y = mainCenterOfMass.y;
                    }
                    rb.centerOfMass = rb.transform.InverseTransformPoint(transform.TransformPoint(cm));
                }
            }
        }

        private void Update()
        {
            SetLandingGear(ReleaseLandingGear);
        }

        private void SetLandingGear(bool release)
        {
            foreach (var landingLeg in _landingLegs)
                landingLeg.Release = release;
        }

        public void DropFlares()
        {
            SpawnFlare(RightFlareSpawnPos, RightFlareVelocity);
            SpawnFlare(new Vector3(-RightFlareSpawnPos.x, RightFlareSpawnPos.y),
                new Vector2(-RightFlareVelocity.x, RightFlareVelocity.y));
        }

        void SpawnFlare(Vector2 localPos, Vector2 localVelocity)
        {
            var worldPos = MainRigidbody.transform.TransformPoint(localPos);
            var worldVel = (Vector2)MainRigidbody.transform.TransformDirection(localVelocity) + MainRigidbody.velocity;
            var worldRot = Quaternion.Euler(0, 0,
                FlaresPrefab.transform.rotation.eulerAngles.z*Mathf.Sign(localVelocity.x) +
                MainRigidbody.rotation);
            var flareObj = (GameObject)Instantiate(FlaresPrefab, worldPos, worldRot);
            var flareRigidbody = flareObj.GetComponent<Rigidbody2D>();
            flareRigidbody.velocity = worldVel;
            flareRigidbody.angularVelocity = FlareAngularVelocity*Mathf.Sign(localVelocity.x);
        }
    }
}
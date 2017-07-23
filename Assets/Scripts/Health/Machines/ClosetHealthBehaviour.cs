using UnityEngine;

namespace Objects
{
    public class ClosetHealthBehaviour : HealthBehaviour
    {
        public override void onDeathActions()
        {
            Debug.Log("I am closet and I'm dying");
        }
    }
}
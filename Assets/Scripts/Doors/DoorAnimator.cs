using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Doors
{
    public abstract class DoorAnimator : MonoBehaviour
    {
        public DoorController doorController;


        public abstract void OpenDoor();
        public abstract void CloseDoor();
        public abstract void AccessDenied();

    }
}

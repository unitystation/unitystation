using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace InputControl
{

    public abstract class ObjectTrigger : NetworkBehaviour
    {

        public abstract void Trigger(bool state);
    }
}
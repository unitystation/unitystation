using System;

using UnityEngine;

namespace Adrenak.UniVoice.MirrorNetwork {
    public class UpdateHook : MonoBehaviour {
        public event Action OnUpdate;

        [Obsolete("Use UpdateHook.Create() instead", true)]
        public UpdateHook() { }

        public static UpdateHook Create() {
            var go = new GameObject("UpdateHook");
            return go.AddComponent<UpdateHook>();
        }

        void Update() {
            OnUpdate?.Invoke();
        }
    }
}

using UnityEngine;

public class SingletonManager<T> : MonoBehaviour where T : MonoBehaviour
{
    private static bool shuttingDown = false;
    private static readonly object @lock = new object();
    private static T instance;

    public static T Instance
    {
        get
        {
            if (shuttingDown)
                return null;

            lock (@lock)
            {
                if (instance == null)
                {
                    instance = (T)FindObjectOfType(typeof(T));
                    if (instance == null)
                    {
                        GameObject singletonObject = new GameObject();
                        instance = singletonObject.AddComponent<T>();
                        singletonObject.name = typeof(T).ToString() + " (Singleton)";

                        DontDestroyOnLoad(singletonObject);
                    }
                }
                return instance;
            }
        }
    }

    private void OnApplicationQuit() => shuttingDown = true;

    private void OnDestroy() => shuttingDown = true;
}
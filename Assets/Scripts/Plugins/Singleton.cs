// http://wiki.unity3d.com/index.php/Toolbox

using UnityEngine;
using FullInspector;

public class Singleton<T> : BaseBehavior<BinaryFormatterSerializer> where T : BaseBehavior<BinaryFormatterSerializer> {

    private static T _instance;
    private static object _lock = new object();

    public static T Instance {
        get {
            if (applicationIsQuitting) {
                Debug.LogWarning("[Singleton Instance '" + typeof(T) + "' already destroyed on application quit. Won't create again - returning null.");
                return null;
            }

            lock(_lock) {
                if (_instance == null) {
                    _instance = (T)FindObjectOfType(typeof(T));
                    if (FindObjectsOfType(typeof(T)).Length > 1) {
                        Debug.LogError("[Singleton] Something went really wrong - there should never be more than 1 singleton. Reopening the scene might fix it.");
                        return _instance;
                    }

                    if (_instance == null) {
                        GameObject singleton = new GameObject();
                        _instance = singleton.AddComponent<T>();
                        singleton.name = "(singleton) " + typeof(T).ToString();

                        DontDestroyOnLoad(singleton);

                        Debug.Log("[Singleton] An instance of " + typeof(T) + " is needed in the scene, so " + singleton + " was created with DontDestroyOnLoad.");
                        Debug.Log("Note that this creation does NOT use a prefab, so if you have a particular configuration for the singleton as part of a prefab, you must instead load that yourself.");
                    } else {
                        Debug.Log("[Singleton] Using instance already created: " + _instance.gameObject.name);
                    }
                }
                return _instance;
            }
        }
    }


    //void Awake () {
   //if(i == null) {
     //i = this;
     //DontDestroyOnLoad(gameObject);
   //}
   //else Destroy(this); // or gameObject
//}

    private static bool applicationIsQuitting = false;

    // When Unity quits, it destroys objects in a random order.
	// In principle, a Singleton is only destroyed when application quits.
	// If any script calls Instance after it have been destroyed,
	//   it will create a buggy ghost object that will stay on the Editor scene
	//   even after stopping playing the Application. Really bad!
	// So, this was made to be sure we're not creating that buggy ghost object.
    public void OnDestroy() {
        applicationIsQuitting = true;
    }
}
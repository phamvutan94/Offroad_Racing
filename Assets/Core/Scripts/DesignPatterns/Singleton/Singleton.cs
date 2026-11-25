using UnityEngine;

namespace Core.DesignPatterns.Singleton
{

    public class Singleton<T> : MonoBehaviour where T : Component
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = (T)FindFirstObjectByType(typeof(T));

                    if (_instance == null)
                    {
                        Debug.LogError("[Singleton] \" + typeName null");
                    }
                    else
                    {
                        string typeName = typeof(T).Name;

                        GameLog.Log("[Singleton] " + typeName + " Instance already created: " +
                            _instance.gameObject.name);
                    }
                }

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            RemoveDuplicates();
        }


        private void RemoveDuplicates()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
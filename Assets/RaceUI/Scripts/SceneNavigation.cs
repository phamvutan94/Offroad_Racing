using UnityEngine;
using UnityEngine.SceneManagement;

namespace RaceUI
{
    /// <summary>Scene changes go through the persistent loading screen (falls back to a direct load if Boot never ran).</summary>
    public static class SceneNavigation
    {
        public const string GarageScene = "Garage";

        public static void Load(string sceneName, Sprite preview = null, string label = null, Sprite title = null)
        {
            Time.timeScale = 1f;
            if (SceneLoadingService.Instance != null) SceneLoadingService.Instance.Load(sceneName, preview, label, title);
            else SceneManager.LoadScene(sceneName);
        }

        public static void ReloadActive(Sprite preview = null, string label = null) => Load(SceneManager.GetActiveScene().name, preview, label);
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

namespace RaceUI
{
    /// <summary>
    /// Boot scene entry point. The managers in the Boot scene (audio, loading screen) make themselves persistent;
    /// this then opens the first game scene through the loading screen.
    /// In the Editor, playing any other scene loads Boot additively first (see EnsureBootLoaded) so the managers
    /// always exist; in that case Boot just unloads itself and leaves the current scene running.
    /// </summary>
    public sealed class GameBoot : MonoBehaviour
    {
        public const string BootSceneName = "Boot";

        [SerializeField] private SceneLoadingService loadingService;
        [SerializeField] private string firstScene = "Garage";

        private static bool loadedAdditively;

        private void Start()
        {
            if (loadedAdditively)
            {
                loadedAdditively = false;
                SceneManager.UnloadSceneAsync(gameObject.scene);
                return;
            }
            loadingService.Load(firstScene);
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureBootLoaded()
        {
            if (SceneManager.GetActiveScene().name == BootSceneName) return;
            loadedAdditively = true;
            SceneManager.LoadScene(BootSceneName, LoadSceneMode.Additive);
        }
#endif
    }
}

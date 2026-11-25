using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;


	[InitializeOnLoad]
	public static class SceneAutoLoader
	{
		private const string EditorPrefLoadEntryOnPlayKey = "SceneAutoLoader.LoadEntryOnPlay";
		private const string EditorPrefEntrySceneKey = "SceneAutoLoader.EntryScene";
		private const string EditorPrefPreviousSceneKey = "SceneAutoLoader.PreviousScene";

		static SceneAutoLoader()
		{
			EditorApplication.playModeStateChanged += OnPlayModeChanged;
		}

		[MenuItem("File/Scene Autoload/Select Entry Scene...")]
		private static void SelectEntryScene()
		{
			var masterScene = EditorUtility.OpenFilePanel("Select Entry Scene", Application.dataPath, "unity");
			masterScene = masterScene.Replace(Application.dataPath, "Assets");
			if (string.IsNullOrEmpty(masterScene) == false)
			{
				EntryScene = masterScene;
				LoadEntryOnPlay = true;
			}
		}

		[MenuItem("File/Scene Autoload/Load Entry On Play", true)]
		private static bool ShowLoadEntryOnPlay()
		{
			return LoadEntryOnPlay == false;
		}
		[MenuItem("File/Scene Autoload/Load Entry On Play")]
		private static void EnableLoadEntryOnPlay()
		{
			LoadEntryOnPlay = true;
		}

		[MenuItem("File/Scene Autoload/Don't Load Entry On Play", true)]
		private static bool ShowDontLoadEntryOnPlay()
		{
			return LoadEntryOnPlay;
		}
		[MenuItem("File/Scene Autoload/Don't Load Entry On Play")]
		private static void DisableLoadEntryOnPlay()
		{
			LoadEntryOnPlay = false;
		}

		private static void OnPlayModeChanged(PlayModeStateChange state)
		{
			if (LoadEntryOnPlay == false)
			{
				return;
			}

			if (EditorApplication.isPlaying == false && EditorApplication.isPlayingOrWillChangePlaymode == true)
			{
				PreviousScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
				if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == true)
				{
					try
					{
						UnityEditor.SceneManagement.EditorSceneManager.OpenScene(EntryScene);
					}
					catch
					{
						Debug.LogError($"error: scene not found: {EntryScene}");
						EditorApplication.isPlaying = false;

					}
				}
				else
				{
					EditorApplication.isPlaying = false;
				}
			}

			if (EditorApplication.isPlaying == false && EditorApplication.isPlayingOrWillChangePlaymode == false)
			{
				try
				{
					UnityEditor.SceneManagement.EditorSceneManager.OpenScene(PreviousScene);
				}
				catch
				{
					Debug.LogError($"error: scene not found: {PreviousScene}");
				}
			}
		}

		private static bool LoadEntryOnPlay
		{
			get
			{
				return EditorPrefs.GetBool(EditorPrefLoadEntryOnPlayKey, false);
			}
			set
			{
				EditorPrefs.SetBool(EditorPrefLoadEntryOnPlayKey, value);
			}
		}

		private static string EntryScene
		{
			get
			{
				//return EditorPrefs.GetString(EditorPrefEntrySceneKey, "Assets/Scenes/Entry.unity");
				return SceneUtility.GetScenePathByBuildIndex(0);
			}
			set
			{
				EditorPrefs.SetString(EditorPrefEntrySceneKey, value);
			}
		}

		private static string PreviousScene
		{
			get
			{
				return EditorPrefs.GetString(EditorPrefPreviousSceneKey, UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
			}
			set
			{
				EditorPrefs.SetString(EditorPrefPreviousSceneKey, value);
			}
		}
	}

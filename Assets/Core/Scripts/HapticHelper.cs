
using UnityEngine;

namespace Core
{
    public static class HapticHelper
    {
        public static void VibrateLight()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        VibrateAndroid(35);
#else
            Handheld.Vibrate();
#endif
        }

        public static void VibrateMedium()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        VibrateAndroid(60);
#else
            Handheld.Vibrate();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void VibrateAndroid(long milliseconds)
    {
        try
        {
            using (AndroidJavaClass unityPlayer =
                   new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject activity =
                       unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (activity == null)
                    {
                        Handheld.Vibrate();
                        return;
                    }

                    using (AndroidJavaClass buildVersion =
                           new AndroidJavaClass("android.os.Build$VERSION"))
                    {
                        int sdkInt = buildVersion.GetStatic<int>("SDK_INT");

                        AndroidJavaObject vibrator = null;

                        try
                        {
                            if (sdkInt >= 31)
                            {
                                using (AndroidJavaObject vibratorManager =
                                       activity.Call<AndroidJavaObject>(
                                           "getSystemService",
                                           "vibrator_manager"))
                                {
                                    if (vibratorManager != null)
                                    {
                                        vibrator = vibratorManager.Call<AndroidJavaObject>(
                                            "getDefaultVibrator");
                                    }
                                }
                            }
                            else
                            {
                                vibrator = activity.Call<AndroidJavaObject>(
                                    "getSystemService",
                                    "vibrator");
                            }

                            if (vibrator == null)
                            {
                                Handheld.Vibrate();
                                return;
                            }

                            bool hasVibrator = vibrator.Call<bool>("hasVibrator");

                            if (!hasVibrator)
                            {
                                return;
                            }

                            if (sdkInt >= 26)
                            {
                                using (AndroidJavaClass vibrationEffect =
                                       new AndroidJavaClass("android.os.VibrationEffect"))
                                {
                                    int defaultAmplitude =
                                        vibrationEffect.GetStatic<int>("DEFAULT_AMPLITUDE");

                                    using (AndroidJavaObject effect =
                                           vibrationEffect.CallStatic<AndroidJavaObject>(
                                               "createOneShot",
                                               milliseconds,
                                               defaultAmplitude))
                                    {
                                        vibrator.Call("vibrate", effect);
                                    }
                                }
                            }
                            else
                            {
                                vibrator.Call("vibrate", milliseconds);
                            }
                        }
                        finally
                        {
                            if (vibrator != null)
                            {
                                vibrator.Dispose();
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Android vibration failed: " + e.Message);
            Handheld.Vibrate();
        }
    }
#endif
    }
}
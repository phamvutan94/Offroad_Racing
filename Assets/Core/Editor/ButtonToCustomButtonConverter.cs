using Core;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ButtonToCustomButtonConverter
{
    [MenuItem("Tools/UI/Convert Selected Prefabs Button To CustomButton")]
    private static void ConvertSelectedPrefabs()
    {
        Object[] selectedObjects = Selection.objects;

        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("Bạn chưa chọn prefab nào.");
            return;
        }

        MonoScript customButtonScript = GetCustomButtonMonoScript();

        if (customButtonScript == null)
        {
            Debug.LogError(
                "Không tìm thấy MonoScript của CustomButton.\n" +
                "Kiểm tra:\n" +
                "- Class phải là public class CustomButton : Button\n" +
                "- File nên tên là CustomButton.cs\n" +
                "- File CustomButton.cs không được nằm trong thư mục Editor\n" +
                "- Unity không có lỗi compile"
            );
            return;
        }

        int totalConverted = 0;

        foreach (Object selectedObject in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(selectedObject);

            if (string.IsNullOrEmpty(path))
                continue;

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefabAsset == null)
                continue;

            if (PrefabUtility.GetPrefabAssetType(prefabAsset) == PrefabAssetType.NotAPrefab)
                continue;

            int converted = ConvertPrefabAtPath(path, customButtonScript);
            totalConverted += converted;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Convert xong. Tổng số Button đã đổi: {totalConverted}");
    }

    private static int ConvertPrefabAtPath(string prefabPath, MonoScript customButtonScript)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        int convertedCount = 0;

        try
        {
            Button[] buttons = prefabRoot.GetComponentsInChildren<Button>(true);

            // Copy sang list để tránh lỗi khi component bị Unity replace
            List<Button> buttonList = new List<Button>(buttons);

            foreach (Button button in buttonList)
            {
                if (button == null)
                    continue;

                // Bỏ qua nếu đã là CustomButton
                if (button.GetType() == typeof(CustomButton))
                    continue;

                // Chỉ đổi đúng UnityEngine.UI.Button gốc
                // Không đổi các class khác đang kế thừa Button
                if (button.GetType() != typeof(Button))
                    continue;

                bool success = ReplaceButtonScript(button, customButtonScript);

                if (success)
                    convertedCount++;
            }

            if (convertedCount > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                Debug.Log($"Converted {convertedCount} Button trong prefab: {prefabPath}");
            }
            else
            {
                Debug.Log($"Không có Button nào cần convert trong prefab: {prefabPath}");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        return convertedCount;
    }

    private static bool ReplaceButtonScript(Button oldButton, MonoScript customButtonScript)
    {
        if (oldButton == null)
            return false;

        // Phải lưu lại trước.
        // Sau khi đổi m_Script, object oldButton sẽ bị Unity invalid/destroy.
        GameObject owner = oldButton.gameObject;
        string objectName = owner != null ? owner.name : "Unknown";

        SerializedObject serializedObject = new SerializedObject(oldButton);
        serializedObject.Update();

        SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");

        if (scriptProperty == null)
        {
            Debug.LogError($"Không tìm thấy m_Script ở object: {objectName}");
            return false;
        }

        scriptProperty.objectReferenceValue = customButtonScript;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        // Không được dùng oldButton sau dòng ApplyModifiedPropertiesWithoutUndo.
        // oldButton lúc này có thể đã bị Unity destroy.
        if (owner != null)
        {
            EditorUtility.SetDirty(owner);
        }

        Debug.Log($"Converted Button -> CustomButton: {objectName}");

        return true;
    }

    private static MonoScript GetCustomButtonMonoScript()
    {
        string[] guids = AssetDatabase.FindAssets("CustomButton t:MonoScript");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            MonoScript monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path);

            if (monoScript == null)
                continue;

            System.Type scriptClass = monoScript.GetClass();

            if (scriptClass == typeof(CustomButton))
            {
                return monoScript;
            }
        }

        return null;
    }
}
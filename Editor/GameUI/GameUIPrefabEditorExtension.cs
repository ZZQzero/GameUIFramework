using GameUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class GameUIPrefabEditorExtension
{
    static GameUIPrefabEditorExtension()
    {
        Selection.selectionChanged += OnSelectionChanged;
    }

    private static void OnSelectionChanged()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null) return;

        // 检查选中的对象是否是UI元素（拥有RectTransform组件）
        RectTransform rectTransform = selected.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // 只对Project窗口中的Prefab资源本身进行操作
            bool isPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(selected);
            
            if (isPrefabAsset)
            {
                // 检查是否已经挂载了GameUIPrefab组件
                if (selected.GetComponent<GameUIPrefab>() == null)
                {
                    // 为Prefab资源添加组件
                    Undo.AddComponent<GameUIPrefab>(selected);
                    // 刷新Inspector显示
                    EditorUtility.SetDirty(selected);
                }
            }
        }
    }
}
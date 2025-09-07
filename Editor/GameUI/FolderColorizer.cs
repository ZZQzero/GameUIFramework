using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

[InitializeOnLoad]
public class FolderColorizer
{
    // --- 1. 在这里配置您想要的颜色 ---
    private static readonly Dictionary<string, Color> FolderColors = new Dictionary<string, Color>
    {
        { "Assets/StreamingAssets", Color.blueViolet },
        { "Assets/Editor", Color.cornflowerBlue },
        { "Assets/Scenes", Color.darkOrange },
        { "Assets/Plugins", Color.blueViolet },
        { "Assets/Scripts", Color.cyan },
        { "Assets/Scripts/Core", Color.red },
        { "Assets/Scripts/Editor", Color.cornflowerBlue },
        { "Assets/Scripts/GameEntry", Color.green },
        { "Assets/Scripts/Hotfix", Color.green },
        { "Assets/Scripts/HotfixView", Color.green },
        { "Assets/Scripts/Loader", Color.red },
        { "Assets/Scripts/Model", Color.green },
        { "Assets/Scripts/ModelView", Color.green },
        { "Assets/Scripts/ThirdParty", Color.red },
        { "Assets/Config", Color.aquamarine },
        { "Assets/Resources", Color.bisque },
    };

    // --- 2. 在这里配置您想要的文件夹说明 ---
    private static readonly Dictionary<string, string> FolderDescriptions = new Dictionary<string, string>
    {
        { "Assets/Scripts", "代码文件" },
        { "Assets/Config", "配置文件" },
        { "Assets/Resources", "持久化数据配置文件" },
        { "Assets/Scripts/Core", "框架核心代码（非热更）" },
        { "Assets/Scripts/Editor", "编辑器扩展文件" },
        { "Assets/Scripts/GameEntry", "游戏开始主入口文件（可热更）" },
        { "Assets/Scripts/Hotfix", "Model的System实现（可热更）" },
        { "Assets/Scripts/HotfixView", "ModelView的System实现（可热更）" },
        { "Assets/Scripts/Loader", "游戏启动入口（非热更）" },
        { "Assets/Scripts/Model", "数据层（可热更）" },
        { "Assets/Scripts/ModelView", "UI，资源相关数据层（可热更）" },
        { "Assets/Scripts/ThirdParty", "第三方插件（非热更）" },
        { "Assets/Editor", "编辑器脚本" },
        { "Assets/Scenes", "游戏入口/启动器场景" },
        { "Assets/Plugins", "原生插件或第三方库" },
        { "Assets/StreamingAssets", "首包引用库" },
    };
    // ------------------------------------

    // 缓存说明文本的样式，避免重复创建
    private static GUIStyle _descriptionStyle;

    static FolderColorizer()
    {
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
    }

    private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);

        // 如果路径无效或不是文件夹，则跳过
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            return;
        }

        // --- 功能一：绘制文件夹颜色 ---
        if (FolderColors.TryGetValue(path, out Color color))
        {
            DrawFolderColor(selectionRect, color);
        }

        // --- 功能二：在文件夹名称右侧绘制说明 ---
        if (FolderDescriptions.TryGetValue(path, out string description))
        {
            DrawFolderDescription(selectionRect, description);
        }
    }

    /// <summary>
    /// 绘制文件夹颜色高亮
    /// </summary>
    private static void DrawFolderColor(Rect rect, Color color)
    {
        Rect backgroundRect = rect;

        // 两列视图 (图标大)
        if (rect.height > 20f)
        {
            backgroundRect.width = backgroundRect.height;
        }
        // 单列视图 (列表)
        else
        {
            backgroundRect.x += 16f; // 空出图标位置
            backgroundRect.width -= 16f;
        }

        // 绘制半透明背景
        Color backgroundColor = color;
        backgroundColor.a = 0.4f;
        EditorGUI.DrawRect(backgroundRect, backgroundColor);

        // 绘制左侧竖线
        /*Rect lineRect = new Rect(rect.x, rect.y, 3, rect.height);
        EditorGUI.DrawRect(lineRect, color);*/
    }

    /// <summary>
    /// 在项的矩形区域内，靠右绘制说明文字
    /// </summary>
    private static void DrawFolderDescription(Rect rect, string description)
    {
        // 初始化样式 (只在第一次时创建)
        if (_descriptionStyle == null)
        {
            _descriptionStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight, // 右对齐
                fontStyle = FontStyle.Italic,
                normal = { textColor = Color.white }
            };
        }

        // 在单列视图下，为了不和可能存在的文件大小、类型等信息重叠，我们稍微向左移动
        if (rect.height <= 20f)
        {
            rect.x -= 70; // 向左偏移，给右侧的其它信息留出空间
        }

        // 增加一点右边距
        rect.x -= 5;

        // 绘制带括号的说明文字
        GUI.Label(rect, $"({description})", _descriptionStyle);
    }
}
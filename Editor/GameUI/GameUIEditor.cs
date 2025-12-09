using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using GameUI;
using GameUI.Editor;
using UnityEditor.Callbacks;
using UnityEngine.UIElements;

[CustomEditor(typeof(GameUIPrefab))]
public class GameUIEditor : Editor
{

    public static string PrefabPathKey = "PrefabPathKey";
    public static string ClassNameKey = "ClassNameKey";
    public static string AutoAddCSKey = "AutoAddCSKey";
    public static string PropertyNamesKey = "PropertyNamesKey"; // 保存用户修改的属性名
    
    // 全局路径配置 Key
    public static string GlobalComponentPathKey = "GlobalComponentPathKey";
    public static string GlobalPanelPathKey = "GlobalPanelPathKey";
    public static string GlobalPanelNamePathKey = "GlobalPanelNamePathKey";
    public static string GlobalItemPathKey = "GlobalItemPathKey";

    private static string _prefabPath;
    private string fileStr;
    private static GameObject _currentObj;
    private static GameUICreateFile _createFile;

    private static bool isCreateFile = false;
    private bool isPrefabRename = false;
    private bool anewFile = false;
    private Vector2 _scrollPos;
    
    private GUIStyle _fileStyle = new GUIStyle();
    private GUIStyle _guiStyle = new GUIStyle();
    private GUIStyle _errorStyle = new GUIStyle();
    private GUIStyle _warningStyle = new GUIStyle();
    private ErrorType _errorType = ErrorType.None;
    
    // 新增：搜索和过滤
    private string _searchText = "";
    private bool _showOnlySelected = false;
    private int _selectedNodeIndex = -1; // 下拉列表选中的节点索引（-1表示未选中）
    
    // 重命名功能相关
    private string _renameSearchText = "";
    private string _batchReplaceFrom = "";
    private string _batchReplaceTo = "";
    private string _batchPrefix = "";
    private string _batchSuffix = "";
    
    public enum ErrorType
    {
        None = 0,
        OptionError,
        PropertyError,
        AssetError,
        SelectNodeError,
    }
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        ShowBtn();
        ShowError();
        if (_createFile == null)
        {
            return;
        }
        
        if(isPrefabRename)
        {
            ShowPrefabNode();
        }
        
        if (isCreateFile)
        {
            var obj = (GameUIPrefab)target;
            if (_currentObj != null && _currentObj != obj.gameObject)
            {
                _createFile = null;
                _currentObj = null;
                _prefabPath = null;
                isCreateFile = false;
                _errorType = ErrorType.None;
                return;
            }
            ShowPrefabComponent();
        }
    }

    [DidReloadScripts]
    private static void OnScriptUpdateLoaded()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(AutoAddCSKey))
            {
                _prefabPath = EditorPrefs.GetString(PrefabPathKey);
                if (_currentObj == null && !string.IsNullOrEmpty(_prefabPath))
                {
                    _currentObj = PrefabUtility.LoadPrefabContents(_prefabPath);
                }
                if (_createFile == null)
                {
                    _createFile =  new GameUICreateFile();
                }
                _createFile.Init(_currentObj.transform);
                // 加载保存的路径
                LoadSavedPaths(_createFile);
                
                // 恢复用户修改的属性名
                RestorePropertyNameMappingInternal(_createFile, _currentObj);
                _createFile.AddScriptToPrefab(_currentObj);
                PrefabUtility.SaveAsPrefabAssetAndConnect(_currentObj, _prefabPath, InteractionMode.AutomatedAction);
                _createFile = null;
                _currentObj = null;
                _prefabPath = null;
                isCreateFile = false;
                // 清除EditorPrefs，避免下次误用
                EditorPrefs.DeleteKey(PrefabPathKey);
                EditorPrefs.DeleteKey(ClassNameKey);
                EditorPrefs.DeleteKey(AutoAddCSKey);
                EditorPrefs.DeleteKey(PropertyNamesKey);
                AssetDatabase.Refresh();
                Debug.Log("✅ 生成代码成功并自动绑定引用");
            }
        };
    }

    private void ShowError()
    {
        switch (_errorType)
        {
            case ErrorType.None:
                break;
            case ErrorType.OptionError:
                EditorGUILayout.HelpBox("请先完成或者取消上一项操作！！！", MessageType.Error);
                break;
            case ErrorType.PropertyError:
                EditorGUILayout.HelpBox("有重复的属性名字！！！", MessageType.Error);
                break;
            case ErrorType.AssetError:
                EditorGUILayout.HelpBox($"资源路劲获取失败！！！---> {target.name}", MessageType.Error);
                break;
            case ErrorType.SelectNodeError:
                EditorGUILayout.HelpBox($"是否选中UI根节点，是否正确标记UI！！！---> {target.name}", MessageType.Error);
                break;
        }
    }
    
    private void ShowPrefabComponent()
    {
        if (_currentObj == null)
        {
            _createFile = null;
            _prefabPath = null;
            EditorPrefs.DeleteAll();
            return;
        }
        _createFile.Init(_currentObj.transform);
        LoadSavedPaths(_createFile);
        
        fileStr = _createFile.CheckPropertyExists();
        bool isItemPrefab = _createFile.IsItemPrefab;
        
        
        _fileStyle.normal.textColor = Color.green;
        EditorGUILayout.BeginVertical();

        // 显示统计信息
        int totalNodes = _createFile.ComponentDataList?.Count ?? 0;
        int totalComponents = 0;
        int selectedComponents = 0;
        if (_createFile.ComponentDataList != null)
        {
            foreach (var item in _createFile.ComponentDataList)
            {
                totalComponents += item.ComponentList?.Count ?? 0;
                if (item.ComponentList != null)
                {
                    selectedComponents += item.ComponentList.Count(c => c.IsSelect);
                }
            }
        }
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"识别节点数：{totalNodes}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"待生成属性：{selectedComponents}/{totalComponents}", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);

        if (isItemPrefab)
        {
            EditorGUILayout.LabelField("生成模式：Cell/Item (MonoBehaviour)", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            DrawPathSelection("脚本生成路径：", _createFile.ComponentCodeGeneratePath, p => _createFile.ComponentCodeGeneratePath = p);
            //EditorGUILayout.LabelField("脚本生成路径：", _createFile.ComponentCodeGeneratePath, _fileStyle);
            EditorGUILayout.LabelField("脚本名字：", _createFile.ComponentFileName, _fileStyle);
            EditorGUILayout.Space(10);
        }
        else
        {
            EditorGUILayout.LabelField("生成模式：面板 (GameUIBase)", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            DrawPathSelection("代码生成路径1：", _createFile.ComponentCodeGeneratePath, p => _createFile.ComponentCodeGeneratePath = p);
            //EditorGUILayout.LabelField("代码生成路径1：", _createFile.ComponentCodeGeneratePath, _fileStyle);
            EditorGUILayout.LabelField("脚本名字1：", _createFile.ComponentFileName, _fileStyle);
            EditorGUILayout.Space(10);
            DrawPathSelection("代码生成路径2：", _createFile.PanelCodeGeneratePath, p => _createFile.PanelCodeGeneratePath = p);
            //EditorGUILayout.LabelField("代码生成路径2：", _createFile.PanelCodeGeneratePath, _fileStyle);
            EditorGUILayout.LabelField("脚本名字2：", _createFile.PanelFileName, _fileStyle);
            EditorGUILayout.Space(10);
            DrawPathSelection("代码生成路径3：", _createFile.PanelNameCodeGeneratePath, p => _createFile.PanelNameCodeGeneratePath = p);
            //EditorGUILayout.LabelField("代码生成路径3：", _createFile.PanelNameCodeGeneratePath, _fileStyle);
            EditorGUILayout.LabelField("脚本名字3：", _createFile.StaticCSFileName, _fileStyle);
        }

        _warningStyle.normal.textColor = Color.yellow;
        anewFile = EditorGUILayout.ToggleLeft("重新生成！（清空原有的代码，慎用！）", anewFile, _warningStyle);
        
        // 提示：属性名修改说明
        GUIStyle infoStyle = new GUIStyle(EditorStyles.helpBox);
        infoStyle.normal.textColor = new Color(0.6f, 0.9f, 1.0f);
        EditorGUILayout.HelpBox("💡 提示：修改属性名后，系统会自动保存并在编译后恢复，确保引用绑定正确", MessageType.Info);
        if (isItemPrefab)
        {
            EditorGUILayout.HelpBox("📦 当前预制体名称以 Cell/Item 结尾，将生成继承 MonoBehaviour 的脚本，并在 #region 区域内自动维护字段。", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
        
        // 批量操作按钮区域
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("批量操作", EditorStyles.boldLabel);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("全选", GUILayout.Height(30)))
        {
            _createFile.SelectAllComponents();
        }
        if (GUILayout.Button("全不选", GUILayout.Height(30)))
        {
            _createFile.DeselectAllComponents();
        }
        GUILayout.EndHorizontal();
        
        // 按类型选择
        EditorGUILayout.LabelField("按类型选择：", EditorStyles.miniLabel);
        var componentTypes = _createFile.GetAllComponentTypes();
        
        // 分两行显示按钮
        int buttonsPerRow = 4;
        for (int i = 0; i < componentTypes.Count; i += buttonsPerRow)
        {
            GUILayout.BeginHorizontal();
            for (int j = i; j < Mathf.Min(i + buttonsPerRow, componentTypes.Count); j++)
            {
                string typeName = componentTypes[j];
                if (GUILayout.Button(typeName, GUILayout.Height(25)))
                {
                    _createFile.SelectComponentsByType(typeName);
                }
            }
            GUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space(10);
        
        // 搜索和过滤功能
        EditorGUILayout.LabelField("搜索和过滤", EditorStyles.boldLabel);
        
        // 快速跳转下拉列表
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("快速跳转：", GUILayout.Width(80));
        if (_createFile.ComponentDataList != null && _createFile.ComponentDataList.Count > 0)
        {
            // 添加一个"无选择"选项
            string[] nodeNames = new string[_createFile.ComponentDataList.Count + 1];
            nodeNames[0] = "-- 选择节点 --";
            for (int i = 0; i < _createFile.ComponentDataList.Count; i++)
            {
                var item = _createFile.ComponentDataList[i];
                int selectedCount = 0;
                foreach (var comp in item.ComponentList)
                {
                    if (comp.IsSelect) selectedCount++;
                }
                // 只显示节点名称，替换 "/" 为 "-" 避免被Unity识别为子菜单
                string nodeName = item.Item.name.Replace("/", "-");
                nodeNames[i + 1] = $"{nodeName} ({selectedCount}:{item.ComponentList.Count})";
            }
            
            // 显示索引需要+1（因为有"无选择"选项）
            int displayIndex = _selectedNodeIndex + 1;
            int newDisplayIndex = EditorGUILayout.Popup(displayIndex, nodeNames, GUILayout.Width(250));
            int newIndex = newDisplayIndex - 1; // 转换回实际索引
            
            if (newIndex != _selectedNodeIndex)
            {
                _selectedNodeIndex = newIndex;
                if (_selectedNodeIndex >= 0)
                {
                    // 展开选中的节点
                    _createFile.ComponentDataList[_selectedNodeIndex].IsFoldout = true;
                    // 清空搜索和过滤，确保能看到目标节点
                    _searchText = "";
                    _showOnlySelected = false;
                }
            }
        }
        GUILayout.EndHorizontal();
        
        // 搜索框
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("搜索节点名：", GUILayout.Width(80));
        _searchText = EditorGUILayout.TextField(_searchText, GUILayout.Width(200));
        if (GUILayout.Button("清空", GUILayout.Width(50)))
        {
            _searchText = "";
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        _showOnlySelected = EditorGUILayout.ToggleLeft("只显示已勾选的组件", _showOnlySelected);
        if (GUILayout.Button("全部展开", GUILayout.Width(80)))
        {
            foreach (var item in _createFile.ComponentDataList)
            {
                item.IsFoldout = true;
            }
        }
        if (GUILayout.Button("全部折叠", GUILayout.Width(80)))
        {
            foreach (var item in _createFile.ComponentDataList)
            {
                item.IsFoldout = false;
            }
        }
        GUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);

        GUILayout.BeginHorizontal(); //1
        EditorGUILayout.Space(10);

        _scrollPos = GUILayout.BeginScrollView(_scrollPos); //2
        if (_createFile.ComponentDataList is { Count: > 0 })
        {
            int displayIndex = 0;
            for (int i = 0; i < _createFile.ComponentDataList.Count; i++)
            {
                var item = _createFile.ComponentDataList[i];
                
                // 搜索过滤
                if (!string.IsNullOrEmpty(_searchText) && 
                    !item.Item.name.ToLower().Contains(_searchText.ToLower()))
                {
                    continue;
                }
                
                // 只显示已勾选的过滤
                if (_showOnlySelected)
                {
                    bool hasSelected = false;
                    foreach (var comp in item.ComponentList)
                    {
                        if (comp.IsSelect)
                        {
                            hasSelected = true;
                            break;
                        }
                    }
                    if (!hasSelected) continue;
                }
                
                // 判断是否是通过下拉列表选中的节点
                bool isSelectedNode = (_selectedNodeIndex >= 0 && i == _selectedNodeIndex);
                
                // 如果是选中的节点，使用高亮样式
                GUIStyle boxStyle = isSelectedNode ? new GUIStyle(EditorStyles.helpBox) 
                {
                    normal = { background = MakeColorTexture(new Color(0.2f, 0.6f, 1.0f, 0.5f)) }
                } : EditorStyles.helpBox;
                
                GUILayout.BeginVertical(boxStyle); //3
                
                // 节点标题行（带折叠按钮）
                GUILayout.BeginHorizontal();
                item.IsFoldout = EditorGUILayout.Foldout(item.IsFoldout, "", true);
                
                // 显示节点名称（使用明显的青色，包含层级路径）
                GUIStyle nodeNameStyle = new GUIStyle(EditorStyles.boldLabel);
                nodeNameStyle.normal.textColor = new Color(0.2f, 1.0f, 0.8f); // 明亮的青色
                
                // 生成层级路径
                string nodePath = GetNodePath(item.Item, _currentObj.transform);
                EditorGUILayout.LabelField(nodePath, nodeNameStyle, GUILayout.Width(300));
                
                // 显示该节点已选中组件数量
                int selectedCount = 0;
                int totalCount = item.ComponentList.Count;
                foreach (var comp in item.ComponentList)
                {
                    if (comp.IsSelect) selectedCount++;
                }
                EditorGUILayout.LabelField($"({selectedCount}:{totalCount})", GUILayout.Width(60));
                
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("全选节点", GUILayout.Width(80)))
                {
                    _createFile.SetNodeComponentsSelection(item, true);
                }
                if (GUILayout.Button("全不选", GUILayout.Width(80)))
                {
                    _createFile.SetNodeComponentsSelection(item, false);
                }
                
                GUILayout.EndHorizontal();
                
                // 只有展开时才显示组件列表
                if (item.IsFoldout)
                {
                    GUILayout.Space(5);
                    foreach (var component in item.ComponentList)
                    {
                        // 只显示已勾选的过滤
                        if (_showOnlySelected && !component.IsSelect)
                        {
                            continue;
                        }
                    GUILayout.BeginHorizontal(); //5
                    if (component.IsSelect && !component.IsError)
                    {
                        _guiStyle.normal.textColor = Color.green;
                    }
                    else if (component.IsError)
                    {
                        _guiStyle.normal.textColor = Color.red;
                    }
                    else
                    {
                        _guiStyle.normal.textColor = Color.white;
                    }

                    if (string.IsNullOrEmpty(component.ComponentPath))
                    {
                        component.ComponentPath = _createFile.GetChildPath(item.Item, item.Root);
                        component.ComponentRootPath = _createFile.GetChildPath(item.Item, _currentObj.transform);
                    }
                    GUILayoutOption option1 = GUILayout.Width(200);
                    if (component.ComponentType != null)
                    {
                        EditorGUILayout.LabelField(component.ComponentType, _guiStyle, option1);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(component.GameObjectName, _guiStyle, option1);
                    }
                    //EditorGUILayout.Space(5);
                    component.PropertyName = EditorGUILayout.TextField(component.PropertyName,option1);

                    //EditorGUILayout.Space(5);
                    if (!string.IsNullOrEmpty(fileStr) && !component.HasSyncedSelection)
                    {
                        string typeName = component.ComponentType;
                        if (string.IsNullOrEmpty(typeName) && component.GameObject != null)
                        {
                            typeName = component.GameObject.GetType().ToString();
                        }

                        if (!string.IsNullOrEmpty(typeName) && !string.IsNullOrEmpty(component.PropertyName) &&
                            fileStr.Contains(typeName) && fileStr.Contains(component.PropertyName))
                        {
                            component.IsSelect = true;
                        }

                        component.HasSyncedSelection = true;
                    }
                    component.IsSelect = EditorGUILayout.Toggle(component.IsSelect);
                    GUILayout.EndHorizontal(); //5
                    }
                }
                
                GUILayout.EndVertical(); //3
                
                displayIndex++;
            }
        }
        else
        {
            _errorType = ErrorType.SelectNodeError;
        }

        GUILayout.EndScrollView(); //2

        GUILayout.EndHorizontal(); //1
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("确认",GUILayout.Height(50)))
        {
            if (_createFile == null)
            {
                return;
            }

            _errorType = ErrorType.None;
            if (!string.IsNullOrEmpty(_prefabPath))
            {
                EditorPrefs.SetString(PrefabPathKey, _prefabPath);
                string classNameForPref = _createFile.IsItemPrefab ? _createFile.GeneratedClassName : _createFile.PanelFileName.Replace(".cs", "");
                EditorPrefs.SetString(ClassNameKey,classNameForPref);
                EditorPrefs.SetBool(AutoAddCSKey,true);
                
                // 保存用户修改的属性名映射（节点路径 → 属性名）
                SavePropertyNameMapping();
            }
            
            if (_createFile.CheckRepeatPropertyName())
            {
                _errorType = ErrorType.PropertyError;
            }
            else
            {
                SavePaths(_createFile);
                _createFile.StartGenerate(anewFile);
                AssetDatabase.Refresh();
            }
        }
        
        if (GUILayout.Button("取消",GUILayout.Height(50)))
        {
            _createFile = null;
            _currentObj = null;
            _prefabPath = null;
            isCreateFile = false;
            _errorType = ErrorType.None;
            _selectedNodeIndex = -1; // 重置选择
            // 清除保存的属性名映射
            EditorPrefs.DeleteKey(PropertyNamesKey);
            AssetDatabase.Refresh();
        }
        GUILayout.EndHorizontal();
    }

    private void ShowPrefabNode()
    {
        // 批量操作区域
        EditorGUILayout.LabelField("批量重命名操作", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // 批量替换
        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("批量替换", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("查找：", GUILayout.Width(60));
        _batchReplaceFrom = EditorGUILayout.TextField(_batchReplaceFrom, GUILayout.Width(120));
        EditorGUILayout.LabelField("替换为：", GUILayout.Width(60));
        _batchReplaceTo = EditorGUILayout.TextField(_batchReplaceTo, GUILayout.Width(120));
        if (GUILayout.Button("执行替换", GUILayout.Width(80)))
        {
            if (!string.IsNullOrEmpty(_batchReplaceFrom))
            {
                foreach (var item in _createFile.TransformList)
                {
                    if (item.name.Contains(_batchReplaceFrom))
                    {
                        item.name = item.name.Replace(_batchReplaceFrom, _batchReplaceTo);
                    }
                }
            }
        }
        GUILayout.EndHorizontal();
        EditorGUILayout.HelpBox("示例：查找\"@\"，替换为空，可批量移除@标记", MessageType.Info);
        GUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // 批量添加/移除前后缀
        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("批量前后缀操作", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("前缀：", GUILayout.Width(60));
        _batchPrefix = EditorGUILayout.TextField(_batchPrefix, GUILayout.Width(80));
        if (GUILayout.Button("批量添加", GUILayout.Width(80)))
        {
            if (!string.IsNullOrEmpty(_batchPrefix))
            {
                foreach (var item in _createFile.TransformList)
                {
                    if (!item.name.StartsWith(_batchPrefix))
                    {
                        item.name = _batchPrefix + item.name;
                    }
                }
            }
        }
        if (GUILayout.Button("批量移除", GUILayout.Width(80)))
        {
            if (!string.IsNullOrEmpty(_batchPrefix))
            {
                foreach (var item in _createFile.TransformList)
                {
                    if (item.name.StartsWith(_batchPrefix))
                    {
                        item.name = item.name.Substring(_batchPrefix.Length);
                    }
                }
            }
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("后缀：", GUILayout.Width(60));
        _batchSuffix = EditorGUILayout.TextField(_batchSuffix, GUILayout.Width(80));
        if (GUILayout.Button("批量添加", GUILayout.Width(80)))
        {
            if (!string.IsNullOrEmpty(_batchSuffix))
            {
                foreach (var item in _createFile.TransformList)
                {
                    if (!item.name.EndsWith(_batchSuffix))
                    {
                        item.name = item.name + _batchSuffix;
                    }
                }
            }
        }
        if (GUILayout.Button("批量移除", GUILayout.Width(80)))
        {
            if (!string.IsNullOrEmpty(_batchSuffix))
            {
                foreach (var item in _createFile.TransformList)
                {
                    if (item.name.EndsWith(_batchSuffix))
                    {
                        item.name = item.name.Substring(0, item.name.Length - _batchSuffix.Length);
                    }
                }
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // 搜索过滤
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("搜索节点：", GUILayout.Width(80));
        _renameSearchText = EditorGUILayout.TextField(_renameSearchText, GUILayout.Width(200));
        if (GUILayout.Button("清空", GUILayout.Width(50)))
        {
            _renameSearchText = "";
        }
        GUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        GUILayout.BeginHorizontal(); //1
        _scrollPos = GUILayout.BeginScrollView(_scrollPos); //2
        if (_createFile.TransformList is { Count: > 0 })
        {
            foreach (var item in _createFile.TransformList)
            {
                // 搜索过滤
                if (!string.IsNullOrEmpty(_renameSearchText) && 
                    !item.name.ToLower().Contains(_renameSearchText.ToLower()))
                {
                    continue;
                }
                
                GUILayout.BeginVertical(EditorStyles.helpBox); //3
                
                // 显示层级路径（青色）
                GUILayout.BeginHorizontal();
                GUIStyle pathStyle = new GUIStyle(EditorStyles.miniLabel);
                pathStyle.normal.textColor = new Color(0.2f, 1.0f, 0.8f); // 青色
                string nodePath = GetNodePath(item, _currentObj.transform);
                EditorGUILayout.LabelField($"📍 {nodePath}", pathStyle);
                GUILayout.EndHorizontal();
                
                GUILayout.Space(3);
                
                GUILayout.BeginHorizontal();
                GUILayoutOption option = GUILayout.Width(200);
                
                // 显示当前名称（只读，灰色）
                GUIStyle oldNameStyle = new GUIStyle(EditorStyles.label);
                oldNameStyle.normal.textColor = Color.gray;
                EditorGUILayout.LabelField("当前：", GUILayout.Width(40));
                EditorGUILayout.LabelField(item.name, oldNameStyle, option);
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                // 新名称输入框
                EditorGUILayout.LabelField("修改：", GUILayout.Width(40));
                string newName = EditorGUILayout.TextField(item.name, option);
                if (newName != item.name)
                {
                    item.name = newName;
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical(); //3
            }
        }
        GUILayout.EndScrollView(); //2
        GUILayout.EndHorizontal(); //1
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("保存",GUILayout.Height(50)))
        {
            EditorUtility.SetDirty(_currentObj);
            PrefabUtility.SaveAsPrefabAsset(_currentObj, _prefabPath);
            PrefabUtility.UnloadPrefabContents(_currentObj);
            AssetDatabase.SaveAssets();
            _createFile = null;
            _currentObj = null;
            _prefabPath = null;
            isPrefabRename = false;
            _errorType =  ErrorType.None;
            _renameSearchText = ""; // 重置搜索
            AssetDatabase.Refresh();
        }
        if (GUILayout.Button("取消",GUILayout.Height(50)))
        {
            PrefabUtility.UnloadPrefabContents(_currentObj);
            _createFile = null;
            _currentObj = null;
            _prefabPath = null;
            isPrefabRename = false;
            _errorType = ErrorType.None;
            _renameSearchText = ""; // 重置搜索
            AssetDatabase.Refresh();
        }
        GUILayout.EndHorizontal();
    }
    
    // 创建单色纹理（用于高亮背景）
    private Texture2D MakeColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
    
    // 获取节点的层级路径
    private string GetNodePath(Transform node, Transform root)
    {
        if (node == root)
        {
            return node.name;
        }
        
        System.Collections.Generic.List<string> pathParts = new System.Collections.Generic.List<string>();
        Transform current = node;
        
        while (current != null && current != root.parent)
        {
            pathParts.Add(current.name);
            current = current.parent;
        }
        
        pathParts.Reverse();
        return string.Join("/", pathParts);
    }
    
    // 保存用户修改的属性名映射
    private void SavePropertyNameMapping()
    {
        if (_createFile == null || _createFile.ComponentDataList == null)
            return;
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        bool hasCustomProperty = false;
        
        foreach (var item in _createFile.ComponentDataList)
        {
            foreach (var component in item.ComponentList)
            {
                if (!component.IsSelect)
                {
                    continue;
                }

                string originalName = component.OriginalPropertyName;
                if (string.IsNullOrEmpty(originalName))
                {
                    originalName = component.PropertyName;
                }

                if (string.Equals(component.PropertyName, originalName, StringComparison.Ordinal))
                {
                    continue;
                }

                // 确保ComponentRootPath已生成
                if (string.IsNullOrEmpty(component.ComponentRootPath))
                {
                    component.ComponentRootPath = _createFile.GetChildPath(item.Item, _currentObj.transform);
                }
                
                // 格式：节点路径|组件类型|属性名
                string line = $"{component.ComponentRootPath}|{component.ComponentType ?? "GameObject"}|{component.PropertyName}";
                sb.AppendLine(line);
                hasCustomProperty = true;
            }
        }
        
        if (hasCustomProperty)
        {
            EditorPrefs.SetString(PropertyNamesKey, sb.ToString());
        }
        else
        {
            if (EditorPrefs.HasKey(PropertyNamesKey))
            {
                EditorPrefs.DeleteKey(PropertyNamesKey);
            }
        }
    }
    
    // 恢复用户修改的属性名（支持static调用）
    private void RestorePropertyNameMapping()
    {
        RestorePropertyNameMappingInternal(_createFile, _currentObj);
    }
    
    private static void RestorePropertyNameMappingInternal(GameUICreateFile createFile, GameObject currentObj)
    {
        if (createFile == null || createFile.ComponentDataList == null || currentObj == null)
            return;
        
        string mappingData = EditorPrefs.GetString(PropertyNamesKey, "");
        if (string.IsNullOrEmpty(mappingData))
            return;
        
        try
        {
            // 解析映射数据
            System.Collections.Generic.Dictionary<string, string> mapping = new System.Collections.Generic.Dictionary<string, string>();
            string[] lines = mappingData.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string line in lines)
            {
                string[] parts = line.Split('|');
                if (parts.Length == 3)
                {
                    string key = $"{parts[0]}|{parts[1]}"; // 节点路径|组件类型
                    string propertyName = parts[2];
                    mapping[key] = propertyName;
                }
            }
            
            // 恢复属性名
            int restoredCount = 0;
            foreach (var item in createFile.ComponentDataList)
            {
                foreach (var component in item.ComponentList)
                {
                    // 确保ComponentRootPath已生成
                    if (string.IsNullOrEmpty(component.ComponentRootPath))
                    {
                        component.ComponentRootPath = createFile.GetChildPath(item.Item, currentObj.transform);
                    }
                    
                    string key = $"{component.ComponentRootPath}|{component.ComponentType ?? "GameObject"}";
                    if (mapping.TryGetValue(key, out string savedPropertyName))
                    {
                        component.PropertyName = savedPropertyName;
                        restoredCount++;
                    }
                }
            }
            
            if (restoredCount > 0)
            {
                Debug.Log($"✅ 恢复了 {restoredCount} 个自定义属性名");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"恢复属性名映射失败: {e.Message}");
        }
    }
    
    private void ShowBtn()
    {
        
        GUILayout.BeginHorizontal(); //1
        if (GUILayout.Button("prefab节点重命名",GUILayout.Height(50)))
        {
            if (isCreateFile)
            {
                _errorType = ErrorType.OptionError;
                return;
            }
            _fileStyle.normal.textColor = Color.green;
            var obj = (GameUIPrefab)target;
            _prefabPath = AssetDatabase.GetAssetPath(obj.gameObject);
            if (string.IsNullOrEmpty(_prefabPath))
            {
                _errorType = ErrorType.AssetError;
                return;
            }
            _currentObj = PrefabUtility.LoadPrefabContents(_prefabPath);
            if (_createFile == null)
            {
                _createFile = new GameUICreateFile();
            }
            _createFile.TransformList.Clear();
            _createFile.TransformList.Add(_currentObj.transform);
            _createFile.GetPrefabChild(_currentObj.transform);
            isPrefabRename = true;
        }
        
        if (GUILayout.Button("生成脚本",GUILayout.Height(50)))
        {
            if (isPrefabRename)
            {
                _errorType = ErrorType.OptionError;
                return;
            }
            _fileStyle.normal.textColor = Color.green;
            var obj = (GameUIPrefab)target;
            _currentObj = obj.gameObject;
            _prefabPath = AssetDatabase.GetAssetPath(_currentObj);
            if (string.IsNullOrEmpty(_prefabPath))
            {
                _errorType = ErrorType.AssetError;
                return;
            }
            if (_createFile == null)
            {
                _createFile = new GameUICreateFile();
            }
            _createFile.Init(_currentObj.transform);
            _selectedNodeIndex = -1; // 重置选择
            isCreateFile = true;
        }
        
        GUILayout.EndHorizontal(); //1
    }
    
    private void DrawPathSelection(string label, string currentPath, Action<string> onPathChanged)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(120));
        
        // 使用 TextField 允许手动修改
        string newPath = EditorGUILayout.TextField(currentPath);
        if (newPath != currentPath)
        {
            onPathChanged(newPath);
        }

        if (GUILayout.Button("选择", GUILayout.Width(50)))
        {
            string folder = "";
            if (!string.IsNullOrEmpty(currentPath) && System.IO.Directory.Exists(currentPath))
            {
                folder = currentPath;
            }
            else
            {
                folder = Application.dataPath;
            }
            
            // 使用 delayCall 避免 Layout 报错
            EditorApplication.delayCall += () =>
            {
                string selectedPath = EditorUtility.OpenFolderPanel("选择生成路径", folder, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // 确保以 / 结尾
                    string finalPath = selectedPath.Replace("\\", "/") + "/";
                    onPathChanged(finalPath);
                    // 强制重新绘制以显示新路径
                    Repaint();
                }
            };
        }
        EditorGUILayout.EndHorizontal();
    }

    private static void LoadSavedPaths(GameUICreateFile createFile)
    {
        if (createFile == null) return;
        
        if (createFile.IsItemPrefab)
        {
            string savedPath = EditorPrefs.GetString(GlobalItemPathKey, "");
            if (!string.IsNullOrEmpty(savedPath)) createFile.ComponentCodeGeneratePath = savedPath;
        }
        else
        {
            string p1 = EditorPrefs.GetString(GlobalComponentPathKey, "");
            string p2 = EditorPrefs.GetString(GlobalPanelPathKey, "");
            string p3 = EditorPrefs.GetString(GlobalPanelNamePathKey, "");
            
            if (!string.IsNullOrEmpty(p1)) createFile.ComponentCodeGeneratePath = p1;
            if (!string.IsNullOrEmpty(p2)) createFile.PanelCodeGeneratePath = p2;
            if (!string.IsNullOrEmpty(p3)) createFile.PanelNameCodeGeneratePath = p3;
        }
    }

    private void SavePaths(GameUICreateFile createFile)
    {
        if (createFile == null) return;

        if (createFile.IsItemPrefab)
        {
            EditorPrefs.SetString(GlobalItemPathKey, createFile.ComponentCodeGeneratePath);
        }
        else
        {
            EditorPrefs.SetString(GlobalComponentPathKey, createFile.ComponentCodeGeneratePath);
            EditorPrefs.SetString(GlobalPanelPathKey, createFile.PanelCodeGeneratePath);
            EditorPrefs.SetString(GlobalPanelNamePathKey, createFile.PanelNameCodeGeneratePath);
        }
    }
    
}

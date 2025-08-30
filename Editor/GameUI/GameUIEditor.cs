using System;
using UnityEngine;
using UnityEditor;
using GameUI;
using GameUI.Editor;
using UnityEditor.Callbacks;

[CustomEditor(typeof(GameObject))]
public class GameUIEditor : Editor
{

    public static string PrefabPathKey = "PrefabPathKey";
    public static string ClassNameKey = "ClassNameKey";
    public static string AutoAddCSKey = "AutoAddCSKey";

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
            if (_currentObj != null && _currentObj != ((GameObject)target))
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
                _createFile.AddScriptToPrefab(_currentObj);
                PrefabUtility.SaveAsPrefabAssetAndConnect(_currentObj, _prefabPath, InteractionMode.AutomatedAction);
                _createFile = null;
                _currentObj = null;
                _prefabPath = null;
                isCreateFile = false;
                EditorPrefs.DeleteAll();
                AssetDatabase.Refresh();
                Debug.LogError("生成代码成功");
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
        _fileStyle.normal.textColor = Color.green;
        GUILayout.BeginVertical();

        EditorGUILayout.LabelField("代码生成路径1：", _createFile.ComponentCodeGeneratePath, _fileStyle);
        EditorGUILayout.LabelField("脚本名字1：", _createFile.ComponentFileName, _fileStyle);
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("代码生成路径2：", _createFile.PanelCodeGeneratePath, _fileStyle);
        EditorGUILayout.LabelField("脚本名字2：", _createFile.PanelFileName, _fileStyle);
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("代码生成路径3：", _createFile.PanelNameCodeGeneratePath, _fileStyle);
        EditorGUILayout.LabelField("脚本名字3：", _createFile.StaticCSFileName, _fileStyle);

        _warningStyle.normal.textColor = Color.yellow;
        anewFile = EditorGUILayout.ToggleLeft("重新生成！（清空原有的代码，慎用！）", anewFile, _warningStyle);

        GUILayout.EndVertical();

        GUILayout.BeginHorizontal(); //1
        EditorGUILayout.Space(10);
        //EditorGUILayout.ObjectField(_currentObj, typeof(GameObject), true);

        _scrollPos = GUILayout.BeginScrollView(_scrollPos); //2
        if (_createFile.ComponentDataList is { Count: > 0 })
        {
            foreach (var item in _createFile.ComponentDataList)
            {
                GUILayout.BeginHorizontal(); //3
                GUILayoutOption option = GUILayout.Width(150);
                EditorGUILayout.ObjectField(item.Item, typeof(Transform), true,option);
                EditorGUILayout.LabelField(item.Item.name,option);
                GUILayout.Space(10);
                //EditorGUILayout.Space(5);

                GUILayout.BeginVertical(); //4
                foreach (var component in item.ComponentList)
                {
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
                    if (fileStr != null && fileStr.Contains(component.ComponentType) && fileStr.Contains(component.PropertyName))
                    {
                        component.IsSelect = true;
                    }
                    component.IsSelect = EditorGUILayout.Toggle(component.IsSelect);
                    GUILayout.EndHorizontal(); //5
                }

                GUILayout.Space(10);
                GUILayout.EndVertical(); //4

                GUILayout.EndHorizontal(); //3
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
                EditorPrefs.SetString(ClassNameKey,_createFile.PanelFileName.Replace(".cs",""));
                EditorPrefs.SetBool(AutoAddCSKey,true);
            }
            
            if (_createFile.CheckRepeatPropertyName())
            {
                _errorType = ErrorType.PropertyError;
            }
            else
            {
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
            AssetDatabase.Refresh();
        }
        GUILayout.EndHorizontal();
    }

    private void ShowPrefabNode()
    {
        GUILayout.BeginHorizontal(); //1
        _scrollPos = GUILayout.BeginScrollView(_scrollPos); //2
        if (_createFile.TransformList is { Count: > 0 })
        {
            foreach (var item in _createFile.TransformList)
            {
                GUILayout.BeginHorizontal(); //3
                GUILayoutOption option = GUILayout.Width(260);
                EditorGUILayout.ObjectField(item, typeof(Transform), true,option);
                item.name = EditorGUILayout.TextField(item.name,option);
                GUILayout.EndHorizontal(); //3
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
            AssetDatabase.Refresh();
        }
        GUILayout.EndHorizontal();
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
            _prefabPath = AssetDatabase.GetAssetPath((GameObject)target);
            if (string.IsNullOrEmpty(_prefabPath))
            {
                _errorType = ErrorType.AssetError;
                return;
            }
            _currentObj = PrefabUtility.LoadPrefabContents(_prefabPath);
            _createFile = new GameUICreateFile();
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
            _currentObj = (GameObject)target;
            _prefabPath = AssetDatabase.GetAssetPath(_currentObj);
            if (string.IsNullOrEmpty(_prefabPath))
            {
                _errorType = ErrorType.AssetError;
                return;
            }
            if (_createFile == null)
            {
                _createFile = new GameUICreateFile();
                _createFile.Init(_currentObj.transform);
            }
            isCreateFile = true;
        }
        
        GUILayout.EndHorizontal(); //1
    }
    
}

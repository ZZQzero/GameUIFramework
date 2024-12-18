using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

public class GameUIWindow : EditorWindow
{
    
    private static GameUIWindow _uiWindow;
    private static GameUICreateFile _createFile;

    private bool anewFile = false;
    private GameObject _selectObj;
    private Vector2 _scrollPos;
    private GUIStyle _fileStyle = new GUIStyle();
    private GUIStyle _guiStyle = new GUIStyle();
    private GUIStyle _errorStyle = new GUIStyle();
    private GUIStyle _warningStyle = new GUIStyle();

    
    [MenuItem("GameUI/编辑UI")]
    public static void ShowEditorWindow()
    {
        _uiWindow = EditorWindow.GetWindow<GameUIWindow>();
        _createFile = new GameUICreateFile();
        _uiWindow.Show();
    }

    private void OnGUI()
    {
        var obj = Selection.activeObject;
        if (obj is GameObject select)
        {
            if(_selectObj != null && _selectObj != select)
            {
                GUIContent content = EditorGUIUtility.TrTextContentWithIcon("你是不是点了其他的UI！！！", null, MessageType.Warning);
                ShowNotification(content,1.5);
                _createFile = null;
                _createFile = new GameUICreateFile();
            }
            _selectObj = select;
        }
        else
        {
            if (_uiWindow != null)
            {
                _uiWindow.Close();
            }
            else
            {
                _uiWindow = EditorWindow.GetWindow<GameUIWindow>();
                _uiWindow.Close();
            }
            return;
        }

        if (_createFile == null)
        {
            if (_uiWindow != null)
            {
                _uiWindow.Close();
            }
            else
            {
                _uiWindow = EditorWindow.GetWindow<GameUIWindow>();
                _uiWindow.Close();
            }
            return;
        }
        
        ShowItem();
        ShowBtn();
    }

    private void ShowItem()
    {
        _createFile.Init(_selectObj.transform);
        _fileStyle.normal.textColor = Color.green;
        GUILayout.BeginVertical();
        
        EditorGUILayout.LabelField("代码生成路径1：",_createFile.ComponentCodeGeneratePath,_fileStyle);
        EditorGUILayout.LabelField("脚本名字1：",_createFile.ComponentFileName,_fileStyle);
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("代码生成路径2：",_createFile.PanelCodeGeneratePath,_fileStyle);
        EditorGUILayout.LabelField("脚本名字2：",_createFile.PanelFileName,_fileStyle);
        
        _warningStyle.normal.textColor = Color.yellow;
        anewFile = EditorGUILayout.ToggleLeft("重新生成！（清空原有的代码，慎用！）", anewFile,_warningStyle);

        GUILayout.EndVertical();
        
        GUILayout.BeginHorizontal();//1
        EditorGUILayout.Space(10);
        EditorGUILayout.ObjectField(_selectObj,typeof(GameObject),true);
        
        _scrollPos = GUILayout.BeginScrollView(_scrollPos);//2
        if (_createFile.ComponentDataList is {Count: > 0})
        {
            foreach (var item in _createFile.ComponentDataList)
            {
                GUILayout.BeginHorizontal();//3
                EditorGUILayout.ObjectField(item.Item,typeof(Transform),true);
                var text = EditorGUILayout.TextField(item.Item.name);
                if(item.Item.name != text)
                {
                    item.Item.name = text;
                    AssetDatabase.SaveAssets();
                }
                GUILayout.Space(10);
                EditorGUILayout.Space(5);

                GUILayout.BeginVertical();//4
                foreach (var component in item.ComponentList)
                {
                    GUILayout.BeginHorizontal();//5
                    if (component.IsSelect && !component.IsError)
                    {
                        _guiStyle.normal.textColor = Color.green;
                    }
                    else if(component.IsError)
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
                        component.ComponentRootPath = _createFile.GetChildPath(item.Item, _selectObj.transform);
                    }
                    
                    EditorGUILayout.LabelField(component.ComponentType,_guiStyle);
                    EditorGUILayout.Space(5);
                    component.PropertyName = EditorGUILayout.TextField(component.PropertyName);
                    
                    EditorGUILayout.Space(5);
                    component.IsSelect = EditorGUILayout.Toggle(component.SelectIndex.ToString(), component.IsSelect);
                    GUILayout.EndHorizontal();//5
                }
                GUILayout.Space(10);
                GUILayout.EndVertical();//4
                
                GUILayout.EndHorizontal();//3
            }
        }
        else
        {
            _errorStyle.normal.textColor = Color.red;
            EditorGUILayout.LabelField("是否选中UI根节点！！是否正确标记UI！！",_errorStyle);
        }
        GUILayout.EndScrollView();//2

        GUILayout.EndHorizontal();//1
    }

    private void ShowBtn()
    {
        GUILayout.BeginHorizontal();//1
        if (GUILayout.Button("刷新"))
        {
            AssetDatabase.Refresh();
            _createFile = null;
            _createFile = new GameUICreateFile();
        }
        
        if (GUILayout.Button("生成代码"))
        {
            if (_createFile.CheckRepeatPropertyName())
            {
                ShowNotification(new GUIContent("有重复的属性名字！！！"),1);
            }
            else
            {
                _createFile.StartGenerate(anewFile);
                AssetDatabase.Refresh();
            }
        }
        GUILayout.EndHorizontal();//1
    }

    private void OnDestroy()
    {
        _createFile = null;
    }
}

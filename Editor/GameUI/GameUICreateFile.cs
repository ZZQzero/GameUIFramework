using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GameUI.Editor
{
    public class GameUICreateFile
    {
        public class ComponentData
        {
            public Transform Item;
            public Transform Root;
            public string TempName;
            public List<ComponentDataParams> ComponentList = new();
        }

        public class ComponentDataParams
        {
            public Component Component;
            public GameObject GameObject;
            public string ComponentPath; //相对路径
            public string ComponentRootPath; //绝对路径
            public string PropertyName;
            public string ComponentType;
            public string GameObjectName;
            public bool IsSelect = false;
            public bool IsError = false;
            public int SelectIndex = 0;
        }

        //添加过滤组件
        public Dictionary<string, string> FilterComponentDic = new()
        {
            { "UnityEngine.CanvasRenderer", "UnityEngine.CanvasRenderer" },
            { "UnityEngine.Canvas", "UnityEngine.Canvas" },
            { "UnityEngine.UI.GraphicRaycaster", "UnityEngine.UI.GraphicRaycaster" },
            { "GameUI.GameUISetting", "GameUI.GameUISetting" },
        };

        public List<ComponentData> ComponentDataList = new();
        public List<Transform> TransformList = new();
        public string ComponentFileName;
        public string PanelFileName;
        public string StaticCSFileName;
        public string ClassName;
        public string UIName;
        public readonly string StaticName = "GameUIName";
        public readonly string NameSpaceName = "GameUI";
        public string ComponentCodeGeneratePath = Application.dataPath + "/Script/GameUI/UIScriptsGenerate/";
        public string PanelCodeGeneratePath = Application.dataPath + "/Script/GameUI/UIScript/";
        public string PanelNameCodeGeneratePath = Application.dataPath + "/Script/GameUI/UIScript/";
        public Type ScriptType = null;
        
        private Transform uiRoot;
        private bool isInit;

        private readonly string pattern = @"[^a-zA-Z0-9]|(\s+)";
        private readonly string tag = "@";
        private string tab = "\t";
        private string enter = "\n";

        public void Init(Transform selectUI,bool isSelect = true)
        {
            if (!isInit)
            {
                uiRoot = selectUI;
                isInit = true;
                ComponentDataList.Clear();
                UIName = Regex.Replace(selectUI.name, pattern, "");
                ComponentFileName = UIName + "PanelComponent.cs";
                PanelFileName = UIName + "Panel.cs";
                ClassName = UIName + "Panel";
                StaticCSFileName = StaticName + ".cs";
                AddFilterComponentName();
                GetPrefabComponent(uiRoot, uiRoot,isSelect);
                GetPrefabChildComponent(uiRoot, uiRoot,isSelect);
            }
        }

        private void AddFilterComponentName()
        {
            string filterName = $"{NameSpaceName}.{ClassName}";
            FilterComponentDic.Add(filterName,filterName);
        }

        public string CheckPropertyExists()
        {
            string path = ComponentCodeGeneratePath + ComponentFileName;
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }

            return null;
        }
        
        public void StartGenerate(bool newFile = false)
        {
            if (!Directory.Exists(ComponentCodeGeneratePath))
            {
                Directory.CreateDirectory(ComponentCodeGeneratePath);
            }

            if (!Directory.Exists(PanelCodeGeneratePath))
            {
                Directory.CreateDirectory(PanelCodeGeneratePath);
            }
            
            if (!Directory.Exists(PanelNameCodeGeneratePath))
            {
                Directory.CreateDirectory(PanelNameCodeGeneratePath);
            }

            string path = Path.Combine(ComponentCodeGeneratePath, ComponentFileName).Replace("\\", "/");
            string path1 = Path.Combine(PanelCodeGeneratePath,PanelFileName).Replace("\\", "/");
            string path2 = Path.Combine(PanelNameCodeGeneratePath,StaticCSFileName).Replace("\\", "/");
            
            if (newFile)
            {
                CreateComponentFile(path, ClassName);
                CreatePanelFile(path1, ClassName);
                CreatePanelNameFile(path2);
                return;
            }

            if (File.Exists(path))
            {
                var tempStr = File.ReadAllText(path);
                int startIndex = tempStr.IndexOf("#region Auto Generate Code", StringComparison.OrdinalIgnoreCase);
                int endIndex = tempStr.IndexOf("#endregion Auto Generate Code", StringComparison.OrdinalIgnoreCase);

                if (startIndex == -1 || endIndex == -1 || startIndex >= endIndex)
                {
                    throw new InvalidOperationException("Invalid start or end markers in the target file.");
                }

                startIndex += "#region Auto Generate Code".Length;

                string before = tempStr.Substring(0, startIndex);
                string after = tempStr.Substring(endIndex);
                string contentToInsert = WriteComponentLine();
                string newContent = before + Environment.NewLine + contentToInsert + Environment.NewLine + tab + tab + after;

                File.WriteAllText(path, newContent);
            }
            else
            {
                CreateComponentFile(path, ClassName);
            }

            if (!File.Exists(path1))
            {
                CreatePanelFile(path1, ClassName);
            }
            
            CreatePanelNameFile(path2);
        }

        public void GetPrefabChildComponent(Transform parent, Transform root,bool isSelect)
        {
            if (parent.childCount != 0)
            {
                for (int i = 0; i < parent.childCount; i++)
                {
                    var curRoot = root;
                    var child = parent.GetChild(i);
                    if (child.name.StartsWith(tag))
                    {
                        ComponentData data = new ComponentData();
                        data.Item = child;
                        data.TempName =  child.name;
                        if (data.Root == null)
                        {
                            data.Root = root;
                        }

                        var components = child.GetComponents<Component>();
                        for (int j = 0; j < components.Length; j++)
                        {
                            if(components[j] == null)
                            {
                                Debug.LogError($"存在丢失引用的脚本！-->{child.name}");
                                continue;
                            }
                            if (FilterComponentDic.ContainsKey(components[j].GetType().ToString()))
                            {
                                continue;
                            }

                            ComponentDataParams componentDataParams = new ComponentDataParams();
                            componentDataParams.Component = components[j];
                            /*componentDataParams.GameObject = components[j].gameObject;
                            componentDataParams.GameObjectName =  components[j].gameObject.name;*/
                            componentDataParams.SelectIndex = j;
                            componentDataParams.IsSelect = isSelect;
                            var typeStr = components[j].GetType().ToString();
                            componentDataParams.ComponentType = typeStr;
                            var strs = typeStr.Split('.');
                            string replacement = Regex.Replace(child.name, pattern, "");
                            componentDataParams.PropertyName = CapitalizeFirstLetter(replacement + strs[^1]); //取最后一个
                            data.ComponentList.Add(componentDataParams);
                        }
                        
                        //-------------gameObject组件--------------
                        ComponentDataParams componentDataParamsObj = new ComponentDataParams();
                        componentDataParamsObj.GameObject = child.gameObject;
                        componentDataParamsObj.GameObjectName = child.gameObject.name;
                        componentDataParamsObj.Component = null;
                        componentDataParamsObj.SelectIndex = 0;
                        var typeStr1 = componentDataParamsObj.GameObject.GetType().ToString();
                        componentDataParamsObj.ComponentType = null;
                        componentDataParamsObj.IsSelect = isSelect;
                        var strs1 = typeStr1.Split('.');
                        string replacement1 = Regex.Replace(child.name, pattern, "");
                        componentDataParamsObj.PropertyName = CapitalizeFirstLetter(replacement1 + strs1[^1]); //取最后一个
                        data.ComponentList.Add(componentDataParamsObj);
                        //-------------gameObject组件--------------
                        
                        ComponentDataList.Add(data);
                        curRoot = child;
                    }

                    if (child.childCount != 0)
                    {
                        GetPrefabChildComponent(child, curRoot,isSelect);
                    }
                }
            }
        }

        public void GetPrefabComponent(Transform parent, Transform root,bool isSelect)
        {
            if (parent.name.StartsWith(tag))
            {
                ComponentData data = new ComponentData();
                data.Item = parent;
                data.TempName = parent.name;
                if (data.Root == null)
                {
                    data.Root = root;
                }

                var components = parent.GetComponents<Component>();
                for (int j = 0; j < components.Length; j++)
                {
                    if(components[j] == null)
                    {
                        Debug.LogError($"存在丢失引用的脚本！-->{parent.name}");
                        continue;
                    }
                    if (FilterComponentDic.ContainsKey(components[j].GetType().ToString()))
                    {
                        continue;
                    }

                    ComponentDataParams componentDataParams = new ComponentDataParams();
                    componentDataParams.Component = components[j];
                    componentDataParams.SelectIndex = j;
                    componentDataParams.IsSelect = isSelect;
                    var typeStr = components[j].GetType().ToString();
                    componentDataParams.ComponentType = typeStr;
                    var strs = typeStr.Split('.');
                    string replacement = Regex.Replace(parent.name, pattern, "");
                    componentDataParams.PropertyName = CapitalizeFirstLetter(replacement + strs[^1]); //取最后一个
                    data.ComponentList.Add(componentDataParams);
                }

                ComponentDataList.Add(data);
            }
        }

        public string GetChildPath(Transform child, Transform root)
        {
            string path = "/" + child.name;
            while (child.parent != root && child.parent != null)
            {
                child = child.parent;
                path = "/" + child.name + path;
            }

            if (path.StartsWith("/"))
            {
                path = path.Remove(0, 1);
            }

            return path;
        }

        public void GetPrefabChild(Transform root)
        {
            if (root.childCount != 0)
            {
                for (int i = 0; i < root.childCount; i++)
                {
                    var child = root.GetChild(i);
                    TransformList.Add(child);
                    if (child.childCount != 0)
                    {
                        GetPrefabChild(child);
                    }
                }
            }
        }

        public void CreateComponentFile(string path, string className)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("using UnityEngine;" + enter);
            stringBuilder.Append("using UnityEngine.UI;" + enter + enter);
            stringBuilder.Append($"namespace {NameSpaceName}" + enter);
            stringBuilder.Append("{" + enter);
            stringBuilder.Append(tab + "public partial class " + className + enter);
            stringBuilder.Append(tab + "{" + enter);
            stringBuilder.AppendLine(tab + tab + "#region Auto Generate Code");
            stringBuilder.Append(WriteComponentLine());
            stringBuilder.AppendLine(tab + tab + "#endregion Auto Generate Code");

            stringBuilder.Append(tab + "}" + enter);
            stringBuilder.Append("}" + enter);

            CreateScript(stringBuilder.ToString(), path);
        }

        public string WriteComponentLine()
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (var component in ComponentDataList)
            {
                foreach (var item in component.ComponentList)
                {
                    if (item.IsSelect)
                    {
                        if (item.Component != null)
                        {
                            string properity = $"{tab}[SerializeField] private {item.Component.GetType()} {item.PropertyName};";
                            stringBuilder.AppendLine(tab + properity);
                        }
                        else
                        {
                            string properity = $"{tab}[SerializeField] private {item.GameObject.GetType()} {item.PropertyName};";
                            stringBuilder.AppendLine(tab + properity);
                        }
                    }
                }
            }

            /*stringBuilder.AppendLine();
            stringBuilder.Append(tab + tab + "public void InitData()" + enter);
            stringBuilder.Append(tab + tab + "{" + enter);

            foreach (var item in ComponentDataList)
            {
                foreach (var component in item.ComponentList)
                {
                    if (!component.IsSelect)
                    {
                        continue;
                    }

                    if (item.Root == uiRoot)
                    {
                        if (item.Item.parent == null)
                        {
                            stringBuilder.AppendLine(tab + tab + tab + component.PropertyName + $" = GetComponent<{component.Component.GetType()}>();");
                        }
                        else
                        {
                            stringBuilder.AppendLine(tab + tab + tab + component.PropertyName + $" = transform.Find({'"'}{component.ComponentPath}{'"'}).GetComponent<{component.Component.GetType()}>();");
                        }
                    }
                    else
                    {
                        var curItem = ComponentDataList.Find(x => x.Item == item.Root);
                        var curComponent = curItem.ComponentList.Find(x => x.IsSelect == true);
                        if (curComponent != null)
                        {
                            stringBuilder.AppendLine(tab + tab + tab + component.PropertyName + $" = {curComponent.PropertyName}.transform.Find({'"'}{component.ComponentPath}{'"'}).GetComponent<{component.Component.GetType()}>();");
                        }
                        else
                        {
                            stringBuilder.AppendLine(tab + tab + tab + component.PropertyName + $" = transform.Find({'"'}{component.ComponentRootPath}{'"'}).GetComponent<{component.Component.GetType()}>();");
                        }
                    }
                }
            }*/

            //stringBuilder.Append(tab + tab + "}" + enter);
            return stringBuilder.ToString();
        }

        public void CreatePanelFile(string path, string className)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("using UnityEngine;" + enter);
            stringBuilder.Append("using UnityEngine.UI;" + enter + enter);
            stringBuilder.Append($"namespace {NameSpaceName}" + enter);
            stringBuilder.Append("{" + enter);
            stringBuilder.Append(tab + "public partial class " + className + " : GameUIBase" + enter);
            stringBuilder.Append(tab + "{" + enter);

            stringBuilder.Append(tab + tab + "public override void OnInitUI()" + enter);
            stringBuilder.Append(tab + tab + "{" + enter);
            stringBuilder.AppendLine(tab + tab + tab + "base.OnInitUI();");
            //stringBuilder.AppendLine(tab + tab + tab + "#region Auto Generate Code");
            //stringBuilder.Append(tab + tab + tab + "InitData();" + enter);
            //stringBuilder.AppendLine(tab + tab + tab + "#endregion Auto Generate Code");
            stringBuilder.Append(tab + tab + "}" + enter);

            stringBuilder.Append(tab + tab + "public override void OnOpenUI()" + enter);
            stringBuilder.Append(tab + tab + "{" + enter);
            stringBuilder.AppendLine(tab + tab + tab + "base.OnOpenUI();");
            stringBuilder.Append(tab + tab + "}" + enter);
            
            stringBuilder.Append(tab + tab + "public override void OnRefreshUI()" + enter);
            stringBuilder.Append(tab + tab + "{" + enter);
            stringBuilder.AppendLine(tab + tab + tab + "base.OnRefreshUI();");
            stringBuilder.Append(tab + tab + "}" + enter);

            stringBuilder.Append(tab + tab + "public override void OnCloseUI()" + enter);
            stringBuilder.Append(tab + tab + "{" + enter);
            stringBuilder.AppendLine(tab + tab + tab + "base.OnCloseUI();");
            stringBuilder.Append(tab + tab + "}" + enter);

            stringBuilder.Append(tab + tab + "public override void OnDestroyUI()" + enter);
            stringBuilder.Append(tab + tab + "{" + enter);
            stringBuilder.AppendLine(tab + tab + tab + "base.OnDestroyUI();");
            stringBuilder.Append(tab + tab + "}" + enter);

            stringBuilder.Append(tab + "}" + enter);
            stringBuilder.Append("}" + enter);

            CreateScript(stringBuilder.ToString(), path);
        }

        public void CreatePanelNameFile(string path)
        {
            if (File.Exists(path))
            {
                var tempStr = File.ReadAllText(path);
                int startIndex = tempStr.IndexOf("//end", StringComparison.OrdinalIgnoreCase);

                if (tempStr.Contains(UIName) && tempStr.Contains(uiRoot.name))
                {
                    Debug.Log("已经存在相同的名字");
                    return;
                }
                
                if (startIndex < 0)
                {
                    throw new InvalidOperationException("Invalid start or end markers in the target file.");
                }
                string before = tempStr.Substring(0, startIndex);
                string after = tempStr.Substring(startIndex);
                string contentToInsert = WriteUINameLine(UIName,uiRoot.name);
                string newContent = before + contentToInsert + tab + tab + after;

                File.WriteAllText(path, newContent);
            }
            else
            {
                CreatePanelNameFile(path, uiRoot.name);
            }
        }
        
        public void CreatePanelNameFile(string path, string rootName)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"namespace {NameSpaceName}" + enter);
            stringBuilder.Append("{" + enter);
            stringBuilder.Append(tab + "public static class " + StaticName + enter);
            stringBuilder.Append(tab + "{" + enter);
            stringBuilder.Append(tab + tab + WriteUINameLine(UIName,rootName));
            stringBuilder.AppendLine(tab + tab + "//end");

            stringBuilder.Append(tab + "}" + enter);
            stringBuilder.Append("}" + enter);

            CreateScript(stringBuilder.ToString(), path);
        }

        public string WriteUINameLine(string propertyName, string prefabName)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("public const string " + propertyName + " = " + '"' + prefabName + '"' + ";");
            return stringBuilder.ToString();
        }

        public void CreateScript(string content, string path)
        {
            using (StreamWriter streamWriter = File.CreateText(path))
            {
                streamWriter.Write(content);
                streamWriter.Flush();
            }
        }

        //检查列表里是否有重复的属性名
        public bool CheckRepeatPropertyName()
        {
            Dictionary<string, ComponentDataParams> dic = new();
            foreach (var item in ComponentDataList)
            {
                foreach (var component in item.ComponentList)
                {
                    if (component.IsSelect)
                    {
                        if (dic.ContainsKey(component.PropertyName))
                        {
                            component.IsError = true;
                            dic[component.PropertyName].IsError = true;
                        }
                        else
                        {
                            component.IsError = false;
                            dic.Add(component.PropertyName, component);
                        }
                    }
                }
            }

            return dic.Values.Any(c => c.IsError);
        }

        //设置属性名首字母为小写
        private string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            return char.ToLower(input[0]) + input.Substring(1);
        }

        public void AddScriptToPrefab(GameObject prefab)
        {
            var assembly = Assembly.Load("Assembly-CSharp");
            if (assembly == null)
            {
                Debug.LogError($"无法找到程序集:");
                return;
            }
            
            //最耗时，但最全面
            /*foreach (var assemblys in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assemblys.GetTypes())
                {
                    if (type.Name == PanelFileName.Replace(".cs", ""))
                    {
                        scriptType = type;
                        break;
                    }
                }

                if (scriptType != null)
                {
                    break;
                }
            }*/

            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                if (type.Name == PanelFileName.Replace(".cs", ""))
                {
                    ScriptType = type;
                    break;
                }
            }

            if (ScriptType == null)
            {
                Debug.LogError($"无法找到类名: {PanelFileName}");
                return;
            }

            var component = prefab.GetComponent(ScriptType);
            if (component == null)
            {
                prefab.AddComponent(ScriptType);
            }
            EditorUtility.SetDirty(prefab);
            AutoBindFields(prefab);
        }

        private void AutoBindFields(GameObject prefab)
        {
            var curScriptComponent =  prefab.GetComponent(ScriptType);
            if (curScriptComponent == null)
            {
                Debug.LogError($"没有找到Component  {ScriptType}");
                return;
            }
            
            SerializedObject serializedObject = new SerializedObject(curScriptComponent);
            foreach (var component in ComponentDataList)
            {
                foreach (var item in component.ComponentList)
                {
                    string name = item.PropertyName;
                    SerializedProperty property = serializedObject.FindProperty(name);
                    if (property != null)
                    {
                        if (item.Component != null)
                        {
                            property.objectReferenceValue = item.Component;
                        }
                        else if(item.GameObject != null)
                        {
                            property.objectReferenceValue = item.GameObject;
                        }
                    }
                    else
                    {
                        Debug.LogError($"没有找到这个字段： {item.PropertyName}");
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }

}

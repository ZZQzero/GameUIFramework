using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace GameUI.Editor
{
    public class GameUICreateFile
    {
        public class ComponentData
        {
            public Transform Item;
            public Transform Root;
            public List<ComponentDataParams> ComponentList = new();
        }

        public class ComponentDataParams
        {
            public Component Component;
            public string ComponentPath; //相对路径
            public string ComponentRootPath; //绝对路径
            public string PropertyName;
            public string ComponentType;
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
        };

        public List<ComponentData> ComponentDataList = new();
        public string ComponentFileName;
        public string PanelFileName;
        public string PanelNameFileName;
        public string ClassName;
        public string UIName;
        public readonly string StaticName = "GameUIName";
        public readonly string NameSpaceName = "GameUI";
        public readonly string ComponentCodeGeneratePath = Application.dataPath + "/GameUI/Samples/UIScriptsGenerate/";
        public readonly string PanelCodeGeneratePath = Application.dataPath + "/GameUI/Samples/UIScript/";
        public readonly string PanelNameCodeGeneratePath = Application.dataPath + "/GameUI/Samples/UIScript/";

        private Transform uiRoot;
        private bool isInit;

        private readonly string pattern = @"[^a-zA-Z0-9]|(\s+)";
        private readonly string tag = "@";
        private string tab = "\t";
        private string enter = "\n";

        public void Init(Transform selectUI)
        {
            if (!isInit)
            {
                uiRoot = selectUI;
                isInit = true;
                GetItemComponent(uiRoot, uiRoot);
                GetTransformChild(uiRoot, uiRoot);
                UIName = Regex.Replace(selectUI.name, pattern, "");
                ComponentFileName = UIName + "PanelComponent.cs";
                PanelFileName = UIName + "Panel.cs";
                ClassName = UIName + "Panel";
                PanelNameFileName = StaticName + ".cs";
            }
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

            string path = ComponentCodeGeneratePath + ComponentFileName;
            string path1 = PanelCodeGeneratePath + PanelFileName;
            string path2 = PanelNameCodeGeneratePath + PanelNameFileName;

            if (newFile)
            {
                CreateComponentFile(ComponentCodeGeneratePath, ComponentFileName, ClassName);
                CreatePanelFile(PanelCodeGeneratePath, PanelFileName, ClassName);
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
                CreateComponentFile(ComponentCodeGeneratePath, ComponentFileName, ClassName);
            }

            if (!File.Exists(path1))
            {
                CreatePanelFile(PanelCodeGeneratePath, PanelFileName, ClassName);
            }
            
            if (File.Exists(path2))
            {
                var tempStr = File.ReadAllText(path2);
                int startIndex = tempStr.IndexOf("//end", StringComparison.OrdinalIgnoreCase);
                
                if (startIndex < 0)
                {
                    throw new InvalidOperationException("Invalid start or end markers in the target file.");
                }
                string before = tempStr.Substring(0, startIndex);
                string after = tempStr.Substring(startIndex);
                string contentToInsert = WriteUINameLine(UIName,uiRoot.name);
                string newContent = before + contentToInsert + tab + tab + after;

                File.WriteAllText(path2, newContent);
            }
            else
            {
                CreatePanelNameFile(PanelNameCodeGeneratePath, PanelNameFileName, StaticName);
            }
        }

        public void GetTransformChild(Transform parent, Transform root)
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
                            componentDataParams.SelectIndex = j;
                            var typeStr = components[j].GetType().ToString();
                            componentDataParams.ComponentType = typeStr;
                            var strs = typeStr.Split('.');
                            string replacement = Regex.Replace(child.name, pattern, "");
                            componentDataParams.PropertyName = CapitalizeFirstLetter(replacement + strs[^1]); //取最后一个
                            data.ComponentList.Add(componentDataParams);
                        }

                        ComponentDataList.Add(data);
                        curRoot = child;
                    }

                    if (child.childCount != 0)
                    {
                        GetTransformChild(child, curRoot);
                    }
                }
            }
        }

        public void GetItemComponent(Transform parent, Transform root)
        {
            if (parent.name.StartsWith(tag))
            {
                ComponentData data = new ComponentData();
                data.Item = parent;
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

        public void CreateComponentFile(string path, string fileName, string className)
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

            CreateScript(stringBuilder.ToString(), path, fileName);
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
                        string properity = $"{tab}private {item.Component.GetType()} {item.PropertyName};";
                        stringBuilder.AppendLine(tab + properity);
                    }
                }
            }

            stringBuilder.AppendLine();
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
            }

            stringBuilder.Append(tab + tab + "}" + enter);
            return stringBuilder.ToString();
        }

        public void CreatePanelFile(string path, string fileName, string className)
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
            stringBuilder.AppendLine(tab + tab + tab + "#region Auto Generate Code");
            stringBuilder.Append(tab + tab + tab + "InitData();" + enter);
            stringBuilder.AppendLine(tab + tab + tab + "#endregion Auto Generate Code");
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

            CreateScript(stringBuilder.ToString(), path, fileName);
        }
        
        public void CreatePanelNameFile(string path, string fileName, string className)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"namespace {NameSpaceName}" + enter);
            stringBuilder.Append("{" + enter);
            stringBuilder.Append(tab + "public static class " + StaticName + enter);
            stringBuilder.Append(tab + "{" + enter);
            stringBuilder.Append(tab + tab + WriteUINameLine(UIName,className));
            stringBuilder.AppendLine(tab + tab + "//end");

            stringBuilder.Append(tab + "}" + enter);
            stringBuilder.Append("}" + enter);

            CreateScript(stringBuilder.ToString(), path, fileName);
        }

        public string WriteUINameLine(string propertyName, string prefabName)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("public const string " + propertyName + " = " + '"' + prefabName + '"' + ";");
            return stringBuilder.ToString();
        }

        public void CreateScript(string content, string path, string fileName)
        {
            path += fileName;
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

        //设置属性名首字母为大写
        private string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            return char.ToUpper(input[0]) + input.Substring(1);
        }
    }

}

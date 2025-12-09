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
            public bool IsFoldout = true; // 是否展开（默认展开）
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
            public string OriginalPropertyName;
            public bool HasSyncedSelection;
        }

        //添加过滤组件
        public Dictionary<string, string> FilterComponentDic = new()
        {
            { "UnityEngine.CanvasRenderer", "UnityEngine.CanvasRenderer" },
            { "UnityEngine.Canvas", "UnityEngine.Canvas" },
            { "UnityEngine.UI.Mask", "UnityEngine.UI.Mask" },
            { "UnityEngine.UI.GraphicRaycaster", "UnityEngine.UI.GraphicRaycaster" },
            { "GameUI.GameUISetting", "GameUI.GameUISetting" },
            { "GameUI.GameUIPrefab", "GameUI.GameUIPrefab" },
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
        public string ComponentCodeGeneratePath;
        public string PanelCodeGeneratePath;
        public string PanelNameCodeGeneratePath;
        private readonly string defaultComponentCodeGeneratePath = Application.dataPath + "/Scripts/HotfixView/GameUI/UIScriptsGenerate/";
        private readonly string defaultPanelCodeGeneratePath = Application.dataPath + "/Scripts/HotfixView/GameUI/UIScript/";
        private readonly string defaultPanelNameCodeGeneratePath = Application.dataPath + "/Scripts/ModelView/GameUI/UIScript/";
        private readonly string itemComponentCodeGeneratePath = Application.dataPath + "/Scripts/HotfixView/GameUI/UICellScript/";
        public Type ScriptType = null;
        
        private Transform uiRoot;
        private bool isInit;
        private bool isItemPrefab;
        private string originalPrefabName;
        private static readonly string[] ItemPrefabSuffixes = { "Cell", "Item" };

        public bool IsItemPrefab => this.isItemPrefab;
        public string OriginalPrefabName => this.originalPrefabName;
        public string GeneratedClassName => this.ClassName;
        
        private readonly string pattern = @"[^a-zA-Z0-9]|(\s+)";
        private string tab = "\t";
        private string enter = "\n";
        
        // 智能识别的UI组件类型
        private readonly HashSet<string> _autoBindComponentTypes = new()
        {
            "UnityEngine.UI.Button",
            "UnityEngine.UI.Text",
            "UnityEngine.UI.Image",
            "UnityEngine.UI.RawImage",
            "UnityEngine.UI.InputField",
            "UnityEngine.UI.Slider",
            "UnityEngine.UI.Toggle",
            "UnityEngine.UI.ScrollRect",
            "UnityEngine.UI.Dropdown",
            "UnityEngine.UI.ScrollView",
            "TMPro.TextMeshProUGUI",
            "TMPro.TMP_InputField",
            "TMPro.TMP_Dropdown",
        };
        
        // 组件类型的常见缩写映射表
        private readonly Dictionary<string, string[]> _componentAbbreviations = new()
        {
            { "Button", new[] { "Btn", "Button", "button" } },
            { "Image", new[] { "Img", "Image", "Pic", "Icon", "image" } },
            { "Text", new[] { "Txt", "Text", "Label", "text" } },
            { "InputField", new[] { "Input", "Field", "Edit" } },
            { "Toggle", new[] { "Toggle", "Chk", "Check", "Switch" } },
            { "Slider", new[] { "Slider", "Slide", "Bar" } },
            { "ScrollRect", new[] { "Scroll", "ScrollView", "List" } },
            { "Dropdown", new[] { "Dropdown", "Drop", "Select" } },
            { "RawImage", new[] { "Raw", "RawImg" } },
            { "TextMeshProUGUI", new[] { "Text", "Label", "Txt" ,"TMP"} },
            { "TMP_InputField", new[] { "Input", "Field", "Edit" } },
            { "TMP_Dropdown", new[] { "Dropdown", "Drop", "Select" } },
            { "RectTransform", new[] { "Trans", "RectTrans" } },
            { "Transform", new[] { "Trans" } },
            { "GameObject", new[] { "Obj" } },
        };

        // 组件类型对应的简写后缀
        private readonly Dictionary<string, string> _componentTypeToSuffix = new()
        {
            { "Button", "Btn" },
            { "Image", "Img" },
            { "Text", "Txt" },
            { "InputField", "Input" },
            { "Toggle", "Toggle" },
            { "Slider", "Sld" },
            { "ScrollRect", "Scroll" },
            { "Dropdown", "Drop" },
            { "RawImage", "Raw" },
            { "TextMeshProUGUI", "Txt" },
            { "TMP_InputField", "Input" },
            { "TMP_Dropdown", "Drop" },
            { "RectTransform", "RectTrans" },
            { "Transform", "Trans" },
            { "GameObject", "Obj" },
            { "LayoutElement", "Element" },
            { "LoopVerticalScrollRect", "VScroll" },
        };

        public void Init(Transform selectUI,bool isSelect = true)
        {
            if (!isInit)
            {
                uiRoot = selectUI;
                isInit = true;
                ComponentDataList.Clear();
                originalPrefabName = selectUI.name;
                isItemPrefab = CheckIsItemPrefab(originalPrefabName);
                UIName = Regex.Replace(selectUI.name, pattern, "");

                if (isItemPrefab)
                {
                    ComponentCodeGeneratePath = itemComponentCodeGeneratePath;
                    PanelCodeGeneratePath = string.Empty;
                    PanelNameCodeGeneratePath = string.Empty;
                    ComponentFileName = UIName + ".cs";
                    PanelFileName = string.Empty;
                    ClassName = UIName;
                }
                else
                {
                    ComponentCodeGeneratePath = defaultComponentCodeGeneratePath;
                    PanelCodeGeneratePath = defaultPanelCodeGeneratePath;
                    PanelNameCodeGeneratePath = defaultPanelNameCodeGeneratePath;
                    ComponentFileName = UIName + "PanelComponent.cs";
                    PanelFileName = UIName + "Panel.cs";
                    ClassName = UIName + "Panel";
                }

                StaticCSFileName = StaticName + ".cs";
                AddFilterComponentName();
                GetPrefabComponent(uiRoot, uiRoot,isSelect);
                GetPrefabChildComponent(uiRoot, uiRoot,isSelect);
            }
        }
        
        /// <summary>
        /// 判断节点是否需要绑定（自动模式）
        /// </summary>
        private bool ShouldBindNode(Transform node, bool isRootNode = false)
        {
            // 根节点总是识别（至少需要GameObject引用），子节点检查是否有UI组件
            if (isRootNode)
            {
                return true;
            }
            return HasBindableComponent(node);
        }

        private bool CheckIsItemPrefab(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                return false;
            }

            foreach (var suffix in ItemPrefabSuffixes)
            {
                if (prefabName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// 检查节点是否包含可绑定的组件
        /// </summary>
        private bool HasBindableComponent(Transform node)
        {
            var components = node.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component == null) continue;
                
                var componentType = component.GetType().ToString();
                
                // 跳过过滤列表中的组件
                if (FilterComponentDic.ContainsKey(componentType)) continue;
                
                // 检查是否是需要自动绑定的UI组件
                if (_autoBindComponentTypes.Contains(componentType))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 清理节点名称（移除特殊字符）
        /// </summary>
        private string CleanNodeName(string nodeName)
        {
            return Regex.Replace(nodeName, pattern, "");
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

            string componentPath = Path.Combine(ComponentCodeGeneratePath, ComponentFileName).Replace("\\", "/");

            if (isItemPrefab)
            {
                GenerateItemScript(componentPath, ClassName, newFile);
                return;
            }

            if (!Directory.Exists(PanelCodeGeneratePath))
            {
                Directory.CreateDirectory(PanelCodeGeneratePath);
            }
            
            if (!Directory.Exists(PanelNameCodeGeneratePath))
            {
                Directory.CreateDirectory(PanelNameCodeGeneratePath);
            }

            string path1 = Path.Combine(PanelCodeGeneratePath,PanelFileName).Replace("\\", "/");
            string path2 = Path.Combine(PanelNameCodeGeneratePath,StaticCSFileName).Replace("\\", "/");
            
            if (newFile)
            {
                CreateComponentFile(componentPath, ClassName);
                CreatePanelFile(path1, ClassName);
                CreatePanelNameFile(path2);
                return;
            }

            if (File.Exists(componentPath))
            {
                var tempStr = File.ReadAllText(componentPath);
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

                File.WriteAllText(componentPath, newContent);
            }
            else
            {
                CreateComponentFile(componentPath, ClassName);
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
                    
                    // 使用智能识别逻辑替代原来的@标记检查
                    if (ShouldBindNode(child))
                    {
                        ComponentData data = new ComponentData();
                        data.Item = child;
                        data.TempName = child.name;
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
                            componentDataParams.IsSelect = isSelect;
                            var typeStr = components[j].GetType().ToString();
                            componentDataParams.ComponentType = typeStr;
                            var strs = typeStr.Split('.');
                            // 使用智能命名生成属性名
                            componentDataParams.PropertyName = GenerateSmartPropertyName(child.name, strs[^1]);
                            componentDataParams.OriginalPropertyName = componentDataParams.PropertyName;
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
                        // GameObject不需要去重，因为很少有节点名以GameObject结尾
                        // 使用智能命名生成属性名
                        componentDataParamsObj.PropertyName = GenerateSmartPropertyName(child.name, "GameObject");
                        componentDataParamsObj.OriginalPropertyName = componentDataParamsObj.PropertyName;
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
            // 使用智能识别逻辑（根节点总是识别）
            bool isRootNode = (parent == root);
            if (ShouldBindNode(parent, isRootNode))
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
                    // 使用智能命名生成属性名（自动去除冗余）
                    componentDataParams.PropertyName = GenerateSmartPropertyName(parent.name, strs[^1]);
                    componentDataParams.OriginalPropertyName = componentDataParams.PropertyName;
                    data.ComponentList.Add(componentDataParams);
                }
                
                //-------------gameObject组件--------------
                ComponentDataParams componentDataParamsObj = new ComponentDataParams();
                componentDataParamsObj.GameObject = parent.gameObject;
                componentDataParamsObj.GameObjectName = parent.gameObject.name;
                componentDataParamsObj.Component = null;
                componentDataParamsObj.SelectIndex = 0;
                var typeStr1 = componentDataParamsObj.GameObject.GetType().ToString();
                componentDataParamsObj.ComponentType = null;
                componentDataParamsObj.IsSelect = isSelect;
                var strs1 = typeStr1.Split('.');
                // GameObject不需要去重，因为很少有节点名以GameObject结尾
                // 使用智能命名生成属性名
                componentDataParamsObj.PropertyName = GenerateSmartPropertyName(parent.name, "GameObject");
                componentDataParamsObj.OriginalPropertyName = componentDataParamsObj.PropertyName;
                data.ComponentList.Add(componentDataParamsObj);
                //-------------gameObject组件--------------

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

        private void GenerateItemScript(string path, string className, bool newFile)
        {
            if (newFile || !File.Exists(path))
            {
                CreateItemScriptFile(path, className);
                return;
            }

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

        private void CreateItemScriptFile(string path, string className)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("using UnityEngine;" + enter);
            stringBuilder.Append("using UnityEngine.UI;" + enter + enter);
            stringBuilder.Append($"namespace {NameSpaceName}" + enter);
            stringBuilder.Append("{" + enter);
            stringBuilder.Append(tab + "public class " + className + " : MonoBehaviour" + enter);
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
        
        /// <summary>
        /// 智能生成属性名
        /// </summary>
        private string GenerateSmartPropertyName(string nodeName, string componentTypeName)
        {
            string cleanName = CleanNodeName(nodeName);
            string originalCleanName = cleanName;
            
            // 检查节点名末尾是否包含组件类型的缩写
            if (_componentAbbreviations.TryGetValue(componentTypeName, out var abbrevs))
            {
                foreach (var abbrev in abbrevs)
                {
                    // 不区分大小写检查末尾
                    if (cleanName.Length > abbrev.Length && 
                        cleanName.EndsWith(abbrev, StringComparison.OrdinalIgnoreCase))
                    {
                        // 移除缩写部分
                        cleanName = cleanName.Substring(0, cleanName.Length - abbrev.Length);
                        break;
                    }
                }
            }
            
            // 如果去除后为空或太短，使用原始名称
            if (string.IsNullOrEmpty(cleanName) || cleanName.Length < 2)
            {
                cleanName = originalCleanName;
            }
            
            // 获取简写后缀
            string suffix = componentTypeName;
            if (_componentTypeToSuffix.TryGetValue(componentTypeName, out var shortSuffix))
            {
                suffix = shortSuffix;
            }

            return CapitalizeFirstLetter(cleanName + suffix);
        }
        
        //设置属性名首字母为小写
        private string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            return char.ToLower(input[0]) + input.Substring(1);
        }
        
        /// <summary>
        /// 批量选择指定类型的组件
        /// </summary>
        public void SelectComponentsByType(string componentType)
        {
            bool hasMatch = false;
            bool hasUnselected = false;

            foreach (var item in ComponentDataList)
            {
                foreach (var component in item.ComponentList)
                {
                    if (!IsComponentTypeMatch(component, componentType))
                    {
                        continue;
                    }

                    hasMatch = true;
                    if (!component.IsSelect)
                    {
                        hasUnselected = true;
                        break;
                    }
                }

                if (hasMatch && hasUnselected)
                {
                    break;
                }
            }

            if (!hasMatch)
            {
                return;
            }

            bool targetState = hasUnselected;

            foreach (var item in ComponentDataList)
            {
                foreach (var component in item.ComponentList)
                {
                    if (IsComponentTypeMatch(component, componentType))
                    {
                        component.IsSelect = targetState;
                    }
                }
            }
        }

        private bool IsComponentTypeMatch(ComponentDataParams component, string componentType)
        {
            if (string.IsNullOrEmpty(componentType))
            {
                return true;
            }

            if (componentType == "GameObject" && component.GameObject != null && component.ComponentType == null)
            {
                return true;
            }

            if (component.ComponentType != null && component.ComponentType.Contains(componentType))
            {
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// 取消所有选择
        /// </summary>
        public void DeselectAllComponents()
        {
            foreach (var item in ComponentDataList)
            {
                foreach (var component in item.ComponentList)
                {
                    component.IsSelect = false;
                }
            }
        }
        
        /// <summary>
        /// 全选所有组件
        /// </summary>
        public void SelectAllComponents()
        {
            foreach (var item in ComponentDataList)
            {
                foreach (var component in item.ComponentList)
                {
                    component.IsSelect = true;
                }
            }
        }
        
        /// <summary>
        /// 设置指定节点下所有组件的选择状态
        /// </summary>
        public void SetNodeComponentsSelection(ComponentData nodeData, bool isSelect)
        {
            if (nodeData == null)
            {
                return;
            }

            foreach (var component in nodeData.ComponentList)
            {
                component.IsSelect = isSelect;
            }
        }
        
        /// <summary>
        /// 获取所有唯一的组件类型列表
        /// </summary>
        public List<string> GetAllComponentTypes()
        {
            HashSet<string> types = new HashSet<string>();
            foreach (var item in ComponentDataList)
            {
                foreach (var component in item.ComponentList)
                {
                    if (!string.IsNullOrEmpty(component.ComponentType))
                    {
                        // 只取类型名称的最后一部分（如 UnityEngine.UI.Button -> Button）
                        var parts = component.ComponentType.Split('.');
                        types.Add(parts[^1]);
                    }
                    else if (component.GameObject != null)
                    {
                        types.Add("GameObject");
                    }
                }
            }
            return types.OrderBy(t => t).ToList();
        }

        public void AddScriptToPrefab(GameObject prefab)
        {
            /*var assembly = Assembly.Load("Assembly-CSharp");
            if (assembly == null)
            {
                Debug.LogError($"无法找到程序集:");
                return;
            }*/
            
            //最耗时，但最全面
            foreach (var assemblys in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assemblys.GetTypes())
                {
                    string targetTypeName = isItemPrefab ? ClassName : PanelFileName.Replace(".cs", "");
                    if (type.Name == targetTypeName)
                    {
                        ScriptType = type;
                        break;
                    }
                }

                if (ScriptType != null)
                {
                    break;
                }
            }

            /*var types = assembly.GetTypes();
            foreach (var type in types)
            {
                if (type.Name == PanelFileName.Replace(".cs", ""))
                {
                    ScriptType = type;
                    break;
                }
            }*/

            if (ScriptType == null)
            {
                Debug.LogError($"无法找到类名: {(isItemPrefab ? ClassName : PanelFileName.Replace(".cs", ""))}");
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

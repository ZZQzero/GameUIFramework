using UnityEditor;
using UnityEngine;

namespace GameUI.Editor
{
    public class GameObjectPoolWindow : EditorWindow
    {
        
        private static GameObjectPoolWindow _uiWindow;
        private Vector2 _scrollPos;
        private GUIStyle _style = new GUIStyle();
        private GUIStyle _style1 = new GUIStyle();
        private GUIStyle _style2 = new GUIStyle();
        private GUIStyle _style3 = new GUIStyle();
        private GUIStyle _style4 = new GUIStyle();
        private int totalCount;
        private int totalInstanceCount;
        
        [MenuItem("GameUI/对象池数据查看")]
        public static void ShowEditorWindow()
        {
            _uiWindow = GetWindow<GameObjectPoolWindow>();
            _uiWindow.Show();
        }
        
        private void OnGUI()
        {
            _style.normal.textColor = Color.red;
            _style1.normal.textColor = Color.green;
            _style2.normal.textColor = Color.cyan;
            _style3.normal.textColor = Color.magenta;
            _style4.normal.textColor = Color.yellow;
            
            EditorGUILayout.LabelField("对象总数：" + totalCount, _style4);
            EditorGUILayout.LabelField("对象池总数：" + GameObjectPool.Instance.GetPoolDic().Count, _style4);
            EditorGUILayout.LabelField("对象池类型总数：" + GameObjectPool.Instance.GetPoolTypeNameDic().Count, _style4);
            EditorGUILayout.LabelField("实例总数（包含活跃和池中）：" + totalInstanceCount, _style4);
            EditorGUILayout.Space(10);
            
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            totalCount = 0;
            totalInstanceCount = 0;
            
            // ===== 池中不活跃的对象 =====
            EditorGUILayout.LabelField("--------------------被回收到池中不活跃的对象------------------", _style);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("回收池数量：" + GameObjectPool.Instance.GetPoolDic().Count, _style);
            
            foreach (var item in GameObjectPool.Instance.GetPoolDic())
            {
                GUILayout.BeginVertical();
                var (assetName, poolType) = item.Key;
                EditorGUILayout.LabelField($"资源名：{assetName} | 类型：{poolType} | 池中数量：{item.Value.Count}");
                totalCount += item.Value.Count; 
                GUILayout.EndVertical();
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("--------------------被回收到池中不活跃的对象------------------", _style);
            EditorGUILayout.Space(10);
            
            // ===== 活跃对象 =====
            EditorGUILayout.LabelField("-------------------------被激活的对象-----------------------", _style1);
            EditorGUILayout.LabelField("活跃对象池数量：" + GameObjectPool.Instance.GetActivePoolDic().Count, _style1);
            
            foreach (var item in GameObjectPool.Instance.GetActivePoolDic())
            {
                GUILayout.BeginVertical();
                EditorGUILayout.LabelField($"类型：{(PoolType)item.Key} | 活跃对象数量：{item.Value.Count}");
                totalCount += item.Value.Count; 
                GUILayout.EndVertical();
            }
            
            EditorGUILayout.LabelField("-------------------------被激活的对象-----------------------", _style1);
            EditorGUILayout.Space(10);
            
            // ===== 按类型分组的资源 =====
            EditorGUILayout.LabelField("--------------------------对象池类型------------------------", _style2);
            EditorGUILayout.LabelField("对象池类型数量：" + GameObjectPool.Instance.GetPoolTypeNameDic().Count, _style2);
            
            foreach (var item in GameObjectPool.Instance.GetPoolTypeNameDic())
            {
                GUILayout.BeginVertical();
                EditorGUILayout.LabelField($"类型：{item.Key} | 包含资源数量：{item.Value.Count}", _style2);
                
                foreach (var assetName in item.Value)
                {
                    EditorGUILayout.LabelField($"  └─ 资源：{assetName}");
                }
                
                GUILayout.EndVertical();
            }

            EditorGUILayout.LabelField("--------------------------对象池类型------------------------", _style2);
            EditorGUILayout.Space(10);
            
            // ===== 资源句柄和实例计数 =====
            EditorGUILayout.LabelField("---------------------------资源实例统计-------------------------", _style3);
            
            GUILayout.BeginVertical();
            EditorGUILayout.LabelField("资源句柄数量：" + GameObjectPool.Instance.GetAssetHandleDic().Count, _style3);

            var instanceCountDic = GameObjectPool.Instance.GetInstanceCountDic();
            foreach (var item in GameObjectPool.Instance.GetAssetHandleDic())
            {
                var (assetName, poolType) = item.Key;
                int instanceCount = instanceCountDic.TryGetValue(item.Key, out var count) ? count : 0;
                
                EditorGUILayout.LabelField($"资源：{assetName} | 类型：{poolType} | 实例数量：{instanceCount}", _style3);
                totalInstanceCount += instanceCount;
            }

            GUILayout.EndVertical();
            EditorGUILayout.LabelField("---------------------------资源实例统计-------------------------", _style3);
            EditorGUILayout.Space(10);
            
            GUILayout.EndScrollView();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新"))
            {
                Repaint();
            }
            
            if (GUILayout.Button("销毁所有对象池"))
            {
                if (EditorUtility.DisplayDialog("确认", "确定要销毁所有对象池吗？", "确定", "取消"))
                {
                    GameObjectPool.Instance.DestroyAllObjectPool();
                    Repaint();
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
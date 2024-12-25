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
        
        [MenuItem("GameUI/对象池数据查看")]
        public static void ShowEditorWindow()
        {
            _uiWindow = GetWindow<GameObjectPoolWindow>();
            _uiWindow.Show();
        }
        
        private void OnGUI()
        {
            _scrollPos = GUILayout.BeginScrollView(_scrollPos); //1
            _style.normal.textColor = Color.red;
            _style1.normal.textColor = Color.green;
            _style2.normal.textColor = Color.cyan;
            _style3.normal.textColor = Color.magenta;
            
            
            EditorGUILayout.LabelField("--------------------被回收到池中不活跃的对象------------------", _style);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("回收池数量：" + GameObjectPool.Instance.GetPoolDic().Count, _style);
            foreach (var item in GameObjectPool.Instance.GetPoolDic())
            {
                GUILayout.BeginVertical();
                EditorGUILayout.LabelField("对象池名字：" + item.Key + " ----> 池中对象数量：" + item.Value.Count);
                GUILayout.EndVertical();
            }
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("--------------------被回收到池中不活跃的对象------------------",_style);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("-------------------------被激活的对象-----------------------",_style1);
            EditorGUILayout.LabelField("对象池数量：" + GameObjectPool.Instance.GetActivePoolDic().Count, _style1);
            foreach (var item in GameObjectPool.Instance.GetActivePoolDic())
            {
                GUILayout.BeginVertical();
                EditorGUILayout.LabelField("对象池名字：" + item.Key + " ----> 池中对象数量：" + item.Value.Count);
                GUILayout.EndVertical();
            }            
            EditorGUILayout.LabelField("-------------------------被激活的对象-----------------------",_style1);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("--------------------------对象池类型------------------------",_style2);

            EditorGUILayout.LabelField("对象池类型数量：" + GameObjectPool.Instance.GetPoolTypeNameDic().Count, _style2);
            
            GUILayout.BeginHorizontal();
            foreach (var item in GameObjectPool.Instance.GetPoolTypeNameDic())
            {
                GUILayout.BeginVertical();
                EditorGUILayout.LabelField("对象池类型：" + (PoolType)item.Key + " ----> 该类型对象池数量：" + item.Value.Count, _style2);
                foreach (var value in item.Value)
                {
                    EditorGUILayout.LabelField("对象池类型：" + (PoolType)item.Key + " ----> 该类型对象池名字：" + value);
                }
                GUILayout.EndVertical();

            }
            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField("--------------------------对象池类型------------------------",_style2);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("---------------------------对象句柄-------------------------",_style3);
            
            GUILayout.BeginVertical();
            EditorGUILayout.LabelField("对象句柄数量：" + GameObjectPool.Instance.GetAssetHandleDic().Count, _style3);

            foreach (var item in GameObjectPool.Instance.GetAssetHandleDic())
            {
                EditorGUILayout.LabelField("对象句柄名字：" + item.Key);
            }

            GUILayout.EndVertical();
            EditorGUILayout.LabelField("---------------------------对象句柄-------------------------",_style3);
            EditorGUILayout.Space(10);
            
            GUILayout.EndScrollView(); //1
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新"))
            {
                OnGUI();
            }
            GUILayout.EndHorizontal();
        }
    }
}

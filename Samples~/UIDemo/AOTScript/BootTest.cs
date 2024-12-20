using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameUI;
using UnityEngine;
using YooAsset;

/// <summary>
/// 远端资源地址查询服务类
/// </summary>
public class RemoteServices : IRemoteServices
{
    private readonly string _defaultHostServer;
    private readonly string _fallbackHostServer;

    public RemoteServices(string defaultHostServer, string fallbackHostServer)
    {
        _defaultHostServer = defaultHostServer;
        _fallbackHostServer = fallbackHostServer;
    }
    string IRemoteServices.GetRemoteMainURL(string fileName)
    {
        return $"{_defaultHostServer}/{fileName}";
    }
    string IRemoteServices.GetRemoteFallbackURL(string fileName)
    {
        return $"{_fallbackHostServer}/{fileName}";
    }
}

public class BootTest : MonoBehaviour
{
    /// <summary>
    /// 资源系统运行模式
    /// </summary>
    public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;
    public string appVersion = "V1.0";
    public string hostServerIP = "http://127.0.0.1:8080";
    
    private string defaultPackageName = "DefaultPackage";
    void Awake()
    {
        Debug.Log($"资源系统运行模式：{PlayMode}");
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        Init().Forget();
    }

    private async UniTask Init()
    {
        // 初始化资源系统
        YooAssets.Initialize();
        
        GameUIManager.Instance.Init();
        await LoadLocalPackage(defaultPackageName,PlayMode);
    }
    
    private async UniTask LoadLocalPackage(string packageName,EPlayMode mode)
    {
        var package = YooAssets.CreatePackage(packageName);

        InitializationOperation initializationOperation = null;

        if (mode == EPlayMode.EditorSimulateMode)
        {
            var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(EDefaultBuildPipeline.BuiltinBuildPipeline, packageName);
            var createParameters = new EditorSimulateModeParameters();
            createParameters.EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(simulateBuildResult);
            initializationOperation = package.InitializeAsync(createParameters);
        }
        else if (mode == EPlayMode.OfflinePlayMode)
        {
            var createOfflineParameters = new OfflinePlayModeParameters();
            createOfflineParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            initializationOperation = package.InitializeAsync(createOfflineParameters);
        }
        else if (mode == EPlayMode.HostPlayMode)
        {
            string defaultHostServer = GetHostServerURL();
            string fallbackHostServer = GetHostServerURL();
            IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
            var createParameters = new HostPlayModeParameters();
            createParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            createParameters.CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
            initializationOperation = package.InitializeAsync(createParameters);
        }
        
        await initializationOperation;
        if (initializationOperation != null && initializationOperation.Status == EOperationStatus.Succeed)
        {
            Debug.Log("资源包初始化成功！");
            var operation = package.RequestPackageVersionAsync();
            await operation;
            if (operation.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"请求资源包版本号失败：{operation.Error}");
                return;
            }
            
            Debug.Log($"Request package version : {operation.PackageVersion}");
            var updateManifest = package.UpdatePackageManifestAsync(operation.PackageVersion);
            await updateManifest;
            if (updateManifest.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"更新资源清单失败：{updateManifest.Error}");
                return;
            }
            
            Debug.Log("更新资源清单成功！");
            YooAssets.SetDefaultPackage(package);
            GameUIManager.Instance.SetPackage(package);
            GameUIManager.Instance.OpenUI(GameUIName.PatchPanel, package).Forget();
        }
        else
        {
            Debug.LogError($"资源包初始化失败：{initializationOperation.Error}");
        }
    }

    /// <summary>
    /// 获取资源服务器地址
    /// </summary>
    private string GetHostServerURL()
    {
        //string hostServerIP = "http://10.0.2.2"; //安卓模拟器地址
        /*string hostServerIP = Boot.Instance.hostServerIP;
        string appVersion = Boot.Instance.appVersion;*/

#if UNITY_EDITOR
        if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
            return $"{hostServerIP}/CDN/Android/{appVersion}";
        else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
            return $"{hostServerIP}/CDN/IPhone/{appVersion}";
        else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL)
            return $"{hostServerIP}/CDN/WebGL/{appVersion}";
        else
            return $"{hostServerIP}/{appVersion}";
#else
        if (Application.platform == RuntimePlatform.Android)
            return $"{hostServerIP}/CDN/Android/{appVersion}";
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
            return $"{hostServerIP}/CDN/IPhone/{appVersion}";
        else if (Application.platform == RuntimePlatform.WebGLPlayer)
            return $"{hostServerIP}/CDN/WebGL/{appVersion}";
        else
            return $"{hostServerIP}/{appVersion}";
#endif
    }
    
    private void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.A))
        {
            GameUIManager.Instance.CloseUI("PatchPanel");
        }
        
        if (Input.GetKeyDown(KeyCode.B))
        {
            GameUIManager.Instance.OpenUI("PatchPanel", null).Forget();
        }
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            GameUIManager.Instance.CloseAndDestroyUI("PatchPanel");
        }
        
        if (Input.GetKeyDown(KeyCode.D))
        {
            StartCoroutine(UnloadUnusedAssets());
        }*/
    }
    
    private IEnumerator UnloadUnusedAssets()
    {
        var package = YooAssets.GetPackage("AotPackage");
        var operation = package.UnloadUnusedAssetsAsync();
        yield return operation;
    }
}

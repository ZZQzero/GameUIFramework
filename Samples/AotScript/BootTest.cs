using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameUI;
using UnityEngine;
using YooAsset;

public class BootTest : MonoBehaviour
{
    /// <summary>
    /// 资源系统运行模式
    /// </summary>
    public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;
    
    void Awake()
    {
        Debug.Log($"资源系统运行模式：{PlayMode}");
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        StartAA().Forget();
    }

    private async UniTask StartAA()
    {
        // 初始化资源系统
        YooAssets.Initialize();
        
        GameUIManager.Instance.Init();
        var aotPackage = await LoadLocalPackage();
        GameUIManager.Instance.SetPackage(aotPackage);
        //await GameUIManager.Instance.OpenUI(GameUIName.PatchPanel, null);
    }
    
    private async UniTask<ResourcePackage> LoadLocalPackage()
    {
        var aotPackage = YooAssets.CreatePackage("AotPackage");

        InitializationOperation initializationOperation = null;

        var createParameters = new OfflinePlayModeParameters();
        createParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
        initializationOperation = aotPackage.InitializeAsync(createParameters);

        await initializationOperation;
        if (initializationOperation.Status == EOperationStatus.Succeed)
        {
            Debug.Log("资源包初始化成功！");
            var operation = aotPackage.RequestPackageVersionAsync();
            await operation;
            if (operation.Status == EOperationStatus.Succeed)
            {
                Debug.Log($"Request package version : {operation.PackageVersion}");
                var updateManifest = aotPackage.UpdatePackageManifestAsync(operation.PackageVersion);
                await updateManifest;
                if (updateManifest.Status == EOperationStatus.Succeed)
                {
                    Debug.Log("更新资源清单成功！");
                }
                else
                {
                    Debug.LogError($"更新资源清单失败：{updateManifest.Error}");
                }
            }
            else
            {
                Debug.LogError($"请求资源包版本号失败：{operation.Error}");
            }
        }
        else
        {
            Debug.LogError($"资源包初始化失败：{initializationOperation.Error}");
        }

        return aotPackage;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
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
        }
    }
    
    private IEnumerator UnloadUnusedAssets()
    {
        var package = YooAssets.GetPackage("AotPackage");
        var operation = package.UnloadUnusedAssetsAsync();
        yield return operation;
    }
}

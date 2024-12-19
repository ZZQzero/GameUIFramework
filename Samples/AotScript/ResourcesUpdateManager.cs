using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

public class ResourcesUpdateManager
{
    private static ResourcesUpdateManager _instance;
    public static ResourcesUpdateManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new ResourcesUpdateManager();
            return _instance;
        }
    }
    
    private ResourcesUpdateManager()
    {
    }
    
    public async UniTask<bool> BeginDownload(ResourceDownloaderOperation downloader,
        DownloaderOperation.OnDownloadError errorCallback,
        DownloaderOperation.OnDownloadProgress progressCallback)
    {
        downloader.OnDownloadErrorCallback = errorCallback;
        downloader.OnDownloadProgressCallback = progressCallback;
        downloader.BeginDownload();
        await downloader;

        // 检测下载结果
        if (downloader.Status != EOperationStatus.Succeed)
        {
            Debug.LogError("下载失败");
            return false;
        }

        return true;
    }
}

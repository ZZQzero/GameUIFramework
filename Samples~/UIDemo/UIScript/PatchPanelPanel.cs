using System;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;

namespace GameUI
{
	public partial class PatchPanelPanel : GameUIBase
	{
		private ResourceDownloaderOperation _downloader;
		private ResourcePackage _package;
		public override void OnInitUI()
		{
			base.OnInitUI();
			#region Auto Generate Code
			InitData();
			#endregion Auto Generate Code
			
		}
		public override void OnOpenUI()
		{
			base.OnOpenUI();
			if(Data != null && Data is ResourcePackage package)
			{
				_package = package;
				OnCreateDownLoad(package);
			}
		}

		private void OnDownloadProgress(int totaldownloadcount, int currentdownloadcount, long totaldownloadbytes, long currentdownloadbytes)
		{
			SliderSlider.value = (float)currentdownloadcount / totaldownloadcount;
			string currentSizeMB = (currentdownloadbytes / 1048576f).ToString("f1");
			string totalSizeMB = (totaldownloadbytes / 1048576f).ToString("f1");
			TxttipsText.text = $"{currentdownloadcount}/{totaldownloadcount} {currentSizeMB}MB/{totalSizeMB}MB";
		}

		private void OnDownError(string filename, string error)
		{
			MessageBoxData messageBoxData = new MessageBoxData();
			messageBoxData.Content = $"下载失败，{filename} , {error}";
			messageBoxData.OnOk = () =>
			{
				OnCreateDownLoad(_package);
			};
			GameUIManager.Instance.OpenUI(GameUIName.MessageBoxPanel, messageBoxData).Forget();
		}

		private void OnCreateDownLoad(ResourcePackage package)
		{
			int downloadingMaxNum = 10;
			int failedTryAgain = 3;
			_downloader = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);
			if (_downloader.TotalDownloadCount == 0)
			{
				TxttipsText.text = "不需要更新资源";
				ChangeScene();
			}
			else
			{
				MessageBoxData messageBoxData = new MessageBoxData();
				float sizeMB = _downloader.TotalDownloadBytes / 1048576f;
				sizeMB = Mathf.Clamp(sizeMB, 0.1f, float.MaxValue);
				string totalSizeMB = sizeMB.ToString("f1");
				messageBoxData.Content = $"发现新资源需要更新，共{_downloader.TotalDownloadCount}个文件，总大小{totalSizeMB}MB";
				messageBoxData.OnOk = async ()=>
				{
					var succeed = await ResourcesUpdateManager.Instance.BeginDownload(_downloader, OnDownError, OnDownloadProgress);
					if (succeed)
					{
						//TODO
						Debug.LogError("下载成功！");
						var clearOperation = package.ClearUnusedBundleFilesAsync();
						clearOperation.Completed += OperationCompleted;
						//资源下载结束
					}
				};
			        
				GameUIManager.Instance.OpenUI(GameUIName.MessageBoxPanel, messageBoxData).Forget();
			}
		}
		
		private void OperationCompleted(AsyncOperationBase obj)
		{
			ChangeScene();
		}

		private void ChangeScene()
		{
			YooAssets.LoadSceneAsync("scene_home");
			GameUIManager.Instance.OpenUI(GameUIName.UIHome,null).Forget();
			CloseSelf();
		}

		public override void OnCloseUI()
		{
			base.OnCloseUI();
		}
		public override void OnDestroyUI()
		{
			base.OnDestroyUI();
		}
	}
}

using UnityEngine;
using UnityEngine.UI;
using YooAsset;

namespace GameUI
{
	public partial class UIHomePanel : GameUIBase
	{
		private ResourcePackage _package;
		public override void OnInitUI()
		{
			base.OnInitUI();
			#region Auto Generate Code
			InitData();
			#endregion Auto Generate Code
			_package = YooAssets.GetPackage("DefaultPackage");
			StartButton.onClick.AddListener(() =>
			{
				CloseSelf();
				YooAssets.LoadSceneAsync("scene_battle");
			});
			AboutButton.onClick.AddListener(() =>
			{
				AboutViewRectTransform.gameObject.SetActive(true);
			});
			MaskButton.onClick.AddListener(() =>
			{
				AboutViewRectTransform.gameObject.SetActive(false);
			});
		}
		public override void OnOpenUI()
		{
			base.OnOpenUI();
			VersionText.text = _package.GetPackageVersion();
			
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

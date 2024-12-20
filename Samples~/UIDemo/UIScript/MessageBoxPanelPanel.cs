using UnityEngine;
using UnityEngine.UI;
using YooAsset;

namespace GameUI
{
	public class MessageBoxData
	{
		public string Content;
		public System.Action OnOk;
	}
	
	public partial class MessageBoxPanelPanel : GameUIBase
	{
		private MessageBoxData _messageBoxData;
		public override void OnInitUI()
		{
			base.OnInitUI();
			#region Auto Generate Code
			InitData();
			#endregion Auto Generate Code
			BtnokButton.onClick.AddListener(OnOkClick);
		}
		public override void OnOpenUI()
		{
			base.OnOpenUI();
			if (Data != null && Data is MessageBoxData messageBoxData)
			{
				_messageBoxData = messageBoxData;
				TxtcontentText.text = messageBoxData.Content;
			}
		}
		public override void OnCloseUI()
		{
			base.OnCloseUI();
		}
		public override void OnDestroyUI()
		{
			base.OnDestroyUI();
		}

		private void OnOkClick()
		{
			if (_messageBoxData != null)
			{
				_messageBoxData.OnOk?.Invoke();
				CloseSelf();
			}
		}
	}
}

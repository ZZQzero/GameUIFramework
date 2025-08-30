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
			btnokButton.onClick.AddListener(OnOkClick);
		}
		public override void OnOpenUI()
		{
			base.OnOpenUI();
			if (Data != null && Data is MessageBoxData messageBoxData)
			{
				_messageBoxData = messageBoxData;
				txtcontentTextMeshProUGUI.text = messageBoxData.Content;
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

using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
	public partial class MessageBoxPanelPanel
	{
		#region Auto Generate Code
		private UnityEngine.UI.Text TxtcontentText;
		private UnityEngine.UI.Button BtnokButton;

		public void InitData()
		{
			TxtcontentText = transform.Find("MessgeBox/@txt_content").GetComponent<UnityEngine.UI.Text>();
			BtnokButton = transform.Find("MessgeBox/@btn_ok").GetComponent<UnityEngine.UI.Button>();
		}
		#endregion Auto Generate Code
	}
}

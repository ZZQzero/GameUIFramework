using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
	public partial class RedPanelPanel
	{
		#region Auto Generate Code
		private UnityEngine.RectTransform RedPanelRectTransform;
		private TMPro.TextMeshProUGUI TextTMPTextMeshProUGUI;

		public void InitData()
		{
			RedPanelRectTransform = GetComponent<UnityEngine.RectTransform>();
			TextTMPTextMeshProUGUI = transform.Find("@TextTMP").GetComponent<TMPro.TextMeshProUGUI>();
		}
		#endregion Auto Generate Code
	}
}

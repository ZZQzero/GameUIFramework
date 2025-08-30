using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
	public partial class PatchPanelPanel
	{
		#region Auto Generate Code
		private UnityEngine.RectTransform TitleRectTransform;
		private TMPro.TextMeshProUGUI TitleTextMeshProUGUI;
		private UnityEngine.RectTransform SliderRectTransform;
		private UnityEngine.UI.Slider SliderSlider;
		private UnityEngine.RectTransform TxttipsRectTransform;
		private TMPro.TextMeshProUGUI TxttipsTextMeshProUGUI;
		private UnityEngine.RectTransform RedPanelRectTransform;
		private GameUI.RedPanelPanel RedPanelRedPanelPanel;

		public void InitData()
		{
			TitleRectTransform = transform.Find("@title").GetComponent<UnityEngine.RectTransform>();
			TitleTextMeshProUGUI = transform.Find("@title").GetComponent<TMPro.TextMeshProUGUI>();
			SliderRectTransform = transform.Find("@Slider").GetComponent<UnityEngine.RectTransform>();
			SliderSlider = transform.Find("@Slider").GetComponent<UnityEngine.UI.Slider>();
			TxttipsRectTransform = SliderRectTransform.transform.Find("@txt_tips").GetComponent<UnityEngine.RectTransform>();
			TxttipsTextMeshProUGUI = SliderRectTransform.transform.Find("@txt_tips").GetComponent<TMPro.TextMeshProUGUI>();
			RedPanelRectTransform = transform.Find("@RedPanel").GetComponent<UnityEngine.RectTransform>();
			RedPanelRedPanelPanel = transform.Find("@RedPanel").GetComponent<GameUI.RedPanelPanel>();
		}

		#endregion Auto Generate Code
	}
}

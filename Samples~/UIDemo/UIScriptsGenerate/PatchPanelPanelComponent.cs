using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
	public partial class PatchPanelPanel
	{
		#region Auto Generate Code
		private UnityEngine.RectTransform TitleRectTransform;
		private UnityEngine.UI.Text TitleText;
		private UnityEngine.RectTransform SliderRectTransform;
		private UnityEngine.UI.Slider SliderSlider;
		private UnityEngine.RectTransform TxttipsRectTransform;
		private UnityEngine.UI.Text TxttipsText;

		public void InitData()
		{
			TitleRectTransform = transform.Find("@title").GetComponent<UnityEngine.RectTransform>();
			TitleText = transform.Find("@title").GetComponent<UnityEngine.UI.Text>();
			SliderRectTransform = transform.Find("@Slider").GetComponent<UnityEngine.RectTransform>();
			SliderSlider = transform.Find("@Slider").GetComponent<UnityEngine.UI.Slider>();
			TxttipsRectTransform = SliderRectTransform.transform.Find("@txt_tips").GetComponent<UnityEngine.RectTransform>();
			TxttipsText = SliderRectTransform.transform.Find("@txt_tips").GetComponent<UnityEngine.UI.Text>();
		}

		#endregion Auto Generate Code
	}
}

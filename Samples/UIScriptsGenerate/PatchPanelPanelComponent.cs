using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
	public partial class PatchPanelPanel
	{
		#region Auto Generate Code
		private UnityEngine.UI.Text TitleText;
		private UnityEngine.UI.Slider SliderSlider;
		private UnityEngine.UI.Text TxttipsText;

		public void InitData()
		{
			TitleText = transform.Find("@title").GetComponent<UnityEngine.UI.Text>();
			SliderSlider = transform.Find("@Slider").GetComponent<UnityEngine.UI.Slider>();
			TxttipsText = SliderSlider.transform.Find("@txt_tips").GetComponent<UnityEngine.UI.Text>();
		}
		#endregion Auto Generate Code
	}
}

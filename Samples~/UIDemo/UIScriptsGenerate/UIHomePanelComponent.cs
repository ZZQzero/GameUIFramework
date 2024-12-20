using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
	public partial class UIHomePanel
	{
		#region Auto Generate Code
		private UnityEngine.UI.Button StartButton;
		private UnityEngine.UI.Image AboutImage;
		private UnityEngine.UI.Button AboutButton;
		private UnityEngine.UI.Text VersionText;
		private UnityEngine.RectTransform AboutViewRectTransform;
		private UnityEngine.UI.Image MaskImage;
		private UnityEngine.UI.Button MaskButton;

		public void InitData()
		{
			StartButton = transform.Find("@Start").GetComponent<UnityEngine.UI.Button>();
			AboutImage = transform.Find("@About").GetComponent<UnityEngine.UI.Image>();
			AboutButton = transform.Find("@About").GetComponent<UnityEngine.UI.Button>();
			VersionText = transform.Find("@version").GetComponent<UnityEngine.UI.Text>();
			AboutViewRectTransform = transform.Find("@AboutView").GetComponent<UnityEngine.RectTransform>();
			MaskImage = AboutViewRectTransform.transform.Find("@mask").GetComponent<UnityEngine.UI.Image>();
			MaskButton = AboutViewRectTransform.transform.Find("@mask").GetComponent<UnityEngine.UI.Button>();
		}
		#endregion Auto Generate Code
	}
}

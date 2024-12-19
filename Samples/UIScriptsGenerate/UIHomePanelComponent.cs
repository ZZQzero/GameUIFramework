using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
	public partial class UIHomePanel
	{
		#region Auto Generate Code
		private UnityEngine.UI.Button StartButton;
		private UnityEngine.UI.Button AboutButton;
		private UnityEngine.UI.Text VersionText;
		private UnityEngine.RectTransform AboutViewRectTransform;
		private UnityEngine.UI.Button MaskButton;

		public void InitData()
		{
			StartButton = transform.Find("@Start").GetComponent<UnityEngine.UI.Button>();
			AboutButton = transform.Find("@About").GetComponent<UnityEngine.UI.Button>();
			VersionText = transform.Find("@version").GetComponent<UnityEngine.UI.Text>();
			AboutViewRectTransform = transform.Find("@AboutView").GetComponent<UnityEngine.RectTransform>();
			MaskButton = AboutViewRectTransform.transform.Find("@mask").GetComponent<UnityEngine.UI.Button>();
		}

		#endregion Auto Generate Code
	}
}

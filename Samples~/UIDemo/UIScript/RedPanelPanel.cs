using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
	public partial class RedPanelPanel : MonoBehaviour
	{
		public ERedDotFuncType redType;
		
		private RedDotData redDotData;
		private void Awake()
		{
			var data = RedDotManager.Instance.GetRedDotData(redType);
			if (data != null)
			{
				redDotData = data;
				data.OnRedDotChangedAction = OnRedDotChanged;
			}
		}

		private void OnEnable()
		{
			if (redDotData == null)
			{
				return;
			}
			if (redDotData.Count == 0)
			{
				gameObject.SetActive(false);
			}
			else
			{
				gameObject.SetActive(true);
			}
		}

		private void OnRedDotChanged(ERedDotFuncType changeType, int count)
		{
			textTMPTextMeshProUGUI.text = count.ToString();
			if (count == 0)
			{
				gameObject.SetActive(false);
			}
			else
			{
				gameObject.SetActive(true);
			}
		}
	}
}

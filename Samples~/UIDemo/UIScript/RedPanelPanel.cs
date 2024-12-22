using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
	public partial class RedPanelPanel : MonoBehaviour
	{
		public ERedDotFuncType redType;
		private void Awake()
		{
			InitData();
			var data = RedDotManager.Instance.GetRedDotData(redType);
			if (data != null)
			{
				data.OnRedDotChangedAction = OnRedDotChanged;
			}
			
		}

		private void OnRedDotChanged(ERedDotFuncType changeType, int count)
		{
			Debug.LogError($"主界面：{changeType}  {count}");
			TextTMPTextMeshProUGUI.text = count.ToString();
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

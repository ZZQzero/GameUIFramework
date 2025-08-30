using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
	public partial class ScrollCell1Panel : GameUIBase
	{
		public override void OnInitUI()
		{
			base.OnInitUI();
		}
		public override void OnOpenUI()
		{
			base.OnOpenUI();
		}
		public override void OnRefreshUI()
		{
			base.OnRefreshUI();
		}
		public override void OnCloseUI()
		{
			base.OnCloseUI();
		}
		public override void OnDestroyUI()
		{
			base.OnDestroyUI();
		}

		public void ScrollCellIndex(int idx,int data)
		{
			text1TextMeshProUGUI.text = idx.ToString();
		}
	}
}

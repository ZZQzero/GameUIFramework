using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
	public partial class ScrollPanelPanel : GameUILoopScrollBase
	{
		private List<int> dataList;
		// Implement your own Cache Pool here. The following is just for example.
		private Stack<Transform> pool = new Stack<Transform>();
		public override void OnInitUI()
		{
			#region Auto Generate Code
			InitData();
			#endregion Auto Generate Code

			ScrollRect = ScrollPanelLoopVerticalScrollRect;
			base.OnInitUI();
		}
		public override void OnOpenUI()
		{
			base.OnOpenUI();
			if (Data is List<int> data)
			{
				dataList = data;
				ScrollPanelLoopVerticalScrollRect.totalCount = dataList.Count;
				ScrollPanelLoopVerticalScrollRect.RefillCells();
			}
		}
		public override void OnCloseUI()
		{
			base.OnCloseUI();
		}
		public override void OnDestroyUI()
		{
			base.OnDestroyUI();
		}

		public override GameObject GetObject(int index)
		{
			if (pool.Count == 0)
			{
				return Instantiate(item);
			}
			Transform candidate = pool.Pop();
			candidate.gameObject.SetActive(true);
			return candidate.gameObject;
		}

		public override void ReturnObject(Transform trans)
		{
			trans.gameObject.SetActive(false);
			trans.SetParent(transform, false);
			pool.Push(trans);
		}

		public override void ProvideData(Transform trans, int idx)
		{
			base.ProvideData(trans, idx);
			var data = dataList[idx];
			trans.GetComponent<ScrollCell1Panel>().ScrollCellIndex(idx,data);
		}
	}
}

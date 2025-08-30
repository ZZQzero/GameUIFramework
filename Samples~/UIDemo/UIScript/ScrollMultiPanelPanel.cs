using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
	public partial class ScrollMultiPanelPanel : GameUILoopScrollMultiBase
	{
		protected LoopListBankData BankData;
		// Implement your own Cache Pool here. The following is just for example.
		protected Stack<Transform> pool = new Stack<Transform>();
		protected Dictionary<string, Stack<GameObject>> _poolType = new Dictionary<string, Stack<GameObject>>();
		public override void OnInitUI()
		{
			#region Auto Generate Code
			InitData();
			#endregion Auto Generate Code
			BankData = new LoopListBankData();
			ScrollRect = ScrollMultiPanelLoopVerticalScrollRectMulti;
			base.OnInitUI();
		}
		public override void OnOpenUI()
		{
			base.OnOpenUI();
			if (Data is List<ScrollMultiData> datalist)
			{
				BankData.InitLoopListBankDataList(datalist);
				ScrollRect.totalCount = BankData.GetLoopListBankDataListCount();
				ScrollRect.RefillCells();
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
			GameObject candidate = null;

			var data = BankData.GetCellPreferredTypeIndex<ScrollMultiData>(index);
			if (ItemList.Count <= data.TypeIndex)
			{
				Debug.LogError("TempPrefab is null!");
				return null;
			}
            
			var tempPrefab = ItemList[data.TypeIndex];
			var obj = GameObjectPool.Instance.GetObjectSync(tempPrefab.name,PoolType.Normal);
			return obj;
			/*if (!_poolType.TryGetValue(data.TypeIndex.ToString(), out var tempStack))
			{
				tempStack = new Stack<GameObject>();
				_poolType.Add(data.TypeIndex.ToString(),tempStack);
			}

			if (tempStack.Count == 0)
			{
				candidate = Instantiate(tempPrefab);
				candidate.name = data.TypeIndex.ToString();
			}
			else
			{
				candidate = tempStack.Pop();
				candidate.SetActive(true);
				candidate.name = data.TypeIndex.ToString();
			}*/

			return candidate;
		}

		public override void ReturnObject(Transform trans)
		{
			/*Stack<GameObject> tempStack = null;
			trans.SetParent(transform, false);
			trans.gameObject.SetActive(false);
			if (_poolType.TryGetValue(trans.name, out tempStack))
			{
				tempStack.Push(trans.gameObject);
			}
			else
			{
				tempStack = new Stack<GameObject>();
				tempStack.Push(trans.gameObject);
				_poolType.Add(trans.name,tempStack);
			}*/
			GameObjectPool.Instance.ReleaseObject(trans.gameObject,PoolType.Normal);
		}

		public override void ProvideData(Transform trans, int index)
		{
			base.ProvideData(trans, index);
			var data = BankData.GetLoopListBankData(index).Data as ScrollMultiData;
			trans.GetComponent<ScrollIndexCallback4>().ScrollCellIndex(index,data);
		}
	}
}

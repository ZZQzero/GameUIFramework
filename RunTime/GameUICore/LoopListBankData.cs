using System.Collections.Generic;
using UnityEngine;

namespace GameUI
{
    public class LoopListBankData: ILoopListBank
    {
        private List<LoopListData> _loopListBankDataList = new();

        public void InitLoopListBankDataList<T>(List<T> dataList)
        {
            _loopListBankDataList.Clear();
            for (int i = 0; i < dataList.Count; ++i)
            {
                LoopListData data = new LoopListData();
                data.Data = dataList[i];
                _loopListBankDataList.Add(data);
            }
        }

        public LoopListData GetLoopListBankData(int index)
        {
            return _loopListBankDataList[index];
        }

        public List<LoopListData> GetLoopListBankDataList()
        {
            return _loopListBankDataList;
        }

        public int GetLoopListBankDataListCount()
        {
            return _loopListBankDataList.Count;
        }

        public T GetCellPreferredTypeIndex<T>(int index) where T : class
        {
            var bankData = GetLoopListBankData(index);
            if (bankData != null)
            {
                var data = bankData.Data as T;
                return data;
            }
            return null;
        }

        public void SetLoopListBankDataList(List<LoopListData> newDataList)
        {
            _loopListBankDataList = newDataList;
        }

        public void AddOneDataList(object newData)
        {
            LoopListData data = new LoopListData();
            data.Data = newData;
            _loopListBankDataList.Add(data);
        }

        public void InsertOneDataList(object newData,int index)
        {
            LoopListData data = new LoopListData();
            data.Data = newData;
            _loopListBankDataList.Insert(index,data);
        }
        
        public void DeleteOneDataListByIndex(int index)
        {
            if (_loopListBankDataList.Count <= index)
            {
                return;
            }
            _loopListBankDataList.RemoveAt(index);
        }
    }
}
using System.Collections.Generic;

namespace GameUI
{
    public interface ILoopListBank
    {
        public void InitLoopListBankDataList<T>(List<T> dataList);
        public LoopListData GetLoopListBankData(int index);
        public List<LoopListData> GetLoopListBankDataList();
        public T GetCellPreferredTypeIndex<T>(int index) where T : class;
        public void SetLoopListBankDataList(List<LoopListData> newDataList);
        public void AddOneDataList(object newData);

        public void InsertOneDataList(object newData, int index);
        public void DeleteOneDataListByIndex(int index);
    }
}
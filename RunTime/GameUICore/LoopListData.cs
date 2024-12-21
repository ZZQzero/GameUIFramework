namespace GameUI
{
    public class LoopListData
    {
        public object Data;

        public bool IsEmpty()
        {
            if(null == Data)
            {
                return true;
            }
            return false;
        }
    }
}
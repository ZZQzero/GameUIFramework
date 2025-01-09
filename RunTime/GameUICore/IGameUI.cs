
namespace GameUI
{
    public interface IGameUI
    {
        public void OnInitUI();

        public void OnOpenUI();
        
        public void OnRefreshUI();

        public void OnCloseUI();

        public void OnDestroyUI();

        public void CloseSelf();
        
        public void CloseAndDestroySelf();
    }
}
 

using System.Threading;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace FortuneWheel
{
    public class CheatFortuneWheelButton : MonoBehaviour
    {
        [SerializeField] private Button _cheatButton;

        private CancellationToken _destroyCt;
        
        private UIManager _uiManager;
        private IFortuneWheelServerService _fortuneWheelServerService;
        
        [Inject]
        public void Install(UIManager uiManager, IFortuneWheelServerService  fortuneWheelServerService)
        {
            _uiManager = uiManager;
            _fortuneWheelServerService = fortuneWheelServerService;
        }
        
        private void Awake()
        {
            _destroyCt = this.GetCancellationTokenOnDestroy();
        }

        private void Start()
        {
            _cheatButton.onClick.AddListener(() => OpenCheatsPanelAsync(_destroyCt).Forget());
        }

        private async UniTask OpenCheatsPanelAsync(CancellationToken ct)
        {
            
            ct.ThrowIfCancellationRequested();
            await UniTask.Yield();
            
            var args = new FortuneWheelArgs(10, );
            _uiManager.Show<FortuneWheelController>(args);
            
        }
    }
}
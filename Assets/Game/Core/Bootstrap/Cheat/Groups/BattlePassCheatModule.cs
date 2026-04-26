using System;
using System.Threading;
using BattlePass;
using cheatModule;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Cheat
{
    public sealed class BattlePassCheatModule : ICheatsModule
    {
        private const string BattlePassGroup = "BattlePass";

        private readonly IBattlePassServerService _battlePassServerService;
        private readonly CancellationToken _ct;

        public BattlePassCheatModule(IBattlePassServerService battlePassServerService, CancellationToken ct)
        {
            _battlePassServerService = battlePassServerService;
            _ct = ct;
        }

        public void Initialize(ICheatsContainer cheatsContainer)
        {
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Add BP XP (int)", amount =>
            {
                HandleAmountInput(amount);
            }).WithGroup(BattlePassGroup));
        }

        private void HandleAmountInput(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[BattlePassCheatModule] XP amount must be greater than zero. Received={amount}.");
                return;
            }

            AddXpAsyncCore(amount).Forget();
        }

        private async UniTask AddXpAsyncCore(int amount)
        {
            try
            {
                var result = await _battlePassServerService.AddXpAsync(amount, _ct);
                if (result == null)
                {
                    Debug.LogError("[BattlePassCheatModule] Add XP returned null result.");
                    return;
                }

                if (!result.Success)
                {
                    Debug.LogError($"[BattlePassCheatModule] Add XP failed. Code={result.ErrorCode}, Message={result.ErrorMessage}");
                    return;
                }

                var updatedState = result.UpdatedUserState;
                Debug.Log(
                    $"[BattlePassCheatModule] XP added. AddedXp={result.AddedXp}, Level={updatedState?.Level ?? 0}, Xp={updatedState?.Xp ?? 0}");
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation when scene/object is destroyed.
            }
            catch (Exception e)
            {
                Debug.LogError($"[BattlePassCheatModule] Add XP request threw exception: {e}");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using BattlePass;
using cheatModule;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Bootstrap.Tests.Editor
{
    public sealed class BattlePassCheatModuleTests
    {
        [Test]
        public void BattlePassCheatModule_RegistersInputItem_InBattlePassGroup()
        {
            var module = new Game.Cheat.BattlePassCheatModule(new StubBattlePassServerService(), CancellationToken.None);
            var container = new RecordingCheatsContainer();

            module.Initialize(container);

            Assert.That(container.AddedItemTypes, Has.Count.EqualTo(1));
            Assert.That(container.AddedItemTypes, Has.Member(typeof(CheatInputItem)));
        }

        [Test]
        public void BattlePassCheatModule_CallsAddXpAsync_WhenAmountPositive()
        {
            var service = new StubBattlePassServerService
            {
                AddXpResponseFactory = _ => new BattlePassAddXpResult(
                    true,
                    10,
                    CreateUserState(110, 3),
                    null,
                    null)
            };
            var module = new Game.Cheat.BattlePassCheatModule(service, CancellationToken.None);

            InvokeHandleAmountInput(module, 10);

            Assert.That(service.AddXpCalls, Is.EqualTo(1));
            Assert.That(service.LastAddXpAmount, Is.EqualTo(10));
        }

        [Test]
        public void BattlePassCheatModule_DoesNotCallApi_WhenAmountIsZeroOrNegative()
        {
            var service = new StubBattlePassServerService();
            var module = new Game.Cheat.BattlePassCheatModule(service, CancellationToken.None);

            LogAssert.Expect(LogType.Warning, "[BattlePassCheatModule] XP amount must be greater than zero. Received=0.");
            InvokeHandleAmountInput(module, 0);
            LogAssert.Expect(LogType.Warning, "[BattlePassCheatModule] XP amount must be greater than zero. Received=-10.");
            InvokeHandleAmountInput(module, -10);

            Assert.That(service.AddXpCalls, Is.EqualTo(0));
        }

        [Test]
        public void BattlePassCheatModule_LogsError_WhenAddXpReturnsSuccessFalse()
        {
            var service = new StubBattlePassServerService
            {
                AddXpResponseFactory = _ => new BattlePassAddXpResult(
                    false,
                    0,
                    null,
                    "season_inactive",
                    "Season is inactive.")
            };
            var module = new Game.Cheat.BattlePassCheatModule(service, CancellationToken.None);

            LogAssert.Expect(LogType.Error, "[BattlePassCheatModule] Add XP failed. Code=season_inactive, Message=Season is inactive.");
            InvokeAddXpAsyncCore(module, 10).GetAwaiter().GetResult();

            Assert.That(service.AddXpCalls, Is.EqualTo(1));
        }

        private static BattlePassUserState CreateUserState(int xp, int level)
        {
            return new BattlePassUserState(
                "season_1",
                level,
                xp,
                BattlePassPassType.None,
                Array.Empty<BattlePassClaimedRewardCell>(),
                Array.Empty<BattlePassClaimableRewardCell>());
        }

        private static void InvokeHandleAmountInput(Game.Cheat.BattlePassCheatModule module, int amount)
        {
            var method = typeof(Game.Cheat.BattlePassCheatModule).GetMethod("HandleAmountInput", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(module, new object[] { amount });
        }

        private static UniTask InvokeAddXpAsyncCore(Game.Cheat.BattlePassCheatModule module, int amount)
        {
            var method = typeof(Game.Cheat.BattlePassCheatModule).GetMethod("AddXpAsyncCore", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (UniTask)method.Invoke(module, new object[] { amount });
        }

        private sealed class RecordingCheatsContainer : Game.Cheat.ICheatsContainer
        {
            public readonly List<Type> AddedItemTypes = new();

            public void AddItem<T>(Action<T> initializer) where T : CheatItem
            {
                AddedItemTypes.Add(typeof(T));
            }
        }

        private sealed class StubBattlePassServerService : IBattlePassServerService
        {
            public Func<int, BattlePassAddXpResult> AddXpResponseFactory { get; set; }
            public int AddXpCalls { get; private set; }
            public int LastAddXpAmount { get; private set; }

            public UniTask<BattlePassSnapshot> GetCurrentAsync(CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                throw new NotImplementedException();
            }

            public UniTask<BattlePassAddXpResult> AddXpAsync(int amount, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                AddXpCalls++;
                LastAddXpAmount = amount;

                var result = AddXpResponseFactory?.Invoke(amount) ?? new BattlePassAddXpResult(
                    true,
                    amount,
                    CreateUserState(amount, 1),
                    null,
                    null);
                return UniTask.FromResult(result);
            }

            public UniTask<BattlePassClaimResult> ClaimAsync(string seasonId, int level, BattlePassRewardTrack rewardTrack, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                throw new NotImplementedException();
            }
        }
    }
}

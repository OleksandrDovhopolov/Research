using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Game.Bootstrap.Loading;
using NUnit.Framework;

namespace Game.Bootstrap.Tests.Editor
{
    public sealed class LoadingOrchestratorTests
    {
        [Test]
        public async Task RunAsync_RetriesOperation_AndSucceeds()
        {
            var attempts = 0;
            var operation = new TestLoadingOperation(
                id: "retry_op",
                description: "Retry Operation",
                isCritical: true,
                retryPolicy: new LoadingRetryPolicy(2, TimeSpan.Zero),
                timeout: null,
                executeAsync: async ct =>
                {
                    ct.ThrowIfCancellationRequested();
                    attempts++;
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    if (attempts == 1)
                    {
                        throw new InvalidOperationException("first attempt fails");
                    }
                });

            var phase = new LoadingPhase("phase_retry", new[]
            {
                new LoadingGroup("group_seq", LoadingGroupExecutionMode.Sequential, new ILoadingOperation[] { operation })
            });

            var orchestrator = new LoadingOrchestrator(new LoadingProgressAggregator(1f));
            orchestrator.SetPhases(new[] { phase });

            var result = await orchestrator.RunAsync(0, null, CancellationToken.None);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(attempts, Is.EqualTo(2));
        }

        [Test]
        public async Task RunAsync_StopsOnCriticalFailure_AndReturnsFailedPhase()
        {
            var nonCriticalAttempts = 0;
            var criticalAttempts = 0;

            var nonCriticalFailingOperation = new TestLoadingOperation(
                id: "optional_op",
                description: "Optional",
                isCritical: false,
                retryPolicy: new LoadingRetryPolicy(1, TimeSpan.Zero),
                timeout: null,
                executeAsync: async ct =>
                {
                    ct.ThrowIfCancellationRequested();
                    nonCriticalAttempts++;
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    throw new InvalidOperationException("optional failed");
                });

            var criticalFailingOperation = new TestLoadingOperation(
                id: "critical_op",
                description: "Critical",
                isCritical: true,
                retryPolicy: new LoadingRetryPolicy(1, TimeSpan.Zero),
                timeout: null,
                executeAsync: async ct =>
                {
                    ct.ThrowIfCancellationRequested();
                    criticalAttempts++;
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    throw new InvalidOperationException("critical failed");
                });

            var phase0 = new LoadingPhase("phase_optional", new[]
            {
                new LoadingGroup("group_seq", LoadingGroupExecutionMode.Sequential, new ILoadingOperation[] { nonCriticalFailingOperation })
            });
            var phase1 = new LoadingPhase("phase_critical", new[]
            {
                new LoadingGroup("group_seq", LoadingGroupExecutionMode.Sequential, new ILoadingOperation[] { criticalFailingOperation })
            });

            var orchestrator = new LoadingOrchestrator(new LoadingProgressAggregator(1f));
            orchestrator.SetPhases(new[] { phase0, phase1 });
            var result = await orchestrator.RunAsync(0, null, CancellationToken.None);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailedPhaseIndex, Is.EqualTo(1));
            Assert.That(nonCriticalAttempts, Is.EqualTo(1));
            Assert.That(criticalAttempts, Is.EqualTo(1));
        }

        [Test]
        public async Task RunAsync_ReturnsTimeoutFailure_WhenOperationTimesOut()
        {
            var operation = new TestLoadingOperation(
                id: "timeout_op",
                description: "Timeout",
                isCritical: true,
                retryPolicy: new LoadingRetryPolicy(1, TimeSpan.Zero),
                timeout: TimeSpan.FromMilliseconds(30),
                executeAsync: async ct =>
                {
                    await UniTask.Delay(200, cancellationToken: ct);
                });

            var phase = new LoadingPhase("phase_timeout", new[]
            {
                new LoadingGroup("group_seq", LoadingGroupExecutionMode.Sequential, new ILoadingOperation[] { operation })
            });

            var orchestrator = new LoadingOrchestrator(new LoadingProgressAggregator(1f));
            orchestrator.SetPhases(new[] { phase });
            var result = await orchestrator.RunAsync(0, null, CancellationToken.None);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Failure, Is.Not.Null);
            Assert.That(result.Failure.TimedOut, Is.True);
        }

        [Test]
        public void Aggregator_UsesWeightedAverage_AndMonotonicSmoothing()
        {
            var opA = new TestLoadingOperation("a", "A", true, new LoadingRetryPolicy(1, TimeSpan.Zero), null, _ => UniTask.CompletedTask)
            {
                ProgressValue = 1f,
                WeightValue = 3f
            };

            var opB = new TestLoadingOperation("b", "B", true, new LoadingRetryPolicy(1, TimeSpan.Zero), null, _ => UniTask.CompletedTask)
            {
                ProgressValue = 0f,
                WeightValue = 1f
            };

            var aggregator = new LoadingProgressAggregator(0.5f);
            var first = aggregator.Update(new[] { opA, opB });
            opA.ProgressValue = 0.8f;
            opB.ProgressValue = 0.2f;
            var second = aggregator.Update(new[] { opA, opB });

            Assert.That(first, Is.GreaterThan(0f));
            Assert.That(second, Is.GreaterThanOrEqualTo(first));
        }

        private sealed class TestLoadingOperation : ILoadingOperation
        {
            private readonly Func<CancellationToken, UniTask> _executeAsync;

            public TestLoadingOperation(
                string id,
                string description,
                bool isCritical,
                LoadingRetryPolicy retryPolicy,
                TimeSpan? timeout,
                Func<CancellationToken, UniTask> executeAsync)
            {
                Id = id;
                Description = description;
                IsCritical = isCritical;
                RetryPolicy = retryPolicy;
                Timeout = timeout;
                WeightValue = 1f;
                _executeAsync = executeAsync;
            }

            public string Id { get; }
            public string Description { get; }
            public LoadingOperationStatus Status { get; private set; } = LoadingOperationStatus.NotStarted;
            public float Progress => ProgressValue;
            public float Weight => WeightValue;
            public bool IsCritical { get; }
            public int DisplayPriority => 0;
            public LoadingRetryPolicy RetryPolicy { get; }
            public TimeSpan? Timeout { get; }
            public float ProgressValue { get; set; }
            public float WeightValue { get; set; }

            public async UniTask ExecuteAsync(CancellationToken ct)
            {
                Status = LoadingOperationStatus.InProgress;
                await _executeAsync(ct);
                ProgressValue = 1f;
                Status = LoadingOperationStatus.Completed;
            }

            public void Reset()
            {
                Status = LoadingOperationStatus.NotStarted;
                ProgressValue = 0f;
            }
        }
    }
}

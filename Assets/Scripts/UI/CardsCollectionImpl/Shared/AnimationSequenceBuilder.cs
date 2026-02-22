using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace core
{
    public class AnimationSequenceBuilder
    {
        private readonly List<Func<CancellationToken, UniTask>> _steps = new();

        public AnimationSequenceBuilder Append(Func<CancellationToken, UniTask> step)
        {
            _steps.Add(step);
            return this;
        }

        public AnimationSequenceBuilder AppendIf(bool condition, Func<CancellationToken, UniTask> step)
        {
            if (condition)
            {
                _steps.Add(step);
            }

            return this;
        }

        public AnimationSequenceBuilder AppendParallel(params Func<CancellationToken, UniTask>[] steps)
        {
            _steps.Add(async ct =>
            {
                var tasks = new UniTask[steps.Length];
                for (var i = 0; i < steps.Length; i++)
                {
                    tasks[i] = steps[i](ct);
                }

                await UniTask.WhenAll(tasks);
            });
            return this;
        }

        public AnimationSequenceBuilder AppendDelay(int milliseconds)
        {
            _steps.Add(ct => UniTask.Delay(milliseconds, cancellationToken: ct));
            return this;
        }

        public async UniTask PlayAsync(CancellationToken ct)
        {
            foreach (var step in _steps)
            {
                ct.ThrowIfCancellationRequested();
                await step(ct);
            }
        }
    }
}

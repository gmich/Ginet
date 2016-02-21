using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Ginet.Async
{
    internal sealed class ParallelTaskStarter : IDisposable
    {
         
        private CancellationTokenSource wtoken;

        private ActionBlock<DateTimeOffset> task;
    
        private readonly TimeSpan repostDelay;

        private Action asyncAction;


        public ParallelTaskStarter(TimeSpan repostDelay)
        {
            this.repostDelay = repostDelay;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed = false;
        private void Dispose(bool disposing)
        {
            if (disposing && !isDisposed)
            {
                wtoken.Dispose();
                isDisposed = true;
            }
        }

        private ITargetBlock<DateTimeOffset> CreateParallelTask(
           Func<DateTimeOffset, CancellationToken, Task> action,
           CancellationToken cancellationToken)
        {
            ActionBlock<DateTimeOffset> block = null;

            block = new ActionBlock<DateTimeOffset>(async now =>
            {
                await action(now, cancellationToken).
                      ConfigureAwait(false);

                await Task.Delay(repostDelay, cancellationToken).
                      ConfigureAwait(false);

                block.Post(DateTimeOffset.Now);

            }, new ExecutionDataflowBlockOptions
            {
                CancellationToken = cancellationToken
            });

            return block;
        }
        
        public void Start(Action action)
        {
            wtoken = new CancellationTokenSource();
            this.asyncAction = action;
            task = (ActionBlock<DateTimeOffset>)CreateParallelTask((now, ct) => DoAsync(ct), wtoken.Token);

            task.Post(DateTimeOffset.Now);
        }

        private Task DoAsync(CancellationToken cancellationToken)
        {
            return Task.Run(asyncAction); 
        }
        
        public void Stop()
        {
            using (wtoken)
            {
                wtoken.Cancel();
            }
            wtoken = null;
            task = null;
        }

    }
}

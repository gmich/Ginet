using System;

namespace Ginet.Infrastructure
{
    internal class DelegateDisposable : IDisposable
    {
        private readonly Action disposableAction;

        public DelegateDisposable(Action disposableAction)
        {
            this.disposableAction = disposableAction;
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
                disposableAction();
                isDisposed = true;
            }
        }
    }
}

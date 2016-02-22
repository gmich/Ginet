using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Ginet.Repositories
{
    internal class ConcurrentRepository<Tid, Titem>
        where Titem : class
    {
        private readonly ConcurrentDictionary<Tid, Titem> items;

        public ConcurrentRepository()
        {
            items = new ConcurrentDictionary<Tid, Titem>();
        }

        public int Count => items.Count;
        public bool HasKey(Tid key) => items.ContainsKey(key);
        public IEnumerable<Titem> GetAll => items.Select(x => x.Value);

        public Titem GetById(Tid id)
        {
            if (items.ContainsKey(id))
            {
                return items[id];
            }
            return null;
        }

        public void IfExists(Tid id, Action<Titem> action)
        {
            var item = GetById(id);
            if (item != null)
            {
                action(item);
            }
        }

        public Titem this[Tid id] => GetById(id);

        public Titem Get(Func<Titem, bool> expression) =>
            items.Where(x => expression(x.Value)).Select(x => x.Value).FirstOrDefault();

        public bool Update(Tid id, Titem item)
        {
            Contract.Requires(item != default(Titem));

            if (items.ContainsKey(id))
            {
                items[id] = item;
                return true;
            }
            return false;
        }
        public bool Delete(Tid id)
        {
            if (items.ContainsKey(id))
            {
                Titem value;
                while (items.TryRemove(id, out value)) ;
                return true;
            }
            return false;
        }

        public IDisposable Add(Tid id, Titem item)
        {
            Contract.Requires(item != default(Titem));

            if (!items.ContainsKey(id))
            {
                items.TryAdd(id, item);
                return new DeregisterDictionary<Tid, Titem>(items, item);
            }
            return new DoNothingDisposable();
        }

        public IDisposable CreateDisposable(Tid id)
        {
            if (items.ContainsKey(id))
            {
                return new DeregisterDictionary<Tid, Titem>(items, items[id]);
            }
            throw new Exception("Id was not found");
        }

        private class DoNothingDisposable : IDisposable
        {
            public void Dispose(){}
        }

        private class DeregisterDictionary<TKey, TValue> : IDisposable
        {
            private readonly ConcurrentDictionary<TKey, TValue> registered;
            private readonly TValue current;

            public DeregisterDictionary(ConcurrentDictionary<TKey, TValue> registered, TValue current)
            {
                this.registered = registered;
                this.current = current;
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
                    foreach (var item in registered.Where(kvp => kvp.Value.Equals(current)).ToList())
                    {
                        TValue outValue;
                        while (registered.TryRemove(item.Key, out outValue)) ;
                    }
                    isDisposed = true;
                }
            }

        }

    }
}

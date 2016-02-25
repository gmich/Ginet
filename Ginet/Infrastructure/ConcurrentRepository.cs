using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Ginet.Infrastructure
{
    internal class ConcurrentRepository<Tid, Titem>
    {
        public ConcurrentDictionary<Tid, Titem> Items { get; }

        public ConcurrentRepository()
        {
            Items = new ConcurrentDictionary<Tid, Titem>();
        }

        public int Count => Items.Count;
        public bool HasKey(Tid key) => Items.ContainsKey(key);

        public IEnumerable<Titem> GetAll => Items.Select(x => x.Value);

        public Titem GetById(Tid id)
        {
            if (Items.ContainsKey(id))
            {
                return Items[id];
            }
            throw new Exception($"Key {id} was not found");
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
            Items.Where(x => expression(x.Value)).Select(x => x.Value).FirstOrDefault();

        public bool Update(Tid id, Titem item)
        {
            if (Items.ContainsKey(id))
            {
                Items[id] = item;
                return true;
            }
            return false;
        }
        public bool Delete(Tid id)
        {
            if (Items.ContainsKey(id))
            {
                Titem value;
                while (Items.TryRemove(id, out value)) ;
                return true;
            }
            return false;
        }

        public IDisposable Add(Tid id, Titem item)
        {
            if (!Items.ContainsKey(id))
            {
                Items.TryAdd(id, item);
                return new DeregisterDictionary<Tid, Titem>(Items, item);
            }
            return new DoNothingDisposable();
        }

        public IDisposable CreateDisposable(Tid id)
        {
            if (Items.ContainsKey(id))
            {
                return new DeregisterDictionary<Tid, Titem>(Items, Items[id]);
            }
            throw new Exception("Id was not found");
        }

        private class DoNothingDisposable : IDisposable
        {
            public void Dispose() { }
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

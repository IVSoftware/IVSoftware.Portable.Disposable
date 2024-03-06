using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace IVSoftware.Portable.Disposable
{
    /// <summary>
    /// Used for making items visible in an OSource.
    /// </summary>
    public interface IVisibleIndex
    {
        /// <summary>
        /// No such thing as a negative visible index, but -1 
        ///is better than uint.MaxValue to represent invalid.
        /// </summary>
        int VisibleIndex { get; } // int, not uint.
    }

    /// <summary>
    /// A good all around OSource class that can also be
    /// used for batch CollectionChanged events.
    /// </summary>
    public class AutoObservableCollection<T> : IEnumerable<T>, INotifyCollectionChanged
    {
        public AutoObservableCollection() => BatchNotifyCollectionChanged.FinalDispose += onDisposeBatchNotifyCollectionChangedInternal;

        private readonly List<T> _items = new List<T>();

        // Indexer
        public T this[int index] => _items[index];
        public void Clear()
        {
            if (_items.Any())
            {
                var oldItems = _items.ToArray();
                _items.Clear();
                OnCollectionChanged(new NotifyCollectionResetEventArgs(oldItems));
            }
        }

        public void Add(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if(_items.Count == 0)
            {
                // This is the FIRST ITEM.
                _items.Add(item);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, 0));
            }
            else
            {
                var oldIndex = _items.IndexOf(item);

                T nextItem;
                if (item is IVisibleIndex viItem)
                {
                    // If there is an existing item with a higher visible
                    // index,this item needs to go at the index before that item.

                    nextItem = 
                    _items
                    .FirstOrDefault(_ =>_ is IVisibleIndex _vi && _vi.VisibleIndex > viItem.VisibleIndex);
                }
                else
                {
                    nextItem = default;
                }
                if(nextItem == null)
                {
                    // If there are is no item with higher requested
                    // index, then this item belongs at the end.
                    // Is it there already?
                    if (oldIndex == Count - 1)
                    {
                        // It IS! We're done here.
                    }
                    else
                    {
                        _items.Add(item);
                        OnCollectionChanged(
                            new NotifyCollectionChangedEventArgs(
                                NotifyCollectionChangedAction.Add,
                                item, _items.IndexOf(item)));
                    }
                }
                else
                {
                    // Now we need to figure out whether this item is already in the list.
                    int newIndex;
                    if (oldIndex == -1)
                    {
                        // It ISN'T.
                        // This means we need to bump the item with the higher index.
                        // We want the ACTUAL INDEX of the item with the higher VISIBLE INDEX.
                        newIndex = _items.IndexOf(nextItem); // Do 'not' subtract one!
                        _items.Insert(newIndex, item);
                        OnCollectionChanged(
                            new NotifyCollectionChangedEventArgs(
                                NotifyCollectionChangedAction.Add,
                                item, _items.IndexOf(item)));
                    }
                    else
                    {
                        // It IS.
                        // This means that it 'might' already be at the correct position.
                        // We want the ACTUAL INDEX of the item with the higher VISIBLE INDEX.
                        newIndex = _items.IndexOf(nextItem) - 1; // Subtracting IS correct here.
                        // Now, is it already in the correct order?
                        if (oldIndex == newIndex)
                        {
                            // It IS! We're done here.
                        }
                        else
                        {
                            // Is ISN'T!
                            // [Careful]
                            // The item first needs to be removed. The removal "may or may not'
                            // affect the index of the 'next' item. We won't know till we try.
                            using (GetNoNotifyToken())
                            {
                                _items.RemoveAt(oldIndex);
                                // The ACTUAL INDEX of nextItem needs to be refreshed now.
                                newIndex = _items.IndexOf(nextItem) - 1; // Subtracting IS correct here.
                                _items.Insert(newIndex, item);
                            }
                            // Document the MOVE:
                            OnCollectionChanged(
                                new NotifyCollectionChangedEventArgs(
                                    NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
                        }
                    }
                }
            }
        }

        public void Remove(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (_items.Remove(item))
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            // The NoNotify token swallows ALL events
            // in the block at the request of the user.
            if (NoNotifyCollectionChanged.IsZero())
            {
                // The BatchNotify token DEFERS all events
                // in the block at the request of the user.
                if (BatchNotifyCollectionChanged.IsZero())
                {
                    CollectionChanged?.Invoke(this, e);
                }
                else
                {
                    _eventsList.Add(e);
                }
            }
        }
        void onDisposeBatchNotifyCollectionChangedInternal(object sender, EventArgs _)
        {
            // Send EVEN IF EMPTY - 
            // Let the User decide whether to refresh or edraw anything.
            OnCollectionChangedBatch(new NotifyCollectionChangedBatchEventArgs(_eventsList.ToArray()));
            _eventsList.Clear();
        }
        public event CollectionChangedBatchEventHandler CollectionChangedBatch;

        /// <summary>
        /// Override in subclass to dequeue and process batch updates.
        /// </summary>
        public virtual void OnCollectionChangedBatch(NotifyCollectionChangedBatchEventArgs e)
        {
            CollectionChangedBatch?.Invoke(this, e);
        }

        public void RemoveAt(int i)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_items).GetEnumerator();
        }

        private List<NotifyCollectionChangedEventArgs> _eventsList { get; } = new List<NotifyCollectionChangedEventArgs>();

        /// <summary>
        /// Suppress all CollectionChanged events
        /// </summary>
        DisposableHost NoNotifyCollectionChanged { get; } = new DisposableHost(nameof(NoNotifyCollectionChanged));
        DisposableHost BatchNotifyCollectionChanged { get; } = new DisposableHost(nameof(BatchNotifyCollectionChanged));
        public int Count => _items.Count;

        public IDisposable GetBatchRefreshToken(object sender = null, Dictionary<string, object> properties = null) =>
            BatchNotifyCollectionChanged.GetToken(sender, properties);
        public IDisposable GetNoNotifyToken(object sender = null, Dictionary<string, object> properties = null) => 
            NoNotifyCollectionChanged.GetToken(sender, properties);
    }

    public class NotifyCollectionResetEventArgs : NotifyCollectionChangedEventArgs
    {
        public NotifyCollectionResetEventArgs(IList oldItems) : base(NotifyCollectionChangedAction.Reset)
        {
            OldItems = oldItems;
        }
        public new IList OldItems
        { get; }
    }

#if false
    
    /// <summary>
    /// Used for batch CollectionChanged events.
    /// </summary>
        //public AutoObservableCollection() => BatchNotifyCollectionChanged.FinalDispose += onDisposeBatchNotifyCollectionChangedInternal;

        //void onDisposeBatchNotifyCollectionChangedInternal(object sender, EventArgs _)
        //{
        //    // Send EVEN IF EMPTY - 
        //    // Let the User decide whether to refresh or edraw anything.
        //    OnCollectionChangedBatch(new NotifyCollectionChangedBatchEventArgs(_eventsList.ToArray()));
        //    _eventsList.Clear();
        //}
        //public event CollectionChangedBatchEventHandler CollectionChangedBatch;
        ///// <summary>
        ///// Override in subclass to dequeue and process batch updates.
        ///// </summary>
        //public virtual void OnCollectionChangedBatch(NotifyCollectionChangedBatchEventArgs e) 
        //{
        //    CollectionChangedBatch?.Invoke(this, e);
        //}

        //private List<NotifyCollectionChangedEventArgs> _eventsList { get; } = new List<NotifyCollectionChangedEventArgs>();

        ///// <summary>
        ///// Suppress all CollectionChanged events
        ///// </summary>
        //public DisposableHost NoNotifyCollectionChanged { get; } = new DisposableHost(nameof(NoNotifyCollectionChanged));

        ///// <summary>
        ///// Enqueue all CollectionChanged events inside block, transmit batch when block disposes.
        ///// </summary>
        //public DisposableHost BatchNotifyCollectionChanged { get; } = new DisposableHost(nameof(BatchNotifyCollectionChanged));
#endif

    public delegate void CollectionChangedBatchEventHandler(Object sender, NotifyCollectionChangedBatchEventArgs e);
    public class NotifyCollectionChangedBatchEventArgs : EventArgs
    {
        public NotifyCollectionChangedBatchEventArgs(NotifyCollectionChangedEventArgs[] notifyCollectionChangedEventArgs)
        {
            CollectionChangedEvents = notifyCollectionChangedEventArgs;
        }

        public NotifyCollectionChangedEventArgs[] CollectionChangedEvents { get; }
    }
}

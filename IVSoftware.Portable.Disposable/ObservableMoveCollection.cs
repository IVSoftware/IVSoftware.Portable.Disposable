using System;
using System.Collections.Generic;
using System.Text;

namespace IVSoftware.Portable.Disposable
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Threading;

    public class ObservableMoveCollection<T> : ObservableCollection<T>
    {
        private readonly ReaderWriterLockSlim lockSlim = new ReaderWriterLockSlim();

        protected override void MoveItem(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= Count || newIndex < 0 || newIndex >= Count || oldIndex == newIndex)
            {
                // Index out of range or trying to move to the same position
                return;
            }

            lockSlim.EnterWriteLock();
            try
            {
                T item = this[oldIndex];
                SuppressCollectionChanged = true;

                base.RemoveItem(oldIndex);
                base.InsertItem(newIndex, item);

                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));

                SuppressCollectionChanged = false;
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }
        }

        protected bool SuppressCollectionChanged { get; set; }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!SuppressCollectionChanged)
            {
                lockSlim.EnterReadLock();
                try
                {
                    base.OnCollectionChanged(e);
                }
                finally
                {
                    lockSlim.ExitReadLock();
                }
            }
        }
    }

}

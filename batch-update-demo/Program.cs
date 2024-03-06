// See https://aka.ms/new-console-template for more information

using IVSoftware.Portable.Disposable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

new TestClass().RunDemo();

Console.ReadKey();


class TestClass
{
    public AutoObservableCollection<MyObservableItem> DataSource { get; } = new AutoObservableCollection<MyObservableItem>();

    public void RunDemo()
    {
        var stopwatch = Stopwatch.StartNew();
        string[] testData = new[] {
            "vivid",
            "radiant",
            "sapphire",
            "magnificent",
            "fiery",
            "splendid",
            "enchanting",
            "brilliant",
            "luminous",
            "vibrant" };

        // Subscribe to CollectionChanged event.
        DataSource.CollectionChanged += (sender, e) =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        Debug.WriteLine(string.Empty);
                        Debug.WriteLine($@"Elapsed {stopwatch.Elapsed:ss\:fff}");
                        foreach (MyObservableItem item in e.NewItems)
                        {
                            Debug.WriteLine($"{nameof(TestClass.DataSource.CollectionChanged)} - {e.Action}: {item.Text}");
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    var type = e.GetType().Name;
                    if (e is NotifyCollectionResetEventArgs ePlus)
                    {
                        Debug.WriteLine(string.Empty);
                        Debug.WriteLine($@"Elapsed {stopwatch.Elapsed:ss\:fff}");
                        foreach (MyObservableItem item in ePlus.OldItems)
                        {
                            Debug.WriteLine($"{nameof(TestClass.DataSource.CollectionChanged)} - {e.Action}: {item.Text}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine(string.Empty);
                        Debug.WriteLine($@"Elapsed {stopwatch.Elapsed:ss\:fff}");
                        Debug.WriteLine($"{nameof(TestClass.DataSource.CollectionChanged)} - {e.Action}: And that's all we know...");
                    }
                    break;
            }
        };

        DataSource.CollectionChangedBatch += (sender, e) =>
        {
            Debug.WriteLine(string.Empty);
            Debug.WriteLine($@"Batch Update @ Elapsed {stopwatch.Elapsed:ss\:fff}");
            var newItemsBatch =
                e.CollectionChangedEvents
                .Where(_ => _.Action == NotifyCollectionChangedAction.Add && _.NewItems != null)
                .SelectMany(_ => _.NewItems!.OfType<MyObservableItem>())
                .Distinct().ToList();

            foreach (MyObservableItem newItem in newItemsBatch)
            {
                Debug.WriteLine($"{nameof(TestClass.DataSource.CollectionChangedBatch)} - {NotifyCollectionChangedAction.Add}: {newItem.Text}");
            }
        };

        // Add ten items with events in real time.
        foreach (var text in testData)
        {
            DataSource.Add(text);
        }

        DataSource.Clear();

        // Now do the same thing, this time wrapping with IDisposable batch token.
        using (DataSource.GetBatchRefreshToken())
        {
            foreach (var text in testData)
            {
                DataSource.Add(text);
            }
        }
    }
}
class MyObservableItem : INotifyPropertyChanged
{
    public static implicit operator MyObservableItem(string value) => new MyObservableItem { Text = value };
    public string Text
    {
        get => _text;
        set
        {
            if (!Equals(_text, value))
            {
                _text = value;
                OnPropertyChanged();
            }
        }
    }

    string _text = string.Empty;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;
}

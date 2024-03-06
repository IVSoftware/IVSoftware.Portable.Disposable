## Disposable Host

This flexible reference counting mechanism hosts `DisposableToken` objects, as well as an object dictionary for the lifetime of the `IDisposable` scope. It raises `BeginUsing` when count increments to 1, `FinalDispose` when count decrements to 0, and `CountChanged` when any movement occurs.

### Canonical Example

Get a WaitCursor for the duration of this operation and make sure it goes away when the operation completes.

___

_Before_

Our good intentions are clear by the finally block that closes out the WaitCursor. It's not always easy to know that we're calling a subroutine that's going to do the same thing. And the bottom line in this case is that there's no wait cursor for the batch processing routine because single update has already killed it off before `UpdateAll` reaches it!!

```
async Task UpdateAll()
{
    try
    {
        UseWaitCursor = true;
        for (int i = 0; i < 10; i++)
        {
            await UpdateSingle();
        }
        // Simulate long-running batch processing routine. Unfortunately,
        // the SingleUpdate killed off the WaitCursor and now there's
        //  no activity indication while this task runs!
        await Task.Delay(TimeSpan.FromSeconds(10));
    }
    finally
    {
        UseWaitCursor = false;
    }
}
async Task UpdateSingle()
{
    try
    {
        UseWaitCursor = true;
        // Simulate long-running single update
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
    finally
    {
        UseWaitCursor = false;
    }
}
```


___

_After_

While it's not strictly necessary to make a singleton instance of DisposableHost, this can be a nice way to do it.

```
public DisposableHost DHostWaitCursor
{
    get
    {
        if (_dhostWaitCursor is null)
        {
            // Making an (optionally) named instance of DisposableHost on demand.
            _dhostWaitCursor = new DisposableHost(nameof(DHostWaitCursor));
            _dhostWaitCursor.BeginUsing += (sender, e) => UseWaitCursor = true;
            _dhostWaitCursor.FinalDispose += (sender, e) => UseWaitCursor = false;
        }
        return _dhostWaitCursor;
    }
}
private DisposableHost _dhostWaitCursor = null;

async Task UpdateAll()
{
    using (DHostWaitCursor.GetToken())
    {
        for (int i = 0; i < 10; i++)
        {
            await UpdateSingle();
        }
        // Simulate long-running batch processing routine
        // with a reference count of 1 (probably).
        await Task.Delay(TimeSpan.FromSeconds(10));
    }
}
async Task UpdateSingle()
{
    using (DHostWaitCursor.GetToken())
    {
        // Simulate long-running single update
        // In this simple example, the reference count
        // is bouncing back and forth between 1 and 2.
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
}
```

___

### Dictionary

The Host object also functions as a Dictionary<string, object> during the lifetime of the disposable block.

#### Example 

Suppose this windows application can have multiple virtual "instances" of MainForm that maintain their own state even though the database logic is shared. This allows UI elements that know which instance they should respond to.

```
// Constructor
public MainForm()
{
    public static DisposableHost MyDHostInstance { get; } = new DisposableHost(nameof(MyDHostInstance);

    // Create a disposable lifetime with an Instance reference in its dictionary.
    using(MyDHostInstance.GetToken(properties: new Dictionary<string, object)
    {
        { "InitializingInstance", this.Instance }
    })
    {
        this.InitializeComponent();
    }
}

// Controls that are instantiated in form designer.
interface IInstanceSpecific 
{
    MyAppInstanceClass Instance { get; }
}

class InstanceAwareControl : Control, IInstanceSpecific
{
    public InstanceAwareControl()
    {
        // Detect the presence of the Instance reference in the dictionary and consume.
        if(MyDHostInstance.TryGetValue("InitializingInstance", out MyAppInstanceClass initializingInstance)
        {
            Instance = initializingInstance;
        }
        else Debug.Fail("Expecting initializing instance.");

        Instance.MainForm.OtherControl.Changed += (sender, e) =>
        {
            if(sender is IInstanceSpecific iis && ReferenceEquals(iis.Instance, Instance))
            {
                // Respond to this control change because it
                // belongs  to the same instance as this control.
            }
        };
    }    
    public MyAppInstanceClass Instance{ get; }
}
```

___

### AutoObservableCollection&lt;T&gt; with Batch Updates

This is a lightweight alternative to `System.Collections.ObjectModel.ObservableCollection`. It encapsulates two improvements that seemed to keep coming up, and opportunistically seemd like a good inclusion for this package since the reference counting was already on hand.

1 - `NotifyCollectionResetEventArgs` that contains information on `OldItems` when the collection is cleared (e.g. for when the items themselves are `IDisposable` and need to be disposed). While things may have changed since this writing, the standard `CollectionChangedEvent` seems to have no information on the items that have been removed as a result of `Clear()`. 

2 - Built-in `DisposableHost` using blocks where `CollectionChanged` events can be batched or entirely suppressed which can be handy when the number of items being added to a collection is large.

___

_Here's a short example of a batch `using` block to demonstrate its usage._

```
public AutoObservableCollection<MyObservableItem> DataSource { get; } = 
    new AutoObservableCollection<MyObservableItem>();

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
```

#### Analysis

The first block of input indicates that `CollectionChanged` has been fired in real time for every item that gets added to the collection.

```text

Elapsed 00:002
CollectionChanged - Add: vivid

Elapsed 00:017
CollectionChanged - Add: radiant

Elapsed 00:020
CollectionChanged - Add: sapphire

Elapsed 00:023
CollectionChanged - Add: magnificent

Elapsed 00:026
CollectionChanged - Add: fiery

Elapsed 00:028
CollectionChanged - Add: splendid

Elapsed 00:031
CollectionChanged - Add: enchanting

Elapsed 00:034
CollectionChanged - Add: brilliant

Elapsed 00:037
CollectionChanged - Add: luminous

Elapsed 00:041
CollectionChanged - Add: vibrant
```

The second block demonstrates the verbosity of the `NotifyCollectionResetEventArgs` allowing enumeration of the items removed as a result of the call to `Clear()`.

```text

Elapsed 00:045
CollectionChanged - Reset: vivid
CollectionChanged - Reset: radiant
CollectionChanged - Reset: sapphire
CollectionChanged - Reset: magnificent
CollectionChanged - Reset: fiery
CollectionChanged - Reset: splendid
CollectionChanged - Reset: enchanting
CollectionChanged - Reset: brilliant
CollectionChanged - Reset: luminous
CollectionChanged - Reset: vibrant
```

The third block demonstrates that the `CollectionChanged` events that occurred during the `using` block have all been delivered en-masse when the scope of the block is exited.

```text
Batch Update @ Elapsed 00:059
CollectionChangedBatch - Add: vivid
CollectionChangedBatch - Add: radiant
CollectionChangedBatch - Add: sapphire
CollectionChangedBatch - Add: magnificent
CollectionChangedBatch - Add: fiery
CollectionChangedBatch - Add: splendid
CollectionChangedBatch - Add: enchanting
CollectionChangedBatch - Add: brilliant
CollectionChangedBatch - Add: luminous
CollectionChangedBatch - Add: vibrant
```

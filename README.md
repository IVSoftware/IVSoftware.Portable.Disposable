## Disposable Host

This flexible reference counting mechanism hosts `DisposableToken` objects, as well as an object dictionary for the lifetime of the `IDisposable` object. It raises `BeginUsing` when count increments to 1 and `FinalDispose` when count decrements to 0 and `CountChanged` when any movement occurs.

___

### Dictionary

The Host object also functions as a Dictionary<string, object> during the disposable lifetime.

######
Example 

Suppose this windows application can have multiple virtual "instances" of MainForm that maintain their own state even though the database logic is shared. This allows UI elements that know which instance they should respond to.

```
// Constructor
public MainForm()
{
    public static DisposableHost MyDHostInstance { get; } = new DisposableHost(nameof(MyDHostInstance);

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

#### ObservableCollection&lt;T&gt; with Batch Updates

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


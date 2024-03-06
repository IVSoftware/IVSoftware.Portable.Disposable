# Disposable Host

This flexible reference counting mechanism hosts `DisposableToken` objects, as well as an object dictionary for the lifetime of the `IDisposable` object. It events when the count changes and raises `BeginUsing` when count increments to 1 and `FinalDispose` when count decrements to 0.

___

#### Dictionary

The Host object also functions as a Dictionary<string, object> during the disposable lifetime.

___

#### ObservableCollection&lt;T&gt; with Batch Updates


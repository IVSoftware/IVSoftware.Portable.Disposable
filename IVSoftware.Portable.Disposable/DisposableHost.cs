using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using static IVSoftware.Portable.Disposable.DisposableHost;

namespace IVSoftware.Portable.Disposable
{
    /// <summary>
    /// Returns true if busy
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{Name}")]
    public class DisposableHost : Dictionary<string, object>
    {
        public DisposableHost() 
            : this(nameof(DisposableHost)) { }

        public DisposableHost(string name)
        {
            IsZero = () => Count == 0;
            NonZero = () => Count != 0;
            Name = name;
        }
        public string Name { get; }

        /// <summary>
        /// Another way of saying Count != 0.
        /// </summary>
        /// <remarks>
        /// USAGE: 
        /// If host name is Loading then if(Loading) returns true if Count is non-zero;
        /// </remarks>
        public static implicit operator bool(DisposableHost @this) =>
            @this.NonZero();

        private readonly DisposableTokenCollection _tokens = new DisposableTokenCollection();

        public DisposableToken[] Tokens => _tokens.ToArray();
        public new object this[string key]
        {
            get 
            {
                if (TryGetValue(key, out var value))
                {
                    return value;
                }
                return default;
            }
            set
            {
                if(ContainsKey(key)) 
                {
                    Debug.WriteLine($"A value for '{key}' already exists in {Name}");
                }
                else
                {
                    base[key] = value;
                }
            }
        }

        object _criticalSection = new object();
        private void Push(DisposableToken token)
        {
            lock (_criticalSection)
            {
                bool isZeroB4 = _tokens.Count == 0;
                _tokens.Add(token);
                if (isZeroB4)
                {
                    BeginUsing?.Invoke(this, new BeginUsingEventArgs(token));
                }
                if (!_releasedSenders.Contains(token.Sender))
                {
                    _releasedSenders.Add(token.Sender);
                }
                CountChanged?.Invoke(this, new CountChangedEventArgs(name: Name, count: Count));
            }
        }
        private List<object> _releasedSenders { get; } = new List<object>();
        private void Pop(DisposableToken token)
        {
            lock (_criticalSection)
            {
                _tokens.Remove(token);
                CountChanged?.Invoke(this, new CountChangedEventArgs(name: Name, count: Count));
                if (_tokens.Count == 0)
                {
                    var arrayRS = _releasedSenders.ToArray();
                    _releasedSenders.Clear();
                    FinalDispose?.Invoke(this, new FinalDisposeEventArgs(releasedSenders: arrayRS));
                    // CLEAR the dictionary values.
                    base.Clear();
                }
            }
        }
        public new int Count => _tokens.Count;

        /// <summary>
        /// Predicate for IsZero.
        /// </summary>
        /// <remarks>
        /// Default is "true if empty"
        /// </remarks>
        public Func<bool> IsZero { get; set; }

        /// <summary>
        /// Predicate for NonZero.
        /// </summary>
        /// <remarks>
        /// Default is "true if not empty"
        /// </remarks>
        public Func<bool> NonZero { get; set; }

        public IDisposable GetToken(object sender = null, Dictionary<string, object> properties = null)
        {
            var token = new DisposableToken(this, sender, properties);
            Push(token);
            return token;
        }

        public IDisposable GetToken(string key, object value) =>
            GetToken(this, key, value);

        public IDisposable GetToken(object sender, string key, object value)
        {
            var dict = new Dictionary<string, object> { { key, value } };
            var token = new DisposableToken(this, sender, dict);
            Push(token);
            return token;
        }

        /// <summary>
        /// Nested class can call Pop()
        /// </summary>
        public class DisposableToken : IDisposable
        {
            public const string DefaultSender = nameof(DefaultSender);
            private readonly DisposableHost _owner;
            public object Sender { get; } = DefaultSender;
            public DisposableToken(DisposableHost owner, object sender, Dictionary<string, object> properties)
            {
                if(sender is Dictionary<string, object> theProperties && properties is null)
                {
                    // We have to assume that the intention was a token
                    // with a dictionary where the sender is not specified.
                    properties = theProperties;
                    sender = null;
                }

                _owner = owner;
                if (sender != null)
                {
                    Sender = sender;
                }
                else
                {   /* G T K */
                    // Otherwise leave at DefaultSender.
                }
                if (properties != null)
                {
                    foreach (var key in properties.Keys)
                    {
                        owner[key] = properties[key];
                    }
                }
            }

            public Dictionary<string, object> Properties => _owner;

            public void Dispose()
            {
                _owner.Pop(this);
            }
        }
        class DisposableTokenCollection : List<DisposableToken>{ }

        public event BeginUsingEventHandler BeginUsing;

        public event FinalDisposeEventHandler FinalDispose;

        public event CountChangedEventHandler CountChanged;
    }

    public delegate void BeginUsingEventHandler(object sender, BeginUsingEventArgs e);
    public class BeginUsingEventArgs : EventArgs
    {
        public BeginUsingEventArgs(DisposableToken token)
        {
            AutoDisposableContext = token;
        }
        public DisposableToken AutoDisposableContext { get; private set; }
    }

    public delegate void FinalDisposeEventHandler(Object sender, FinalDisposeEventArgs e);
    public class FinalDisposeEventArgs : EventArgs
    {
        public FinalDisposeEventArgs(object[] releasedSenders)
        {
            ReleasedSenders = releasedSenders;
        }

        public object[] ReleasedSenders { get; }
    }

    public delegate void CountChangedEventHandler(object sender, CountChangedEventArgs e);
    public class CountChangedEventArgs : EventArgs
    {
        public CountChangedEventArgs(string name, int count)
        {
            Name = name;
            Count = count;
        }

        public string Name { get; }
        public int Count { get; }
    }
}

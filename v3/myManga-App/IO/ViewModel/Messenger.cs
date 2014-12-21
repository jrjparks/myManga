using Core.Other.Singleton;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace myManga_App.IO.ViewModel
{
    /// <summary>
    /// Based on Messenger by: Ciesix
    /// https://stackoverflow.com/questions/23798425/wpf-mvvm-communication-between-view-model
    /// </summary>
    [DebuggerStepThrough]
    public class Messenger
    {
        public static Messenger Default
        { get { return Singleton<Messenger>.Instance; } }

        protected readonly SynchronizationContext SynchronizationContext;
        protected readonly ConcurrentDictionary<MessengerKey, Object> Dictionary = new ConcurrentDictionary<MessengerKey, Object>();

        private Messenger() : this(null) { }
        private Messenger(SynchronizationContext SynchronizationContext = null)
        { this.SynchronizationContext = SynchronizationContext ?? SynchronizationContext.Current ?? new SynchronizationContext(); }

        public Boolean RegisterRecipient<T>(Object Recipient, Action<T> Action)
        { return RegisterRecipient(Recipient, Action, null); }

        public Boolean RegisterRecipient<T>(Object Recipient, Action<T> Action, Object Context)
        { return Dictionary.TryAdd(new MessengerKey(Recipient, Context), Action); }

        public Boolean RegisterRecipient<T>(Object Recipient, Action<T, Object> Action, Object Context)
        { return Dictionary.TryAdd(new MessengerKey(Recipient, Context), Action); }

        public Boolean UnregisterRecipient(Object Recipient)
        { return UnregisterRecipient(Recipient, null); }

        public Boolean UnregisterRecipient(Object Recipient, Object Context)
        { Object Action; return Dictionary.TryRemove(new MessengerKey(Recipient, Context), out Action); }

        public void Send<T>(T Message)
        { Send(Message, null); }

        public void Send<T>(T Message, Object Context)
        {
            IEnumerable<KeyValuePair<MessengerKey, Object>> Results;
            if (Context == null) Results = from Result in Dictionary where Result.Key.Context == null select Result;
            else Results = from Result in Dictionary where Result.Key.Context != null && Result.Key.Context.Equals(Context) select Result;
            foreach (Action<T> Action in Results.Select(x => x.Value).OfType<Action<T>>())
            { this.SynchronizationContext.Post(delegate { Action.Invoke(Message); }, null); }
            foreach (Action<T, Object> Action in Results.Select(x => x.Value).OfType<Action<T, Object>>())
            { this.SynchronizationContext.Post(delegate { Action.Invoke(Message, Context); }, null); }
        }

        protected class MessengerKey
        {
            public Object Recipient { get; private set; }
            public Object Context { get; private set; }

            public MessengerKey(Object Recipient, Object Context)
            {
                this.Recipient = Recipient;
                this.Context = Context;
            }

            public override bool Equals(object obj)
            { return (obj != null && obj is MessengerKey) ? this.Equals(obj as MessengerKey) : false; }

            protected bool Equals(MessengerKey other)
            { return Equals(Recipient, other.Recipient) && Equals(Context, other.Context); }

            public override int GetHashCode()
            { unchecked { return ((Recipient != null ? Recipient.GetHashCode() : 0) * 397) ^ (Context != null ? Context.GetHashCode() : 0); } }
        }

        public enum SimpleContext
        {
            None = 0x00,
            Added = 0x01,
            Updated = 0x02,
            Removed = 0x04,
        }
    }
}

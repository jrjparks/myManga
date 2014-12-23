using Core.Other.Singleton;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Core.MVVM
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

        /// <summary>
        /// Basic Action Register
        /// </summary>
        /// <typeparam name="T">Message Type</typeparam>
        /// <param name="Recipient">Class that is listening.</param>
        /// <param name="Action">Action to perform.</param>
        /// <returns>Register success.</returns>
        public Boolean RegisterRecipient<T>(Object Recipient, Action<T> Action)
        { return RegisterRecipient(Recipient, Action, null); }

        /// <summary>
        /// Basic Action Register with Context
        /// </summary>
        /// <typeparam name="T">Message Type</typeparam>
        /// <param name="Recipient">Class that is listening.</param>
        /// <param name="Action">Action to perform.</param>
        /// <param name="Context">Context of Message.</param>
        /// <returns>Register success.</returns>
        public Boolean RegisterRecipient<T>(Object Recipient, Action<T> Action, Object Context)
        { return Dictionary.TryAdd(new MessengerKey(Recipient, Context), Action); }

        /// <summary>
        /// Basic Action Register with Context in Action
        /// </summary>
        /// <typeparam name="T">Message Type</typeparam>
        /// <param name="Recipient">Class that is listening.</param>
        /// <param name="Action">Action to perform.</param>
        /// <param name="Context">Context of Message.</param>
        /// <returns>Register success.</returns>
        public Boolean RegisterRecipient<T>(Object Recipient, Action<T, Object> Action, Object Context)
        { return Dictionary.TryAdd(new MessengerKey(Recipient, Context), Action); }

        /// <summary>
        /// Unregister Action
        /// </summary>
        /// <param name="Recipient">Class that is listening.</param>
        /// <returns>Unregister success.</returns>
        public Boolean UnregisterRecipient(Object Recipient)
        { return UnregisterRecipient(Recipient, null); }

        /// <summary>
        /// Unregister Action
        /// </summary>
        /// <param name="Recipient">Class that is listening.</param>
        /// <param name="Context">Context of Message.</param>
        /// <returns>Unregister success.</returns>
        public Boolean UnregisterRecipient(Object Recipient, Object Context)
        { Object Action; return Dictionary.TryRemove(new MessengerKey(Recipient, Context), out Action); }

        /// <summary>
        /// Send a message with Context
        /// </summary>
        /// <typeparam name="T">Message Type</typeparam>
        /// <param name="Message">Message Data</param>
        public void Send<T>(T Message)
        { Send(Message, null); }

        /// <summary>
        /// Send a message with Context
        /// </summary>
        /// <typeparam name="T">Message Type</typeparam>
        /// <param name="Message">Message Data</param>
        /// <param name="Context">Context of Message.</param>
        public void Send<T>(T Message, Object Context)
        {
            IEnumerable<KeyValuePair<MessengerKey, Object>> Results;
            if (Context == null) Results = from Result in Dictionary where Object.Equals(Result.Key.Context, null) select Result;
            else Results = from Result in Dictionary where Object.Equals(Result.Key.Context, Context) select Result;
            foreach (Action<T> Action in Results.Select(x => x.Value).OfType<Action<T>>())
            { this.SynchronizationContext.Post(delegate { Action.Invoke(Message); }, null); }
            foreach (Action<T, Object> Action in Results.Select(x => x.Value).OfType<Action<T, Object>>())
            { this.SynchronizationContext.Post(delegate { Action.Invoke(Message, Context); }, null); }
        }

        /// <summary>
        /// Class to store Key for Messenger
        /// </summary>
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

        /// <summary>
        /// Basic Context Data
        /// </summary>
        public enum SimpleContext
        {
            None = 0x00,
            Added = 0x01,
            Updated = 0x02,
            Removed = 0x04,
        }
    }
}

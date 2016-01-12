namespace System
{
    public class GenericEventArgs<EventType> : EventArgs
    {
        public EventType Value
        { get; private set; }

        public GenericEventArgs(EventType Value)
            : base()
        {
            this.Value = Value;
        }
    }
}

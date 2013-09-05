using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows;

namespace Core.IO
{
    [Serializable, DebuggerStepThrough]
    public abstract class SerializableObject : DependencyObject, ISerializable
    {
        public SerializableObject() { }

        protected SerializableObject(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new System.ArgumentNullException("info");
            foreach (PropertyInfo inf in this.GetType().GetProperties())
                inf.SetValue(this, info.GetValue(inf.Name, inf.PropertyType), null);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new System.ArgumentNullException("info");
            foreach (PropertyInfo inf in this.GetType().GetProperties())
                info.AddValue(inf.Name, inf.GetValue(this, null));
        }
    }
}

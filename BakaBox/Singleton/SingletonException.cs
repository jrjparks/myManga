using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BakaBox
{
    [Serializable]
    public class SingletonException
       : Exception
    {
        public SingletonException()
        {
        }

        public SingletonException(string message)
            : base(message)
        {
        }

        public SingletonException(Exception innerException)
            : base(null, innerException)
        {
        }

        public SingletonException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}

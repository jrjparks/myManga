using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Core.Other.Singleton
{
    [DebuggerStepThrough]
    public static class Singleton<T>
       where T : class
    {
        static volatile T instance;
        static object objlock = new object();

        static Singleton() { }

        public static T Instance
        {
            get
            {
                if (instance == null)
                    lock (objlock)
                    {
                        if (instance == null)
                        {
                            ConstructorInfo constructor = null;
                            try
                            {
                                // Binding flags exclude public constructors.
                                constructor = typeof(T).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[0], null);
                            }
                            catch (Exception exception)
                            {
                                throw new Exception(String.Format("Singleton of {0} threw an error.", typeof(T).Name), exception);
                            }

                            if (constructor == null || constructor.IsAssembly)
                                // Also exclude internal constructors.
                                throw new Exception(String.Format("A private or protected constructor is missing for '{0}'.", typeof(T).Name));
                            instance = (T)constructor.Invoke(null);
                        }
                    }

                return instance;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.Sith.Utility
{
    public class Singleton<T> where T : class, new()
    {
        public T value {get; private set;}
        public static T Instance
        {
            get { return _instance.Value; }
        }

        private static readonly Lazy<T>
        _instance = new Lazy<T>(() => new T(), LazyThreadSafetyMode.ExecutionAndPublication);
    }
}

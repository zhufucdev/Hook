using Jint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hook
{
    internal static class Extension
    {
        public static Jint.Native.JsValue Call(this Jint.Native.ICallable callable, Jint.Engine engine, object[] args)
            => callable.Call(args.Select(a => Jint.Native.JsValue.FromObject(engine, a)).ToArray());
        public static Jint.Native.JsValue Invoke(this Jint.Native.ICallable callable, Jint.Engine engine, params object[] args)
            => callable.Call(engine, args);
    }
}

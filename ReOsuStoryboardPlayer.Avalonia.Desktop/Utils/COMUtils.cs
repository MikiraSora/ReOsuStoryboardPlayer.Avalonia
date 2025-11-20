using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.Utils
{
    internal partial class COMUtils
    {
        public static T ActivateClass<T>(Guid clsid, Guid iid) where T : class
        {
            Debug.Assert(iid == typeof(T).GUID);
            int hr = CoCreateInstance(ref clsid, IntPtr.Zero, 1, ref iid, out var obj);
            Marshal.ThrowExceptionForHR(hr);
            return obj as T;
        }

        public static unsafe T GetManaged<T>(nint ptr) where T : class
        {
            var result = UniqueComInterfaceMarshaller<T>.ConvertToManaged((void*)ptr);
            UniqueComInterfaceMarshaller<T>.Free((void*)ptr);
            return result;
        }

        [LibraryImport("Ole32")]
        [return: MarshalAs(UnmanagedType.Error)]
        private static partial int CoCreateInstance(
            ref Guid rclsid,
            IntPtr pUnkOuter,
            int dwClsContext,
            ref Guid riid,
            [MarshalUsing(typeof(UniqueComInterfaceMarshaller<object>))] out object ppObj);
    }
}

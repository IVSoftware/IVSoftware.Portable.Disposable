using System;
using System.Collections.Generic;
using System.Text;

namespace IVSoftware.Portable.Disposable
{
    public static partial class Clients
    {
        /// <usage>
        /// using static IVSoftware.Portable.Disposable.Clients
        /// </usage>
        public static DisposableHost LoadingModel { get; } = new DisposableHost(nameof(LoadingModel));
        public static DisposableHost AutoWaitCursor { get; } = new DisposableHost(nameof(AutoWaitCursor));

    }
}

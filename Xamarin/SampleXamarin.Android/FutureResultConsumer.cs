// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Android.Runtime;
using Java.Util.Functions;
using System;

namespace SampleXamarin
{
    internal class FutureResultConsumer<T> : Java.Lang.Object, IConsumer
        where T : class, IJavaObject
    {
        private Action<T> handler;

        public FutureResultConsumer(Action<T> handler)
        {
            this.handler = handler;
        }

        public void Accept(Java.Lang.Object obj)
        {
            T typedObject = obj.JavaCast<T>();
            handler(typedObject);
        }
    }
}
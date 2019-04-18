// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using UnityEngine;

namespace Microsoft.Azure.SpatialAnchors.Unity.Android
{
    public class UnityAndroidHelper : IDisposable
    {
        private readonly AndroidJavaObject unityActivity;
        private readonly AndroidJavaClass unityPlayer;
        private bool disposedValue = false;

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static UnityAndroidHelper Instance { get; } = Create();

        /// <summary>
        /// Initializes a new instance of the <see cref="UnityAndroidHelper"/> class.
        /// </summary>
        /// <param name="unityPlayer">The unity player.</param>
        /// <param name="unityActivity">The unity activity.</param>
        private UnityAndroidHelper(AndroidJavaClass unityPlayer, AndroidJavaObject unityActivity)
        {
            this.unityPlayer = unityPlayer;
            this.unityActivity = unityActivity;
        }

        /// <summary>
        /// Dispatches an action to the UI thread.
        /// </summary>
        /// <param name="activityAction">The activity action.</param>
        public void DispatchUiThread(Action<AndroidJavaObject> activityAction)
        {
            if (activityAction != null)
            {
                try
                {
                    this.unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() => activityAction.Invoke(this.unityActivity)));
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Shows the Android toast message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void ShowToastMessage(string message)
        {
            this.DispatchUiThread(unityActivity =>
            {
                using (AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast"))
                {
                    AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity,
                        message, 0);
                    toastObject.Call("show");
                }
            });
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.unityPlayer.Dispose();
                    this.unityActivity.Dispose();
                }

                this.disposedValue = true;
            }
        }

        /// <summary>
        /// Creates a <see cref="UnityAndroidHelper"/> instance.
        /// </summary>
        /// <returns><see cref="UnityAndroidHelper"/>.</returns>
        private static UnityAndroidHelper Create()
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            return new UnityAndroidHelper(unityPlayer, unityActivity);
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using UnityEngine;

namespace Microsoft.Azure.SpatialAnchors.Unity.Android
{
    /// <summary>
    /// A utility class to perform actions on Android devices.
    /// </summary>
    public class AndroidHelper : IDisposable
    {
        #region Instance Version

        #region Member Variables
        private readonly AndroidJavaObject unityActivity;
        private readonly AndroidJavaClass unityPlayer;
        private bool disposedValue = false;
        #endregion // Member Variables

        #region Internal Methods
        /// <summary>
        /// Initializes a new instance of the <see cref="AndroidHelper"/> class.
        /// </summary>
        /// <param name="unityPlayer">The unity player.</param>
        /// <param name="unityActivity">The unity activity.</param>
        private AndroidHelper(AndroidJavaClass unityPlayer, AndroidJavaObject unityActivity)
        {
            this.unityPlayer = unityPlayer;
            this.unityActivity = unityActivity;
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
        #endregion // Internal Methods


        #region Public Methods
        /// <summary>
        /// Queues an action to be run on the Android UI thread.
        /// </summary>
        /// <param name="activityAction">
        /// The action to run.
        /// </param>
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
        #endregion // Public Methods

        #endregion // Instance Version

        #region Static Version

        #region Member Variables
        static private AndroidHelper instance;
        #endregion // Member Variables

        #region Internal Methods
        /// <summary>
        /// Creates a <see cref="AndroidHelper"/> instance.
        /// </summary>
        /// <returns>
        /// The newly created <see cref="AndroidHelper"/> instance.
        /// </returns>
        static private void Create()
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            instance = new AndroidHelper(unityPlayer, unityActivity);
        }
        #endregion // Internal Methods

        #region Public Properties
        /// <summary>
        /// Gets the instance.
        /// </summary>
        static public AndroidHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    Create();
                }
                return instance;
            }
        }
        #endregion // Public Properties

        #endregion // Static Version
    }
}

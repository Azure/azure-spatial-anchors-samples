// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Google.AR.Core;
using Java.Util.Concurrent;
using Microsoft.Azure.SpatialAnchors;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SampleXamarin
{
    internal class AzureSpatialAnchorsManager
    {
        private readonly CloudSpatialAnchorSession spatialAnchorsSession;

        private TrackingState lastTrackingState = TrackingState.Stopped;

        private TrackingFailureReason lastTrackingFailureReason = TrackingFailureReason.None;

        public bool CanCreateAnchor => this.CreateScanningProgressValue >= 1;

        public float CreateScanningProgressValue { get; set; } = 0;

        public bool LocatingAnchors => this.spatialAnchorsSession.ActiveWatchers.Count > 0;

        public bool IsArTracking => this.lastTrackingState == TrackingState.Tracking;

        public bool Running { get; private set; }

        public event EventHandler<AnchorLocatedEvent> OnAnchorLocated;

        public event EventHandler<LocateAnchorsCompletedEvent> OnLocateAnchorsCompleted;

        public event EventHandler<LogDebugEventArgs> OnLogDebug;

        public event EventHandler<SessionErrorEvent> OnSessionError;

        public event EventHandler<SessionUpdatedEvent> OnSessionUpdated;

        static AzureSpatialAnchorsManager()
        {
            CloudServices.Initialize(Android.App.Application.Context);
        }

        public AzureSpatialAnchorsManager(Session arCoreSession)
        {
            this.spatialAnchorsSession = new CloudSpatialAnchorSession();
            this.spatialAnchorsSession.Configuration.AccountKey = AccountDetails.SpatialAnchorsAccountKey;
            this.spatialAnchorsSession.Configuration.AccountId = AccountDetails.SpatialAnchorsAccountId;
            this.spatialAnchorsSession.Session = arCoreSession;
            this.spatialAnchorsSession.LogDebug += this.SpatialCloudSession_LogDebug;
            this.spatialAnchorsSession.Error += this.SpatialAnchorsSession_Error;
            this.spatialAnchorsSession.AnchorLocated += this.SpatialAnchorsSession_AnchorLocated;
            this.spatialAnchorsSession.LocateAnchorsCompleted += this.SpatialAnchorsSession_LocateAnchorsCompleted;
            this.spatialAnchorsSession.SessionUpdated += this.SpatialAnchorsSession_SessionUpdated;
        }

        public CloudSpatialAnchorWatcher StartLocating(AnchorLocateCriteria locateCriteria)
        {
            // Only 1 active watcher at a time is permitted.
            this.StopLocating();

            return this.spatialAnchorsSession.CreateWatcher(locateCriteria);
        }

        public Task DeleteAnchorAsync(CloudSpatialAnchor anchor)
        {
            return this.spatialAnchorsSession.DeleteAnchorAsync(anchor).GetAsync();
        }

        public void ResetSession(bool resumeIfRunning = false)
        {
            bool running = this.Running;

            this.StopLocating();
            this.StopSession();
            this.spatialAnchorsSession.Reset();

            if (resumeIfRunning && running)
            {
                this.StartSession();
            }
        }

        public async Task<CloudSpatialAnchor> CreateAnchorAsync(CloudSpatialAnchor newCloudAnchor)
        {
            if (newCloudAnchor == null)
            {
                throw new ArgumentNullException(nameof(newCloudAnchor));
            }

            if (newCloudAnchor.LocalAnchor == null || !string.IsNullOrEmpty(newCloudAnchor.Identifier))
            {
                throw new ArgumentException("The specified cloud anchor cannot be saved.", nameof(newCloudAnchor));
            }

            if (!this.CanCreateAnchor)
            {
                throw new ArgumentException("Not ready to create. Need more data.");
            }

            try
            {
                await this.spatialAnchorsSession.CreateAnchorAsync(newCloudAnchor).GetAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return newCloudAnchor;
        }

        public void StartSession()
        {
            if (!this.Running)
            {
                this.spatialAnchorsSession.Start();
                this.Running = true;
            }
        }

        public void StopLocating()
        {
            CloudSpatialAnchorWatcher watcher = this.spatialAnchorsSession.ActiveWatchers.FirstOrDefault();

            // Only 1 active watcher at a time is permitted.
            watcher?.Stop();
            watcher?.Dispose();
        }

        public void StopSession()
        {
            this.StopLocating();
            if (this.Running)
            {
                this.spatialAnchorsSession.Stop();
                this.Running = false;
            }
        }

        public void Update(Frame frame)
        {
            if (frame.Camera.TrackingState != this.lastTrackingState
                || frame.Camera.TrackingFailureReason != this.lastTrackingFailureReason)
            {
                this.lastTrackingState = frame.Camera.TrackingState;
                this.lastTrackingFailureReason = frame.Camera.TrackingFailureReason;
                Debug.WriteLine($"Tracker state changed: {this.lastTrackingState}, {this.lastTrackingFailureReason}.");
            }

            Task.Run(() => this.spatialAnchorsSession.ProcessFrame(frame));
        }

        private void SpatialAnchorsSession_AnchorLocated(object sender, AnchorLocatedEventArgs e)
        {
            this.OnAnchorLocated?.Invoke(sender, e.Args);
        }

        private void SpatialAnchorsSession_Error(object sender, SessionErrorEventArgs e)
        {
            SessionErrorEvent eventArgs = e?.Args;

            if (eventArgs == null)
            {
                Debug.WriteLine("Azure Spatial Anchors reported an unspecified error.");
                return;
            }

            string message = $"{eventArgs.ErrorCode}: {eventArgs.ErrorMessage}";
            Debug.WriteLine(message);

            this.OnSessionError?.Invoke(sender, eventArgs);
        }

        private void SpatialAnchorsSession_LocateAnchorsCompleted(object sender, LocateAnchorsCompletedEventArgs e)
        {
            this.OnLocateAnchorsCompleted?.Invoke(sender, e.Args);
        }

        private void SpatialAnchorsSession_SessionUpdated(object sender, SessionUpdatedEventArgs e)
        {
            float createScanProgress = Math.Min(e.Args.Status.RecommendedForCreateProgress, 1);

            Debug.WriteLine($"Create scan progress: {createScanProgress:0%}");

            this.CreateScanningProgressValue = createScanProgress;

            this.OnSessionUpdated?.Invoke(sender, e.Args);
        }

        private void SpatialCloudSession_LogDebug(object sender, LogDebugEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.Args.Message))
            {
                return;
            }

            Debug.WriteLine(e.Args.Message);

            this.OnLogDebug?.Invoke(sender, e);
        }
    }
}
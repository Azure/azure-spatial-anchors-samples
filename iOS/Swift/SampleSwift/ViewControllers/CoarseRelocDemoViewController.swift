// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

// This demo creates and saves an anchor. It then locates all anchors near the device based on sensor data.
class CoarseRelocDemoViewController: BaseViewController {

    @IBOutlet var sensorStatusView: SensorStatusView!

    /// Whether the "Access WiFi Information" capability is enabled.
    /// If available, the MAC address of the connected Wi-Fi access point can be used
    /// to help find nearby anchors.
    /// Note: This entitlement requires a paid Apple Developer account.
    private static let haveAccessWifiInformationEntitlement = false

    /// Whitelist of Bluetooth-LE beacons used to find anchors and improve the locatability
    /// of existing anchors.
    /// Add the UUIDs for your own Bluetooth beacons here to use them with Azure Spatial Anchors.
    public static let knownBluetoothProximityUuids = [
        "61687109-905f-4436-91f8-e602f514c96d",
        "e1f54e02-1e23-44e0-9c3d-512eb56adec9",
        "01234567-8901-2345-6789-012345678903",
    ]

    var locationProvider: ASAPlatformLocationProvider?

    var nearDeviceWatcher: ASACloudSpatialAnchorWatcher?
    var numAnchorsFound = 0

    override func onCloudAnchorCreated() {
        ignoreMainButtonTaps = false
        step = .lookForNearbyAnchors

        DispatchQueue.main.async {
            self.feedbackControl.isHidden = true
            self.mainButton.setTitle("Tap to start next Session & look for anchors near device", for: .normal)
        }
    }

    override func onNewAnchorLocated(_ cloudAnchor: ASACloudSpatialAnchor) {
        ignoreMainButtonTaps = false
        step = .stopWatcher

        DispatchQueue.main.async {
            self.numAnchorsFound += 1
            self.feedbackControl.isHidden = true
            self.mainButton.setTitle("\(self.numAnchorsFound) anchor(s) found! Tap to stop watcher.", for: .normal)
        }
    }

    override func renderer(_ renderer: SCNSceneRenderer, updateAtTime time: TimeInterval) {
        super.renderer(renderer, updateAtTime: time)

        DispatchQueue.main.async {
            self.sensorStatusView.update()
        }
    }

    @objc override func mainButtonTap(sender: UIButton) {
        if (ignoreMainButtonTaps) {
            return
        }

        switch (step) {
        case .prepare:
            mainButton.setTitle("Tap to start Session", for: .normal)
            step = .createCloudAnchor
            createLocationProvider()
        case .createCloudAnchor:
            ignoreMainButtonTaps = true
            currentlyPlacingAnchor = true
            saveCount = 0

            startSession()
            attachLocationProviderToSession()

            // When you tap on the screen, touchesBegan will call createLocalAnchor and create a local ARAnchor.
            // We will then put that anchor in the anchorVisuals dictionary with a special key and call CreateCloudAnchor when there is enough data for saving.
            // CreateCloudAnchor will call onCloudAnchorCreated when its async method returns to move to the next step.
            mainButton.setTitle("Tap on the screen to create an Anchor ☝️", for: .normal)
        case .lookForNearbyAnchors:
            ignoreMainButtonTaps = true
            stopSession()
            startSession()
            attachLocationProviderToSession()

            // We will get a call to onLocateAnchorsCompleted which will move to the next step when the locate operation completes.
            lookForAnchorsNearDevice()
        case .stopWatcher:
            step = .stopSession
            nearDeviceWatcher?.stop()
            nearDeviceWatcher = nil
            mainButton.setTitle("Tap to stop Session and return to the main menu", for: .normal)
        case .stopSession:
            stopSession()
            self.locationProvider = nil
            self.sensorStatusView.setModel(nil)
            moveToMainMenu()
        default:
            assertionFailure("Demo has somehow entered an invalid state")
        }
    }

    private func createLocationProvider() {
        locationProvider = ASAPlatformLocationProvider()

        // Register known Bluetooth beacons
        locationProvider!.sensors!.knownBeaconProximityUuids =
            CoarseRelocDemoViewController.knownBluetoothProximityUuids

        // Display the sensor status
        let sensorStatus = LocationProviderSensorStatus(for: locationProvider)
        sensorStatusView.setModel(sensorStatus)

        enableAllowedSensors()
    }

    private func enableAllowedSensors() {
        if let sensors = locationProvider?.sensors {
            sensors.bluetoothEnabled = true
            sensors.wifiEnabled = CoarseRelocDemoViewController.haveAccessWifiInformationEntitlement
            sensors.geoLocationEnabled = true
        }
    }

    private func attachLocationProviderToSession() {
        cloudSession!.locationProvider = locationProvider
    }

    private func lookForAnchorsNearDevice() {
        let nearDevice = ASANearDeviceCriteria()!
        nearDevice.distanceInMeters = 8.0
        nearDevice.maxResultCount = 25

        let criteria = ASAAnchorLocateCriteria()!
        criteria.nearDevice = nearDevice
        nearDeviceWatcher = cloudSession!.createWatcher(criteria)

        mainButton.setTitle("Looking for anchors near device...", for: .normal)
    }
}

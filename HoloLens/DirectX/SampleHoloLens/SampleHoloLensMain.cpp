// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#include "pch.h"
#include "SampleHoloLensMain.h"
#include "Common\DirectXHelper.h"
#include "Content\ShaderFileData.h"

#include <windows.graphics.directx.direct3d11.interop.h>

using namespace SampleHoloLens;
using namespace concurrency;
using namespace Microsoft::WRL;
using namespace std::placeholders;
using namespace winrt::Windows::Foundation::Numerics;
using namespace winrt::Windows::Foundation::Collections;
using namespace winrt::Windows::Gaming::Input;
using namespace winrt::Windows::Graphics::Holographic;
using namespace winrt::Windows::Graphics::DirectX::Direct3D11;
using namespace winrt::Windows::Perception::Spatial;
using namespace winrt::Windows::UI::Input::Spatial;
using namespace winrt::Windows::UI::Core;
using namespace winrt::Windows::ApplicationModel::Core;

// Loads and initializes application assets when the application is loaded.
SampleHoloLensMain::SampleHoloLensMain(std::shared_ptr<DX::DeviceResources> const& deviceResources) :
    m_deviceResources(deviceResources)
{
    // Start shader data loading
    ShaderFileData::GetInstance();

    // Register to be notified if the device is lost or recreated.
    m_deviceResources->RegisterDeviceNotify(this);

    // If connected, a game controller can also be used for input.
    m_gamepadAddedEventToken = Gamepad::GamepadAdded(bind(&SampleHoloLensMain::OnGamepadAdded, this, _1, _2));
    m_gamepadRemovedEventToken = Gamepad::GamepadRemoved(bind(&SampleHoloLensMain::OnGamepadRemoved, this, _1, _2));

    for (Gamepad const& gamepad : Gamepad::Gamepads())
    {
        OnGamepadAdded(nullptr, gamepad);
    }

    // Subscribe for notifications about changes to the state of the default HolographicDisplay
    // and its SpatialLocator.
    m_holographicDisplayIsAvailableChangedEventToken = HolographicSpace::IsAvailableChanged(bind(&SampleHoloLensMain::OnHolographicDisplayIsAvailableChanged, this, _1, _2));

    // Acquire the current state of the default HolographicDisplay and its SpatialLocator.
    OnHolographicDisplayIsAvailableChanged(nullptr, nullptr);

    m_viewController = std::make_unique<ViewController>();

    m_viewController->GetFoundAnchors().MapChanged(bind(&SampleHoloLensMain::OnFoundAnchors, this, _1, _2));
}

void SampleHoloLensMain::OnFoundAnchors(winrt::Windows::Foundation::Collections::IObservableMap<winrt::hstring, winrt::SampleHoloLens::AnchorVisual> const& sender, winrt::Windows::Foundation::Collections::IMapChangedEventArgs<winrt::hstring> const& args)
{
    auto change = args.CollectionChange();
    auto key = args.Key();
    winrt::SampleHoloLens::AnchorVisual value = nullptr;
    if (change == CollectionChange::ItemChanged || change == CollectionChange::ItemInserted)
    {
        value = sender.Lookup(args.Key());
    }
    CoreApplication::MainView().CoreWindow().Dispatcher().RunAsync(CoreDispatcherPriority::Normal, [this, change, key, value]() {
        switch (change)
        {
        case CollectionChange::Reset:
            m_anchorVisuals.clear();
            break;
        case CollectionChange::ItemChanged:
        case CollectionChange::ItemInserted:
        {
            auto labeledCubeRenderer = std::make_unique<LabeledCubeRenderer>(m_deviceResources, value.Anchor().CoordinateSystem(), key, value.Color());
            m_anchorVisuals.emplace(key, std::move(labeledCubeRenderer));
            break;
        }
        case CollectionChange::ItemRemoved:
            m_anchorVisuals.erase(key);
            break;
        }
    });
}

void SampleHoloLensMain::SetHolographicSpace(HolographicSpace const& holographicSpace)
{
    UnregisterHolographicEventHandlers();

    m_holographicSpace = holographicSpace;

#ifdef DRAW_SAMPLE_CONTENT
    // Initialize the text renderer.
    constexpr unsigned int offscreenRenderTargetWidth = 2048;
    m_textRenderer = std::make_unique<TextRenderer>(m_deviceResources, offscreenRenderTargetWidth, offscreenRenderTargetWidth);
    // Initialize the input handler.
    m_spatialInputHandler = std::make_unique<SpatialInputHandler>();
    // Initialize the text hologram.
    m_quadRenderer = std::make_unique<QuadRenderer>(m_deviceResources, true);
#endif

    // Respond to camera added events by creating any resources that are specific
    // to that camera, such as the back buffer render target view.
    // When we add an event handler for CameraAdded, the API layer will avoid putting
    // the new camera in new HolographicFrames until we complete the deferral we created
    // for that handler, or return from the handler without creating a deferral. This
    // allows the app to take more than one frame to finish creating resources and
    // loading assets for the new holographic camera.
    // This function should be registered before the app creates any HolographicFrames.
    m_cameraAddedToken = m_holographicSpace.CameraAdded(std::bind(&SampleHoloLensMain::OnCameraAdded, this, _1, _2));

    // Respond to camera removed events by releasing resources that were created for that
    // camera.
    // When the app receives a CameraRemoved event, it releases all references to the back
    // buffer right away. This includes render target views, Direct2D target bitmaps, and so on.
    // The app must also ensure that the back buffer is not attached as a render target, as
    // shown in DeviceResources::ReleaseResourcesForBackBuffer.
    m_cameraRemovedToken = m_holographicSpace.CameraRemoved(std::bind(&SampleHoloLensMain::OnCameraRemoved, this, _1, _2));

    // Notes on spatial tracking APIs:
    // * Stationary reference frames are designed to provide a best-fit position relative to the
    //   overall space. Individual positions within that reference frame are allowed to drift slightly
    //   as the device learns more about the environment.
    // * When precise placement of individual holograms is required, a SpatialAnchor should be used to
    //   anchor the individual hologram to a position in the real world - for example, a point the user
    //   indicates to be of special interest. Anchor positions do not drift, but can be corrected; the
    //   anchor will use the corrected position starting in the next frame after the correction has
    //   occurred.


}

void SampleHoloLensMain::UnregisterHolographicEventHandlers()
{
    if (m_holographicSpace != nullptr)
    {
        // Clear previous event registrations.
        m_holographicSpace.CameraAdded(m_cameraAddedToken);
        m_cameraAddedToken = {};
        m_holographicSpace.CameraRemoved(m_cameraRemovedToken);
        m_cameraRemovedToken = {};
    }

    if (m_spatialLocator != nullptr)
    {
        m_spatialLocator.LocatabilityChanged(m_locatabilityChangedToken);
    }
}

SampleHoloLensMain::~SampleHoloLensMain()
{
    // Deregister device notification.
    m_deviceResources->RegisterDeviceNotify(nullptr);

    UnregisterHolographicEventHandlers();

    Gamepad::GamepadAdded(m_gamepadAddedEventToken);
    Gamepad::GamepadRemoved(m_gamepadRemovedEventToken);
    HolographicSpace::IsAvailableChanged(m_holographicDisplayIsAvailableChangedEventToken);
}

// Updates the application state once per frame.
HolographicFrame SampleHoloLensMain::Update()
{
    // Before doing the timer update, there is some work to do per-frame
    // to maintain holographic rendering. First, we will get information
    // about the current frame.

    // The HolographicFrame has information that the app needs in order
    // to update and render the current frame. The app begins each new
    // frame by calling CreateNextFrame.
    HolographicFrame holographicFrame = m_holographicSpace.CreateNextFrame();

    // Get a prediction of where holographic cameras will be when this frame
    // is presented.
    HolographicFramePrediction prediction = holographicFrame.CurrentPrediction();

    // Back buffers can change from frame to frame. Validate each buffer, and recreate
    // resource views and depth buffers as needed.
    m_deviceResources->EnsureCameraResources(holographicFrame, prediction);

#ifdef DRAW_SAMPLE_CONTENT
    // Next, we get a coordinate system from the attached frame of reference that is
    // associated with the current frame. Later, this coordinate system is used for
    // for creating the stereo view matrices when rendering the sample content.

    SpatialCoordinateSystem currentCoordinateSystem =
        m_attachedReferenceFrame.GetStationaryCoordinateSystemAtTimestamp(prediction.Timestamp());
    SpatialPointerPose pose = SpatialPointerPose::TryGetAtTimestamp(currentCoordinateSystem, prediction.Timestamp());
    m_quadRenderer->UpdateHologramPosition(currentCoordinateSystem, pose, m_timer);
    m_viewController->Update(currentCoordinateSystem);

    // Maintain positional tracking
    m_stationaryReferenceFrame.CoordinateSystem().TryGetTransformTo(currentCoordinateSystem);

    // Check for new input state since the last frame.
    for (GamepadWithButtonState& gamepadWithButtonState : m_gamepads)
    {
        bool buttonDownThisUpdate = ((gamepadWithButtonState.gamepad.GetCurrentReading().Buttons & GamepadButtons::A) == GamepadButtons::A);
        if (buttonDownThisUpdate && !gamepadWithButtonState.buttonAWasPressedLastFrame)
        {
            m_pointerPressed = true;
        }
        gamepadWithButtonState.buttonAWasPressedLastFrame = buttonDownThisUpdate;
    }

    SpatialInteractionSourceState pointerState = m_spatialInputHandler->CheckForInput();
    SpatialPointerPose inputPose = nullptr;
    if (pointerState != nullptr)
    {
        inputPose = pointerState.TryGetPointerPose(currentCoordinateSystem);
    }
    else if (m_pointerPressed)
    {
        inputPose = pose;
    }
    m_pointerPressed = false;

    // When a Pressed gesture is detected, signal the ViewControler
    if (inputPose != nullptr)
    {
        m_viewController->InputReceived(inputPose);
    }

    m_textRenderer->RenderTextOffscreen(m_viewController->GetTitleText(), m_viewController->GetStatusText(), m_viewController->GetLogText());
#endif

    m_timer.Tick([this, &currentCoordinateSystem]()
    {
        //
        // TODO: Update scene objects.
        //
        // Put time-based updates here. By default this code will run once per frame,
        // but if you change the StepTimer to use a fixed time step this code will
        // run as many times as needed to get to the current step.
        //

#ifdef DRAW_SAMPLE_CONTENT
        for (auto& cube : m_anchorVisuals)
        {
            cube.second->Update(m_timer, currentCoordinateSystem);
        }
        m_quadRenderer->Update(m_timer);
#endif
    });

    // The holographic frame will be used to get up-to-date view and projection matrices and
    // to present the swap chain.
    return holographicFrame;
}

// Renders the current frame to each holographic camera, according to the
// current application and spatial positioning state. Returns true if the
// frame was rendered to at least one camera.
bool SampleHoloLensMain::Render(HolographicFrame const& holographicFrame)
{
    // Don't try to render anything before the first Update.
    if (m_timer.GetFrameCount() == 0)
    {
        return false;
    }

    //
    // TODO: Add code for pre-pass rendering here.
    //
    // Take care of any tasks that are not specific to an individual holographic
    // camera. This includes anything that doesn't need the final view or projection
    // matrix, such as lighting maps.
    //

    // Lock the set of holographic camera resources, then draw to each camera
    // in this frame.
    return m_deviceResources->UseHolographicCameraResources<bool>(
        [this, holographicFrame](std::map<UINT32, std::unique_ptr<DX::CameraResources>>& cameraResourceMap)
    {
        // Up-to-date frame predictions enhance the effectiveness of image stablization and
        // allow more accurate positioning of holograms.
        holographicFrame.UpdateCurrentPrediction();
        HolographicFramePrediction prediction = holographicFrame.CurrentPrediction();

        bool atLeastOneCameraRendered = false;
        for (HolographicCameraPose const& cameraPose : prediction.CameraPoses())
        {
            // This represents the device-based resources for a HolographicCamera.
            DX::CameraResources* pCameraResources = cameraResourceMap[cameraPose.HolographicCamera().Id()].get();

            // Get the device context.
            const auto context = m_deviceResources->GetD3DDeviceContext();
            const auto depthStencilView = pCameraResources->GetDepthStencilView();

            // Set render targets to the current holographic camera.
            ID3D11RenderTargetView *const targets[1] = { pCameraResources->GetBackBufferRenderTargetView() };
            context->OMSetRenderTargets(1, targets, depthStencilView);

            // Clear the back buffer and depth stencil view.
            context->ClearRenderTargetView(targets[0], DirectX::Colors::Transparent);
            context->ClearDepthStencilView(depthStencilView, D3D11_CLEAR_DEPTH | D3D11_CLEAR_STENCIL, 1.0f, 0);

            //
            // TODO: Replace the sample content with your own content.
            //
            // Notes regarding holographic content:
            //    * For drawing, remember that you have the potential to fill twice as many pixels
            //      in a stereoscopic render target as compared to a non-stereoscopic render target
            //      of the same resolution. Avoid unnecessary or repeated writes to the same pixel,
            //      and only draw holograms that the user can see.
            //    * To help occlude hologram geometry, you can create a depth map using geometry
            //      data obtained via the surface mapping APIs. You can use this depth map to avoid
            //      rendering holograms that are intended to be hidden behind tables, walls,
            //      monitors, and so on.
            //    * On HolographicDisplays that are transparent, black pixels will appear transparent
            //      to the user. On such devices, you should clear the screen to Transparent as shown
            //      above. You should still use alpha blending to draw semitransparent holograms.
            //


            // The view and projection matrices for each holographic camera will change
            // every frame. This function refreshes the data in the constant buffer for
            // the holographic camera indicated by cameraPose.
            pCameraResources->UpdateViewProjectionBuffer(m_deviceResources, cameraPose, m_attachedReferenceFrame.GetStationaryCoordinateSystemAtTimestamp(prediction.Timestamp()));

            // Attach the view/projection constant buffer for this camera to the graphics pipeline.
            bool cameraActive = pCameraResources->AttachViewProjectionBuffer(m_deviceResources);

#ifdef DRAW_SAMPLE_CONTENT
            // Only render world-locked content when positional tracking is active.
            if (cameraActive)
            {
                // Draw the sample hologram.
                for (auto& cube : m_anchorVisuals)
                {
                    cube.second->Render();
                }
                m_quadRenderer->Render(m_textRenderer->GetTexture());

                // On versions of the platform that support the CommitDirect3D11DepthBuffer API, we can
                // provide the depth buffer to the system, and it will use depth information to stabilize
                // the image at a per-pixel level.
                HolographicCameraRenderingParameters renderingParameters = holographicFrame.GetRenderingParameters(cameraPose);

                IDirect3DSurface interopSurface = DX::CreateDepthTextureInteropObject(pCameraResources->GetDepthStencilTexture2D());

                // Calling CommitDirect3D11DepthBuffer causes the system to queue Direct3D commands to
                // read the depth buffer. It will then use that information to stabilize the image as
                // the HolographicFrame is presented.
                renderingParameters.CommitDirect3D11DepthBuffer(interopSurface);
            }
#endif
            atLeastOneCameraRendered = true;
        }

        return atLeastOneCameraRendered;
    });
}

void SampleHoloLensMain::SaveAppState()
{
    //
    // TODO: Insert code here to save your app state.
    //       This method is called when the app is about to suspend.
    //
    //       For example, store information in the SpatialAnchorStore.
    //
}

void SampleHoloLensMain::LoadAppState()
{
    //
    // TODO: Insert code here to load your app state.
    //       This method is called when the app resumes.
    //
    //       For example, load information from the SpatialAnchorStore.
    //
}

void SampleHoloLensMain::OnPointerPressed()
{
    m_pointerPressed = true;
}

// Notifies classes that use Direct3D device resources that the device resources
// need to be released before this method returns.
void SampleHoloLensMain::OnDeviceLost()
{
#ifdef DRAW_SAMPLE_CONTENT
    m_textRenderer->ReleaseDeviceDependentResources();
    m_quadRenderer->ReleaseDeviceDependentResources();
    for (auto& cube : m_anchorVisuals)
    {
        cube.second->ReleaseDeviceDependentResources();
    }
#endif
}

// Notifies classes that use Direct3D device resources that the device resources
// may now be recreated.
void SampleHoloLensMain::OnDeviceRestored()
{
#ifdef DRAW_SAMPLE_CONTENT
    m_textRenderer->CreateDeviceDependentResources();
    m_quadRenderer->CreateDeviceDependentResources();
    for (auto& cube : m_anchorVisuals)
    {
        cube.second->CreateDeviceDependentResources();
    }
#endif
}

void SampleHoloLensMain::OnLocatabilityChanged(SpatialLocator const& sender, winrt::Windows::Foundation::IInspectable const& args)
{
    switch (sender.Locatability())
    {
    case SpatialLocatability::Unavailable:
        // Holograms cannot be rendered.
    {
        winrt::hstring message = (L"Warning! Positional tracking is " + std::to_wstring(int(sender.Locatability())) + L".\n").c_str();
        OutputDebugStringW(message.data());
    }
    break;

    // In the following three cases, it is still possible to place holograms using a
    // SpatialLocatorAttachedFrameOfReference.
    case SpatialLocatability::PositionalTrackingActivating:
        // The system is preparing to use positional tracking.

    case SpatialLocatability::OrientationOnly:
        // Positional tracking has not been activated.

    case SpatialLocatability::PositionalTrackingInhibited:
        // Positional tracking is temporarily inhibited. User action may be required
        // in order to restore positional tracking.
        break;

    case SpatialLocatability::PositionalTrackingActive:
        if (m_spatialLocator != nullptr)
        {
            m_stationaryReferenceFrame = m_spatialLocator.CreateStationaryFrameOfReferenceAtCurrentLocation();
        }
        break;
    }
}

void SampleHoloLensMain::OnCameraAdded(
    HolographicSpace const& sender,
    HolographicSpaceCameraAddedEventArgs const& args
)
{
    winrt::Windows::Foundation::Deferral deferral = args.GetDeferral();
    HolographicCamera holographicCamera = args.Camera();
    create_task([this, deferral, holographicCamera]()
    {
        //
        // TODO: Allocate resources for the new camera and load any content specific to
        //       that camera. Note that the render target size (in pixels) is a property
        //       of the HolographicCamera object, and can be used to create off-screen
        //       render targets that match the resolution of the HolographicCamera.
        //

        // Create device-based resources for the holographic camera and add it to the list of
        // cameras used for updates and rendering. Notes:
        //   * Since this function may be called at any time, the AddHolographicCamera function
        //     waits until it can get a lock on the set of holographic camera resources before
        //     adding the new camera. At 60 frames per second this wait should not take long.
        //   * A subsequent Update will take the back buffer from the RenderingParameters of this
        //     camera's CameraPose and use it to create the ID3D11RenderTargetView for this camera.
        //     Content can then be rendered for the HolographicCamera.
        m_deviceResources->AddHolographicCamera(holographicCamera);

        // Holographic frame predictions will not include any information about this camera until
        // the deferral is completed.
        deferral.Complete();
    });
}

void SampleHoloLensMain::OnCameraRemoved(
    HolographicSpace const& sender,
    HolographicSpaceCameraRemovedEventArgs const& args
)
{
    create_task([this]()
    {
        //
        // TODO: Asynchronously unload or deactivate content resources (not back buffer
        //       resources) that are specific only to the camera that was removed.
        //
    });

    // Before letting this callback return, ensure that all references to the back buffer
    // are released.
    // Since this function may be called at any time, the RemoveHolographicCamera function
    // waits until it can get a lock on the set of holographic camera resources before
    // deallocating resources for this camera. At 60 frames per second this wait should
    // not take long.
    m_deviceResources->RemoveHolographicCamera(args.Camera());
}

void SampleHoloLensMain::OnGamepadAdded(winrt::Windows::Foundation::IInspectable, Gamepad const& args)
{
    for (GamepadWithButtonState const& gamepadWithButtonState : m_gamepads)
    {
        if (args == gamepadWithButtonState.gamepad)
        {
            // This gamepad is already in the list.
            return;
        }
    }

    GamepadWithButtonState newGamepad = { args, false };
    m_gamepads.push_back(newGamepad);
}

void SampleHoloLensMain::OnGamepadRemoved(winrt::Windows::Foundation::IInspectable, Gamepad const& args)
{
    m_gamepads.erase(std::remove_if(m_gamepads.begin(), m_gamepads.end(), [&](GamepadWithButtonState& gamepadWithState)
        {
            return gamepadWithState.gamepad == args;
        }),
        m_gamepads.end());
}

void SampleHoloLensMain::OnHolographicDisplayIsAvailableChanged(winrt::Windows::Foundation::IInspectable, winrt::Windows::Foundation::IInspectable)
{
    // Get the spatial locator for the default HolographicDisplay, if one is available.
    SpatialLocator spatialLocator = nullptr;
    HolographicDisplay defaultHolographicDisplay = HolographicDisplay::GetDefault();
    if (defaultHolographicDisplay)
    {
        spatialLocator = defaultHolographicDisplay.SpatialLocator();
    }

    if (m_spatialLocator != spatialLocator)
    {
        // If the spatial locator is disconnected or replaced, we should discard all state that was
        // based on it.
        if (m_spatialLocator != nullptr)
        {
            m_spatialLocator.LocatabilityChanged(m_locatabilityChangedToken);
            m_spatialLocator = nullptr;
        }

        m_stationaryReferenceFrame = nullptr;

        if (spatialLocator != nullptr)
        {
            // Use the SpatialLocator from the default HolographicDisplay to track the motion of the device.
            m_spatialLocator = spatialLocator;

            // Respond to changes in the positional tracking state.
            m_locatabilityChangedToken = m_spatialLocator.LocatabilityChanged(std::bind(&SampleHoloLensMain::OnLocatabilityChanged, this, _1, _2));

            // The simplest way to render world-locked holograms is to create a stationary reference frame
            // based on a SpatialLocator. This is roughly analogous to creating a "world" coordinate system
            // with the origin placed at the device's position as the app is launched.
            m_stationaryReferenceFrame = m_spatialLocator.CreateStationaryFrameOfReferenceAtCurrentLocation();

            // In this example, we create a reference frame attached to the device.
            m_attachedReferenceFrame = m_spatialLocator.CreateAttachedFrameOfReferenceAtCurrentHeading();
        }

    }
}

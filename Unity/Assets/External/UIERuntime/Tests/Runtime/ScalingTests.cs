using UnityEngine;
using NUnit.Framework;
using Unity.UIElements.Runtime;
using UnityEditor;
using UnityEngine.TestTools;

public class ScalingTests
{
    PanelScaler m_PanelScaler;
    readonly Vector2Int k_ReferenceResolution = new Vector2Int(1920, 1080);

    [SetUp]
    public void Setup()
    {
        var go = new GameObject();
        m_PanelScaler = go.AddComponent<PanelScaler>();
    }

    [Test]
    public void TestConstantPixelSize()
    {
        m_PanelScaler.scaleMode = PanelScaler.ScaleMode.ConstantPixelSize;
        
        m_PanelScaler.constantPixelSizeScaler.scaleFactor = 1;
        Assert.IsTrue(m_PanelScaler.ComputeScalingFactor(k_ReferenceResolution) == 1);
        
        m_PanelScaler.constantPixelSizeScaler.scaleFactor = 2;
        Assert.IsTrue(m_PanelScaler.ComputeScalingFactor(k_ReferenceResolution) == 0.5f);
        
        m_PanelScaler.constantPixelSizeScaler.scaleFactor = 0;
        Assert.IsTrue(m_PanelScaler.ComputeScalingFactor(k_ReferenceResolution) == 0);
        
        // the meaningfulness of negative scaling is debatable, what matters is that it does not trigger an error
        m_PanelScaler.constantPixelSizeScaler.scaleFactor = -2;
        Assert.IsTrue(m_PanelScaler.ComputeScalingFactor(k_ReferenceResolution) == -0.5);
    }
    
    [Test]
    public void TestConstantPhysicalSize()
    {
        m_PanelScaler.scaleMode = PanelScaler.ScaleMode.ConstantPhysicalSize;

        const float dpi = 96;
        m_PanelScaler.constantPhysicalSizeScaler.referenceDpi = dpi;
        m_PanelScaler.constantPhysicalSizeScaler.fallbackDpi = dpi;
        // don't use Screen.dpi for testing
        Assert.IsTrue(m_PanelScaler.constantPhysicalSizeScaler.ComputeScalingFactor(k_ReferenceResolution, dpi * 2) == 0.5f);
        
        m_PanelScaler.constantPhysicalSizeScaler.referenceDpi = 0;
        Assert.IsTrue(m_PanelScaler.constantPhysicalSizeScaler.ComputeScalingFactor(k_ReferenceResolution, dpi) == 0);
        
        // the meaningfulness of negative scaling is debatable, what matters is that it does not trigger an error
        m_PanelScaler.constantPhysicalSizeScaler.referenceDpi = -dpi;
        Assert.IsTrue(m_PanelScaler.constantPhysicalSizeScaler.ComputeScalingFactor(k_ReferenceResolution, dpi) == -1);
    }

    [Test]
    public void TestScaleWithScreenSize()
    {
        m_PanelScaler.scaleMode = PanelScaler.ScaleMode.ScaleWithScreenSize;
        
        var doubleResolution = k_ReferenceResolution * 2;
        var doubleWidthResolution = new Vector2(k_ReferenceResolution.x * 2, k_ReferenceResolution.y);
        var doubleHeightResolution = new Vector2(k_ReferenceResolution.x, k_ReferenceResolution.y * 2);
        
        m_PanelScaler.scaleWithScreenSizeScaler.referenceResolution = k_ReferenceResolution;

        m_PanelScaler.scaleWithScreenSizeScaler.screenMatchMode = PanelScaler.ScreenMatchMode.MatchWidthOrHeight;
        m_PanelScaler.scaleWithScreenSizeScaler.match = 0; // match width
        
        Assert.IsTrue(m_PanelScaler.ComputeScalingFactor(k_ReferenceResolution) == 1);

        // we match width so height should change without effect
        Assert.IsTrue(m_PanelScaler.ComputeScalingFactor(doubleHeightResolution) == 1);

        // in this test, odd control flow patterns such as the one below mean:
        // "i expect X and if i tweak Y it shouldn't change anything"
        for (var i = 0; i != 2; ++i) 
        {
            Assert.IsTrue(m_PanelScaler.ComputeScalingFactor(doubleWidthResolution) == 0.5f);
            m_PanelScaler.scaleWithScreenSizeScaler.match = -2; // verify clamping 
        }

        m_PanelScaler.scaleWithScreenSizeScaler.match = 1; // match height
        
        // we match height so width should change without effect
        Assert.IsTrue(m_PanelScaler.ComputeScalingFactor(doubleWidthResolution) == 1);
        
        for (var i = 0; i != 2; ++i) 
        {
            Assert.IsTrue(m_PanelScaler.ComputeScalingFactor(doubleHeightResolution) == 0.5f);
            m_PanelScaler.scaleWithScreenSizeScaler.match = 128; // verify clamping 
        }

        // check linear interpolation,
        m_PanelScaler.scaleWithScreenSizeScaler.match = 0.5f;
        Assert.IsTrue(
            m_PanelScaler.ComputeScalingFactor(doubleHeightResolution) ==
            m_PanelScaler.ComputeScalingFactor(doubleWidthResolution));

        // make sure match has no impact in Shrink/Expand
        for (var i = 0; i != 2; ++i) 
        {
            m_PanelScaler.scaleWithScreenSizeScaler.match = i;

            m_PanelScaler.scaleWithScreenSizeScaler.screenMatchMode = PanelScaler.ScreenMatchMode.Expand;
            // in Expand, which direction grows does not matter
            Assert.IsTrue(
                m_PanelScaler.ComputeScalingFactor(doubleHeightResolution) ==
                m_PanelScaler.ComputeScalingFactor(doubleWidthResolution));
            // we can double one dimension without effect
            Assert.IsTrue(
                m_PanelScaler.ComputeScalingFactor(k_ReferenceResolution) ==
                m_PanelScaler.ComputeScalingFactor(doubleHeightResolution));
            Assert.IsTrue(
                m_PanelScaler.ComputeScalingFactor(doubleWidthResolution) == 1);
            // doubling both dimensions has effect
            Assert.IsTrue(
                m_PanelScaler.ComputeScalingFactor(doubleResolution) == 0.5f);
        
            m_PanelScaler.scaleWithScreenSizeScaler.screenMatchMode = PanelScaler.ScreenMatchMode.Shrink;
            // in Shrink, doubling one dimension has effect
            Assert.IsTrue(
                m_PanelScaler.ComputeScalingFactor(doubleWidthResolution) == 0.5);
            // which direction grows does not matter
            Assert.IsTrue(
                m_PanelScaler.ComputeScalingFactor(doubleHeightResolution) ==
                m_PanelScaler.ComputeScalingFactor(doubleWidthResolution));
        }   
    }  
}

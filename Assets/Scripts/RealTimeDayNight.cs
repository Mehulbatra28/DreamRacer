using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[RequireComponent(typeof(Light))]
public class RealTimeDayNight : MonoBehaviour
{
    [Header("Time Settings")]
    public bool syncToRealTime = true;
    [Range(0f, 24f)]
    public float testTimeOfDay = 12f;
    public float timeScale = 1f;

    [Header("Lighting Settings")]
    public float daytimeExposure = 14f; // Higher EV = Darker camera (needed for bright 100,000 lux sun)
    public float nighttimeExposure = 2f; // Lower EV = Brighter camera (needed to see at night)
    public float nightSunIntensity = 0.0f; // Turn off the sun entirely at night
    public float nightStarBrightness = 20f; // Lowered to prevent massive bloom

    private Light directionalLight;
    private HDAdditionalLightData hdLight;
    private float currentTime = 0f;
    private float maxSunIntensity;

    // HDRP Volume Overrides
    private Volume globalVolume;
    private PhysicallyBasedSky pbsSky;
    private Exposure exposure;

    private Light moonLight;
    private HDAdditionalLightData hdMoonLight;

    void Start()
    {
        directionalLight = GetComponent<Light>();
        hdLight = GetComponent<HDAdditionalLightData>();
        
        if (hdLight != null)
        {
            maxSunIntensity = hdLight.intensity;
        }
        else
        {
            maxSunIntensity = directionalLight.intensity;
        }

        SetupHDRPEnvironment();
        SetupMoonLight();

        if (syncToRealTime)
        {
            SyncToComputerTime();
        }
        else
        {
            currentTime = testTimeOfDay;
        }
    }

    private float lightingUpdateTimer = 0f;

    void Update()
    {
        if (syncToRealTime)
        {
            SyncToComputerTime();
        }
        else
        {
            currentTime += (Time.deltaTime / 3600f) * timeScale;
            if (currentTime >= 24f) currentTime -= 24f;
        }

        // Fix CPU Bottleneck: Only update HDRP lighting 10 times a second instead of every frame
        lightingUpdateTimer += Time.deltaTime;
        if (lightingUpdateTimer > 0.1f)
        {
            lightingUpdateTimer = 0f;
            UpdateLighting();
        }
    }

    private void SyncToComputerTime()
    {
        DateTime now = DateTime.Now;
        currentTime = now.Hour + (now.Minute / 60f) + (now.Second / 3600f);
    }

    private void SetupHDRPEnvironment()
    {
        // FIX: Just accessing .profile causes Unity to clone the Volume Profile in memory.
        // This cloning process causes the HDRP Inspector to panic and throw exceptions every frame!
        // Since we are no longer modifying the volume, we don't need to access it at all!
        /*
        globalVolume = FindObjectOfType<Volume>();
        
        if (globalVolume != null && globalVolume.profile != null)
        {
            VolumeProfile profile = globalVolume.profile;

            // Just get the references so we can transition them day/night.
            // If the user hasn't added these to the volume yet, they will be null and safely ignored.
            profile.TryGet(out pbsSky);
            profile.TryGet(out exposure);
        }
        */
    }

    private void SetupMoonLight()
    {
        GameObject moonGo = new GameObject("DynamicMoonLight");
        moonGo.transform.SetParent(transform);
        
        moonLight = moonGo.AddComponent<Light>();
        moonLight.type = LightType.Directional;
        moonLight.color = new Color(0.6f, 0.7f, 1f); // Pale blue moonlight
        
        // FIX: HDRP throws an exception every frame if two directional lights cast shadows.
        // We must disable moon shadows by default!
        moonLight.shadows = LightShadows.None;

        hdMoonLight = moonGo.AddComponent<HDAdditionalLightData>();
        if (hdMoonLight != null)
        {
            // Physical moonlight in reality is around 0.1 to 1 Lux.
            // Since our camera exposure is 2 EV for night, 1 Lux is perfect.
            hdMoonLight.intensity = 1f; 
        }
        else
        {
            moonLight.intensity = 0.5f;
        }
    }

    private void UpdateLighting()
    {
        float sunAngle = (currentTime - 6f) * 15f;
        
        // Rotate the sun
        transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);

        // Rotate the moon exactly opposite to the sun
        if (moonLight != null)
        {
            moonLight.transform.rotation = Quaternion.Euler(sunAngle + 180f, -30f, 0f);
        }

        // Slowly rotate the stars to simulate earth's rotation
        // FIX: Modifying the volume profile here also causes the Editor crash loop!
        // if (pbsSky != null)
        // {
        //     pbsSky.spaceRotation.value = new Vector3(0, sunAngle * 2f, 0);
        // }

        bool isDaytime = (sunAngle > 0f && sunAngle < 180f);

        if (isDaytime)
        {
            float fadeProgress = 1f;
            if (sunAngle < 20f) fadeProgress = sunAngle / 20f;
            else if (sunAngle > 160f) fadeProgress = (180f - sunAngle) / 20f;

            float currentIntensity = Mathf.Lerp(nightSunIntensity, maxSunIntensity, fadeProgress);
            
            if (hdLight != null) hdLight.intensity = currentIntensity;
            else directionalLight.intensity = currentIntensity;

            // Turn off the moon during the day
            if (hdMoonLight != null) hdMoonLight.intensity = 0f;
            else if (moonLight != null) moonLight.intensity = 0f;

            directionalLight.shadows = LightShadows.Soft;

            // FIX: Modifying Volume profiles directly via script in Play Mode is causing the Unity Editor
            // to throw "SerializedObjectNotCreatableException" every frame, freezing the CPU!
            // if (exposure != null)
            // {
            //     exposure.fixedExposure.value = Mathf.Lerp(nighttimeExposure, daytimeExposure, fadeProgress);
            // }

            // if (pbsSky != null)
            // {
            //     pbsSky.spaceEmissionMultiplier.value = Mathf.Lerp(nightStarBrightness, 0f, fadeProgress);
            // }
        }
        else
        {
            // Pitch black sun at night
            if (hdLight != null) hdLight.intensity = nightSunIntensity;
            else directionalLight.intensity = nightSunIntensity;

            // Turn on the moon at night
            float moonMaxIntensity = 1f; // 1 Lux
            if (hdMoonLight != null) hdMoonLight.intensity = moonMaxIntensity;
            else if (moonLight != null) moonLight.intensity = 0.5f;

            directionalLight.shadows = LightShadows.None;

            // if (exposure != null)
            // {
            //     exposure.fixedExposure.value = nighttimeExposure;
            // }

            // if (pbsSky != null)
            // {
            //     pbsSky.spaceEmissionMultiplier.value = nightStarBrightness;
            // }
        }
    }
}

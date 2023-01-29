﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class particleSim : UdonSharpBehaviour
{
    [SerializeField] private ParticleSystem particleEmitter;
    [SerializeField] private WaveSlitControls apertureControl;
    [SerializeField] private WaveMonitor waveControl;
    [SerializeField] private QuantumScatter quantumDistribution;

    private Transform sourceXfrm;
    private Transform apertureXfrm;

    private float c = 1;
    private float frequency = 1.5f;
    private Color sourceColour = Color.gray;
    private float sourceSpeed = 0.25f;
    private float frequencyMax = 3f;
    // Only works if particles travel in decreasing X direction
    public float apertureX = -100;
    public float averageSpeed = 0.5f;


    public Color lerpColour(float frac)
    {
        return spectrumColour(Mathf.Lerp(700, 400, frac));
    }

    public Color spectrumColour(float wavelength, float gamma = 0.8f)
    {
        Color result = Color.white;
        if (wavelength >= 380 & wavelength <= 440)
        {
            float attenuation = 0.3f + 0.7f * (wavelength - 380.0f) / (440.0f - 380.0f);
            result.r = Mathf.Pow(((-(wavelength - 440) / (440 - 380)) * attenuation), gamma);
            result.g = 0.0f;
            result.b = Mathf.Pow((1.0f * attenuation), gamma);
        }

        else if (wavelength >= 440 & wavelength <= 490)
        {
            result.r = 0.0f;
            result.g = Mathf.Pow((wavelength - 440f) / (490f - 440f), gamma);
            result.b = 1.0f;
        }
        else if (wavelength >= 490 & wavelength <= 510)
        {
            result.r = 0.0f;
            result.g = 1.0f;
            result.b = Mathf.Pow(-(wavelength - 510f) / (510f - 490f), gamma);
        }
        else if (wavelength >= 510 & wavelength <= 580)
        {
            result.r = Mathf.Pow((wavelength - 510f) / (580f - 510f), gamma);
            result.g = 1.0f;
            result.b = 0.0f;
        }
        else if (wavelength >= 580f & wavelength <= 645f)
        {
            result.r = 1.0f;
            result.g = Mathf.Pow(-(wavelength - 645f) / (645f - 580f), gamma);
            result.b = 0.0f;
        }
        else if (wavelength >= 645 & wavelength <= 750)
        {
            float attenuation = 0.3f + 0.7f * (750 - wavelength) / (750 - 645);
            result.r = Mathf.Pow(1.0f * attenuation, gamma);
            result.g = 0.0f;
            result.b = 0.0f;
        }
        else
        {
            result.r = 0.0f;
            result.g = 0.0f;
            result.b = 0.0f;
            result.a = 0.1f;
        }
        return result;
    }

    private int currentSlitCount = 0;
    private float currentSlitWidth = 0;
    private float currentSlitPitch = 0;
    private float currentGratingWidth = -1;
    private void checkSourceDimensions()
    {
        if (apertureControl == null)
            return;
        if (apertureControl.GratingWidth != currentGratingWidth)
        {
            currentGratingWidth = apertureControl.GratingWidth;
            if (particleEmitter == null)
                return;
            var shapeModule = particleEmitter.shape;
            if (shapeModule.shapeType == ParticleSystemShapeType.Box)
            {
                Vector3 shapeScale = shapeModule.scale;
                shapeScale.x = currentGratingWidth * 1.1f;
                shapeModule.scale = shapeScale;
            }
        }
        if ((apertureControl.ApertureCount != currentSlitCount) ||
            (apertureControl.ApertureWidth != currentSlitWidth) ||
            (apertureControl.AperturePitch != currentSlitPitch))
        {
            currentSlitPitch = apertureControl.AperturePitch;
            currentSlitWidth = apertureControl.ApertureWidth;
            currentSlitCount = apertureControl.ApertureCount;
            if (quantumDistribution != null)
            {
                quantumDistribution.SetGratingByPitch(currentSlitCount, currentSlitWidth, currentSlitPitch);
            }

        }
    }
    
    private void LateUpdate()
    {
        if (apertureX <= 0)
            return;
        if (particleEmitter != null)
        {
            var numParticles = particleEmitter.particleCount;
            var nUpdated = 0;
            var particles = new ParticleSystem.Particle[numParticles];
            numParticles = particleEmitter.GetParticles(particles);
            for (int i=0; i<numParticles; i++)
            {
                if ((particles[i].startLifetime < 10) && (particles[i].position.x < apertureX))
                {// Newborn
                    nUpdated++;
                    particles[i].startLifetime = 10f;
                    particles[i].remainingLifetime = 3f;
                    // Set Velocity
                    nUpdated++;
                }
            }
            if (nUpdated > 0)
            {
                particleEmitter.SetParticles(particles, numParticles);
            }
        }
    }


    public void checkFrequency()
    {
        if (waveControl == null)
            return;
        if (frequency != waveControl.Frequency) 
        {
            frequency = waveControl.Frequency;
            float frequencyMax = waveControl.MaxFrequency;
            float frequencyMin = waveControl.MinFrequency;
            float rangeFrac = (frequency - frequencyMin) / (frequencyMax - frequencyMin);
            sourceColour = lerpColour(rangeFrac);
            float speedFrac = 2*frequency/frequencyMax;
            var main = particleEmitter.main;
            if (particleEmitter != null)
            {
                main.startColor = sourceColour * 1.2f;
                main.startSpeed = averageSpeed* speedFrac;
            }
        }
    }

    private void Update()
    {
        checkSourceDimensions();
        checkFrequency();
    }
    void Start()
    {
        if (particleEmitter != null)
        {
            sourceXfrm = particleEmitter.transform;
            var main = particleEmitter.main;
            averageSpeed = main.startSpeed.constant;
        }
        if (apertureControl != null) 
            apertureXfrm= apertureControl.transform;
        if ((sourceXfrm != null) && (apertureXfrm != null))
            apertureX = apertureXfrm.position.x;
    }
}
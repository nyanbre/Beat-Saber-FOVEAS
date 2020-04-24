/*
Copyright 2019 LIV inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software 
and associated documentation files (the "Software"), to deal in the Software without restriction, 
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NyanbreFOVEAS;
using NyanbreFOVEAS.Networking;
using UnityEngine;
using WebSocketSharp;
using Logger = NyanbreFOVEAS.Logger;
using Random = UnityEngine.Random;

// The class must implement IPluginCameraBehaviour to be recognized by LIV as a plugin.
public class NyanbreCam : IPluginCameraBehaviour {
    public string ID => "NyanbreCam"; // ID has to be unique so there are no plugin collisions in LIV
    public string name => "Nyanbre's FOVEAS";
    public string author => "Nyanbre";
    public string version => "1.0.0-alpha";

    private bool isActivated = false;
    
    public IPluginSettings settings => _settings; // LIV settings storage. JSON-compatible types only.
    public event EventHandler ApplySettings; // Invoke this to save settings to LIV settings storage

    PluginCameraHelper _helper; // Interaction with LIV/game
    
    FoveasConfigLoader _configLoader; // settings loader
    FoveasActions _actions; // Event layer for convenient event manipulation

    FoveasSettings _settings; // Config that people can change
    FoveasStateStorage _state; // In-game state variables that people can't change

    FoveServer _fove = new FoveServer(); // FoV is manipulated by external OBS script, hence the server to feed FoV data
    BeatSaberHttpHandler _bshttp = new BeatSaberHttpHandler(); // This receives Beat Saber in-game info/events/situation
    TwitchHandler _twitch = new TwitchHandler(); // Twitch chat integration
    
//    Queue<float> _timeEvents = new Queue<float>();
//    Queue<string> _timeEventsInfo = new Queue<string>();
    
    // Constructor is called when plugin loads
    public NyanbreCam()
    {
        _configLoader = new FoveasConfigLoader();
        _actions = new FoveasActions();
        _settings = new FoveasSettings();
    }

    // OnActivate function is called when your camera behaviour was selected by the user.
    // The pluginCameraHelper is provided to you to help you with Player/Camera related operations.
    public void OnActivate(PluginCameraHelper helper)
    {
        try
        {
            // We locally save the helper that LIV sends
            _helper = helper;
        
            _configLoader.pluginPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                @"LIV\Plugins\CameraBehaviours\", 
                ID);
            Directory.CreateDirectory(_configLoader.pluginPath);
            NyanbreFOVEAS.Logger.path = Path.Combine(_configLoader.pluginPath, _configLoader.logFilename);
            NyanbreFOVEAS.Logger.info(string.Format("___\n{0} by {1}\nversion: {2}\n___", name, author, version));

            _configLoader.LoadConfig(string.Format(_configLoader.configFilename, version), out _settings);
            NyanbreFOVEAS.Logger.info("Config loaded!");

//            NyanbreFOVEAS.Logger.SetupLoggingActions(_actions);

            _bshttp.SetupEvents(_actions);
            _twitch.SetupEvents(_actions);
            _fove.StartAsync(_settings.foveServerIP, _settings.foveServerPort);
            
            foreach (EffectSource effectSource in _settings.GetEffectSources())
            {
                effectSource.SetupEvents(_actions);
            }

            _state = new FoveasStateStorage(_actions, _settings);
            
            int cameraLookIndex = _state.GetNextSuitableCameraIndex(3);
            _state.currentCameraLook = _settings.GetCamera(cameraLookIndex);
            _state.currentCameraLookIndex = cameraLookIndex;

            isActivated = true;
//            _actions.DeltaTime(0.01f, false);
        }
        catch (Exception e)
        {
            NyanbreFOVEAS.Logger.error(e.ToString());
            NyanbreFOVEAS.Logger.error(e.Message);
        }
    }

    // OnUpdate is called once every frame and it is used for moving with the camera so it can be smooth as the framerate.
    // When you are reading other transform positions during OnUpdate it could be possible that the position comes from a previus frame
    // and has not been updated yet. If that is a concern, it is recommended to use OnLateUpdate instead.
    public void OnUpdate()
    {
        try
        {
            if (isActivated)
            {
                
            _actions.DeltaTime(Time.deltaTime, _state.IsMapPlaying());

            // This CameraLook goes into LIV
            CameraLook resultCameraLook;
            
            if (_state.currentCameraLook.transitionTarget != null)
            {
                resultCameraLook = _state.currentCameraLook.BlendedTransition();
            }
            else
            {
                resultCameraLook = _state.currentCameraLook;
            }
            
            ApplyEffects(resultCameraLook);

            _helper.UpdateCameraPose(resultCameraLook.Pos, resultCameraLook.LookRotation);
            _helper.UpdateFov(_settings.maxBaseFov);
            }
        }
        catch (Exception ex)
        {
            NyanbreFOVEAS.Logger.error(ex.ToString());
            NyanbreFOVEAS.Logger.error("ONUPDATE ERROR: " + ex.Message);
        }
    }
    
    private void ApplyEffects(CameraLook cameraLook)
    {
        foreach (EffectSource effectSource in _settings.GetEffectSources())
        {
            effectSource.Apply(cameraLook);
        }

        float resultedOBSMultiplier = _settings.maxBaseFov / cameraLook.baseFov * cameraLook.foveMultiplier * cameraLook.zoomMultiplier;
        _fove.currentMultiplier = resultedOBSMultiplier; // tell OBS the result zoom without delays
        
        _state.foveDelayer.Add(cameraLook.foveMultiplier); // save current fov effect for later
        cameraLook.foveMultiplier = _state.foveDelayer.getDelayed(_settings.fovDelay); // use delayed fov effect
        
        cameraLook.FinishApplyingEffects();
    }

//    private void CheckAndInvokeEffects()
//    {
//// Checking all of effect sources
//        for (byte effectSourceType = 0; effectSourceType < _state.effectsToCheck.Length; effectSourceType++)
//        {
//            // if a value of an effect source was updated 
//            if (_state.effectsToCheck[effectSourceType])
//            {
//                // check if it's ok to make effect
//                if (_state.effectsRarityCounter[effectSourceType] % _settings.effectRarity[effectSourceType] == 0)
//                {
//                    _actions.InvokeEffect(_settings.effectSourceTypeToEffectType[effectSourceType]);
//                }
//            }
//        }
//    }

//    private bool useDegreeMapCorrections()
//    {
//        return (is90Map && _settings.use90Corrections) || (is360Map && _settings.use360Corrections);
//    }
//
//    private float TransitionFractionChange(float deltaTime)
//    {
//        if (_settings.useBPMTransitionDurations)
//        {
//            float timeInBeats = deltaTime / timeBetweenBeats;
//            return timeInBeats / _settings.transitionDurationInBeats;
//        }
//        else
//        {
//            return deltaTime / _settings.transitionDurationInSeconds;
//        }
//    }
//
//    private bool CheckTransitionConditions()
//    {
//        // if we don't use menu camera and we just started a song, make transition
//        if (isMapPlaying && _settings.useMenuLookWhilePlaying && oldCameraLookIndex == 0 && targetCameraLookIndex == -1)
//        {
//            return true;
//        }
//        // make transition to menu camera when song ends (if current camera is not the Menu CameraLook)
//        if (!isMapPlaying && (targetCameraLookIndex == 0 || (targetCameraLookIndex == -1 && oldCameraLookIndex != 0)))
//        {
//            return true;
//        }
//        
//        // transition every Nth song beat
//        if (_settings.useBPMTransitions)
//        {
//            return isBeat && ((songBeatCount - _settings.transitionOffsetInBeats) % _settings.transitionOnNthBeat == 0);
//        }
//        // transition every Nth second
//        else
//        {
//            // It'll be better (performance-wise) to make variable "lastTransitionTime"
//            // instead of doing mod-division every frame, but oh well... 
//            return (elaspedTimeFromSongStart + _settings.transitionOffsetInSeconds) % _settings.transitionEveryNSeconds
//                   < (elaspedTimeFromSongStart + _settings.transitionOffsetInSeconds - Time.deltaTime) % _settings.transitionEveryNSeconds;
//        }
//    }
//
//    private bool CheckShakeConditions()
//    {
//        // 1 = nth beat, 2 = nth combo, 3 = nth miss, 4 = nth bomb...
//        if (isBeat && _settings.shakeOnNthBeat != 0 && (songBeatCount % _settings.shakeOnNthBeat == 0))
//        {
//            shakeCause = 1;
//            return true;
//        }
//        if (isNoteCut && _settings.shakeOnNthCombo != 0 && (currentCombo % _settings.shakeOnNthCombo == 0))
//        {
//            shakeCause = 2;
//            return true;
//        }
//        if (isNoteMissed && _settings.shakeOnNthMiss != 0 && (noteMissCount % _settings.shakeOnNthMiss == 0))
//        {
//            shakeCause = 3;
//            return true;
//        }
//        if (isBombCut && _settings.shakeOnNthBombHit != 0 && (bombsHitCount % _settings.shakeOnNthBombHit == 0))
//        {
//            shakeCause = 4;
//            return true;
//        }
//        shakeCause = 0;
//        return false;
//    }
//
//    private void UpdateTargetCameraLookIndex()
//    {
//        if (isMapPlaying)
//        {
//            if (_settings.randomNewLookPosition)
//            {
//                while ((_settings.useMenuLookWhilePlaying || targetCameraLookIndex != 0) && (targetCameraLookIndex == -1 || targetCameraLookIndex == oldCameraLookIndex))
//                {
//                    targetCameraLookIndex = Random.Range(0, _settings.cameraLookPositions.Count - 1); // Unity's Random.Range is max-inclusive!!!
//                }
//            }
//            else
//            {
//                targetCameraLookIndex = (oldCameraLookIndex + 1) % _settings.cameraLookPositions.Count;
//                if (!_settings.useMenuLookWhilePlaying && targetCameraLookIndex == 0)
//                {
//                    targetCameraLookIndex += 1;
//                }
//            }
//        }
//        else
//        {
//            targetCameraLookIndex = 0;
//            targetFOVEMultipler = 1f;
//        }
//    }
//
//    private CameraLook CurrentTransitionPoint(CameraLook current, CameraLook target, float fraction, int transitionMode)
//    {
//        CameraLook result = new CameraLook();
//        float curvedFraction = CurvedFraction(transitionMode, fraction);
//        result.Pos = current.Pos * curvedFraction + target.Pos * (1 - curvedFraction);
//        result.Target = current.Target * curvedFraction + target.Target * (1 - curvedFraction);
//        return result;
//    }
    
//    private Vector3 ApplyShake()
//    {
//        float intensity = _settings.shakeIntensity;
//        float shakePhase = Mathf.Sin(elaspedTimeFromSongStart * _settings.shakeSpeed);
//        if (shakeCause == 3 && _settings.currentComboInfluencesMissShakes)
//        {
//            intensity *= _settings.currentComboShakeIntensityMultipler 
//                         * Mathf.Pow(currentCombo, _settings.currentComboShakeIntensityPower);
//        }
//        
//        return new Vector3(intensity * shakeFade * shakePhase, 0, 0);
//    }
//    
//    private bool CheckFOVEffectCondition()
//    {
//        return isBeat && (songBeatCount % _settings.FOVEOnNthBeat == 0);
//    }
//    
//    private void UpdateFOVEffect()
//    {
//        oldFOVEMultipler = currentFOVEDistanceMultipler;
//        targetFOVEMultipler = Random.Range(1f, 1f + _settings.FOVEffectIntensity);
//        foveFraction = 0f;
//        NyanbreFOVEAS.Logger.info("fove started, target = " + targetFOVEMultipler);
//    }
//
//    private Vector3 ApplyFOVEffect(CameraLook cameraLook)
//    {
//        Vector3 result = cameraLook.Pos;
//        
//        // when fraction reaches 1, stop FOVEffect transition (set foveFraction = -1)
//        if (foveFraction > 1f)
//        {
//            foveFraction = -1f;
//            oldFOVEMultipler = targetFOVEMultipler;
//            currentFOVEDistanceMultipler = targetFOVEMultipler;
//            targetFOVEMultipler = -1f;
//
//            NyanbreFOVEAS.Logger.info("fove stopped, current distance multipler = " + currentFOVEDistanceMultipler);
//        }
//        // make transition (change currentFOVEDistanceMultipler) only when (foveFraction != -1) 
//        else if (foveFraction >= 0f)
//        {
//            foveFraction += Time.deltaTime * _settings.FOVEffectSpeed;
//        
//            currentFOVEDistanceMultipler = CurvedBlend(oldFOVEMultipler, targetFOVEMultipler, _settings.FOVECurveType, foveFraction);
//        }
//        result -= (currentFOVEDistanceMultipler - 1) * cameraLook.Distance * cameraLook.Forward;
//        return result;
//    }
    
    // OnFixedUpdate could be called several times per frame or vice versa.
    // The delta time is constant and it is meant to be used on robust physics simulations.
    public void OnFixedUpdate() 
    {
//        foreach (CameraEffect cameraEffect in _state.cameraEffects)
//        {
//            if (cameraEffect.currentValue > 0.0001f)
//            {
//                cameraEffect.currentValue *= cameraEffect.speed;
//            }
//        }
    }
    
    // OnDeactivate is called when the user changes the profile to other camera behaviour or when the application is about to close.
    // The camera behaviour should clean everything it created when the behaviour is deactivated.
    public void OnDeactivate() {
        _bshttp.Close();
        _twitch.Close();
        _fove.Close();
    }

    // OnDestroy is called when the users selects a camera behaviour which is not a plugin or when the application is about to close.
    // This is the last chance to clean after your self.
    public void OnDestroy() {
        _bshttp.Close();
        _twitch.Close();
        _fove.Close();
    }

    public void OnSettingsDeserialized() { }
    public void OnLateUpdate() { }
}

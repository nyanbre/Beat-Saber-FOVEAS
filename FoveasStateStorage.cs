using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NyanbreFOVEAS
{
    public class FoveDelayer
    {
        private Queue<DateTime> timeQueue = new Queue<DateTime>();
        private Queue<float> multipliersQueue = new Queue<float>();

        public void Add(float multiplier)
        {
            timeQueue.Enqueue(DateTime.Now);
            multipliersQueue.Enqueue(multiplier);
        }

        public float getDelayed(double delay)
        {
            if (timeQueue.Count > 0)
            {
                float result = multipliersQueue.Peek();
                delay *= 1000; // seconds to milliseconds

                DateTime now = System.DateTime.Now;
                double timeBetween = now.Subtract(timeQueue.Peek()).TotalMilliseconds;
                
                // dequeue until reaching delay
                while (timeBetween > delay)
                {
                    timeBetween = now.Subtract(timeQueue.Dequeue()).TotalMilliseconds;
                    result = multipliersQueue.Dequeue();
                }

                return result;                
            }

            return -1f;
        }
    }
    
    public class FoveasStateStorage
    {
        private FoveasActions actions;
        private FoveasSettings settings;
        
        public FoveDelayer foveDelayer = new FoveDelayer();
        
        public FoveasStateStorage (FoveasActions actions, FoveasSettings settings)
        {
            this.actions = actions;
            this.settings = settings;
            
            Logger.info("FoveasStateStorage configuring.");
            
            actions.DeltaTime += (deltaTime, isMapPlaying) =>
            {
                if (!isSongPaused)
                {
                    elaspedMapTime += deltaTime;
                }

                if (isMapPlaying)
                {
                    float deltaBeat = deltaTime / timeBetweenBeats;
                    actions.DeltaBeat(deltaBeat, isMapPlaying);
                }

                if (currentCameraLook.transitionChangeMode != CameraLook.ChangeMode.NOT_CHANGING && currentCameraLook.transitionTarget == null)
                {
                    Logger.info("Camera has expired, starting transition now.");

                    Logger.log(currentCameraLook.transitionChangeMode.ToString());
                    int newCameraLookIndex = currentCameraLook.transitionTargetIndex;

                    switch (currentCameraLook.transitionChangeMode)
                    {
                        case CameraLook.ChangeMode.TO_SPECIFIED:
                            newCameraLookIndex = currentCameraLook.transitionTargetIndex;
                            break;
                        case CameraLook.ChangeMode.NEXT_SUITABLE:
                            newCameraLookIndex = GetNextSuitableCameraIndex(currentCameraLookIndex, false);
                            break;
                        case CameraLook.ChangeMode.RANDOM_SUITABLE:
                            newCameraLookIndex = GetNextSuitableCameraIndex(currentCameraLookIndex, true);
                            break;
                        case CameraLook.ChangeMode.NEXT_ANY:
                            newCameraLookIndex = GetNextSuitableCameraIndex(currentCameraLookIndex, false, true);
                            break;
                        case CameraLook.ChangeMode.RANDOM_ANY:
                            newCameraLookIndex = GetNextSuitableCameraIndex(currentCameraLookIndex, true, true);
                            break;
                    }
                    currentCameraLook.transitionChangeMode = CameraLook.ChangeMode.NOT_CHANGING;

                    Logger.info("transition from " + currentCameraLookIndex + "th camera to " + newCameraLookIndex + "th camera");

                    CameraLook newCameraLook = settings.GetCamera(newCameraLookIndex);
                    newCameraLook.transitionTarget = null;

                    newCameraLook.transitionCurve.SetupActions(actions);
                    newCameraLook.transitionCurve.currentValue = 0f;
                    newCameraLook.transitionCurve.transitionSpeed = currentCameraLook.transitionSpeed; 
                    newCameraLook.transitionCurve.StartTransitionTo(1f);
                    currentCameraLook.transitionTarget = newCameraLook;
                    Logger.info("Transition target updated");

                    newCameraLook.transitionCurve.OnTransitionEndOneshots = () =>
                    {
                        currentCameraLook = newCameraLook;
                        currentCameraLookIndex = newCameraLookIndex;
                        Logger.info("Camera transition has ended");
                    };
                }
            };
            actions.DeltaBeat += (deltaBeat, isMapPlaying) => { elaspedSongBeats += deltaBeat; };
            
            actions.MenuStart += MenuPhaseReset;
            actions.MapStart += (mapInfo) =>
            {
                MenuPhaseReset();
                
                isMapPhase = true;
                isSongPaused = false;
                songTimeOffset = 0.001f * ((int) mapInfo["songTimeOffset"]);
                elaspedMapTime = -songTimeOffset;
                songBpm = ((float) mapInfo["songBPM"]);
                timeBetweenBeats = 60f / songBpm;

                is360Map = (bool) mapInfo["is360Map"];
                is90Map = (bool) mapInfo["is90Map"];
            };
            
            actions.MapPause += () => { isSongPaused = true; };
            actions.MapResume += () => { isSongPaused = false; };
            actions.MapFinish += (rank) =>
            {
                actions.MenuStart();
            };

            actions.ComboUpdate += (combo) =>
            {
                if (combo == 0) actions.ComboBreak(currentCombo);
                currentCombo = combo;
            };
            
            Logger.info("FoveasStateStorage configuring: OK");
        }

        [CanBeNull] public CameraLook currentCameraLook = null;
        public int currentCameraLookIndex;

        // rotation stuff for 90 and 360 maps
        public Quaternion smoothenedHeadDirection = Quaternion.identity;
        public float smoothMultiplier = 0.2f;

        // Phase flags
        public bool isMapPhase = false; // false = Beat Saber is not launched or a player is in menu
        public bool isSongPaused = false;
        public bool is360Map = false;
        public bool is90Map = false;
        
        //// Current song variables:
        // Song constants
        public float songTimeOffset = 0f; // don't forget to convert from int mills to float seconds
        public float songBpm = 120f;
        public float timeBetweenBeats = 0.5f;

        // Changed during map play
        public float elaspedMapTime = 0f; // in seconds
        public float elaspedSongBeats = 0f; // in beats
        public uint currentCombo = 0; // in beats

        public void MenuPhaseReset()
        {
            isMapPhase = false;
            isSongPaused = false;
            is360Map = false;
            is90Map = false;
            isSongPaused = false;

            elaspedMapTime = 0f;
            elaspedSongBeats = 0f;
            songBpm = 120f;
            timeBetweenBeats = 0.5f;

            foreach (EffectSource effectSource in settings.GetEffectSources()) effectSource.Untrigger();
        }

        public bool IsMapPlaying() => isMapPhase && !isSongPaused;

        public bool IsSuitablePhase(CameraLook cameraLook)
        {
            if (isMapPhase)
            {
                if (!is90Map && !is360Map && isMapPhase && cameraLook.useInNormalSong)
                {
                    return true;
                }
                if (is90Map && cameraLook.useIn90Song || is360Map && cameraLook.useIn360Song)
                {
                    return true;
                }

                return false;
            }
            else
            {
                return cameraLook.useInMenu;
            }
        }
        
        public int GetNextSuitableCameraIndex(int previousIndex, bool isRandom = false, bool ignorePhase = false)
        {
            if (isRandom)
            {
                return GetNextSuitableCameraIndex(Random.Range(0, settings.cameras.Count), false, ignorePhase);
            }
            else
            {
                int i = previousIndex;
                CameraLook cl;
                do
                {
                    Logger.info("divide? - [ (" + i + " + 1) % " + settings.cameras.Count + " ] = ...");
                    i = (i + 1) % settings.cameras.Count;
                    Logger.info("... = " + i);
                    cl = settings.GetCamera(i);
                } while ((!ignorePhase && !IsSuitablePhase(cl)) || i == previousIndex);
                return i;
            }
        }
    }
}
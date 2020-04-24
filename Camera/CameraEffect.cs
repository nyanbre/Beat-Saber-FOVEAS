using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NyanbreFOVEAS
{
    public class CameraEffect
    {
        public enum Type
        {
            DISABLE,
            CAMERA_CHANGE, // value = -3 is "don't change", -1 is "next suitable", -2 is "random suitable"
            FOVE,
            ZOOM,
            ROTATION_TILT,
            ROTATION_HORIZONTAL,
            ROTATION_VERTICAL,
            POSITION_FORWARD,
            POSITION_HORIZONTAL,
            POSITION_VERTICAL,
        }

        public enum TransitionMode
        {
            INSTANT_CHANGE,
            ONE_WAY,
            TWO_WAY,
            PULSE, // SHAKE is "PULSE + isPeriodic"
        }
        
        public Type type;
        public TransitionMode mode = TransitionMode.INSTANT_CHANGE;

        // settings
        public float defaultValue = 0f;
        public float intensity = 1.5f; 

        public bool useRandomValues = false;
        public bool allowNegativeRandomValues = false;
        public bool isRelativeToDefaultValue = false;
        
        public bool isPeriodic = false; // == "use phase for shaking?"
        public float phasingSpeed = 32f; // phase is for shaking

        public float speed
        {
            get { return transitionCurve.transitionSpeed; }
            set { transitionCurve.transitionSpeed = value; }
        }
        
        // current state
        public float currentPhase = 0f; // phase is for shaking
        public TransitionCurve transitionCurve; 
        
        // value of current effect intensity is transitionCurve.currentValue
        public float currentValue => isPeriodic
            ? transitionCurve.currentValue * Mathf.Sin(currentPhase * phasingSpeed)
            : transitionCurve.currentValue;

        
        public CameraEffect(Type type, TransitionCurve.CurveType curveType = TransitionCurve.CurveType.EXP_OUT)
        {
            this.type = type;
            this.defaultValue = FoveasConstStorage.typeDefaultValues[type];
            transitionCurve = new TransitionCurve(curveType, FoveasConstStorage.typeDefaultValues[type]);
        }
        
        public CameraEffect(Type type, float defaultValue, TransitionCurve.CurveType curveType = TransitionCurve.CurveType.EXP_OUT)
        {
            this.type = type;
            this.defaultValue = defaultValue;
            transitionCurve = new TransitionCurve(curveType, defaultValue);
        }

        public void Untrigger()
        {
//            transitionCurve.StartTransitionTo(defaultValue);
            switch (mode)
            {
                case TransitionMode.TWO_WAY:
                case TransitionMode.PULSE:
                    transitionCurve.StartTransitionTo(defaultValue);
                    break;
                case TransitionMode.INSTANT_CHANGE:
                case TransitionMode.ONE_WAY:
                default:
                    transitionCurve.currentValue = defaultValue;
                    break;
            }
        }

        public void Reset()
        {
            transitionCurve.currentValue = defaultValue;
            transitionCurve.isTransitioning = false;
            currentPhase = 0f;
        }
        
        public void SetupEvents(FoveasActions actions)
        {
            transitionCurve.SetupActions(actions);
            if (isPeriodic)
            {
                actions.DeltaTime += (deltaTime, isMapPlaying) => currentPhase += deltaTime;
            }
        }

        public void Trigger()
        {
            float oldValue = currentValue;
            
            var targetValue = GenerateTargetValue();

            Logger.info(string.Format("CameraEffect has been triggered! Type {0}, from value = {1}, to value = {2}", type.ToString(), oldValue.ToString(), targetValue.ToString()));

            switch (mode)
            {
                case TransitionMode.INSTANT_CHANGE:
                    transitionCurve.currentValue = targetValue;
                    break;
                case TransitionMode.ONE_WAY:
                    transitionCurve.StartTransitionTo(targetValue);
                    break;
                case TransitionMode.TWO_WAY:
                    transitionCurve.StartTransitionTo(targetValue);
//                    transitionCurve.OnTransitionEndOneshots += () =>
//                    {
//                        transitionCurve.StartTransitionTo(oldValue);
//                    };
                    break;
                case TransitionMode.PULSE:
                    transitionCurve.currentValue = targetValue;
                    transitionCurve.StartTransitionTo(oldValue);
                    break;
                default:
                    transitionCurve.currentValue = targetValue;
                    break;
            }
        }

        private float GenerateTargetValue()
        {
            float targetValue;
            if (useRandomValues)
            {
                targetValue = Random.Range(allowNegativeRandomValues ? -intensity : 0f, intensity); // TODO: use blue noise
            }
            else
            {
                targetValue = intensity;
            }

            if (isRelativeToDefaultValue)
            {
                targetValue += defaultValue;
            }

            return targetValue;
        }

        public void Apply(CameraLook cameraLook)
        {
            Logger.info(string.Format("isPeriodic={0}, currentPhase={1}, v={2}, pv={3}", isPeriodic, currentPhase, transitionCurve.currentValue, currentValue));
//            Logger.info(string.Format("Applying CameraEffect... Type {0}, current value = {1}", type.ToString(), currentValue.ToString()));
            switch (type)
            {
                case Type.DISABLE:
                    break;
                case Type.CAMERA_CHANGE:
                    int val = Mathf.RoundToInt(currentValue);
                    switch (val)
                    {
                        case -2:
                            cameraLook.transitionChangeMode = CameraLook.ChangeMode.RANDOM_SUITABLE;
                            break;
                        case -1:
                            cameraLook.transitionChangeMode = CameraLook.ChangeMode.NEXT_SUITABLE;
                            break;
                        default:
                            cameraLook.transitionChangeMode = CameraLook.ChangeMode.TO_SPECIFIED;
                            cameraLook.transitionTargetIndex = val;
                            break;
                    }
                    cameraLook.transitionSpeed = speed;
                    break;
                case Type.FOVE:
                    cameraLook.foveMultiplier /= currentValue;
//                    cameraLook.effectsVector += (1 - currentValue) * cameraLook.Forward;
                    break;
                case Type.ZOOM:
                    cameraLook.zoomMultiplier /= currentValue;
                    break;
                case Type.ROTATION_TILT:
                    cameraLook.effectsRotation *= Quaternion.AngleAxis(currentValue, cameraLook.Forward);
                    break;
                case Type.ROTATION_HORIZONTAL:
                    cameraLook.effectsRotation *= Quaternion.AngleAxis(currentValue, Vector3.up);
                    break;
                case Type.ROTATION_VERTICAL:
                    cameraLook.effectsRotation *= Quaternion.AngleAxis(currentValue, Vector3.Cross(cameraLook.Forward, Vector3.up).normalized);
                    break;
                case Type.POSITION_FORWARD:
                    cameraLook.effectsVector += currentValue * cameraLook.Forward;
                    break;
                case Type.POSITION_HORIZONTAL:
                    cameraLook.effectsVector += currentValue * Vector3.Cross(cameraLook.Forward, Vector3.up).normalized;
                    break;
                case Type.POSITION_VERTICAL:
                    cameraLook.effectsVector += currentValue * Vector3.up;
                    break;
            }
        }

        public static CameraEffect FromJObject(JObject cameraEffectSerialized)
        {
            Logger.info("Deserializing to CameraEffect: \n" + cameraEffectSerialized.ToString());
            CameraEffect result;

            Type type = (Type) Enum.Parse(typeof(Type), (string) cameraEffectSerialized["type"]);
            TransitionCurve.CurveType curveType = TransitionCurve.CurveType.EXP_OUT;
            if (cameraEffectSerialized.ContainsKey("curveType"))
            {
                curveType = (TransitionCurve.CurveType) Enum.Parse(typeof(TransitionCurve.CurveType), (string) cameraEffectSerialized["curveType"]);
            }

            if (cameraEffectSerialized.ContainsKey("defaultValue"))
            {
                result = new CameraEffect(type, Convert.ToSingle(cameraEffectSerialized["defaultValue"]), curveType);
            }
            else
            {
                result = new CameraEffect(type, curveType);
            }

            if (cameraEffectSerialized.ContainsKey("mode")) result.mode = (TransitionMode) Enum.Parse(typeof(TransitionMode), (string) cameraEffectSerialized["mode"]);
            if (cameraEffectSerialized.ContainsKey("intensity")) result.intensity = Convert.ToSingle(cameraEffectSerialized["intensity"]);
            if (cameraEffectSerialized.ContainsKey("phasingSpeed")) result.phasingSpeed = Convert.ToSingle(cameraEffectSerialized["phasingSpeed"]);
            if (cameraEffectSerialized.ContainsKey("speed")) result.speed = Convert.ToSingle(cameraEffectSerialized["speed"]);
            if (cameraEffectSerialized.ContainsKey("isPeriodic")) result.isPeriodic = (bool) cameraEffectSerialized["isPeriodic"];
            if (cameraEffectSerialized.ContainsKey("useRandomValues")) result.useRandomValues = (bool) cameraEffectSerialized["useRandomValues"];
            if (cameraEffectSerialized.ContainsKey("allowNegativeValues")) result.allowNegativeRandomValues = (bool) cameraEffectSerialized["allowNegativeValues"];
            if (cameraEffectSerialized.ContainsKey("isRelativeToDefaultValue")) result.isRelativeToDefaultValue = (bool) cameraEffectSerialized["isRelativeToDefaultValue"];
            
            return result;
        }
    }
}
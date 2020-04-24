using System;
using UnityEngine;

namespace NyanbreFOVEAS
{
    public class TransitionCurve
    {
        public enum CurveType
        {
            LINEAR,
            COSINE,
            LOGISTIC,
            EXP_OUT,
            EXP_IN,
        }

        public bool isActive = false;

        public Action OnTransitionStartOneshots;
        public Action OnTransitionEndOneshots;

        public CurveType type = CurveType.LINEAR;
        public float transitionSpeed = 1f;

        public float oldValue = 0f;
        public float currentValue = 0f;
        public float targetValue = 0f;

        public float transitionPosition = 0f; // [0..1]
        public bool isTransitioning = false;
        
        public TransitionCurve(CurveType curveType, float initialValue)
        {
            this.oldValue = initialValue;
            this.currentValue = initialValue;
            this.type = curveType;
        }

        public void SetupActions(FoveasActions actions)
        {
            if (!isActive)
            {
                actions.DeltaTime += (deltaSeconds, isMapPlaying) =>  Update(deltaSeconds);
                isActive = true;
            }
        }

        private void Update(float deltaSeconds)
        {
//            Logger.info("Transition?");
            if (isTransitioning)
            {
//                Logger.info("Transition update, value=" + transitionPosition);

                transitionPosition += deltaSeconds * transitionSpeed;
                currentValue = ApplyCurve(oldValue, targetValue, transitionPosition, type);

                if (transitionPosition >= 1f)
                {
                    Logger.info("Transition end");
                    transitionPosition = 1f;
                    currentValue = targetValue;

                    isTransitioning = false;
                    OnTransitionEndOneshots?.Invoke();
                    OnTransitionEndOneshots = delegate {  };
                }
            }
        }

        public void StartTransitionTo(float target)
        {
            oldValue = currentValue;
            targetValue = target;
            transitionPosition = 0f;
            isTransitioning = true;

            OnTransitionStartOneshots?.Invoke();
            OnTransitionStartOneshots = delegate {  };
        }

        public static float ApplyCurve(float from, float to, float transitionPosition, CurveType curveType)
        {
            float trasnitionValue;
            switch (curveType)
            {
                case CurveType.LINEAR:
                    trasnitionValue = transitionPosition;
                    break;
                case CurveType.COSINE:
                    trasnitionValue = 0.5f - Mathf.Cos(Mathf.PI * transitionPosition) / 2;
                    break;
                case CurveType.LOGISTIC:
                    trasnitionValue = 1 - 1/(1 + Mathf.Exp(24 * transitionPosition - 12));
                    break;
                case CurveType.EXP_OUT:
                    trasnitionValue = 1 - Mathf.Exp(-8 * transitionPosition);
                    break;
                case CurveType.EXP_IN:
                    trasnitionValue = Mathf.Exp(-8 * (1 - transitionPosition));
                    break;
                default:
                    return ApplyCurve(from, to, transitionPosition, CurveType.LINEAR);
            }
            return (from * (1 - trasnitionValue) + to * trasnitionValue);
        }
    }
}
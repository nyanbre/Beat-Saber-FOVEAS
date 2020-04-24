using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace NyanbreFOVEAS
{
    public class CameraLook
    {
        public enum ChangeMode
        {
            NOT_CHANGING,
            TO_SPECIFIED,
            NEXT_SUITABLE,
            RANDOM_SUITABLE,
            NEXT_ANY,
            RANDOM_ANY
        }
        public string alias = "";

        public float baseFov = 80f;
        public float zoomMultiplier = 1f;
        public float foveMultiplier = 1f;

        public TransitionCurve transitionCurve = new TransitionCurve(TransitionCurve.CurveType.EXP_OUT, 0f);
        [CanBeNull] public CameraLook transitionTarget = null; // is refreshed every frame!!!
        public int transitionTargetIndex = 2; 
        public ChangeMode transitionChangeMode = ChangeMode.NOT_CHANGING; 
        public float transitionSpeed = 0.2f; 

        public bool useSongBeatUnits = true; // false = stayTime is in seconds, true = stayTime is in song beats
        public float duration = 16f; // in units: seconds or beats

        public bool use360Corrections = true;
        public bool use90Corrections = false;
        
        public bool followHeadRotation = false;
        public float followHeadPositionMultipler = 0f;
        public float followHeadTargetMultipler = -1f;
        
        public bool constrainPosX = false;
        public bool constrainPosY = false;
        public bool constrainPosZ = false;
        public bool constrainTargetX = false;
        public bool constrainTargetY = false;
        public bool constrainTargetZ = false;

        public bool useInMenu = true; // TODO: remind in documentation to not to forget setting camera phases
        public bool useInNormalSong = true;
        public bool useIn360Song = true;
        public bool useIn90Song = true;

        public Vector3 originalHeadPos;
        public Quaternion originalHeadRotation;


        private Vector3 pos = Vector3.zero;
        private Vector3 posBackup = Vector3.zero; // for constrains
        public Vector3 Pos => pos;

        private Vector3 target = Vector3.zero;
        private Vector3 targetBackup = Vector3.zero; // for constrains
        public Vector3 Target => target;

        private float distance;
        public float Distance => distance;

        private Vector3 forward;
        public Vector3 Forward => forward;

        private Quaternion lookRotation;
        public Quaternion LookRotation => lookRotation;

        public Vector3 effectsVector = Vector3.zero;
        public Quaternion effectsRotation = Quaternion.identity;

        public CameraLook(Vector3 pos, Vector3 target)
        {
            this.pos = pos;
            this.posBackup = pos;

            this.target = target;
            this.targetBackup = target;

            RecalculateVectorInfo();
        }

        public void SetPosTarget(Vector3 newPos, Vector3 newTarget)
        {
            this.pos = ConstrainedPos(newPos);
            this.target = ConstrainedTarget(newTarget);

            RecalculateVectorInfo();
        }

        private void RecalculateVectorInfo()
        {
            distance = Vector3.Distance(pos, target);
            forward = Vector3.Normalize(target - pos);
            lookRotation = Quaternion.LookRotation(target - pos);
        }
        
        public void ResetEffects()
        {
            pos = posBackup;
            target = targetBackup;
            effectsRotation = Quaternion.identity;
            effectsVector = Vector3.zero;
        }
        
        public void FinishApplyingEffects()
        {
            if (Utils.NotEqual(foveMultiplier, 1f))
            {
                effectsVector -= Forward * (foveMultiplier - 1); // fov effect back slide
            }
            
            SetPosTarget(posBackup + effectsVector, targetBackup);
            lookRotation *= effectsRotation;

            if (transitionTarget != null)
            {
                transitionTarget.SetPosTarget(transitionTarget.posBackup + effectsVector, transitionTarget.targetBackup);
                transitionTarget.lookRotation *= effectsRotation;
            }
            
            zoomMultiplier = 1f;
            foveMultiplier = 1f;
            effectsRotation = Quaternion.identity;
            effectsVector = Vector3.zero;
        }

//        public CameraLook ApplyDegreeMapCorrection(Quaternion headRotation)
//        {
//            Quaternion flattenedRotation = (new Quaternion(
//                headRotation.x,
//                0,
//                1,
//                0)).normalized;
//            CameraLook corrected = new CameraLook();
//            corrected.Pos = flattenedRotation * pos;
//            corrected.Target = flattenedRotation * target;
//            return corrected;
//        }

        private Vector3 ConstrainedPos(Vector3 v)
        {
            return new Vector3(
                constrainPosX ? Pos.x : v.x,
                constrainPosY ? Pos.y : v.y,
                constrainPosZ ? Pos.z : v.z
            );
        }
        private Vector3 ConstrainedTarget(Vector3 v)
        {
            return new Vector3(
                constrainPosX ? Target.x : v.x,
                constrainPosY ? Target.y : v.y,
                constrainPosZ ? Target.z : v.z
            );
        }

        public static CameraLook AdjustCamera(Quaternion direction, CameraLook cameraLook)
        {
            Vector3 adjustedPos = direction * cameraLook.Pos;
            Vector3 adjustedTarget = direction * cameraLook.Target;
            cameraLook.SetPosTarget(adjustedPos, adjustedTarget);
            return cameraLook;
        }

        public Dictionary<string, object> ToDictionary()
        {
            CameraLook defaultValues = new CameraLook(Vector3.zero, Vector3.zero);
            Dictionary<string, object> result = new Dictionary<string, object>();

            result.Add("position", Vector3ToString(this.Pos));
            result.Add("target", Vector3ToString(this.Target));

            if (!string.IsNullOrEmpty(this.alias)) result.Add("alias", this.alias);
            if (Utils.NotEqual(this.baseFov, defaultValues.baseFov)) result.Add("fov", this.baseFov);

            if (this.use90Corrections != defaultValues.use90Corrections)
                result.Add("use90Corrections", this.use90Corrections);
            if (this.use360Corrections != defaultValues.use360Corrections)
                result.Add("use360Corrections", this.use360Corrections);

            if (this.followHeadRotation != defaultValues.followHeadRotation)
                result.Add("followHeadRotation", this.followHeadRotation);
            if (Utils.NotEqual(this.followHeadPositionMultipler, defaultValues.followHeadPositionMultipler))
                result.Add("followHeadPositionMultipler", this.followHeadPositionMultipler);
            if (Utils.NotEqual(this.followHeadTargetMultipler, defaultValues.followHeadTargetMultipler))
                result.Add("followHeadTargetMultipler", this.followHeadTargetMultipler);

            List<string> constrainsPos = new List<string>();
            if (constrainPosX) constrainsPos.Add("X");
            if (constrainPosY) constrainsPos.Add("Y");
            if (constrainPosZ) constrainsPos.Add("Z");
            if (constrainsPos.Count > 0) result.Add("constrainPositionAxis", constrainsPos);

            List<string> constrainsTarget = new List<string>();
            if (constrainTargetX) constrainsTarget.Add("X");
            if (constrainTargetY) constrainsTarget.Add("Y");
            if (constrainTargetZ) constrainsTarget.Add("Z");
            if (constrainsTarget.Count > 0) result.Add("constrainTargetAxis", constrainsTarget);

            List<string> phases = new List<string>();
            if (this.useInMenu && this.useInNormalSong && this.useIn360Song && this.useIn90Song)
            {
                phases.Add("ALL");
            }
            else if (this.useInNormalSong && this.useIn360Song && this.useIn90Song)
            {
                phases.Add("ALL_SONGS");
            }
            else
            {
                if (this.useInNormalSong) phases.Add("NORMAL_SONG");
                if (this.useIn360Song) phases.Add("360_SONG");
                if (this.useIn90Song) phases.Add("90_SONG");
            }
            result.Add("usage", phases);
            
            return result;
        }

        public static CameraLook FromDictionary(Dictionary<string, object> rawCameraLook)
        {
            CameraLook cameraLook = new CameraLook(
                StringToVector3((string) rawCameraLook["position"]),
                StringToVector3((string) rawCameraLook["target"])
            );
            cameraLook.transitionTarget = null;
            cameraLook.transitionChangeMode = ChangeMode.NOT_CHANGING;
            cameraLook.transitionTargetIndex = 2;

            if (rawCameraLook.ContainsKey("alias")) cameraLook.alias = (string) rawCameraLook["alias"];
            if (rawCameraLook.ContainsKey("fov")) cameraLook.baseFov = Convert.ToSingle(rawCameraLook["fov"]);
            
            if (rawCameraLook.ContainsKey("use360Corrections")) cameraLook.use360Corrections = (bool) rawCameraLook["use360Corrections"];
            if (rawCameraLook.ContainsKey("use90Corrections")) cameraLook.use90Corrections = (bool) rawCameraLook["use90Corrections"];
            
            if (rawCameraLook.ContainsKey("followHeadRotation")) cameraLook.followHeadRotation = (bool) rawCameraLook["followHeadRotation"];
            if (rawCameraLook.ContainsKey("followHeadPositionMultipler")) cameraLook.followHeadPositionMultipler = Convert.ToSingle(rawCameraLook["followHeadPositionMultipler"]);
            if (rawCameraLook.ContainsKey("followHeadTargetMultipler")) cameraLook.followHeadTargetMultipler = Convert.ToSingle(rawCameraLook["followHeadTargetMultipler"]);

            if (rawCameraLook.ContainsKey("constrainPositionAxis"))
            {
                JArray constrainPositionAxisArray = (JArray) rawCameraLook["constrainPositionAxis"];
                List<string> constrainPositionAxis = constrainPositionAxisArray.ToObject<List<string>>();
                for (int i = 0; i < constrainPositionAxis.Count; i++) constrainPositionAxis[i] = constrainPositionAxis[i].ToUpper();
                
                if (constrainPositionAxis.Contains("X")) cameraLook.constrainPosX = true;
                if (constrainPositionAxis.Contains("Y")) cameraLook.constrainPosY = true;
                if (constrainPositionAxis.Contains("Z")) cameraLook.constrainPosZ = true;
            }
            if (rawCameraLook.ContainsKey("constrainTargetAxis"))
            {
                JArray constrainTargetAxisArray = (JArray) rawCameraLook["constrainTargetAxis"];
                List<string> constrainTargetAxis = constrainTargetAxisArray.ToObject<List<string>>();
                for (int i = 0; i < constrainTargetAxis.Count; i++) constrainTargetAxis[i] = constrainTargetAxis[i].ToUpper();

                if (constrainTargetAxis.Contains("X")) cameraLook.constrainTargetX = true;
                if (constrainTargetAxis.Contains("Y")) cameraLook.constrainTargetY = true;
                if (constrainTargetAxis.Contains("Z")) cameraLook.constrainTargetZ = true;
            }
            
            if (rawCameraLook.ContainsKey("usage"))
            {
                JArray phasesArray = (JArray) rawCameraLook["usage"];
                List<string> phases = phasesArray.ToObject<List<string>>();
                for (int i = 0; i < phases.Count; i++) phases[i] = phases[i].ToUpper();
                
                if (phases.Contains("MENU")) cameraLook.useInMenu = true;
                if (phases.Contains("NORMAL_SONG")) cameraLook.useInNormalSong = true;
                if (phases.Contains("360_SONG")) cameraLook.useIn360Song = true;
                if (phases.Contains("90_SONG")) cameraLook.useIn90Song = true;

                if (phases.Contains("ALL_SONGS"))
                {
                    cameraLook.useInNormalSong = true;
                    cameraLook.useIn360Song = true;
                    cameraLook.useIn90Song = true;
                }

                if (phases.Contains("ALL"))
                {
                    cameraLook.useInMenu = true;
                    cameraLook.useInNormalSong = true;
                    cameraLook.useIn360Song = true;
                    cameraLook.useIn90Song = true;
                }
            }

            return cameraLook;
        }

        private static string Vector3ToString(Vector3 v)
        {
            return string.Format("({0}, {1}, {2})", v.x, v.y, v.z);
        }

        private static Vector3 StringToVector3(string s)
        {
            char[] _ = {' ', '\t', '(', ')', '[', ']', '{', '}'};
            string[] sa = s.Split(',');
            return new Vector3(float.Parse(sa[0].Trim(_)), float.Parse(sa[1].Trim(_)), float.Parse(sa[2].Trim(_)));
        }

        public CameraLook BlendedTransition()
        {
            if (transitionTarget != null)
            {

                float old = 1 - transitionTarget.transitionCurve.currentValue; 
                float nw = transitionTarget.transitionCurve.currentValue;

                Vector3 resultPos = posBackup * old + transitionTarget.posBackup * nw;
                Vector3 resultTarget = targetBackup * old + transitionTarget.targetBackup * nw;
                float baseFov = this.baseFov * old + transitionTarget.baseFov * nw;
                float foveMultiplier = this.foveMultiplier * old + transitionTarget.foveMultiplier * nw;
                float zoomMultiplier = this.zoomMultiplier * old + transitionTarget.zoomMultiplier * nw;
                
                CameraLook result = new CameraLook(resultPos, resultTarget);
                result.baseFov = baseFov;
                result.foveMultiplier = foveMultiplier;
                result.zoomMultiplier = zoomMultiplier;
                
                return result;
            }
            else
            {
                return this;
            }
        }
    }
}
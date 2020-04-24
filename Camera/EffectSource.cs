using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace NyanbreFOVEAS
{
    public class EffectSource
    {
        public enum Type
        {
            ON_SONG_START,
            ON_SONG_END, // Should I name it "MENU_START"?
            
            ON_NTH_SECOND,
            ON_NTH_BEAT,
            ON_NTH_COMBO,
            
            EVERY_NTH_SECOND,
            EVERY_NTH_BEAT,
            EVERY_NTH_COMBO,
            
            XN_COMBO_BREAK,
            NTH_NOTE_MISS,
            NTH_NOTE_HIT,
            NTH_BOMB_HIT,
            WALL_STUCK,
            
            RINGS_ZOOM
        }

        public Type type;
        public bool useInMenu = false;
        public bool isGlobal = false;
        
        public float rarity = 1f;
        public float offset = 0f;
        public float duration = -1f; // set it to -1f to disable; duration is NOT applicable to combo-related events

        public float currentCount = 0f; // count is for "rarity"
        public float currentDuration = -1f; // in the same units as rarity
        
        private HashSet<CameraEffect> effects = new HashSet<CameraEffect>();

        public void AddEffect(CameraEffect cameraEffect) => effects.Add(cameraEffect);

        private void Trigger()
        {
            Logger.info(string.Format("EffectSource has been triggered! Type {0}, rarity = {1}", type.ToString(), rarity.ToString()));
            currentDuration = 0f;
            foreach (CameraEffect cameraEffect in effects)
            {
                cameraEffect.Trigger();
            }
        }

        public void Untrigger()
        {
            currentDuration = -1f;
            foreach (CameraEffect cameraEffect in effects)
            {
                cameraEffect.Untrigger();
            }
        }
        
        public void Reset(bool forced = false)
        {
            if (!isGlobal || forced)
            {
                Untrigger();
                foreach (CameraEffect cameraEffect in effects)
                {
                    cameraEffect.Reset();
                }
            }
        }

        public void SetupEvents(FoveasActions actions)
        {
            Reset(true);
            
            foreach (CameraEffect cameraEffect in effects)
            {
                cameraEffect.SetupEvents(actions);
            }

            switch (type)
            {
                case Type.ON_SONG_START:
                    actions.MapStart += (mapInfo) => Trigger();
                    break;
                case Type.ON_SONG_END:
                    actions.MenuStart += () => Trigger();
                    break;
                case Type.ON_NTH_SECOND:
                    actions.DeltaTime += (deltaSeconds, isMapPlaying) =>
                    {
                        if (isMapPlaying || useInMenu) AddCheckUnits(deltaSeconds, false);
                    };
                    break;
                case Type.ON_NTH_BEAT:
                    actions.DeltaBeat += (deltaBeats, isMapPlaying) =>
                    {
                        if (isMapPlaying || useInMenu) AddCheckUnits(deltaBeats, false);
                    };
                    break;
                case Type.ON_NTH_COMBO:
                    actions.ComboUpdate += (combo) =>
                    {
                        if (combo != 0) CheckUnits(combo, false);
                    };
                    break;
                case Type.EVERY_NTH_SECOND:
                    actions.DeltaTime += (deltaSeconds, isMapPlaying) =>
                    {
                        if (isMapPlaying || useInMenu) AddCheckUnits(deltaSeconds);
                    };
                    break;
                case Type.EVERY_NTH_BEAT:
                    actions.DeltaBeat += (deltaBeats, isMapPlaying) =>
                    {
                        if (isMapPlaying || useInMenu) AddCheckUnits(deltaBeats);
                    };
                    break;
                case Type.EVERY_NTH_COMBO:
                    actions.ComboUpdate += (combo) =>
                    {
                        if (combo != 0) CheckUnits(combo);
                    };
                    break;
                case Type.XN_COMBO_BREAK:
                    actions.ComboBreak += (combo) =>
                    {
                        if (combo > rarity) Trigger();
                    };
                    break;
                case Type.NTH_NOTE_MISS:
                    actions.MissNote += () =>
                    {
                        AddCheckUnits(1);
                    };
                    break;
                case Type.NTH_NOTE_HIT:
                    actions.HitNote += () =>
                    {
                        AddCheckUnits(1);
                    };
                    break;
                case Type.NTH_BOMB_HIT:
                    actions.HitBomb += () =>
                    {
                        AddCheckUnits(1);
                    };
                    break;
                case Type.RINGS_ZOOM:
                    actions.RingsZoom += () =>
                    {
                        AddCheckUnits(1);
                    };
                    break;
                case Type.WALL_STUCK:
                    actions.WallStuck += () => { Trigger(); };
                    actions.WallUnstuck += () => { Reset(); };
                    break;
            }
        }

        private void AddCheckUnits(float addCount, bool repeated = true)
        {
            if (duration >= 0f && currentDuration >= 0f)
            {
                currentDuration += addCount;
                if (currentDuration >= duration)
                {
                    Untrigger();
                }
            }
            
            CheckUnits(currentCount + addCount, repeated);
            currentCount += addCount;
        }

        private void CheckUnits(float newCount, bool repeated = true)
        {
//            Logger.info(string.Format("Checking SourceEffect... Type {0}, rarity = {1}, currentCount = {2}, newCount = {3}, offset = {4}", type.ToString(), rarity, currentCount, newCount, offset));

            if (repeated)
            {
                int before = Mathf.FloorToInt((currentCount - offset) / rarity);
                int after = Mathf.FloorToInt((newCount - offset) / rarity);

                if (before < after)
                {
                    Trigger();
                }
            }
            else
            {
                if (currentCount - offset < rarity && newCount - offset >= rarity)
                {
                    Trigger();
                }
            }
        }

        public void Apply(CameraLook cameraLook)
        {
            foreach (CameraEffect cameraEffect in effects)
            {
                cameraEffect.Apply(cameraLook);
            }
        }

        public static EffectSource FromDictionary(Dictionary<string,object> effectSource)
        {
            Logger.info("Deserializing to EffectSource: \n{" + string.Join(",", effectSource.Select(kv => kv.Key + "=" + kv.Value).ToArray()) + "}");
            EffectSource result = new EffectSource();

            result.type = (Type) Enum.Parse(typeof(Type), (string) effectSource["type"]);
            if (effectSource.ContainsKey("useInMenu")) result.useInMenu = (bool) effectSource["useInMenu"];
            if (effectSource.ContainsKey("isGlobal")) result.isGlobal = (bool) effectSource["isGlobal"];
            if (effectSource.ContainsKey("rarity")) result.rarity = Convert.ToSingle(effectSource["rarity"]);
            if (effectSource.ContainsKey("offset")) result.offset = Convert.ToSingle(effectSource["offset"]);
            if (effectSource.ContainsKey("duration")) result.duration = Convert.ToSingle(effectSource["duration"]);

            JArray cameraEffectsSerialized = (JArray) effectSource["effects"];
            foreach (JObject cameraEffectSerialized in cameraEffectsSerialized)
            {
                CameraEffect cameraEffect = CameraEffect.FromJObject(cameraEffectSerialized);
                result.effects.Add(cameraEffect);
            }
            
            return result;
        }
    }
}
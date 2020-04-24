using System.Collections.Generic;

namespace NyanbreFOVEAS
{
    // User defined settings which will be serialized and deserialized with Newtonsoft Json.Net.
    // Only public variables will be serialized.
    public class FoveasSettings : IPluginSettings
    {
        public bool compatibleMode = false;
        
        public float maxBaseFov = 80f;
        public float fovDelay = 0.02f; // in seconds

        // all positions and vectors are in meters, (0,0,0) is the center of a room at the floor level
        public List<Dictionary<string, object>> cameras = new List<Dictionary<string, object>>();

        private List<CameraLook> camerasDeserialized = new List<CameraLook>();
        private Dictionary<string, int> camerasAliases = new Dictionary<string, int>();
        public CameraLook GetCamera(int i) => camerasDeserialized[i];
        public CameraLook GetCamera(string alias) => camerasDeserialized[camerasAliases[alias]];
        
        public List<Dictionary<string, object>> effectSources = new List<Dictionary<string, object>>();
        private HashSet<EffectSource> effectSourcesDeserialized = new HashSet<EffectSource>();
        public HashSet<EffectSource> GetEffectSources(bool antiDeserialization = true) => effectSourcesDeserialized;

        // 0 = disable, 1 = only broadcaster, 2 = +moderators, 3 = +VIPs, 4 = +subs, 5 = +follows, 6 = everybody
        // if isGlobalTwitchChatControlsSlowMode, nobody can send commands in {durationOfTwitchChatControlsSlowMode} seconds after last command
        public int allowTwitchChatChangePosition = 2;
        public int allowTwitchChatChangeSettings = 2;
        public int disableTwitchChatControlsSlowMode = 2;
        public bool isGlobalTwitchChatControlsSlowMode = false;
        public float durationOfTwitchChatControlsSlowMode = 5f; // in seconds

        public string foveServerIP = "127.0.0.1"; // DON'T FORGET TO INSTALL OBS PYTHON SCRIPT OR DISABLE FOV EFFECT!
        public int foveServerPort = 50734; // 50734 = FOVEA

        public void deserializeObjects()
        {
            for (int i = 0; i < cameras.Count; i++)
            {
                CameraLook cameraLook = CameraLook.FromDictionary(cameras[i]);
                camerasAliases.Add(cameraLook.alias, i);
                camerasDeserialized.Add(cameraLook);
            }
            DetermineMaxFov();
            
            for (int i = 0; i < effectSources.Count; i++)
            {
                EffectSource effectSource = EffectSource.FromDictionary(effectSources[i]);
                effectSourcesDeserialized.Add(effectSource);
            }
        }

        private void DetermineMaxFov()
        {
            for (int i = 0; i < camerasDeserialized.Count; i++)
            {
                if (camerasDeserialized[i].baseFov > maxBaseFov) maxBaseFov = camerasDeserialized[i].baseFov;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NyanbreFOVEAS
{
    public class FoveasConfigLoader
    {
        // Path to plugin folder
        public string pluginPath;
        public string configFilename = "settings_{0}.json";
        public string logFilename = "log.txt";

        public void LoadConfig(string filename, out FoveasSettings settings)
        {
            settings = new FoveasSettings();

            bool isConfigLoadedFromFile = false;
            string configFile = Path.Combine(pluginPath, filename);
            
            //Check to see if the file exists
            if (File.Exists(configFile))
            {
                try
                {
                    settings = JsonConvert.DeserializeObject<FoveasSettings>(File.ReadAllText(@configFile));
                    NyanbreFOVEAS.Logger.info("Loading config from file: SUCCESS");
                    isConfigLoadedFromFile = true;
                }
                catch (Exception e)
                {
                    NyanbreFOVEAS.Logger.info("Loading config from file: FAILURE. Reason: " + e.Message);
                    File.Delete(configFile);
                    NyanbreFOVEAS.Logger.info("Corrupted config file successfully deleted!");
                }
            }

            if (!isConfigLoadedFromFile)
            {
                settings.cameras.Clear();
                
                Dictionary<string, object> defaultLook = new Dictionary<string, object>();
                defaultLook.Add("position", "(2.0, 1.8, -3)");
                defaultLook.Add("target", "(0, 1.2, 0.5)");
                defaultLook.Add("alias", "menu");
                settings.cameras.Add(defaultLook);

                Dictionary<string, object> omoteaCameraLook = new Dictionary<string, object>();
                omoteaCameraLook.Add("position", "(0.5, 1.15, -2.6)");
                omoteaCameraLook.Add("target", "(0, 0.9, 1.8)");
                omoteaCameraLook.Add("alias", "omotea");
                settings.cameras.Add(omoteaCameraLook);

                Dictionary<string, object> shanaCameraLook = new Dictionary<string, object>();
                shanaCameraLook.Add("position", "(0.4, 1, -1.9)");
                shanaCameraLook.Add("target", "(0.2, 0.8, 0.4)");
                shanaCameraLook.Add("alias", "shana");
                settings.cameras.Add(shanaCameraLook);

                Dictionary<string, object> chairo1CameraLook = new Dictionary<string, object>();
                chairo1CameraLook.Add("position", "(2, 0.5, -2)");
                chairo1CameraLook.Add("target", "(0.5, 0.65, 1.7)");
                chairo1CameraLook.Add("alias", "chairo1");
                settings.cameras.Add(chairo1CameraLook);

                Dictionary<string, object> chairo2CameraLook = new Dictionary<string, object>();
                chairo2CameraLook.Add("position", "(-1, 1.2, -2.4)");
                chairo2CameraLook.Add("target", "(-0.2, 0.8, 1)");
                chairo2CameraLook.Add("alias", "chairo2");
                settings.cameras.Add(chairo2CameraLook);
                
                Dictionary<string, object> everyBeat4 = new Dictionary<string, object>();
                everyBeat4.Add("type", "ON_NTH_BEAT");
                everyBeat4.Add("rarity", 4d);
                everyBeat4.Add("useInMenu", true);
//                List<Dictionary<string, object>> everyBeat4Effects = new List<Dictionary<string, object>>();
                JArray everyBeat4Effects = new JArray();
                Dictionary<string, object> everyBeat4Effect = new Dictionary<string, object>();
                everyBeat4Effect.Add("type", "POSITION_HORIZONTAL");
                everyBeat4Effect.Add("mode", "TWO_WAY");
                everyBeat4Effect.Add("isPeriodic", true);
                everyBeat4Effect.Add("useRandomValues", true);
                everyBeat4Effect.Add("allowNegativeRandomValues", true);
                everyBeat4Effects.Add(everyBeat4Effect);
                everyBeat4.Add("effects", everyBeat4Effects);
                
                settings.effectSources.Add(everyBeat4);

                Dictionary<string, object> everyBeat8 = new Dictionary<string, object>();
                everyBeat8.Add("type", "ON_NTH_BEAT");
                everyBeat8.Add("rarity", 8d);
                everyBeat8.Add("useInMenu", true);
//                List<Dictionary<string, object>> everyBeat8Effects = new List<Dictionary<string, object>>();
                JArray everyBeat8Effects = new JArray();
                Dictionary<string, object> everyBeat8Effect = new Dictionary<string, object>();
                everyBeat8Effect.Add("type", "CAMERA_CHANGE");
                everyBeat8Effect.Add("value", -2d);
                everyBeat8Effects.Add(everyBeat8Effect);
                everyBeat8.Add("effects", everyBeat8Effects);
                
                settings.effectSources.Add(everyBeat8);


                File.WriteAllText(@configFile, JsonConvert.SerializeObject(settings, Formatting.Indented));
                NyanbreFOVEAS.Logger.info("Applying default config. Default config has been saved to file: " + configFile);
            }
            
            settings.deserializeObjects();
            NyanbreFOVEAS.Logger.info("Current config is:\n" + JsonConvert.SerializeObject(settings, Formatting.Indented));
            NyanbreFOVEAS.Logger.info(settings.cameras.Count + " camera positions registered.");
        }
    }
}
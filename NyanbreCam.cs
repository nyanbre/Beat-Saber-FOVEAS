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
using UnityEngine;
using WebSocketSharp;
using Random = UnityEngine.Random;

namespace BeatSaberHTTP
{
    public class EventObject
    {
        [JsonProperty("event")]
        public string Event; // hello | songStart | finished | failed | menu | pause | resume | noteCut | noteFullyCut | noteMissed | bombCut | bombMissed | obstacleEnter | obstacleExit | scoreChanged | beatmapEvent
        public long time;
        public Events.StatusObject status;
        public Events.NoteCutObject noteCut;
        public Events.BeatmapEvent beatmapEvent;
    }

    namespace Events
    {
        public class StatusObject
        {
            public class Game
            {
                public string pluginVersion; // Currently running version of the BSHTTP plugin
                public string gameVersion; // Version of the game the current BSHTTP is targetting
                public string scene; // Indicates player's current activity
                [CanBeNull] public string mode; // null | "SoloStandard" | "SoloOneSaber" | "SoloNoArrows" | "PartyStandard" | "PartyOneSaber" | "PartyNoArrows"
            }
            public class Beatmap
            {
                public string songName; // Song name
                public string songSubName; // Song sub name
                public string songAuthorName; // Song author name
                public string levelAuthorName; // Beatmap author name
                public string songCover; // Base64 encoded PNG image of the song cover
                public string songHash; // Unique beatmap identifier. At most 32 characters long. Same for all difficulties.
                public float songBPM; // Song Beats Per Minute
                public float noteJumpSpeed; // Song note jump movement speed, how fast the notes move towards the player.
                public int songTimeOffset; // Time in millis of where in the song the beatmap starts. Adjusted for song speed multiplier.
                public long? start; // UNIX timestamp in millis of when the map was started. Changes if the game is resumed. Might be altered by practice settings.
                public long? paused; // If game is paused, UNIX timestamp in millis of when the map was paused. null otherwise.
                public int length; // Length of map in millis. Adjusted for song speed multiplier.
                public string difficulty; // Beatmap difficulty: "Easy" | "Normal" | "Hard" | "Expert" | "ExpertPlus"
                public int notesCount; // Map cube count
                public int bombsCount; // Map bomb count. Set even with No Bombs modifier enabled.
                public int obstaclesCount; // Map obstacle count. Set even with No Obstacles modifier enabled.
                public int maxScore; // Max score obtainable on the map with modifier multiplier
                public string maxRank; // Max rank obtainable using current modifiers: "SSS" | "SS" | "S" | "A" | "B" | "C" | "D" | "E"
                public string environmentName; // Name of the environment this beatmap requested
            }
            public class Perfomance
            {
                public int score; // Current score with modifier multiplier
                public int currentMaxScore; // Maximum score with modifier multiplier achievable at current passed notes
                public string rank; // Current rank: "SSS" | "SS" | "S" | "A" | "B" | "C" | "D" | "E"
                public int passedNotes; // Amount of hit or missed cubes
                public int hitNotes; // Amount of hit cubes
                public int missedNotes; // Amount of missed cubes
                public int passedBombs; // Amount of hit or missed bombs
                public int hitBombs; // Amount of hit bombs
                public int combo; // Current combo
                public int maxCombo; // Max obtained combo
                public int multiplier; // Current combo multiplier {1, 2, 4, 8}
                public float multiplierProgress; // Current combo multiplier progress [0..1)
                public int? batteryEnergy; // Current amount of battery lives left. null if Battery Energy and Insta Fail are disabled.
            }
            public class Mod
            {
                public float multiplier; // Current score multiplier for gameplay modifiers
                public object obstacles; // No Obstacles (FullHeightOnly is not possible from UI): false | "FullHeightOnly" | "All"
                public bool instaFail; // Insta Fail
                public bool noFail; // No Fail
                public bool batteryEnergy; // Battery Energy
                public int? batteryLives; // Amount of battery energy available. 4 with Battery Energy, 1 with Insta Fail, null with neither enabled.
                public bool disappearingArrows; // Disappearing Arrows
                public bool noBombs; // No Bombs
                public string songSpeed; // Song Speed (Slower = 85%, Faster = 120%): "Normal" | "Slower" | "Faster"
                public float songSpeedMultiplier; // Song speed multiplier. Might be altered by practice settings.
                public bool noArrows; // No Arrows
                public bool ghostNotes; // Ghost Notes
                public bool failOnSaberClash; // Fail on Saber Clash (Hidden)
                public bool strictAngles; // Strict Angles (Hidden. Requires more precise cut direction; changes max deviation from 60deg to 15deg)
                public bool fastNotes; // Does something (Hidden)
            }
            public class PlayerSettings
            {
                public bool staticLights; // Static lights
                public bool leftHanded; // Left handed
                public float playerHeight; // Player's height
                public float sfxVolume; // Disable sound effects [0..1]
                public bool reduceDebris; // Reduce debris
                public bool noHUD; // No text and HUDs
                public bool advancedHUD; // Advanced HUD
                public bool autoRestart; // Auto Restart on Fail
            }

            [CanBeNull] public Game game;
            [CanBeNull] public Beatmap beatmap;
            [CanBeNull] public Perfomance performance;
            [CanBeNull] public Mod mod;
            [CanBeNull] public PlayerSettings playerSettings;
        }

        public class NoteCutObject
        {
            public int noteID; // ID of the note
            public string noteType; // // Type of note: "NoteA" | "NoteB" | "GhostNote" | "Bomb"
            public string noteCutDirection; // Direction the note is supposed to be cut in: "Up" | "Down" | "Left" | "Right" | "UpLeft" | "UpRight" | "DownLeft" | "DownRight" | "Any" | "None"
            public int noteLine; // The horizontal position of the note, from left to right [0..3]
            public int noteLayer; // The vertical position of the note, from bottom to top [0..2]
            public bool speedOK; // Cut speed was fast enough
            public bool? directionOK; // Note was cut in the correct direction. null for bombs.
            public bool? saberTypeOK; // Note was cut with the correct saber. null for bombs.
            public bool wasCutTooSoon; // Note was cut too early
            public int? initalScore; // Score without multipliers for the cut. Doesn't include the score for swinging after cut. null for bombs.
            public int? finalScore; // Score without multipliers for the entire cut, including score for swinging after cut. Available in [`noteFullyCut` event](#notefullycut-event). null for bombs.
            public int multiplier; // Combo multiplier at the time of cut
            public float saberSpeed; // Speed of the saber when the note was cut
            public float[] saberDir;
            public string saberType; // Saber used to cut this note: "SaberA" | "SaberB"
            public float? swingRating; // Game's swing rating. Uses the before cut rating in noteCut events and after cut rating for noteFullyCut events. -1 for bombs.
            public float timeDeviation; // Time offset in seconds from the perfect time to cut a note
            public float cutDirectionDeviation; // Offset from the perfect cut angle in degrees
            public float[] cutPoint;
            public float[] cutNormal;
            public float cutDistanceToCenter; // Distance from the center of the note to the cut plane
            public float timeToNextBasicNote; // Time until next note in seconds
        }

        public class BeatmapEvent
        {
            public int type;
            public int value;
        }
    }
}

public class CameraLook
{
    private Vector3 pos = Vector3.zero;
    public Vector3 Pos
    {
        get { return pos; }
        set
        {
            pos = value;
            distance = Vector3.Distance(pos, target);
            forward = Vector3.Normalize(target - pos);
            lookRotation = Quaternion.LookRotation(target - pos);
        }
    }
    
    private Vector3 target = Vector3.zero;
    public Vector3 Target
    {
        get { return target; }
        set
        {
            target = value;
            distance = Vector3.Distance(pos, target);
            forward = Vector3.Normalize(target - pos);
            lookRotation = Quaternion.LookRotation(target - pos);
        }
    }

    private float distance;
    public float Distance => distance;

    private Vector3 forward;
    public Vector3 Forward => forward;

//    private float horizontalViewLength;
//    public float HorizontalViewLength => horizontalViewLength;

    private Quaternion lookRotation;
    public Quaternion LookRotation => lookRotation;

    public float getHorizontalViewLength(float fovD2) // fovD2 is fov divided by 2
    {
        return 2 * distance * Mathf.Tan(fovD2);
    }

    public CameraLook ApplyDegreeMapCorrection(Quaternion headRotation)
    {
        Quaternion flattenedRotation = (new Quaternion(
            headRotation.x,
            0,
            1,
            0)).normalized;
        CameraLook corrected = new CameraLook();
        corrected.Pos = flattenedRotation * pos;
        corrected.Target = flattenedRotation * target;
        return corrected;
    }
}

// User defined settings which will be serialized and deserialized with Newtonsoft Json.Net.
// Only public variables will be serialized.
public class NyanbreCamSettings : IPluginSettings {
    public float fov = 90;
    
    // all positions and vectors are in meters, (0,0,0) is the center of a room at the floor level
    public List<List<float>> cameraLookPositions = new List<List<float>>(); // the 0th position is a MENU POSITION, key is camera position, value is target look
    public List<List<float>> cameraLookTargets = new List<List<float>>(); // the 0th position is a MENU POSITION, key is camera position, value is target look
    public Dictionary<string, int> cameraLookAliases = new Dictionary<string, int>(); // used for choosing CameraLook via twitch chat
    
    public bool randomNewLookPosition = true; // false = switch camera positions in order, true = get targetLookPosition randomly
    public bool useMenuLookWhilePlaying = false; // false = only non-menu CameraLooks will be used while playing maps, true = 0th CameraLook will be included in CameraLooks that are used while playing maps
    public byte transitionCurveType = 1; // 0 = instant switch, 1 = use logistic curve (in-out), 2 = use sin curve (in-out), 3 = use exponential decay (only "out" is smoothened)
    public bool use360Corrections = true; // makes camera positions be relative to player's front direction
    public bool use90Corrections = true; // but maybe it looks weird sometimes? TODO: improve this.

    // 0 = disable, 1 = only broadcaster, 2 = +moderators, 3 = +VIPs, 4 = +subs, 5 = +follows, 6 = everybody
    public int allowTwitchChatChangePosition = 2;
    public int allowTwitchChatChangeSettings = 2;
    public int disableTwitchChatControlsSlowMode = 2;
    public bool isGlobalTwitchChatControlsSlowMode = false; // if global, nobody can send commands in {durationOfTwitchChatControlsSlowMode} seconds after last command
    public float durationOfTwitchChatControlsSlowMode = 5f; // in seconds

    public bool useBPMTransitions = true;
    public int transitionOnNthBeat = 32;
    public int transitionOffsetInBeats = 0;
    public float transitionEveryNSeconds = 25f;
    public float transitionOffsetInSeconds = 0;

    public bool useBPMTransitionDurations = true;
    public float transitionDurationInBeats = 8f;
    public float transitionDurationInSeconds = 7f;

    public float shakeIntensity = 0.11f; // TODO: individual multipliers for different shake event sources
    public float shakeSpeed = 24f;
    public float shakeFadeMultipler = 0.9f; // in [0, 1) interval
    public int shakeOnNthCombo = 0; // 0 = don't use it
    public int shakeOnNthBeat = 0; // 0 = don't use it
    public int shakeOnNthMiss = 0;  // 0 = don't use it
    public int shakeOnNthBombHit = 1;  // 0 = don't use it
    
    public bool currentComboInfluencesMissShakes = true;
    public float currentComboShakeIntensityPower = 0.6f;         // makes shakes multiplied by
    public float currentComboShakeIntensityMultipler = 0.3f;     // currentComboShakeIntensityMultipler * CURRENT_COMBO^currentComboShakeIntensityPower

    public string foveServerIP = "127.0.0.1"; // DON'T FORGET TO INSTALL OBS PYTHON SCRIPT OR DISABLE FOV EFFECT!
    public int foveServerPort = 50734; // 50734 = FOVEA
    public int FOVEOnNthBeat = 4; // 0 = don't use it. TODO: use Beat Saber "Ring Of Squares" events to control FOV Effect
    public int FOVECurveType = 3; // 0 = instant switch, 1 = use logistic curve (in-out), 2 = use sin curve (in-out), 3 = use exponential decay (only "out" is smoothened)
    public float FOVEffectIntensity = 0.8f; // Should be positive, and generally it should be small, I think it'll behave weirdly on values greater than 3
    public float FOVEffectSpeed = 0.5f;

    public int tiltOnWallStuckCurveType = 2; // 0 = instant switch, 1 = use logistic curve (in-out), 2 = use sin curve (in-out), 3 = use exponential decay (only "out" is smoothened)
    public float tiltOnWallStuckIntensity = 0.2f; // 0 = don't use it
    public float tiltOnWallStuckSpeed = 5f;

    public bool fallOnFail = false; // not implemented yet
    public bool wooOnSRank = false; // not implemented yet
    public bool wooOnSSRank = false; // not implemented yet
}

// The class must implement IPluginCameraBehaviour to be recognized by LIV as a plugin.
public class NyanbreCam : IPluginCameraBehaviour {

    #region BORING STUFF

    // Store your settings localy so you can access them.
    NyanbreCamSettings _settings = new NyanbreCamSettings();
    // Path to plugin folder
    private string pluginPath;
    private string configFilename = "settings.json";
    private string debugFilename = "debug.txt";

    // Provide your own settings to store user defined settings .   
    public IPluginSettings settings => _settings;

    // Invoke ApplySettings event when you need to save your settings.
    // Do not invoke event every frame if possible.
    public event EventHandler ApplySettings;

    // ID is used for the camera behaviour identification when the behaviour is selected by the user.
    // It has to be unique so there are no plugin collisions.
    public string ID => "NyanbreCam";
    // Readable plugin name "Keep it short".
    public string name => "Nyanbre's BS FOVEAS";
    // Author name.
    public string author => "Nyanbre";
    // Plugin version.
    public string version => "0.1.0";
    
    // Localy store the camera helper provided by LIV.
    PluginCameraHelper _helper;

    #endregion
    
    /*
     * Mori's stuff for networking!
     */
    //websocket declaration for Beat Saber HTTP status
    private WebSocket ws;
    private string wsStatus = "closed";
    private float wsRetry = 5.0f;

    //websocket for Twitch
    private WebSocket tw;
    private string twStatus = "closed";
    private float twRetry = 5.0f;
    //Login info for twitch.
    private string twOath = "none";
    private string twName = "none";
    
    /*
     * Nyanbre's variables~
     */
    private int oldCameraLookIndex = 0; // CURRENT CAMERA POSITION
    [CanBeNull] private CameraLook oldCameraLookSaved; // CURRENT CAMERA POSITION, save for 90/360-map degree corrections
    private int targetCameraLookIndex = -1; // Target camera position during transition
    [CanBeNull] private CameraLook targetCameraLookSaved; // save for 90/360-map degree corrections
    private float transitionFraction = -1f; // from 0 to 1, is used for smoothing camera position transitions, something like Lerp
    
    private float foveFraction = -1f; // from 0 to 1, is used for smoothing FOVE transitions, something like Lerp
    private float oldFOVEMultipler = 1f; // FOVE multipler multiplies distance to CameraLook target point (moves camera away)
    private float targetFOVEMultipler = 1f;
    private float currentFOVEDistanceMultipler = 1f; // greater or equal to 1
    private TcpListener foveServer = null;


    private Quaternion smoothenedHeadDirection = Quaternion.identity; // for 90 and 360 maps
    private float smoothMultiplier = 0.2f;
    
    //// Current song variables:
    // Song constants
    private float songTimeOffset = 0f; // don't forget to convert from int mills to float seconds
    private float songBpm = 60f; // BS HTTP Status returns int
    private float timeBetweenBeats = 1f; // = 60f / songBPM;
    // Changed during map play
    private float elaspedTimeFromSongStart = 0f; // in seconds
    private float lastBeatTime = -1f; // updates every song beat
    private int songBeatCount = 0; // updates every song beat. Is used to "shake camera every Nth beat, switch position every Mth" etc.
    private int noteMissCount = 0;
    private int bombsHitCount = 0;
    private int currentCombo = 0;

    private float shakeFade = 0f;

    // Phase flags
    private bool isMapPlaying = false; // false = Beat Saber is not launched or a player is in menu 
    private bool isSongPaused = false;
    private bool is360Map = false;
    private bool is90Map = false;

    // One-frame events:
    //// During playing
    private bool isBeat = false;
    private bool isNoteCut = false;
    private bool isNoteMissed = false;
    private bool isBombCut = false;

    private bool isWallStuck = false;
    private float wallStuckSmoothFraction = 0f;

    private int shakeCause = 0; // 1 = nth beat, 2 = nth combo, 3 = nth miss, 4 = nth bomb...

    //// Song finished
    private bool isMapFailed = false;
    private bool isSSRank = false;
    private bool isSRank = false;
    
    // Constructor is called when plugin loads
    public NyanbreCam() { }

    private void LoadConfig(string filename)
    {
        bool isConfigLoadedFromFile = false;
        string configFile = Path.Combine(pluginPath, filename);
        
        //Check to see if the file exists
        if (File.Exists(configFile))
        {
            try
            {
                _settings = JsonConvert.DeserializeObject<NyanbreCamSettings>(File.ReadAllText(@configFile));
                debug("Loading config from file: SUCCESS");
                isConfigLoadedFromFile = true;
            }
            catch (Exception e)
            {
                debug("Loading config from file: FAILURE. Reason: " + e.Message);
                File.Delete(configFile);
                debug("Corrupted config file successfully deleted!");
            }
        }

        if (!isConfigLoadedFromFile)
        {
            _settings.cameraLookPositions.Clear();
            _settings.cameraLookTargets.Clear();
            
            List<float> defaultLookPosition = new List<float> {2.0f, 1.8f, -3f};
            List<float> defaultLookTarget = new List<float> {0f, 1.2f, 0.5f};
            _settings.cameraLookPositions.Add(defaultLookPosition);
            _settings.cameraLookTargets.Add(defaultLookTarget);

            List<float> omoteaCameraLookPosition = new List<float> {0.5f, 1.15f, -2.6f};
            List<float> omoteaCameraLookTarget = new List<float> {0f, 0.9f, 1.8f};
            _settings.cameraLookPositions.Add(omoteaCameraLookPosition);
            _settings.cameraLookTargets.Add(omoteaCameraLookTarget);

            List<float> shanaCameraLookPosition = new List<float> {0.4f, 1f, -1.9f};
            List<float> shanaCameraLookTarget = new List<float> {0.2f, 0.8f, 0.4f};
            _settings.cameraLookPositions.Add(shanaCameraLookPosition);
            _settings.cameraLookTargets.Add(shanaCameraLookTarget);

            List<float> chairo1CameraLookPosition = new List<float> {2f, 0.5f, -2f};
            List<float> chairo1CameraLookTarget = new List<float> {0.5f, 0.65f, 1.7f};
            _settings.cameraLookPositions.Add(chairo1CameraLookPosition);
            _settings.cameraLookTargets.Add(chairo1CameraLookTarget);

            List<float> chairo2CameraLookPosition = new List<float> {-1f, 1.2f, -2.4f};
            List<float> chairo2CameraLookTarget = new List<float> {-0.2f, 0.8f, 1f};
            _settings.cameraLookPositions.Add(chairo2CameraLookPosition);
            _settings.cameraLookTargets.Add(chairo2CameraLookTarget);

            File.WriteAllText(@configFile, JsonConvert.SerializeObject(_settings, Formatting.Indented));
            debug("Applying default config. Default config has been saved to file: " + configFile);
        }
        ApplySettings?.Invoke(this, EventArgs.Empty);
        debug("Current config is:\n" + JsonConvert.SerializeObject(_settings, Formatting.Indented));
        debug(_settings.cameraLookPositions.Count + " camera positions registered.");
    }
    
    // OnActivate function is called when your camera behaviour was selected by the user.
    // The pluginCameraHelper is provided to you to help you with Player/Camera related operations.
    public void OnActivate(PluginCameraHelper helper)
    { 
        // We locally save the helper that LIV sends
        _helper = helper;
        
        pluginPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            @"LIV\Plugins\CameraBehaviours\", 
            ID);
        Directory.CreateDirectory(pluginPath);
        
        debug(string.Format("___\n{0} by {1}\nversion: {2}\n___", name, author, version));

        LoadConfig(configFilename);
        debug("Config loaded!");

        ConfigureBSHTTPMessages();
        ConfigureTwitchMessages();
        Task.Factory.StartNew(() => StartFOVEServer());
    }
    
    private void ResetView(string debugMessage)
    {
        isMapPlaying = false;
        isSongPaused = false;
        
        // reset map flags and events
        elaspedTimeFromSongStart = 0f;
        lastBeatTime = -1f;
        songBeatCount = 0;
        noteMissCount = 0;
        currentCombo = 0;
        isWallStuck = false;
        
        is360Map = false;
        is90Map = false;
        isSongPaused = false;
        
        debug(debugMessage);
    }

    private void ConfigureBSHTTPMessages()
    {
        debug("ConfigureBSHTTPMessages!");
        ws = new WebSocket("ws://localhost:6557/socket");
        ws.OnOpen += (sender, e) => {
            debug("BS connection open");
            wsStatus = "open";
        };
        ws.OnMessage += (sender, e) =>
        {
            try
            {
                // Avoid deserializing every message. "e.Data.Contain()" checks are faster
//                debug("[BSHTTP]: " + e.Data);

                // Menu phase
                if (e.Data.Contains("scene\":\"Menu"))
                {
                    ResetView("Should be entering Menu Phase.");
                }

                // Song phase
                if (e.Data.Contains("scene\":\"Song"))
                {
                    debug("Should be entering Song Phase.");
                    BeatSaberHTTP.EventObject eventObject = JsonConvert.DeserializeObject<BeatSaberHTTP.EventObject>(e.Data);
                    isMapPlaying = true;
                    isSongPaused = false;

                    elaspedTimeFromSongStart = 0f;
                    songTimeOffset = eventObject.status.beatmap.songTimeOffset;
                    songBpm = eventObject.status.beatmap.songBPM;
                    timeBetweenBeats = 60f / songBpm;
                    lastBeatTime = songTimeOffset;
                    songBeatCount = 0;
                    noteMissCount = 0;
                    currentCombo = 0;
                    isWallStuck = false;

                    // 360 and 90 degree maps
                    if (eventObject.status.game.mode.Contains("360Degree"))
                    {
                        debug("Song is 360deg");
                        is360Map = true;
                        is90Map = false;
                    }
                    if (eventObject.status.game.mode.Contains("90Degree"))
                    {
                        debug("Song is 90deg");
                        is360Map = false;
                        is90Map = true;
                    }
                }
                
                if (e.Data.Contains("t\":\"noteCut\""))
                {
                    int indexOfComboField = e.Data.IndexOf("\"combo\":") + 8;
                    string comboTxt = e.Data.Substring(indexOfComboField, e.Data.Length - indexOfComboField);
                    comboTxt = comboTxt.Split(',')[0];

                    debug("Note cut! Combo: x" + comboTxt);

                    isNoteCut = true;
                    currentCombo = int.Parse(comboTxt);
                }
                
                if (e.Data.Contains("t\":\"noteMissed"))
                {
                    noteMissCount += 1;
                    isNoteMissed = true;
                    currentCombo = 0;
                    debug("Note miss!");
                }
                if (e.Data.Contains("t\":\"bombCut"))
                {
                    bombsHitCount += 1;
                    isBombCut = true;
                    currentCombo = 0;
                    debug("Bomb cut!");
                }
                if (e.Data.Contains("t\":\"obstacleEnter"))
                {
                    isWallStuck = true;
                    currentCombo = 0;
                    debug("Wall stuck.");
                }
                if (e.Data.Contains("t\":\"obstacleExit"))
                {
                    isWallStuck = false;
                    debug("Wall unstuck.");
                }
                 
                if (e.Data.Contains("t\":\"failed"))
                {
                    isMapFailed = true;
                }
                if (e.Data.Contains("t\":\"finished"))
                {
                    if (e.Data.Contains("rank\":\"SS"))
                    {
                        isSSRank = true;
                    }
                    else if (e.Data.Contains("rank\":\"S"))
                    {
                        isSRank = true;
                    }
                }
                
                if (e.Data.Contains("t\":\"pause"))
                {
                    isSongPaused = true;
                    debug("Song pause.");
                }
                if (e.Data.Contains("t\":\"resume"))
                {
                    isSongPaused = false;
                    debug("Song resume.");
                }
            }
            catch (Exception ex)
            {
                debug("ERROR: " + ex.Message);
            }
        };
        ws.OnClose += (sender, e) =>
        {
            debug("Beat Saber websocket closed because: " + e.Reason);
            debug("Retry timer is at " + wsRetry.ToString());
            debug("ws Status was " + wsStatus);
            wsStatus = "closed";
            wsRetry = 0.0f;
        };
        ws.OnError += (sender, e) => { debug("Beatsaber websocket error: " + e.Message); };
    }

    private void ConfigureTwitchMessages()
    {
            tw = new WebSocket("ws://irc-ws.chat.twitch.tv:80");
            tw.OnOpen += (sender, e) =>
            {
                twStatus = "open";
                debug("Twitch connection established");
                //send configuration info
                //Authentication
                tw.Send("PASS " + twOath);
                tw.Send("NICK " + twName);
                tw.Send("CAP REQ :twitch.tv/tags");
                tw.Send("CAP REQ :twitch.tv/commands");
                tw.Send("JOIN #" + twName);
            };
            tw.OnMessage += (sender, e) =>
            {
                debug("[TWITCH]: " + e.Data);

                if (e.Data.StartsWith("PING"))
                {
                    tw.Send("PONG :tmi.twitch.tv");
                }
                else
                {
//                    //Split into array.
//                    char[] delim = {' '};
//                    string[] str = e.Data.Split(delim, 5);
//                    foreach (string i in str)
//                    {
//                        //debug(i);
//                    }
//
//                    if (str[2] == "PRIVMSG")
//                    {
//                        //All the User-Command-type stuff goes here. 
//                        if (str[0].Contains("broadcaster/") || str[0].Contains("moderator/") || str[0].Contains("vip/"))
//                        {
//                            //Moderator-level protected twitch command stuff.
//                            //debug("Broadcaster or Moderator is detected");
//                            if (str[4].StartsWith(":!cam"))
//                            {
//                                //cam command. 
//                                //twitchMessage("Cam Command detected!");
//                                //debug("Should have posted a note to the chat");
//                                if (str[4].Contains("fp"))
//                                {
//                                    gctype = "fp";
//                                    twitchMessage("GameCam to FP Mode");
//                                    debug("GameCam to FP Mode");
//                                }
//                                else if (str[4].Contains("follow"))
//                                {
//                                    gctype = "follow";
//                                    twitchMessage("GameCam to Follow Mode");
//                                    debug("GameCam to Follow Mode");
//                                }
//                                else if (str[4].Contains("menu"))
//                                {
//                                    gctype = "menu";
//                                    twitchMessage("GameCam to Menu Mode");
//                                    debug("GameCam to Menu Mode");
//                                }
//                            }
//                        }
//                    }
                }
            };
            tw.OnError += (sender, e) => { tw.CloseAsync(); };
            tw.OnClose += (sender, e) => { twStatus = "closed"; };
    }

    private void ConnectionToBSHTTP(float deltaTime)
    {
        if (wsStatus == "closed")
        {
            wsRetry += deltaTime;
            if (wsRetry > 1.0f)
            {
                debug("Attempting to connect to BS");
                wsStatus = "open";
                wsRetry = 0.0f;
                ws.ConnectAsync();
            }
        }
    }
    
    private void ConnectionToTwitch(float deltaTime)
    {
        if (twOath != "none")
        {
            if (twStatus == "closed")
            {
                twRetry += deltaTime;
                if (twRetry > 5.0f)
                {
                    debug("Attemption to connect to Twitch IRC");
                    debug("oath is " + twOath);
                    twRetry = 0.0f;
                    twStatus = "open";
                    tw.ConnectAsync();
                }
            }
        }
    }

    // OnUpdate is called once every frame and it is used for moving with the camera so it can be smooth as the framerate.
    // When you are reading other transform positions during OnUpdate it could be possible that the position comes from a previus frame
    // and has not been updated yet. If that is a concern, it is recommended to use OnLateUpdate instead.
    public void OnUpdate()
    {
        try
        {
            ConnectionToBSHTTP(Time.deltaTime);
            ConnectionToTwitch(Time.deltaTime);

            if (isMapPlaying && !isSongPaused)
            {
                elaspedTimeFromSongStart += Time.deltaTime;
                if (transitionFraction >= 0f)
                {
                    transitionFraction += TransitionFractionChange(Time.deltaTime);
                }
            }

            // On transition end, target camera becomes old camera,
            // and we have no target camera anymore to transition to
            if (transitionFraction >= 1f)
            {
                debug("Transition end! From " + oldCameraLookIndex + " to " + targetCameraLookIndex);
                transitionFraction = -1f;
                oldCameraLookIndex = targetCameraLookIndex;
                oldCameraLookSaved = targetCameraLookSaved;
                targetCameraLookIndex = -1;
                targetCameraLookSaved = null;
            }

            if (oldCameraLookSaved == null) oldCameraLookSaved = GetCameraLook(oldCameraLookIndex);
            if (targetCameraLookSaved == null && targetCameraLookIndex != -1) targetCameraLookSaved = GetCameraLook(targetCameraLookIndex);
            CameraLook resultLook;
            // When we have a target camera to transition to,
            // calculate a camera look between old and target cameras
            if (targetCameraLookIndex != -1)
            {
                resultLook = CurrentTransitionPoint(
                    oldCameraLookSaved,
                    targetCameraLookSaved,
                    transitionFraction,
                    _settings.transitionCurveType
                );
            }
            else
            {
                resultLook = oldCameraLookSaved;
            }
            
            if (elaspedTimeFromSongStart > lastBeatTime + timeBetweenBeats)
            {
                lastBeatTime += timeBetweenBeats;
                songBeatCount += 1;
                isBeat = true;
                //                debug("Beat number " + songBeatCount);
            }

            if (isMapPlaying)
            {
                if (CheckFOVEffectCondition())
                {
                    UpdateFOVEffect();
                }

                if (_settings.shakeIntensity > 0f)
                {
                    if (CheckShakeConditions())
                    {
                        shakeFade = 1;
                        debug("Shake!");
                    }
                }
            }
                
            if (useDegreeMapCorrections())
            {
                Quaternion headTransformRotation = _helper.playerHead.rotation;
                var angularVelocity = Quaternion.Angle(smoothenedHeadDirection, headTransformRotation) / Time.unscaledDeltaTime;
                smoothenedHeadDirection = Quaternion.Slerp(smoothenedHeadDirection, headTransformRotation, angularVelocity * smoothMultiplier);
            }

            // use transitions only when we have at least 2 different CameraLooks, and when there's no transition currently
            if (_settings.cameraLookPositions.Count > 1 && targetCameraLookIndex == -1 && CheckTransitionConditions())
            {
                UpdateTargetCameraLookIndex();
                transitionFraction = 0f;
                debug("Transition start! From " + oldCameraLookIndex + " to " + targetCameraLookIndex);
            }

            Vector3 effectUpwardVector = Vector3.up;

            if (_settings.tiltOnWallStuckIntensity > 0f)
            {
                wallStuckSmoothFraction += (isWallStuck ? 1 : -1) * _settings.tiltOnWallStuckSpeed * Time.deltaTime;
                if (wallStuckSmoothFraction > 1f)
                {
                    wallStuckSmoothFraction = 1f;
                }
                else if (wallStuckSmoothFraction < 0f)
                {
                    wallStuckSmoothFraction = 0f;
                }

                effectUpwardVector += _settings.tiltOnWallStuckIntensity * (1 - CurvedFraction(_settings.tiltOnWallStuckCurveType, wallStuckSmoothFraction)) * Vector3.right;
            }

            if (_settings.shakeIntensity > 0f)
            {
                effectUpwardVector += ApplyShake();
            }

            Quaternion resultRotation = Quaternion.LookRotation(resultLook.Forward, effectUpwardVector);
            Vector3 resultPosition = ApplyFOVEffect(resultLook);
            _helper.UpdateCameraPose(resultPosition, resultRotation);
            _helper.UpdateFov(_settings.fov);
            ResetOnetimeEvents();
        }
        catch (Exception ex)
        {
            debug("ONUPDATE ERROR: " + ex.Message);
        }
    }

    private bool useDegreeMapCorrections()
    {
        return (is90Map && _settings.use90Corrections) || (is360Map && _settings.use360Corrections);
    }

    private void ResetOnetimeEvents()
    {
        isNoteCut = false;
        isBeat = false;
        isBombCut = false;
        isNoteMissed = false;
    }

    private float TransitionFractionChange(float deltaTime)
    {
        if (_settings.useBPMTransitionDurations)
        {
            float timeInBeats = deltaTime / timeBetweenBeats;
            return timeInBeats / _settings.transitionDurationInBeats;
        }
        else
        {
            return deltaTime / _settings.transitionDurationInSeconds;
        }
    }

    private bool CheckTransitionConditions()
    {
        // if we don't use menu camera and we just started a song, make transition
        if (isMapPlaying && _settings.useMenuLookWhilePlaying && oldCameraLookIndex == 0 && targetCameraLookIndex == -1)
        {
            return true;
        }
        // make transition to menu camera when song ends (if current camera is not the Menu CameraLook)
        if (!isMapPlaying && (targetCameraLookIndex == 0 || (targetCameraLookIndex == -1 && oldCameraLookIndex != 0)))
        {
            return true;
        }
        
        // transition every Nth song beat
        if (_settings.useBPMTransitions)
        {
            return isBeat && ((songBeatCount - _settings.transitionOffsetInBeats) % _settings.transitionOnNthBeat == 0);
        }
        // transition every Nth second
        else
        {
            // It'll be better (performance-wise) to make variable "lastTransitionTime"
            // instead of doing mod-division every frame, but oh well... 
            return (elaspedTimeFromSongStart + _settings.transitionOffsetInSeconds) % _settings.transitionEveryNSeconds
                   < (elaspedTimeFromSongStart + _settings.transitionOffsetInSeconds - Time.deltaTime) % _settings.transitionEveryNSeconds;
        }
    }

    private bool CheckShakeConditions()
    {
        // 1 = nth beat, 2 = nth combo, 3 = nth miss, 4 = nth bomb...
        if (isBeat && _settings.shakeOnNthBeat != 0 && (songBeatCount % _settings.shakeOnNthBeat == 0))
        {
            shakeCause = 1;
            return true;
        }
        if (isNoteCut && _settings.shakeOnNthCombo != 0 && (currentCombo % _settings.shakeOnNthCombo == 0))
        {
            shakeCause = 2;
            return true;
        }
        if (isNoteMissed && _settings.shakeOnNthMiss != 0 && (noteMissCount % _settings.shakeOnNthMiss == 0))
        {
            shakeCause = 3;
            return true;
        }
        if (isBombCut && _settings.shakeOnNthBombHit != 0 && (bombsHitCount % _settings.shakeOnNthBombHit == 0))
        {
            shakeCause = 4;
            return true;
        }
        shakeCause = 0;
        return false;
    }

    private void UpdateTargetCameraLookIndex()
    {
        if (isMapPlaying)
        {
            if (_settings.randomNewLookPosition)
            {
                while ((_settings.useMenuLookWhilePlaying || targetCameraLookIndex != 0) && (targetCameraLookIndex == -1 || targetCameraLookIndex == oldCameraLookIndex))
                {
                    targetCameraLookIndex = Random.Range(0, _settings.cameraLookPositions.Count - 1); // Unity's Random.Range is max-inclusive!!!
                }
            }
            else
            {
                targetCameraLookIndex = (oldCameraLookIndex + 1) % _settings.cameraLookPositions.Count;
                if (!_settings.useMenuLookWhilePlaying && targetCameraLookIndex == 0)
                {
                    targetCameraLookIndex += 1;
                }
            }
        }
        else
        {
            targetCameraLookIndex = 0;
            targetFOVEMultipler = 1f;
        }
    }

    private CameraLook CurrentTransitionPoint(CameraLook current, CameraLook target, float fraction, int transitionMode)
    {
        CameraLook result = new CameraLook();
        float curvedFraction = CurvedFraction(transitionMode, fraction);
        result.Pos = current.Pos * curvedFraction + target.Pos * (1 - curvedFraction);
        result.Target = current.Target * curvedFraction + target.Target * (1 - curvedFraction);
        return result;
    }
    
    private float CurvedBlend(float from, float to, int mode, float fraction)
    {
        float curvedFraction = CurvedFraction(mode, fraction);
        return from * curvedFraction + to * (1 - curvedFraction);
    }

    // 0 = instant switch, 1 = use logistic curve (in-out), 2 = use sin curve (in-out), 3 = use exponential decay (only "out" is smoothened)
    private float CurvedFraction(int mode, float fraction)
    {
        switch (mode)
        {
            case 1: return LogisticCurve(fraction);
            case 2: return SinCurve(fraction);
            case 3: return ExpCurve(fraction);
            default: return fraction > 0 ? 1 : 0;
        }
    }

    private float LogisticCurve(float fraction)
    {
        return 1/(1 + Mathf.Exp(24 * fraction - 12));
    }
    
    private float SinCurve(float fraction)
    {
        return 0.5f + Mathf.Cos(Mathf.PI * fraction) / 2;
    }

    private float ExpCurve(float fraction)
    {
        return Mathf.Exp(-8 * fraction);
    }

    private Vector3 ApplyShake()
    {
        float intensity = _settings.shakeIntensity;
        float shakePhase = Mathf.Sin(elaspedTimeFromSongStart * _settings.shakeSpeed);
        if (shakeCause == 3 && _settings.currentComboInfluencesMissShakes)
        {
            intensity *= _settings.currentComboShakeIntensityMultipler 
                         * Mathf.Pow(currentCombo, _settings.currentComboShakeIntensityPower);
        }
        
        return new Vector3(intensity * shakeFade * shakePhase, 0, 0);
    }
    
    private bool CheckFOVEffectCondition()
    {
        return isBeat && (songBeatCount % _settings.FOVEOnNthBeat == 0);
    }
    
    private void UpdateFOVEffect()
    {
        oldFOVEMultipler = currentFOVEDistanceMultipler;
        targetFOVEMultipler = Random.Range(1f, 1f + _settings.FOVEffectIntensity);
        foveFraction = 0f;
        debug("fove started, target = " + targetFOVEMultipler);
    }

    private Vector3 ApplyFOVEffect(CameraLook cameraLook)
    {
        Vector3 result = cameraLook.Pos;
        
        // when fraction reaches 1, stop FOVEffect transition (set foveFraction = -1)
        if (foveFraction > 1f)
        {
            foveFraction = -1f;
            oldFOVEMultipler = targetFOVEMultipler;
            currentFOVEDistanceMultipler = targetFOVEMultipler;
            targetFOVEMultipler = -1f;

            debug("fove stopped, current distance multipler = " + currentFOVEDistanceMultipler);
        }
        // make transition (change currentFOVEDistanceMultipler) only when (foveFraction != -1) 
        else if (foveFraction >= 0f)
        {
            foveFraction += Time.deltaTime * _settings.FOVEffectSpeed;
        
            currentFOVEDistanceMultipler = CurvedBlend(oldFOVEMultipler, targetFOVEMultipler, _settings.FOVECurveType, foveFraction);
        }
        result -= (currentFOVEDistanceMultipler - 1) * cameraLook.Distance * cameraLook.Forward;
        return result;
    }
    
    private void StartFOVEServer()
    {
        try
        {
            debug("[FOVEServer]: Starting...");
            IPAddress localAddr = IPAddress.Parse(_settings.foveServerIP);
            foveServer = new TcpListener(localAddr, _settings.foveServerPort);
 
            // start listener
            foveServer.Start();
 
            while (true)
            {
//                debug("[FOVEServer]: Awaiting for connections...");
 
                // recieve incoming connection
                TcpClient client = foveServer.AcceptTcpClient();
//                debug("[FOVEServer]: A client has connected.");
 
                // recieve network stream for read/write operations
                NetworkStream stream = client.GetStream();
 
                string response = FormatNumDigits(currentFOVEDistanceMultipler, 8).Substring(1);
                // encode response as a byte array
                byte[] data = Encoding.UTF8.GetBytes(response);
 
                stream.Write(data, 0, data.Length);
//                debug("[FOVEServer]: response: " + response);

                stream.Close();
                client.Close();
            }
        }
        catch (Exception e)
        {
            debug("[FOVEServer]: ERROR: " + e.Message);
//            _settings.foveServerPort += 1;
        }
        finally
        {
            debug("[FOVEServer]: Stopping.");
            foveServer?.Stop();
        }
    }
    
    // Copied from stackoverflow 'cause why not~
    // https://stackoverflow.com/questions/11789194/string-format-how-can-i-format-to-x-digits-regardless-of-decimal-place
    public string FormatNumDigits(float number, int x) {
        string asString = (number >= 0? "+":"") + number.ToString("F50",System.Globalization.CultureInfo.InvariantCulture);

        if (asString.Contains('.')) {
            if (asString.Length > x + 2) {
                return asString.Substring(0, x + 2);
            } else {
                // Pad with zeros
                return asString.Insert(asString.Length, new String('0', x + 2 - asString.Length));
            }
        } else {
            if (asString.Length > x + 1) {
                return asString.Substring(0, x + 1);
            } else {
                // Pad with zeros
                return asString.Insert(1, new String('0', x + 1 - asString.Length));
            }
        }
    }
    
    private Vector3 ListToVector3(List<float> list)
    {
        return new Vector3(list[0], list[1], list[2]);
    }

    private CameraLook GetCameraLook(int index)
    {
        CameraLook result = new CameraLook();
        result.Pos = ListToVector3(_settings.cameraLookPositions[index]);
        result.Target = ListToVector3(_settings.cameraLookTargets[index]);

        if (useDegreeMapCorrections())
        {
            result.Pos = smoothenedHeadDirection * result.Pos;
            result.Target = smoothenedHeadDirection * result.Target;
        }
        return result;
    }

    private void debug(string str)
    {
        string target = Path.Combine(pluginPath, debugFilename);
        using (var sw = new StreamWriter(target, true))
        {
            sw.WriteLine(str);
        }
    }
    
    // OnFixedUpdate could be called several times per frame. 
    // The delta time is constant and it is meant to be used on robust physics simulations.
    public void OnFixedUpdate() 
    {
        if (shakeFade > 0.01f)
        {
            shakeFade *= _settings.shakeFadeMultipler;
        }
        else
        {
            shakeFade = 0f;
        }
    }
    
    // OnDeactivate is called when the user changes the profile to other camera behaviour or when the application is about to close.
    // The camera behaviour should clean everything it created when the behaviour is deactivated.
    public void OnDeactivate() {
        ws.CloseAsync();
        tw.CloseAsync();
    }

    // OnDestroy is called when the users selects a camera behaviour which is not a plugin or when the application is about to close.
    // This is the last chance to clean after your self.
    public void OnDestroy() {
        ws.CloseAsync();
        tw.CloseAsync();
        ws.Close();
        tw.Close();
    }

    public void OnSettingsDeserialized() { }
    public void OnLateUpdate() { }
}

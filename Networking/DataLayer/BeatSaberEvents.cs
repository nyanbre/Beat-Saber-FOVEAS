using JetBrains.Annotations;
using Newtonsoft.Json;

namespace NyanbreFOVEAS.Networking.DataLayer
{
    public class EventObject
    {
        [JsonProperty("event")]
        public string
            Event; // hello | songStart | finished | failed | menu | pause | resume | noteCut | noteFullyCut | noteMissed | bombCut | bombMissed | obstacleEnter | obstacleExit | scoreChanged | beatmapEvent

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

                [CanBeNull]
                public string
                    mode; // null | "SoloStandard" | "SoloOneSaber" | "SoloNoArrows" | "PartyStandard" | "PartyOneSaber" | "PartyNoArrows"
            }

            public class Beatmap
            {
                public string songName; // Song name
                public string songSubName; // Song sub name
                public string songAuthorName; // Song author name
                public string levelAuthorName; // Beatmap author name
                public string songCover; // Base64 encoded PNG image of the song cover

                public string
                    songHash; // Unique beatmap identifier. At most 32 characters long. Same for all difficulties.

                public float songBPM; // Song Beats Per Minute

                public float
                    noteJumpSpeed; // Song note jump movement speed, how fast the notes move towards the player.

                public int
                    songTimeOffset; // Time in millis of where in the song the beatmap starts. Adjusted for song speed multiplier.

                public long?
                    start; // UNIX timestamp in millis of when the map was started. Changes if the game is resumed. Might be altered by practice settings.

                public long?
                    paused; // If game is paused, UNIX timestamp in millis of when the map was paused. null otherwise.

                public int length; // Length of map in millis. Adjusted for song speed multiplier.
                public string difficulty; // Beatmap difficulty: "Easy" | "Normal" | "Hard" | "Expert" | "ExpertPlus"
                public int notesCount; // Map cube count
                public int bombsCount; // Map bomb count. Set even with No Bombs modifier enabled.
                public int obstaclesCount; // Map obstacle count. Set even with No Obstacles modifier enabled.
                public int maxScore; // Max score obtainable on the map with modifier multiplier

                public string
                    maxRank; // Max rank obtainable using current modifiers: "SSS" | "SS" | "S" | "A" | "B" | "C" | "D" | "E"

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

                public int?
                    batteryEnergy; // Current amount of battery lives left. null if Battery Energy and Insta Fail are disabled.
            }

            public class Mod
            {
                public float multiplier; // Current score multiplier for gameplay modifiers

                public object
                    obstacles; // No Obstacles (FullHeightOnly is not possible from UI): false | "FullHeightOnly" | "All"

                public bool instaFail; // Insta Fail
                public bool noFail; // No Fail
                public bool batteryEnergy; // Battery Energy

                public int?
                    batteryLives; // Amount of battery energy available. 4 with Battery Energy, 1 with Insta Fail, null with neither enabled.

                public bool disappearingArrows; // Disappearing Arrows
                public bool noBombs; // No Bombs
                public string songSpeed; // Song Speed (Slower = 85%, Faster = 120%): "Normal" | "Slower" | "Faster"
                public float songSpeedMultiplier; // Song speed multiplier. Might be altered by practice settings.
                public bool noArrows; // No Arrows
                public bool ghostNotes; // Ghost Notes
                public bool failOnSaberClash; // Fail on Saber Clash (Hidden)

                public bool
                    strictAngles; // Strict Angles (Hidden. Requires more precise cut direction; changes max deviation from 60deg to 15deg)

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

            public string
                noteCutDirection; // Direction the note is supposed to be cut in: "Up" | "Down" | "Left" | "Right" | "UpLeft" | "UpRight" | "DownLeft" | "DownRight" | "Any" | "None"

            public int noteLine; // The horizontal position of the note, from left to right [0..3]
            public int noteLayer; // The vertical position of the note, from bottom to top [0..2]
            public bool speedOK; // Cut speed was fast enough
            public bool? directionOK; // Note was cut in the correct direction. null for bombs.
            public bool? saberTypeOK; // Note was cut with the correct saber. null for bombs.
            public bool wasCutTooSoon; // Note was cut too early

            public int?
                initalScore; // Score without multipliers for the cut. Doesn't include the score for swinging after cut. null for bombs.

            public int?
                finalScore; // Score without multipliers for the entire cut, including score for swinging after cut. Available in [`noteFullyCut` event](#notefullycut-event). null for bombs.

            public int multiplier; // Combo multiplier at the time of cut
            public float saberSpeed; // Speed of the saber when the note was cut
            public float[] saberDir;
            public string saberType; // Saber used to cut this note: "SaberA" | "SaberB"

            public float?
                swingRating; // Game's swing rating. Uses the before cut rating in noteCut events and after cut rating for noteFullyCut events. -1 for bombs.

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
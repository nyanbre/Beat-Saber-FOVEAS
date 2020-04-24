using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NyanbreFOVEAS.Networking.DataLayer;
using WebSocketSharp;

namespace NyanbreFOVEAS.Networking
{
    public class BeatSaberHttpHandler
    {
        /*
         * Mori's stuff for networking!
         */
        //websocket declaration for Beat Saber HTTP status
        private WebSocket ws;
        private string wsStatus = "closed";
        private float wsRetry = 5.0f;
        
        public void SetupEvents(FoveasActions actions)
        {
            Logger.info("ConfigureBSHTTPMessages!");
            ws = new WebSocket("ws://localhost:6557/socket");
            ws.OnOpen += (sender, e) => {
                Logger.info("BS connection open");
                wsStatus = "open";
            };
            ws.OnMessage += (sender, e) =>
            {
                try
                {
                    bool comboUpdated = false;

                    // Avoid deserializing every message. "e.Data.Contain()" checks are faster
//                    NyanbreFOVEAS.Logger.info("[BSHTTP]: " + e.Data);

                    // Menu phase
                    if (e.Data.Contains("scene\":\"Menu"))
                    {
                        actions.MenuStart();
                    }
                    else if (e.Data.Contains("beatmapEvent"))
                    {
                        // type 9 is RINGS_ZOOM event
                        if (e.Data.Contains("\"type\":9"))
                        {
                            actions.RingsZoom();
                        }
                    }
                    else if (e.Data.Contains("scene\":\"Song"))
                    {
                        // Song phase
                        Logger.info("Should be entering Song Phase.");
                        EventObject eventObject = JsonConvert.DeserializeObject<EventObject>(e.Data);

                        Dictionary<string, object> songInfo = new Dictionary<string, object>();

                        float songSpeedMultiplier = 1f;
                        if (eventObject.status.mod.songSpeedMultiplier != null)
                        {
                            songSpeedMultiplier = eventObject.status.mod.songSpeedMultiplier;
                        }
                        switch (eventObject.status.mod.songSpeed.ToLower())
                        {
                            case "slower":
                                songSpeedMultiplier *= FoveasConstStorage.slowerSongSpeedMultiplier;
                                break;
                            case "faster":
                                songSpeedMultiplier *= FoveasConstStorage.fasterSongSpeedMultiplier;
                                break;
                        }
                        
                        songInfo.Add("songTimeOffset", eventObject.status.beatmap.songTimeOffset); // TODO: find out HOW it's "adjusted"
//                        songInfo.Add("songTimeOffset", eventObject.status.beatmap.songTimeOffset / songSpeedMultiplier);
                        songInfo.Add("songBPM", eventObject.status.beatmap.songBPM * songSpeedMultiplier);
                        songInfo.Add("is360Map", eventObject.status.game.mode.Contains("360Degree"));
                        songInfo.Add("is90Map", eventObject.status.game.mode.Contains("90Degree"));
                        actions.MapStart(songInfo);
                    }
                    else if (e.Data.Contains("t\":\"noteCut\""))
                    {
                        Logger.info("Note hit!");
                        actions.HitNote();
                        comboUpdated = true;
                    }
                    else if (e.Data.Contains("t\":\"noteMissed"))
                    {
                        Logger.info("Note miss!");
                        actions.MissNote();
                        comboUpdated = true;
                    }
                    else if (e.Data.Contains("t\":\"bombCut"))
                    {
                        Logger.info("Bomb hit!");
                        actions.HitBomb();
                        comboUpdated = true;
                    }
                    else if (e.Data.Contains("t\":\"obstacleEnter"))
                    {
                        Logger.info("Wall stuck!");
                        actions.WallStuck();
                        comboUpdated = true;
                    }
                    else if (e.Data.Contains("t\":\"obstacleExit"))
                    {
                        Logger.info("Wall unstuck.");
                        actions.WallUnstuck();
                    }
                    else if (e.Data.Contains("t\":\"failed"))
                    {
                        actions.MapFinish(MapFinishRank.MENU_EXIT);
                    }
                    else if (e.Data.Contains("t\":\"finished"))
                    {
                        string extractFeatureStart = "rank\":\"";
                        string extractFeatureEnd = "\"";
                        string rankStr = extractStringBetween(e.Data, extractFeatureStart, extractFeatureEnd);
                        Logger.info("Result rank: \"" + rankStr + "\"");

                        if (Enum.TryParse(rankStr, true, out MapFinishRank rank))
                        {
                            Logger.info("Result rank understood as: \"" + rank.ToString() + "\"");
                            actions.MapFinish(rank);
                        }
                        else
                        {
                            Logger.info("Result rank wasn't understood.");
                            actions.MapFinish(MapFinishRank.MENU_EXIT);
                        }
                    }
                    else if (e.Data.Contains("t\":\"pause"))
                    {
                        actions.MapPause();
                        Logger.info("Song pause.");
                    }
                    else if (e.Data.Contains("t\":\"resume"))
                    {
                        actions.MapResume();
                        Logger.info("Song resume.");
                    }
                    
                    if (comboUpdated)
                    {
                        Logger.info("Combo updated:");
                        string extractFeatureStart = "\"combo\":";
                        string extractFeatureEnd = ",";
                        string comboTxt = extractStringBetween(e.Data, extractFeatureStart, extractFeatureEnd);

                        Logger.info("combo x" + comboTxt);

                        actions.ComboUpdate(uint.Parse(comboTxt));
                    }
                }
                catch (Exception ex)
                {
                    Logger.info("ERROR: " + ex.Message);
                    Logger.info(ex.ToString());
                }
            };
            ws.OnClose += (sender, e) =>
            {
                Logger.info("Beat Saber websocket closed because: " + e.Reason);
                Logger.info("Retry timer is at " + wsRetry.ToString());
                Logger.info("ws Status was " + wsStatus);
                wsStatus = "closed";
                wsRetry = 0.0f;
            };
            ws.OnError += (sender, e) => { Logger.info("Beatsaber websocket error: " + e.Message); };
            actions.DeltaTime += (deltaSeconds, isMapPlaying) => EnsureConnect(deltaSeconds);
        }

        private static string extractStringBetween(string content, string extractFeatureStart, string extractFeatureEnd)
        {
            string result = content.Substring(extractFeatureStart.Length + content.IndexOf(extractFeatureStart, StringComparison.Ordinal));
            result = result.Substring(0, result.IndexOf(extractFeatureEnd, StringComparison.Ordinal));
            return result;
        }

        public void EnsureConnect(float deltaTime)
        {
            if (wsStatus == "closed")
            {
                wsRetry += deltaTime;
                if (wsRetry > 1.0f)
                {
                    Logger.info("Attempting to connect to BS");
                    wsStatus = "open";
                    wsRetry = 0.0f;
                    ws.ConnectAsync();
                }
            }
        }

        public void Close()
        {
            ws.Close();
            ws.CloseAsync();
        }
    }
}
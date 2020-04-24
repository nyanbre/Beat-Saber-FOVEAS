using WebSocketSharp;

namespace NyanbreFOVEAS.Networking
{
    public class TwitchHandler
    {
        /*
         * Mori's stuff for networking!
         */
        //websocket for Twitch
        private WebSocket tw;
        private string twStatus = "closed";

        private float twRetry = 5.0f;

        //Login info for twitch.
        private string twOath = "none";
        private string twName = "none";

        public void SetupEvents(FoveasActions actions)
        {
                tw = new WebSocket("ws://irc-ws.chat.twitch.tv:80");
                tw.OnOpen += (sender, e) =>
                {
                    twStatus = "open";
                    NyanbreFOVEAS.Logger.info("Twitch connection established");
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
                    NyanbreFOVEAS.Logger.info("[TWITCH]: " + e.Data);

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
    //                        //NyanbreFOVEAS.Logger.info(i);
    //                    }
    //
    //                    if (str[2] == "PRIVMSG")
    //                    {
    //                        //All the User-Command-type stuff goes here. 
    //                        if (str[0].Contains("broadcaster/") || str[0].Contains("moderator/") || str[0].Contains("vip/"))
    //                        {
    //                            //Moderator-level protected twitch command stuff.
    //                            //NyanbreFOVEAS.Logger.info("Broadcaster or Moderator is detected");
    //                            if (str[4].StartsWith(":!cam"))
    //                            {
    //                                //cam command. 
    //                                //twitchMessage("Cam Command detected!");
    //                                //NyanbreFOVEAS.Logger.info("Should have posted a note to the chat");
    //                                if (str[4].Contains("fp"))
    //                                {
    //                                    gctype = "fp";
    //                                    twitchMessage("GameCam to FP Mode");
    //                                    NyanbreFOVEAS.Logger.info("GameCam to FP Mode");
    //                                }
    //                                else if (str[4].Contains("follow"))
    //                                {
    //                                    gctype = "follow";
    //                                    twitchMessage("GameCam to Follow Mode");
    //                                    NyanbreFOVEAS.Logger.info("GameCam to Follow Mode");
    //                                }
    //                                else if (str[4].Contains("menu"))
    //                                {
    //                                    gctype = "menu";
    //                                    twitchMessage("GameCam to Menu Mode");
    //                                    NyanbreFOVEAS.Logger.info("GameCam to Menu Mode");
    //                                }
    //                            }
    //                        }
    //                    }
                    }
                };
                tw.OnError += (sender, e) => { tw.CloseAsync(); };
                tw.OnClose += (sender, e) => { twStatus = "closed"; };

                actions.DeltaTime += (deltaSeconds, isMapPlaying) => EnsureConnect(deltaSeconds);
        }
                
        public void EnsureConnect(float deltaTime)
        {
            if (twOath != "none")
            {
                if (twStatus == "closed")
                {
                    twRetry += deltaTime;
                    if (twRetry > 5.0f)
                    {
                        NyanbreFOVEAS.Logger.info("Attemption to connect to Twitch IRC");
                        NyanbreFOVEAS.Logger.info("oath is " + twOath);
                        twRetry = 0.0f;
                        twStatus = "open";
                        tw.ConnectAsync();
                    }
                }
            }
        }
        
        public void Close()
        {
            tw.Close();
            tw.CloseAsync();
        }
    }
}
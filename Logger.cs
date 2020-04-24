using System.IO;
using System.Linq;
using System.Text;

namespace NyanbreFOVEAS
{
    // Yeah-yeah, here I use "static" instead of a Singleton, why not (>_>)
    public class Logger
    {
        public static string path = null;
        public static bool logTime = true;
        public static int logLevel = 0;

        public static void trace(string content, string source = null) => log(content, 0, source);
        public static void debug(string content, string source = null) => log(content, 1, source);
        public static void info(string content, string source = null) => log(content, 2, source);
        public static void warning(string content, string source = null) => log(content, 3, source);
        public static void error(string content, string source = null) => log(content, 4, source);
        public static void severe(string content, string source = null) => log(content, 5, source);

        public static void log(string content, int level = 0, string source = null)
        {
            if (level >= logLevel)
            {
                StringBuilder builder = new StringBuilder();
                if (logTime)
                {
                    builder.Append("[");
                    builder.Append(System.DateTime.Now.ToString());
                    builder.Append("] \t");
                }

                if (source != null)
                {
                    builder.Append("(");
                    builder.Append(source);
                    builder.Append("): \t");
                }

                builder.Append(content);

                write(builder.ToString());
            }
        }
        
        private static void write(string str)
        {
            if (path != null)
            {
                using (var sw = new StreamWriter(path, true))
                {
                    sw.WriteLine(str);
                }
            }
        }

        public static void SetupLoggingActions(FoveasActions actions)
        {
            actions.MenuStart += () => debug("[invoke]", "actions.MenuStart");
//            actions.DeltaTime += (deltaSeconds, isMapPlaying) => debug(deltaSeconds.ToString(), "actions.DeltaTime");
//            actions.DeltaBeat += (deltaBeat, isMapPlaying) => debug(deltaBeat.ToString(), "actions.DeltaBeat");
            actions.MapStart += (mapInfo) => debug("{\n" + string.Join(",\n", mapInfo.Select(kv => kv.Key + ": " + kv.Value).ToArray()) + "\n}", "actions.MapStart");
            actions.MapPause += () => debug("[invoke]", "actions.MapPause");
            actions.MapResume += () => debug("[invoke]", "actions.MapResume");
            actions.MapFinish += (rank) => debug(rank.ToString(), "actions.MapFinish");
            actions.HitBomb += () => debug("[invoke]", "actions.HitBomb");
            actions.HitNote += () => debug("[invoke]", "actions.HitNote");
            actions.MissNote += () => debug("[invoke]", "actions.MissNote");
            actions.ComboBreak += (combo) => debug(combo.ToString(), "actions.ComboBreak");
            actions.ComboUpdate += (combo) => debug(combo.ToString(), "actions.ComboUpdate");
            actions.WallStuck += () => debug("[invoke]", "actions.WallStuck");
            actions.WallUnstuck += () => debug("[invoke]", "actions.WallUnstuck");
            actions.RingsZoom += () => debug("[invoke]", "actions.RingsZoom");
        }
    }
}
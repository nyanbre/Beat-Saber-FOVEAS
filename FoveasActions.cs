using System;
using System.Collections.Generic;

namespace NyanbreFOVEAS
{
    public class FoveasActions
    {
        public Action MenuStart { get; set; }
        public Action<float, bool> DeltaTime { get; set; }
        public Action<float, bool> DeltaBeat { get; set; }

        public Action<Dictionary<string, object>> MapStart { get; set; }
        public Action MapPause { get; set; }
        public Action MapResume { get; set; }
        public Action<MapFinishRank> MapFinish { get; set; } // -1 is none, 0 is exit, 1 is fail, 2..9 are E..SSS

        public Action HitBomb { get; set; }
        public Action HitNote { get; set; }
        public Action MissNote { get; set; }
        public Action<uint> ComboBreak { get; set; }
        public Action<uint> ComboUpdate { get; set; }
        public Action WallStuck { get; set; }
        public Action WallUnstuck { get; set; }
        public Action RingsZoom { get; set; }
        
//        public Action<int> CameraTransitionStart { get; set; }
//        public Action CameraTransitionEnd { get; set; }

        public FoveasActions()
        {
            this.MenuStart = delegate() {};
            this.DeltaTime = delegate(float deltaSecond, bool isMapPlaying) {};
            this.DeltaBeat = delegate(float deltaBeat, bool isMapPlaying) {};
            this.MapStart = delegate(Dictionary<string, object> mapInfo) {};
            this.MapPause = delegate() {};
            this.MapResume = delegate() {};
            this.MapFinish = delegate(MapFinishRank rank) {};
            this.HitBomb = delegate() {};
            this.HitNote = delegate() {};
            this.MissNote = delegate() {};
            this.ComboBreak = delegate(uint combo) {};
            this.ComboUpdate = delegate(uint combo) {};
            this.WallStuck = delegate() {};
            this.WallUnstuck = delegate() {};
            this.RingsZoom = delegate() {};
        }
    }
}
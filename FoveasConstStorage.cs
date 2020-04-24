using System.Collections.Generic;

namespace NyanbreFOVEAS
{
    public static class FoveasConstStorage
    {
        public const float slowerSongSpeedMultiplier = 0.85f;
        public const float fasterSongSpeedMultiplier = 1.20f;
        
//        // semantic normalization of intensity values
//        public static readonly Dictionary<CameraEffect.Type, float> typeIntensityConst = new Dictionary<CameraEffect.Type, float>
//        {
//            {CameraEffect.Type.DISABLE, 0},
//            {CameraEffect.Type.FOVE_TRANSITION, 1f},
//            {CameraEffect.Type.FOVE_PULSE, 0.15f},
//            {CameraEffect.Type.CAMERA_TILT_RETURN, 0.1f},
//            {CameraEffect.Type.CAMERA_SHAKE_TILT, 0.1f},
//            {CameraEffect.Type.CAMERA_SHAKE_ROTATE, 0.3f},
//            {CameraEffect.Type.CAMERA_SHAKE_HORIZONTALLY, 0.03f},
//            {CameraEffect.Type.CAMERA_SLIDE_HORIZONTALLY, 0.03f}
//        }; 
//        
//        public static readonly Dictionary<CameraEffect.Type, CameraEffect.TypeCategory> typeCategories = new Dictionary<CameraEffect.Type, CameraEffect.TypeCategory>
//        {
//            {CameraEffect.Type.DISABLE, CameraEffect.TypeCategory.DISABLE},
//            {CameraEffect.Type.FOVE_PULSE, CameraEffect.TypeCategory.JUMP},
//            {CameraEffect.Type.FOVE_TRANSITION, CameraEffect.TypeCategory.ONE_WAY},
//            {CameraEffect.Type.TILT_SLIDE, CameraEffect.TypeCategory.ONE_WAY},
//            {CameraEffect.Type.CAMERA_TILT_RETURN, CameraEffect.TypeCategory.JUMP},
//            {CameraEffect.Type.CAMERA_SHAKE_TILT, CameraEffect.TypeCategory.JUMP},
//            {CameraEffect.Type.CAMERA_SHAKE_ROTATE, CameraEffect.TypeCategory.JUMP},
//            {CameraEffect.Type.CAMERA_SHAKE_HORIZONTALLY, CameraEffect.TypeCategory.JUMP},
//            {CameraEffect.Type.CAMERA_SLIDE_HORIZONTALLY, CameraEffect.TypeCategory.ONE_WAY},
//        };
//
//        public static readonly Dictionary<CameraEffect.Type, bool> typeRandomness = new Dictionary<CameraEffect.Type, bool>
//        {
//            {CameraEffect.Type.DISABLE, false},
//            {CameraEffect.Type.FOVE_PULSE, false},
//            {CameraEffect.Type.FOVE_TRANSITION, true},
//            {CameraEffect.Type.TILT_SLIDE, true},
//            {CameraEffect.Type.CAMERA_TILT_RETURN, false},
//            {CameraEffect.Type.CAMERA_SHAKE_TILT, false},
//            {CameraEffect.Type.CAMERA_SHAKE_ROTATE, false},
//            {CameraEffect.Type.CAMERA_SHAKE_HORIZONTALLY, false},
//            {CameraEffect.Type.CAMERA_SLIDE_HORIZONTALLY, true},
//        };
//
//        public static readonly Dictionary<CameraEffect.Type, bool> typePeriodicity = new Dictionary<CameraEffect.Type, bool>
//        {
//            {CameraEffect.Type.DISABLE, false},
//            {CameraEffect.Type.FOVE_PULSE, false},
//            {CameraEffect.Type.FOVE_TRANSITION, false},
//            {CameraEffect.Type.TILT_SLIDE, false},
//            {CameraEffect.Type.CAMERA_TILT_RETURN, false},
//            {CameraEffect.Type.CAMERA_SHAKE_TILT, true},
//            {CameraEffect.Type.CAMERA_SHAKE_ROTATE, true},
//            {CameraEffect.Type.CAMERA_SHAKE_HORIZONTALLY, true},
//            {CameraEffect.Type.CAMERA_SLIDE_HORIZONTALLY, false},
//        };
//
        public static readonly Dictionary<CameraEffect.Type, float> typeDefaultValues = new Dictionary<CameraEffect.Type, float>
        {
            {CameraEffect.Type.DISABLE, 0f},
            {CameraEffect.Type.CAMERA_CHANGE, -3f},
            {CameraEffect.Type.FOVE, 1f},
            {CameraEffect.Type.ZOOM, 1f},
            {CameraEffect.Type.ROTATION_TILT, 0f},
            {CameraEffect.Type.ROTATION_HORIZONTAL, 0f},
            {CameraEffect.Type.ROTATION_VERTICAL, 0f},
            {CameraEffect.Type.POSITION_FORWARD, 0f},
            {CameraEffect.Type.POSITION_HORIZONTAL, 0f},
            {CameraEffect.Type.POSITION_VERTICAL, 0f},
        };
    }
}
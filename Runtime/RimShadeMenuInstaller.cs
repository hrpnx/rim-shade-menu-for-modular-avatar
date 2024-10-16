using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;

namespace dev.hrpnx.rim_shade_menu_for_modular_avatar.runtime
{
    public class RimShadeMenuInstaller : MonoBehaviour, IEditorOnly
    {
        public bool Default;
        public bool Saved;
        public List<Material> ColorExclusions;
        public Color Color;
        [CustomLabel("ノーマルマップ強度"), Range(0, 1)]
        public float NormalStrength;
        [CustomLabel("範囲"), Range(0, 1)]
        public float Border;
        [CustomLabel("ぼかし"), Range(0, 1)]
        public float Blur;
        [CustomLabel("リムライトの細さ"), Range(0, 50)]
        public float FresnelPower;
        
        public VRCExpressionsMenu RootMenu;
        public bool AnimationWriteDefault = false;
    }
}

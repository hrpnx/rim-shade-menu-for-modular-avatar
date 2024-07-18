using UnityEngine;
using VRC.SDKBase;

namespace dev.hrpnx.rim_shade_menu_for_modular_avatar.runtime
{
    public class Installer : MonoBehaviour, IEditorOnly
    {
        public Color Color;
        public float NormalStrength;
        public float Border;
        public float Blur;
        public float FresnelPower;
        public bool Saved;
    }
}

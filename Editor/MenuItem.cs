using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace dev.hrpnx.rim_shade_menu_installer.plugin
{
    public class RimShadeMenuInstaller : MonoBehaviour, IEditorOnly
    {
        public Color Color;
        public float NormalStrength;
        public float Border;
        public float Blur;
        public float FresnelPower;
    }

    public class MenuItem : MonoBehaviour
    {
        [UnityEditor.MenuItem("GameObject/Create RimShadeMenuInstaller", false, 0)]
        public static void Create()
        {
            var avatarRoot = Selection.activeGameObject;
            // TODO: Validate that avatarRoot is indeed the avatar root game object

            var menuInstallerName = "RimShadeMenuInstaller";
            var oldMenuInstaller = GameObject.Find(menuInstallerName);
            DestroyImmediate(oldMenuInstaller);

            var menuInstaller = new GameObject(menuInstallerName);
            menuInstaller.transform.SetParent(avatarRoot.transform);
            var component = menuInstaller.AddComponent<RimShadeMenuInstaller>();
            component.Color = new Color(0.5f, 0.5f, 0.5f, 1);
            component.NormalStrength = 1.0f;
            component.Border = 0.5f;
            component.Blur = 1.0f;
            component.FresnelPower = 1.0f;
        }
    }
}

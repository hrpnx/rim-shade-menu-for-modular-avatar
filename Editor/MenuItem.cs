using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace dev.hrpnx.rim_shade_menu_installer.plugin
{
    public class MenuItem : MonoBehaviour, IEditorOnly
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
            menuInstaller.AddComponent<RimShadeMenuInstaller>();
        }
    }
}

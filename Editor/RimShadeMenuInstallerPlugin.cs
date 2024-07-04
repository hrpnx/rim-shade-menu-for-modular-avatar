using dev.hrpnx.rim_shade_menu_installer.plugin;
using nadena.dev.ndmf;
using UnityEngine;

[assembly: ExportsPlugin(typeof(RimShadeMenuInstallerPlugin))]

namespace dev.hrpnx.rim_shade_menu_installer.plugin
{
    public class RimShadeMenuInstallerPlugin : Plugin<RimShadeMenuInstallerPlugin>
    {
        // TODO: override QualifiedName and DisplayName

        protected override void Configure() => this
            .InPhase(BuildPhase.Transforming)
            .Run("Install RimShadeMenu", ctx =>
            {
                var menuInstaller = ctx.AvatarRootObject.GetComponentInChildren<RimShadeMenuInstaller>();
                var avatarRoot = menuInstaller.gameObject.transform.parent.gameObject;
                Debug.Log($"hello {avatarRoot.name}!");
            }
        );
    }
}

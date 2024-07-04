using System.Collections.Generic;
using System.IO;
using ExpressionControl = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using dev.hrpnx.rim_shade_menu_installer.plugin;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using System;
using nadena.dev.modular_avatar.core;
using UnityEngine.SceneManagement;

[assembly: ExportsPlugin(typeof(RimShadeMenuInstallerPlugin))]

namespace dev.hrpnx.rim_shade_menu_installer.plugin
{
    public class RimShadeMenuInstallerPlugin : Plugin<RimShadeMenuInstallerPlugin>
    {
        // TODO: override QualifiedName and DisplayName

        protected override void Configure() => this
            .InPhase(BuildPhase.Transforming)
            .BeforePlugin("nadena.dev.modular-avatar")
            .Run("Install RimShadeMenu", ctx =>
            {
                var menuInstaller = ctx.AvatarRootObject.GetComponentInChildren<RimShadeMenuInstaller>();
                var avatarRoot = menuInstaller.gameObject.transform.parent.gameObject;

                if (!avatarRoot.GetComponent<VRCAvatarDescriptor>())
                {
                    EditorUtility.DisplayDialog("Error", "Avatar root does not have VRCAvatarDescriptor component.", "OK");
                    return;
                }

                this.CreateMenu(avatarRoot, menuInstaller);
            }
        );

        private void CreateMenu(GameObject avatarRoot, RimShadeMenuInstaller menuInstaller)
        {
            List<Renderer> renderers = new();
            this.CollectRenderersRecursive(avatarRoot.transform, renderers);

            var destDir = Path.Combine(
                "Assets",
                "Editor",
                "RimShadeMenuInstaller",
                "Generated",
                SceneManager.GetActiveScene().name,
                avatarRoot.name
            );

            if (Directory.Exists(destDir))
            {
                Directory.Delete(destDir, true);
            }

            Directory.CreateDirectory(destDir);
            AssetDatabase.Refresh();

            var baseName = "RimShadeMenu";

            // create animation clip (on)
            var destAnimClipOnFilePath = Path.Combine(destDir, $"{baseName}_On.anim");
            var animOnClip = new AnimationClip();

            foreach (var renderer in renderers)
            {
                var transform = renderer.gameObject.transform;
                this.AddAnimation(transform, avatarRoot.transform, animOnClip, "material._UseRimShade", 1);
                this.AddAnimation(transform, avatarRoot.transform, animOnClip, "material._RimShadeColor.r", menuInstaller.Color.r);
                this.AddAnimation(transform, avatarRoot.transform, animOnClip, "material._RimShadeColor.g", menuInstaller.Color.g);
                this.AddAnimation(transform, avatarRoot.transform, animOnClip, "material._RimShadeColor.b", menuInstaller.Color.b);
                this.AddAnimation(transform, avatarRoot.transform, animOnClip, "material._RimShadeColor.a", menuInstaller.Color.a);
                this.AddAnimation(transform, avatarRoot.transform, animOnClip, "material._RimShadeNormalStrength", menuInstaller.NormalStrength);
                this.AddAnimation(transform, avatarRoot.transform, animOnClip, "material._RimShadeBorder", menuInstaller.Border);
                this.AddAnimation(transform, avatarRoot.transform, animOnClip, "material._RimShadeBlur", menuInstaller.Blur);
                this.AddAnimation(transform, avatarRoot.transform, animOnClip, "material._RimShadeFresnelPower", menuInstaller.FresnelPower);
            }

            this.CreateAsset(animOnClip, destAnimClipOnFilePath);

            // create animation clip (off)
            var destAnimClipOffFilePath = Path.Combine(destDir, $"{baseName}_Off.anim");
            var animOffClip = new AnimationClip();

            foreach (var renderer in renderers)
            {
                var transform = renderer.gameObject.transform;
                this.AddAnimation(transform, avatarRoot.transform, animOffClip, "material._UseRimShade", 0);
            }

            this.CreateAsset(animOffClip, destAnimClipOffFilePath);

            // create controller
            var controller = new AnimatorController();
            controller.AddParameter(
                new AnimatorControllerParameter
                {
                    name = baseName,
                    type = AnimatorControllerParameterType.Bool,
                    defaultBool = false,
                }
            );
            if (controller.layers.Length == 0)
            {
                controller.AddLayer(baseName);
            }

            var layer = controller.layers[0];
            layer.name = baseName;
            layer.stateMachine.name = baseName;
            layer.stateMachine.entryPosition = new Vector3(-300, 0);
            layer.stateMachine.anyStatePosition = new Vector3(400, 0);

            var idleState = layer.stateMachine.AddState($"{baseName}_Idle", new Vector3(-150, 0));
            idleState.motion = animOffClip;
            idleState.writeDefaultValues = false;

            var offState = layer.stateMachine.AddState($"{baseName}_Off", new Vector3(150, 50));
            offState.motion = animOffClip;
            offState.writeDefaultValues = false;

            var onState = layer.stateMachine.AddState($"{baseName}_On", new Vector3(150, -50));
            onState.motion = animOnClip;
            onState.writeDefaultValues = false;
            layer.stateMachine.defaultState = idleState;

            var idleToOn = idleState.AddTransition(onState);
            idleToOn.exitTime = 0;
            idleToOn.duration = 0;
            idleToOn.hasExitTime = false;
            idleToOn.conditions = new AnimatorCondition[]
            {
            new() {mode = AnimatorConditionMode.If,parameter = baseName,threshold = 1},
            };

            var idleToOff = idleState.AddTransition(offState);
            idleToOff.exitTime = 0;
            idleToOff.duration = 0;
            idleToOff.hasExitTime = false;
            idleToOff.conditions = new AnimatorCondition[]
            {
            new() { mode = AnimatorConditionMode.IfNot, parameter = baseName, threshold = 1},
            };
            this.CreateAsset(controller, Path.Combine(destDir, $"{baseName}.controller"));

            var toOn = offState.AddTransition(onState);
            toOn.exitTime = 0;
            toOn.duration = 0;
            toOn.hasExitTime = false;
            toOn.conditions = new AnimatorCondition[]
            {
            new() { mode = AnimatorConditionMode.If, parameter = baseName, threshold = 1},
            };
            var toOff = onState.AddTransition(offState);
            toOff.exitTime = 0;
            toOff.duration = 0;
            toOff.hasExitTime = false;
            toOff.conditions = new AnimatorCondition[]
            {
            new() { mode = AnimatorConditionMode.IfNot, parameter = baseName, threshold = 1},
            };

            // create menu
            var menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            menu.name = baseName;

            var exControl = new ExpressionControl
            {
                name = baseName,
                type = ExpressionControl.ControlType.Toggle,
                value = 1,
                parameter = new ExpressionControl.Parameter { name = baseName }
            };
            menu.controls.Add(exControl);
            this.CreateAsset(menu, Path.Combine(destDir, $"{baseName}.asset"));

            // attach maMenuInstaller to menuInstaller
            var maMenuInstaller = menuInstaller.gameObject.AddComponent<ModularAvatarMenuInstaller>();
            maMenuInstaller.menuToAppend = menu;
            var maParameters = menuInstaller.gameObject.AddComponent<ModularAvatarParameters>();
            maParameters.parameters.Add(new ParameterConfig
            {
                nameOrPrefix = baseName,
                defaultValue = 0,
                saved = false,
                syncType = ParameterSyncType.Bool
            });
            var maMergeAnimator = menuInstaller.gameObject.AddComponent<ModularAvatarMergeAnimator>();
            maMergeAnimator.animator = controller;
            maMergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
            maMergeAnimator.pathMode = MergeAnimatorPathMode.Absolute;
            maMergeAnimator.matchAvatarWriteDefaults = false;
        }

        private void CollectRenderersRecursive(Transform current, List<Renderer> renderers)
        {
            if (current.gameObject.TryGetComponent<Renderer>(out var r))
            {
                renderers.Add(r);
            }

            foreach (Transform child in current)
            {
                this.CollectRenderersRecursive(child, renderers);
            }
        }

        private string GetRelativePath(Transform transform, Transform root)
        {
            if (transform == root)
            {
                return "";
            }

            var path = transform.name;
            var parent = transform.parent;
            while (parent && parent != root)
            {
                path = $"{parent.name}/{path}";
                parent = parent.parent;
            }

            return parent == root ? path : null;
        }

        private void AddAnimation(Transform transform, Transform rootTransform, AnimationClip clip, string propertyName, float value)
        {
            var path = this.GetRelativePath(transform, rootTransform);
            var renderer = transform.GetComponent<Renderer>();

            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat == null || mat.shader.name.IndexOf("lilToon") < 0)
                {
                    continue;
                }

                this.SetCurve(clip, propertyName, value, path, renderer.GetType());
            }
        }

        private void SetCurve(AnimationClip clip, string propertyName, float value, string path, Type type)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (!binding.propertyName.StartsWith(propertyName) || binding.path != path)
                {
                    continue;
                }

                var editorCurve = AnimationUtility.GetEditorCurve(clip, binding);
                foreach (var _ in editorCurve.keys)
                {
                    editorCurve.AddKey(0, value);
                    AnimationUtility.SetEditorCurve(clip, binding, editorCurve);
                    return;
                }
            }

            var curveBinding = new EditorCurveBinding
            {
                path = path,
                type = type,
                propertyName = propertyName
            };

            var curve = new AnimationCurve();
            curve.AddKey(0, value);
            AnimationUtility.SetEditorCurve(clip, curveBinding, curve);
        }

        private void CreateAsset(UnityEngine.Object asset, string dest)
        {
            if (File.Exists(dest))
            {
                AssetDatabase.DeleteAsset(dest);
            }

            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(dest));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}

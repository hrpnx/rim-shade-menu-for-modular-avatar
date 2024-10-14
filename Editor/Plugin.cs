using System.Collections.Generic;
using System.IO;
using ExpressionControl = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using dev.hrpnx.rim_shade_menu_for_modular_avatar.editor;
using dev.hrpnx.rim_shade_menu_for_modular_avatar.runtime;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using System;
using nadena.dev.modular_avatar.core;
using UnityEngine.SceneManagement;

[assembly: ExportsPlugin(typeof(Plugin))]

namespace dev.hrpnx.rim_shade_menu_for_modular_avatar.editor
{
    public class Plugin : Plugin<Plugin>
    {
        // TODO: override QualifiedName and DisplayName

        protected override void Configure() => this
            .InPhase(BuildPhase.Transforming)
            .BeforePlugin("nadena.dev.modular-avatar")
            .Run("Install RimShadeMenu", ctx =>
            {
                var menuInstaller = ctx.AvatarRootObject.GetComponentInChildren<RimShadeMenuInstaller>();
                if (menuInstaller == null)
                {
                    return;
                }

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
                "__Generated"
            );

            if (Directory.Exists(destDir))
            {
                Directory.Delete(destDir, true);
            }

            Directory.CreateDirectory(destDir);
            AssetDatabase.Refresh();

            var baseName = "RimShade";

            // create animation clip (on)
            var destAnimClipOnFilePath = Path.Combine(destDir, $"{baseName}_On.anim");
            var animOnClip = new AnimationClip();

            foreach (var renderer in renderers)
            {
                var transform = renderer.gameObject.transform;
                this.AddAnimation(transform, avatarRoot.transform, animOnClip, "material._UseRimShade", 1, null);
                this.AddAnimation(transform, avatarRoot.transform, animOnClip, "material._RimShadeColor.r", menuInstaller.Color.r, menuInstaller.ColorExclusions);
                this.AddAnimation(transform, avatarRoot.transform, animOnClip, "material._RimShadeColor.g", menuInstaller.Color.g, menuInstaller.ColorExclusions);
                this.AddAnimation(transform, avatarRoot.transform, animOnClip, "material._RimShadeColor.b", menuInstaller.Color.b, menuInstaller.ColorExclusions);
                this.AddAnimation(transform, avatarRoot.transform, animOnClip, "material._RimShadeColor.a", menuInstaller.Color.a, menuInstaller.ColorExclusions);
                this.AddAnimation(transform, avatarRoot.transform, animOnClip, "material._RimShadeNormalStrength", menuInstaller.NormalStrength, null);
                this.AddAnimation(transform, avatarRoot.transform, animOnClip, "material._RimShadeBorder", menuInstaller.Border, null);
                this.AddAnimation(transform, avatarRoot.transform, animOnClip, "material._RimShadeBlur", menuInstaller.Blur, null);
                this.AddAnimation(transform, avatarRoot.transform, animOnClip, "material._RimShadeFresnelPower", menuInstaller.FresnelPower, null);
            }

            this.CreateAsset(animOnClip, destAnimClipOnFilePath);

            // create animation clip (off)
            var destAnimClipOffFilePath = Path.Combine(destDir, $"{baseName}_Off.anim");
            var animOffClip = new AnimationClip();

            foreach (var renderer in renderers)
            {
                var transform = renderer.gameObject.transform;
                this.AddAnimation(transform, avatarRoot.transform, animOffClip, "material._UseRimShade", 0, null);
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
            controller.AddLayer(baseName);

            var layer = controller.layers[0];
            layer.name = baseName;
            layer.stateMachine.name = baseName;
            layer.stateMachine.entryPosition = new Vector3(0, 0);
            layer.stateMachine.anyStatePosition = new Vector3(300, 0);
            layer.stateMachine.exitPosition = new Vector3(0, -75);

            var offState = layer.stateMachine.AddState($"{baseName}_Off", new Vector3(150, 150));
            offState.motion = animOffClip;
            offState.writeDefaultValues = false;

            var toOffTransition = layer.stateMachine.AddAnyStateTransition(offState);
            toOffTransition.AddCondition(AnimatorConditionMode.IfNot, 0, baseName);
            toOffTransition.hasExitTime = false;
            toOffTransition.duration = 0f;

            var onState = layer.stateMachine.AddState($"{baseName}_On", new Vector3(150, -150));
            onState.motion = animOnClip;
            onState.writeDefaultValues = false;

            var toOnTransition = layer.stateMachine.AddAnyStateTransition(onState);
            toOnTransition.AddCondition(AnimatorConditionMode.If, 0, baseName);
            toOnTransition.hasExitTime = false;
            toOnTransition.duration = 0f;

            this.CreateAsset(controller, Path.Combine(destDir, $"{baseName}_Controller.controller"));

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

            this.CreateAsset(menu, Path.Combine(destDir, $"{baseName}_Menu.asset"));

            // attach maMenuInstaller to menuInstaller
            var maMenuInstaller = menuInstaller.gameObject.AddComponent<ModularAvatarMenuInstaller>();
            if (null != menuInstaller.RootMenu)
            {
                maMenuInstaller.installTargetMenu = menuInstaller.RootMenu;
            }
            maMenuInstaller.menuToAppend = menu;
            var maParameters = menuInstaller.gameObject.AddComponent<ModularAvatarParameters>();
            maParameters.parameters.Add(new ParameterConfig
            {
                nameOrPrefix = baseName,
                defaultValue = menuInstaller.Default ? 1 : 0,
                saved = menuInstaller.Saved,
                syncType = ParameterSyncType.Bool
            });
            var maMergeAnimator = menuInstaller.gameObject.AddComponent<ModularAvatarMergeAnimator>();
            maMergeAnimator.animator = controller;
            maMergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
            maMergeAnimator.pathMode = MergeAnimatorPathMode.Absolute;
            maMergeAnimator.matchAvatarWriteDefaults = false;
            // NOTE:
            //   If the layer priority is lower than FaceEmo,
            //   when the Gesture(Left|Right)Weight values are changed (e.g., by a triggering press),
            //   the RimShade switches On/Off, causing the avatar to flicker.
            //   Therefore, the priority should be higher than FaceEmo.
            maMergeAnimator.layerPriority = 1;
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

        private void AddAnimation(Transform transform, Transform rootTransform, AnimationClip clip, string propertyName, float value, List<Material> exclusions = null)
        {
            var path = this.GetRelativePath(transform, rootTransform);
            var renderer = transform.GetComponent<Renderer>();

            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat == null || mat.shader.name.IndexOf("lilToon") < 0)
                {
                    continue;
                }

                if(null != exclusions && exclusions.Contains(mat))
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

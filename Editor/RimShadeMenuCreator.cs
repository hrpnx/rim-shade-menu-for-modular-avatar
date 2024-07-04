using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using System.IO;
using ExpressionControl = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using UnityEditor.Animations;
using nadena.dev.modular_avatar.core;
using System;
using System.Collections.Generic;

namespace dev.hrpnx.rim_shade_menu_creator.editor
{
    public class UI : EditorWindow
    {
        private static readonly string PrefKeyColor = "RimShadeMenuCreator_RimColor";
        private static readonly string PrefKeyNormalMapIntensity = "RimShadeMenuCreator_NormalMapIntensity";
        private static readonly string PrefKeyRange = "RimShadeMenuCreator_Range";
        private static readonly string PrefKeyBlur = "RimShadeMenuCreator_Blur";
        private static readonly string PrefKeyRimLightIntensity = "RimShadeMenuCreator_RimLightIntensity";
        private static readonly string PrefKeyIsDefault = "RimShadeMenuCreator_IsDefault";
        private static readonly string PrefKeyIsSaved = "RimShadeMenuCreator_IsSaved";

        private GameObject avatarRoot;

        private Color color;
        private float normalMapIntensity;
        private float range;
        private float blur;
        private float rimLightIntensity;

        private bool isDefault;
        private bool isSaved;

        [MenuItem("Tools/Modular Avatar/RimShadeMenuCreator")]
        private static void Create()
        {
            var window = GetWindow<UI>("UIElements");
            window.titleContent = new GUIContent("RimShadeMenuCreator");
        }

        public void OnGUI()
        {
            this.LoadSettings();

            EditorGUI.BeginChangeCheck();

            this.color = EditorGUILayout.ColorField("Color", this.color);
            this.normalMapIntensity = EditorGUILayout.Slider("Normal Map Strength", this.normalMapIntensity, 0.0f, 1.0f);
            this.range = EditorGUILayout.Slider("Border", this.range, 0.0f, 1.0f);
            this.blur = EditorGUILayout.Slider("Blur", this.blur, 0.0f, 1.0f);
            this.rimLightIntensity = EditorGUILayout.Slider("Fresnel Power", this.rimLightIntensity, 0.01f, 50.0f);
            this.isDefault = GUILayout.Toggle(this.isDefault, "Default On");
            this.isSaved = GUILayout.Toggle(this.isSaved, "Saved");

            if (EditorGUI.EndChangeCheck())
            {
                this.SaveSettings();
            }

            this.avatarRoot = (GameObject)EditorGUILayout.ObjectField("Avatar", this.avatarRoot, typeof(GameObject), true);
            if (this.avatarRoot == null)
            {
                EditorGUILayout.LabelField("Please select an avatar object.");
                return;
            }

            if (!this.avatarRoot.GetComponent<VRCAvatarDescriptor>())
            {
                EditorGUILayout.LabelField("Please select an avatar object.");
                return;
            }

            if (GUILayout.Button("Create Menu"))
            {
                GUI.enabled = false;
                this.SaveSettings();
                this.CreateToggleMenu();
                EditorUtility.DisplayDialog("Info", "Menu created successfully.", "OK");
                GUI.enabled = true;
            }
        }

        private void CreateToggleMenu()
        {
            List<Renderer> renderers = new();
            this.CollectRenderersRecursive(this.avatarRoot.transform, renderers);

            var destDir = Path.Combine(
                "Assets",
                "Editor",
                "RimShadeMenuCreator",
                "Created",
                this.avatarRoot.name
            );

            if (Directory.Exists(destDir))
            {
                Directory.Delete(destDir, true);
            }

            Directory.CreateDirectory(destDir);
            AssetDatabase.Refresh();

            var baseName = "RimShadeToggle";

            // create animation clip (on)
            var destAnimClipOnFilePath = Path.Combine(destDir, $"{baseName}_On.anim");
            var animOnClip = new AnimationClip();

            foreach (var renderer in renderers)
            {
                var transform = renderer.gameObject.transform;
                this.AddAnimation(transform, animOnClip, "material._UseRimShade", 1);
                this.AddAnimation(transform, animOnClip, "material._RimShadeColor.r", this.color.r);
                this.AddAnimation(transform, animOnClip, "material._RimShadeColor.g", this.color.g);
                this.AddAnimation(transform, animOnClip, "material._RimShadeColor.b", this.color.b);
                this.AddAnimation(transform, animOnClip, "material._RimShadeColor.a", this.color.a);
                this.AddAnimation(transform, animOnClip, "material._RimShadeNormalStrength", this.normalMapIntensity);
                this.AddAnimation(transform, animOnClip, "material._RimShadeBorder", this.range);
                this.AddAnimation(transform, animOnClip, "material._RimShadeBlur", this.blur);
                this.AddAnimation(transform, animOnClip, "material._RimShadeFresnelPower", this.rimLightIntensity);
            }

            this.CreateAsset(animOnClip, destAnimClipOnFilePath);

            // create animation clip (off)
            var destAnimClipOffFilePath = Path.Combine(destDir, $"{baseName}_Off.anim");
            var animOffClip = new AnimationClip();

            foreach (var renderer in renderers)
            {
                var transform = renderer.gameObject.transform;
                this.AddAnimation(transform, animOffClip, "material._UseRimShade", 0);
            }

            this.CreateAsset(animOffClip, destAnimClipOffFilePath);

            // create controller
            var controller = new AnimatorController();
            controller.AddParameter(
                new AnimatorControllerParameter
                {
                    name = baseName,
                    type = AnimatorControllerParameterType.Bool,
                    defaultBool = this.isDefault,
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
            var menu = CreateInstance<VRCExpressionsMenu>();
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

            var menuInstallerName = "RimShadeToggleMenu";
            var oldMenuInstaller = GameObject.Find(menuInstallerName);
            DestroyImmediate(oldMenuInstaller);

            // create menu installer
            var menuInstaller = new GameObject(menuInstallerName);
            menuInstaller.transform.SetParent(this.avatarRoot.transform);
            var maMenuInstaller = menuInstaller.AddComponent<ModularAvatarMenuInstaller>();
            maMenuInstaller.menuToAppend = menu;
            // maMenuInstaller.
            var maParameters = menuInstaller.AddComponent<ModularAvatarParameters>();
            maParameters.parameters.Add(new ParameterConfig
            {
                nameOrPrefix = baseName,
                defaultValue = this.isDefault ? 1 : 0,
                saved = this.isSaved,
                syncType = ParameterSyncType.Bool
            });
            var maMergeAnimator = menuInstaller.AddComponent<ModularAvatarMergeAnimator>();
            maMergeAnimator.animator = controller;
            maMergeAnimator.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
            maMergeAnimator.pathMode = MergeAnimatorPathMode.Absolute;
            maMergeAnimator.matchAvatarWriteDefaults = true;
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

        private void AddAnimation(Transform transform, AnimationClip clip, string propertyName, float value)
        {
            var path = this.GetRelativePath(transform, this.avatarRoot.transform);
            var renderer = transform.GetComponent<Renderer>();

            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat == null || mat.shader.name.IndexOf("lilToon") < 0)
                {
                    continue;
                }

                this.MakeAnimation(clip, propertyName, value, path, renderer.GetType());
            }
        }

        private void MakeAnimation(AnimationClip clip, string propertyName, float value, string path, Type type)
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

        private void SaveSettings()
        {
            EditorPrefs.SetString(PrefKeyColor, ColorUtility.ToHtmlStringRGBA(this.color));
            EditorPrefs.SetFloat(PrefKeyNormalMapIntensity, this.normalMapIntensity);
            EditorPrefs.SetFloat(PrefKeyRange, this.range);
            EditorPrefs.SetFloat(PrefKeyBlur, this.blur);
            EditorPrefs.SetFloat(PrefKeyRimLightIntensity, this.rimLightIntensity);
            EditorPrefs.SetBool(PrefKeyIsSaved, this.isSaved);
            EditorPrefs.SetBool(PrefKeyIsDefault, this.isDefault);
        }

        private void LoadSettings()
        {
            var rawColor = EditorPrefs.GetString(PrefKeyColor, "808080");
            if (ColorUtility.TryParseHtmlString("#" + rawColor, out var parsed))
            {
                this.color = parsed;
            }

            this.normalMapIntensity = EditorPrefs.GetFloat(PrefKeyNormalMapIntensity, 1.0f);
            this.range = EditorPrefs.GetFloat(PrefKeyRange, 0.5f);
            this.blur = EditorPrefs.GetFloat(PrefKeyBlur, 1.0f);
            this.rimLightIntensity = EditorPrefs.GetFloat(PrefKeyRimLightIntensity, 1.0f);
            this.isSaved = EditorPrefs.GetBool(PrefKeyIsSaved, false);
            this.isDefault = EditorPrefs.GetBool(PrefKeyIsDefault, false);
        }
    }
}

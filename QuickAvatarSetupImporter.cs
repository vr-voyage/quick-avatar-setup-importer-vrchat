#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using System;
using UnityEditor.Animations;
using System.IO;

/* FIXME : Set that as a Component, instead of a floating window.
 * There's almost no need for a floating window anyway, beside testing
 */
public class QuickAvatarSetupImporter : EditorWindow
{
    

    SerializedObject serialO;

    public VRCAvatarDescriptor avatar;
    SerializedProperty avatarSerialized;
    
    public TextAsset avatarSetupJsonAsset;
    SerializedProperty avatarSetupJsonAssetSerialized;

    public string emoteParamName = "MyyEmoteParam";
    SerializedProperty emoteParamNameSerialized;

    public string emoteMenuParamName = "MyyMenuParamName";
    SerializedProperty emoteMenuParamNameSerialized;

    /* Note : You cannot move this before variables declaration */
    [MenuItem("Tools / Quick Avatar Setup importer")]

    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(QuickAvatarSetupImporter));

    }

    private VRCExpressionsMenu VRCMenuCreate()
    {
        return ScriptableObject.CreateInstance<VRCExpressionsMenu>();
    }

    const int nParamsByDefault = 3;
    private VRCExpressionParameters VRCExpressionParamsCreate(int nParamsExpected)
    {
        VRCExpressionParameters menuParams = ScriptableObject.CreateInstance<VRCExpressionParameters>();
        menuParams.parameters = new VRCExpressionParameters.Parameter[nParamsByDefault + nParamsExpected];
        VRCExpressionParameters.Parameter menuParam = new VRCExpressionParameters.Parameter();
        menuParam.name = "VRCEmote";
        menuParam.valueType = VRCExpressionParameters.ValueType.Int;
        menuParam.defaultValue = 0;
        menuParam.saved = true;
        menuParams.parameters[0] = menuParam;

        menuParam = new VRCExpressionParameters.Parameter();
        menuParam.name = "VRCFaceBlendH";
        menuParam.valueType = VRCExpressionParameters.ValueType.Float;
        menuParam.defaultValue = 0;
        menuParam.saved = true;
        menuParams.parameters[1] = menuParam;

        menuParam = new VRCExpressionParameters.Parameter();
        menuParam.name = "VRCFaceBlendV";
        menuParam.valueType = VRCExpressionParameters.ValueType.Float;
        menuParam.defaultValue = 0;
        menuParam.saved = true;
        menuParams.parameters[2] = menuParam;

        return menuParams;
    }

    void VRCExpressionParamsSetParam(
        VRCExpressionParameters menuParams,
        int paramIndex,
        string paramName,
        VRCExpressionParameters.ValueType paramType,
        float paramDefaultValue,
        bool isParamSaved)
    {
        VRCExpressionParameters.Parameter menuParam = new VRCExpressionParameters.Parameter();
        menuParam.name         = paramName;
        menuParam.valueType    = paramType;
        menuParam.defaultValue = paramDefaultValue;
        menuParam.saved        = isParamSaved;
        menuParams.parameters[nParamsByDefault + paramIndex] = menuParam;
    }

    private VRCExpressionsMenu[] VRCSubMenusCreate(int amount)
    {
        VRCExpressionsMenu[] subMenus = new VRCExpressionsMenu[amount];
        for (int i = 0; i < amount; i++)
        {
            subMenus[i] = VRCMenuCreate();
        }
        return subMenus;
    }

    private VRCExpressionsMenu.Control VRCMenuAddControl(
        VRCExpressionsMenu menu,
        string label,
        VRCExpressionsMenu.Control.ControlType controlType,
        string parameterName,
        float value)
    {
        VRCExpressionsMenu.Control control = new VRCExpressionsMenu.Control();
        control.name = label;
        control.value = value;
        control.type = controlType;
        control.parameter = new VRCExpressionsMenu.Control.Parameter();
        control.parameter.name = parameterName;
        control.style = VRCExpressionsMenu.Control.Style.Style1; // ??
        menu.controls.Add(control);
        return control;
    }
    
    private VRCExpressionsMenu.Control VRCMenuAddSubMenu(
        VRCExpressionsMenu mainMenu,
        string label,
        VRCExpressionsMenu subMenu,
        string parameterName,
        float value)
    {
        VRCExpressionsMenu.Control control = VRCMenuAddControl(
            mainMenu, label,
            VRCExpressionsMenu.Control.ControlType.SubMenu, parameterName, value);
        control.subMenu = subMenu;
        return control;
    }

    [Serializable]
    public class AvatarSetup
    {
        public int version;
        public Emotions emotions;
    }

    [Serializable]
    public class Emotions
    {
        public Emotion[] data;
    }

    [Serializable]
    public class Emotion
    {
        public string emotion_name;
        public EmotionBlendshape[] blendshapes;
    }

    [Serializable]
    public class EmotionBlendshape
    {
        public string blendshape_name;
        public float current_value;
        public string rel_path; // Godot path
        public string short_path;
    }

    private void OnEnable()
    {
        serialO = new SerializedObject(this);
        avatarSerialized = serialO.FindProperty("avatar");
        emoteParamNameSerialized = serialO.FindProperty("emoteParamName");
        emoteMenuParamNameSerialized = serialO.FindProperty("emoteMenuParamName");
        avatarSetupJsonAssetSerialized = serialO.FindProperty("avatarSetupJsonAsset");
    }

    private void OnGUI()
    {

        serialO.Update();
        EditorGUILayout.PropertyField(avatarSerialized);
        EditorGUILayout.PropertyField(avatarSetupJsonAssetSerialized);
        EditorGUILayout.PropertyField(emoteParamNameSerialized);
        EditorGUILayout.PropertyField(emoteMenuParamNameSerialized);
        serialO.ApplyModifiedProperties();

        if (GUILayout.Button("Add emotions"))
        {
            if (emoteParamName == null || emoteMenuParamName == null || avatarSetupJsonAsset == null)
            {
                Debug.LogError("[QuickAvatarSetupImporter] Some fields are not setup !");
                return;
            }

            string avatarSetupJsonFilePath = AssetDatabase.GetAssetPath(avatarSetupJsonAsset);
            string extensionName = ".avatar_setup.json";
            string extensionNameWithGlb = ".glb" + extensionName;

            if (!avatarSetupJsonFilePath.EndsWith(extensionName))
            {
                Debug.LogError(
                    "The JSON file you provided does not end with " + extensionName + "\n" +
                    "Aborting...");
                return;
            }

            /* We keep the path, without the .glb.avatar_setup.json or
             * .avatar_setup.json extension, in order to determine where to save
             * the various generated files.
             */
            string baseFilePath;
            if (avatarSetupJsonFilePath.EndsWith(extensionNameWithGlb))
                baseFilePath = avatarSetupJsonFilePath.Replace(extensionNameWithGlb, "");
            else
                baseFilePath = avatarSetupJsonFilePath.Replace(extensionName, "");

            /* Controller */
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(baseFilePath + "_emotions_controller.controller");
            AnimatorControllerParameter param = new AnimatorControllerParameter();
            param.type = AnimatorControllerParameterType.Int;
            param.defaultInt = 0;
            param.name = emoteParamName;
            controller.AddParameter(param);
            AnimatorStateMachine rootMachine = controller.layers[0].stateMachine;
            AnimatorState entryState = rootMachine.AddState("DummyEntry");
            //rootMachine.AddEntryTransition(entryState);
            /* -- Controller -- */

            int emoteIndex = 1;
            AvatarSetup setup = JsonUtility.FromJson<AvatarSetup>(avatarSetupJsonAsset.text);
            Emotions emotions = setup.emotions;
            int nEmotions = emotions.data.Length;
            VRCExpressionsMenu[] subMenus = VRCSubMenusCreate(nEmotions / 8 + 1);

            for (int i = 0; i < nEmotions; i++)
            {
                /* To create an animation that moves blendshapes from 0 to 100,
                    * you need to :
                    * - Create an animation clip
                    * Then for each blendshape (property) you want to move :
                    * - Create at least two key frames
                    * - Set the key frames values at '100' (meaning that the driven
                    *   values are set at 100 when hitting these keyframes)
                    * - Create a curve that uses these two frames
                    * - Add the curve to the animation clip, and tell the clip
                    *  to use this curve to drive the blendshape property.
                    */
                Emotion emotion = emotions.data[i];
                VRCExpressionsMenu subMenu = subMenus[i / 8];
                AnimationClip clip = new AnimationClip();

                clip.ClearCurves();
                foreach (EmotionBlendshape blendshape in emotion.blendshapes)
                {

                    if (blendshape.blendshape_name.StartsWith("vrc.")) continue;
                    AnimationCurve curve = new AnimationCurve();
                    Keyframe frame1 = new Keyframe();
                    Keyframe frame2 = new Keyframe();
                    frame1.time = 0;
                    frame1.value = Mathf.Floor(blendshape.current_value * 100.0f);
                    frame2.time = 1 / 60.0f;
                    frame2.value = Mathf.Floor(blendshape.current_value * 100.0f);

                    curve.AddKey(frame1);
                    curve.AddKey(frame2);

                    clip.SetCurve(
                        blendshape.short_path,
                        typeof(SkinnedMeshRenderer),
                        "blendShape." + blendshape.blendshape_name,
                        curve);

                }
                /* Create the animation file, so that we can create a state without issues */
                string animationFilePath = baseFilePath + "_emote_anim_" + emotion.emotion_name + ".anim";
                AssetDatabase.CreateAsset(clip, animationFilePath);
                /* Controller */

                /* Setup a state, inside the controller, using the generated clip.
                    * Then setup a transition between the entry state (DummyState)
                    * and the created state.
                    * Add a `emoteParamName == emoteIndex (i + 1)` condition for
                    * the transition from Entry to New State, and make it emoteParamName != emoteIndex
                    * when returning back to the main Entry state.
                    * That way, the animation will be triggered correctly when toggling
                    * the right value in the VRChat menu.
                    */
                var s = rootMachine.AddState(emotion.emotion_name);
                s.writeDefaultValues = false;
                s.motion = clip;
                AnimatorStateTransition entryToState = entryState.AddTransition(s, false);
                entryToState.AddCondition(AnimatorConditionMode.Equals, emoteIndex, emoteParamName);
                AnimatorStateTransition stateToEntry = s.AddTransition(entryState, false);
                stateToEntry.AddCondition(AnimatorConditionMode.NotEqual, emoteIndex, emoteParamName);

                /* Add an entry to the VRChat menu to toggle this animation,
                    * by setting the animation parameter value to the current
                    * emoteIndex when toggled. */
                VRCMenuAddControl(
                    subMenu, emotion.emotion_name, 
                    VRCExpressionsMenu.Control.ControlType.Toggle,
                    emoteParamName, emoteIndex);
                emoteIndex += 1;
            }

            /* Generate the VRChat menus and submenu files */
            VRCExpressionsMenu menu = VRCMenuCreate();
                
            for (int i = 0; i < subMenus.Length; i++)
            {
                string subMenuFilePath = baseFilePath + "_vrc_sub_menu_" + i + ".asset";
                AssetDatabase.CreateAsset(subMenus[i], subMenuFilePath);
                VRCMenuAddSubMenu(menu, "SubMenu" + i, subMenus[i], emoteMenuParamName, i + 1);
            }

            string menuFilePath = baseFilePath + "_vrc_menu.asset";
            AssetDatabase.CreateAsset(menu, menuFilePath);

            VRCExpressionParameters menuParams = VRCExpressionParamsCreate(2);
            VRCExpressionParamsSetParam(
                menuParams, 0, emoteParamName,
                VRCExpressionParameters.ValueType.Int, 0,
                false);
            VRCExpressionParamsSetParam(
                menuParams, 1, emoteMenuParamName,
                VRCExpressionParameters.ValueType.Int, 0,
                false);

            string paramsFilePath = baseFilePath + "_vrc_menu_parameters.asset";
            AssetDatabase.CreateAsset(menuParams, paramsFilePath);

            /* Setting up the avatar */
            avatar.customExpressions = true;
            avatar.customizeAnimationLayers = true;
            avatar.expressionsMenu = menu;
            avatar.expressionParameters = menuParams;
            // avatar.baseAnimationLayers[4] = FX Layer
            avatar.baseAnimationLayers[4].animatorController = controller;
            avatar.baseAnimationLayers[4].isDefault = false;

        }
    }
}
#endif
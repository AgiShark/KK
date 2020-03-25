﻿/*
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
MMM               MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM    M7    MZ    MMO    MMMMM
MMM               MMMMMMMMMMMMM   MMM     MMMMMMMMMM    M$    MZ    MMO    MMMMM
MMM               MMMMMMMMMM       ?M     MMMMMMMMMM    M$    MZ    MMO    MMMMM
MMMMMMMMMMMM8     MMMMMMMM       ~MMM.    MMMMMMMMMM    M$    MZ    MMO    MMMMM
MMMMMMMMMMMMM     MMMMM        MMM                 M    M$    MZ    MMO    MMMMM
MMMMMMMMMMMMM     MM.         ZMMMMMM     MMMM     MMMMMMMMMMMMZ    MMO    MMMMM
MMMMMMMMMMMMM     MM      .   ZMMMMMM     MMMM     MMMMMMMMMMMM?    MMO    MMMMM
MMMMMMMMMMMMM     MMMMMMMM    $MMMMMM     MMMM     MMMMMMMMMMMM?    MM8    MMMMM
MMMMMMMMMMMMM     MMMMMMMM    7MMMMMM     MMMM     MMMMMMMMMMMMI    MM8    MMMMM
MMM               MMMMMMMM    7MMMMMM     MMMM    .MMMMMMMMMMMM.    MMMM?ZMMMMMM
MMM               MMMMMMMM.   ?MMMMMM     MMMM     MMMMMMMMMM ,:MMMMMM?    MMMMM
MMM           ..MMMMMMMMMM    =MMMMMM     MMMM     M$ MM$M7M $MOM MMMM     ?MMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM .+Z: M   :M M  MM   ?MMMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
*/

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
using Extension;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace KK_StudioDualScreen {
    [BepInPlugin(GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("CharaStudio")]
    public class KK_StudioDualScreen : BaseUnityPlugin {
        internal const string PLUGIN_NAME = "Studio Dual Screen";
        internal const string GUID = "com.jim60105.kk.studiodualscreen";
        internal const string PLUGIN_VERSION = "20.03.24.3";
        internal const string PLUGIN_RELEASE_VERSION = "1.0.2";

        public static ConfigEntry<KeyboardShortcut> Hotkey { get; set; }
        internal static new ManualLogSource Logger;

        public void Start() {
            Logger = base.Logger;
            HarmonyWrapper.PatchAll(typeof(Patches));

            Hotkey = Config.Bind<KeyboardShortcut>("Hotkey", "Active Key", new KeyboardShortcut(KeyCode.None), "You must have two monitors to make it work.");
        }

        public void Update() => Patches.Update();
    }

    class Patches {
        private static GameObject mainCamera;
        private static Camera cloneCamera;
        private static GameObject cloneCanvas;

        public static void Update() {
            //監聽滑鼠按下
            if (KK_StudioDualScreen.Hotkey.Value.IsDown()) {
                if (null == mainCamera) {
                    mainCamera = GameObject.Find("StudioScene/Camera/Main Camera");
                }

                if (Display.displays.Length > 1 && null == cloneCamera) {
                    cloneCamera = UnityEngine.Object.Instantiate(mainCamera.GetComponentInChildren<Camera>());
                    Studio.CameraControl camCtrl = cloneCamera.GetComponent<Studio.CameraControl>();
                    camCtrl.ReflectOption();
                    camCtrl.isOutsideTargetTex = false;
                    camCtrl.subCamera.gameObject.SetActive(false);
                    camCtrl.SetField("isInit", false);

                    //cloneCamera.name = "Camera Clone";
                    cloneCamera.CopyFrom(mainCamera.GetComponent<Camera>());
                    cloneCamera.targetDisplay = 1;
                    cloneCamera.transform.SetParent(mainCamera.transform.parent.transform);

                    //Frame
                    Camera cameraUI = GameObject.Find("StudioScene/Camera/Camera UI").GetComponent<Camera>();
                    Camera cloneCameraUI = UnityEngine.Object.Instantiate(cameraUI);
                    //cloneCameraUI.name = "Camera UI Clone";
                    cloneCameraUI.CopyFrom(cameraUI);
                    cloneCameraUI.targetDisplay = 1;
                    cloneCameraUI.transform.SetParent(cameraUI.transform.parent.transform);

                    GameObject canvas = GameObject.Find("StudioScene/Canvas Frame Cap");
                    cloneCanvas = UnityEngine.Object.Instantiate(canvas);
                    cloneCanvas.GetComponent<Canvas>().worldCamera = cloneCameraUI;
                    cloneCanvas.GetComponent<Studio.FrameCtrl>().SetField("cameraUI", cloneCameraUI);
                    cloneCanvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(Display.displays[1].systemWidth, Display.displays[1].systemHeight);
                    cloneCanvas.transform.SetParent(canvas.transform.parent.transform);
                    //Frame end

                    Display.displays[0].SetRenderingResolution(Display.displays[0].renderingWidth, Display.displays[0].renderingHeight);
                    Display.displays[0].Activate();
                    Display.displays[1].SetRenderingResolution(Display.displays[1].renderingWidth, Display.displays[1].renderingHeight);
                    Display.displays[1].Activate();
                }

                if (null != cloneCamera) {
                    ResetView();
                }
            }
        }

        public static void ResetView() {
            cloneCamera.GetComponent<Studio.CameraControl>().Import(mainCamera.GetComponentInChildren<Camera>().GetComponent<Studio.CameraControl>().Export());

            //Set Neck Look & Eye Look
            Singleton<Manager.Character>.Instance.GetCharaList(0).Concat(Singleton<Manager.Character>.Instance.GetCharaList(1)).ToList().ForEach((ChaControl chaCtrl) => {
                chaCtrl.neckLookCtrl.target = cloneCamera.transform;
                if (chaCtrl.fileStatus.eyesLookPtn == 1) {
                    chaCtrl.eyeLookCtrl.target = cloneCamera.transform;
                }
            });

            //Reset VMD
            try {
                string path = Extension.Extension.TryGetPluginInstance("KKVMDPlayPlugin.KKVMDPlayPlugin")?.Info.Location;
                Assembly ass = Assembly.LoadFrom(path);
                System.Type VMDAniMgrType = ass.GetType("KKVMDPlayPlugin.VMDAnimationMgr");
                if (null != VMDAniMgrType) {
                    object VMDAniMgr = VMDAniMgrType.GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null);
                    VMDAniMgr?.GetField("CameraMgr").SetField("cameraControl", cloneCamera.GetComponent<Studio.CameraControl>());
                } else {
                    throw new System.Exception("Load assembly FAILED: VMDPlayPlugin");
                }
                KK_StudioDualScreen.Logger.LogDebug("Reset VMD");
            } catch (System.Exception ex) {
                KK_StudioDualScreen.Logger.LogDebug(ex.Message);
            }
        }

        private static bool isLoading = false;
        [HarmonyPrefix, HarmonyPatch(typeof(Studio.SceneLoadScene), "OnClickLoad")]
        public static void OnClickLoadPrefix() {
            isLoading = true;
        }
        [HarmonyPostfix, HarmonyPatch(typeof(Manager.Scene), nameof(Manager.Scene.LoadStart))]
        public static void LoadReservePostfix(Manager.Scene __instance, Manager.Scene.Data data) {
            if (isLoading && data.levelName == "StudioNotification") {
                isLoading = false;
                if (null != cloneCamera) {
                    ResetView();
                }
            }
        }

        //Frame
        [HarmonyPostfix, HarmonyPatch(typeof(Studio.FrameList), "OnClickSelect")]
        public static void OnClickSelectPostfix(Studio.FrameList __instance, int _idx) {
            if (null != cloneCanvas) {
                cloneCanvas.GetComponent<Studio.FrameCtrl>().Load(((int)__instance.GetField("select") == -1) ? string.Empty : __instance.GetField("listPath").ToList<string>()[_idx]);
            }
        }
    }
}

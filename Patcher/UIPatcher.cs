using App.KatamariSin;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace OnceUponAnArchipelago.Patcher;

public class UIPatcher {

	// UI
	[HarmonyPrefix, HarmonyPatch(typeof(UITextSetter), nameof(UITextSetter.SetText), argumentTypes: [typeof(string)])]
	private static bool UITextSetter_SetText_Prefix(UITextSetter __instance, ref string text) {
		if (__instance.name == "Deteil") { // AP connection status
			Plugin.apConnectionUI = __instance;
			text = Plugin.apConnectionText;
		} else if (__instance.transform.parent.name == "Caption") { // stage names
			Transform transform = __instance.transform;
			while (transform.parent != null) {
				transform = transform.parent;

				if (transform.name == "CommonStageInfo") {
					SelectHirabaTalkOptionController controller = transform.parent.GetComponent<SelectHirabaTalkOptionController>();
					if (controller != null) {
						int stageId = controller._idStages[0];
						int selectedStage = Plugin.fansToStages[stageId][controller.NowPageIndex];
						text = Plugin.levelNames[selectedStage];
					}
					break;
				}
			}
		}

		return true;
	}

	[HarmonyPrefix, HarmonyPatch(typeof(UITextSetter), nameof(UITextSetter.SetText), argumentTypes: [typeof(string), typeof(TextLocalizer.LocalizeSheet)])]
	private static bool UITextSetter_SetText_Prefix(UITextSetter __instance) {
		if (__instance.name == "Deteil") {
			__instance.SetText(Plugin.apConnectionText);
			return false;
		}

		return true;
	}

	// shows how many planets you have and need in the select hiroba
	[HarmonyPostfix, HarmonyPatch(typeof(ClearRouteSetup), nameof(ClearRouteSetup.Start))]
	private static void ClearRouteSetup_Start(ClearRouteSetup __instance) {
		Plugin.planetsText = __instance.transform.FindChild("MapStage").FindChild("StageName").GetComponent<TextMeshProUGUI>();
	}

	[HarmonyPostfix, HarmonyPatch(typeof(ClearRouteSetTxet), nameof(ClearRouteSetTxet.SetText))]
	private static void ClearRouteSetTxet_SetText_Postfix() {
		Plugin.SetPlanetsText(Plugin.planets, Plugin.planetsNeeded);
	}
}
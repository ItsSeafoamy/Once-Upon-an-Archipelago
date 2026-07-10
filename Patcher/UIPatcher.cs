using App.KatamariSin;
using HarmonyLib;
using OnceUponAnArchipelago.Archipelago;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

	// show checks done in pre-level details
	[HarmonyPostfix, HarmonyPatch(typeof(SelectHirabaTalkOptionController), nameof(SelectHirabaTalkOptionController.ActiveTalkUI))]
	private static void SelectHirabaTalkOptionController_ActiveTalkUI_Postfix(SelectHirabaTalkOptionController __instance) {
		if (__instance.StageID == 999) return;

		int selectedStage = Plugin.fansToStages[__instance._idStages[0]][__instance.NowPageIndex];
		GlobalManager glb = GlobalManager.Instance;
		List<long> checks = ArchipelagoClient.ServerData.CheckedLocations;

		// crowns
		if (Plugin.randomizeCrowns) {
			for (int i = 0; i < 3; i++) {
				int crownId = glb.GetStageCollective(selectedStage)[i];

				__instance._collectedIcons[i].enabled = checks.Contains(crownId + Plugin.CROWN_ID_OFFSET);
			}
		}

		// presents
		if (Plugin.randomizePresents) {
			int presentId = glb.GetStagePresent(selectedStage);

			if (presentId == -1) {
				__instance._talkPresent._present.SetActive(false);

			} else {
				__instance._talkPresent._present.SetActive(true);
				Image image = __instance._talkPresent._present.GetComponent<Image>();

				if (checks.Contains(presentId + Plugin.PRESENT_ID_OFFSET)) {
					image.color = Color.white;
				} else {
					image.color = new Color(1, 1, 1, 0.5f);
				}
			}
		}

		// cousins
		if (Plugin.randomizeCousins) {
			int[] cousins = glb.GetStageOujiItoko(selectedStage);
			SelectHirobaTalkItokoIcon itokoIcon = __instance._itokoIcon;

			for (int i = 0; i < 3; i++) {
				if (i >= cousins.Length) {
					itokoIcon._icon[i].SetActive(false);
				} else {
					itokoIcon._icon[i].SetActive(true);
					Image image = itokoIcon._icon[i].GetComponent<Image>();

					if (checks.Contains(cousins[i] + Plugin.COUSIN_ID_OFFSET)) {
						Sprite sprite = SubjectListData.instance.GetCustomSpritesData(cousins[i] + 1);
						image.sprite = sprite;
					} else {
						image.sprite = itokoIcon._notGetIconItoko;
					}
				}
			}
		}
	}
}
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
		if (__instance.transform.parent.name is "Caption" or "StageName") { // stage names
			Transform transform = __instance.transform;
			while (transform.parent != null) {
				transform = transform.parent;

				if (transform.name is "CommonStageInfo" or "AASelectDisplay") {
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

		ShowPreLevelDetails(__instance);
	}

	// RLH pre level details
	[HarmonyPostfix, HarmonyPatch(typeof(SelectHirabaTalkOptionController), nameof(SelectHirabaTalkOptionController.ActiveTalkAASelectUI))]
	private static void SelectHirabaTalkOptionController_ActiveTalkAASelectUI_Postfix(SelectHirabaTalkOptionController __instance) {
		if (__instance.StageID == 999) return;
		
		ShowPreLevelDetails(__instance);
	}

	private static void ShowPreLevelDetails(SelectHirabaTalkOptionController controller) {
		int selectedStage = Plugin.fansToStages[controller._idStages[0]][controller.NowPageIndex];
		GlobalManager glb = GlobalManager.Instance;
		List<long> checks = ArchipelagoClient.serverData.CheckedLocations;

		// base game levels
		if (!controller._aaMode) {
			// crowns
			if (Plugin.randomizeCrowns) {
				for (int i = 0; i < 3; i++) {
					int crownId = glb.GetStageCollective(selectedStage)[i];

					controller._collectedIcons[i].enabled = checks.Contains(crownId + Offsets.CROWNS);
				}
			}

			// presents
			if (Plugin.randomizePresents) {
				int presentId = glb.GetStagePresent(selectedStage);

				if (presentId == -1) {
					controller._talkPresent._present.SetActive(false);
				} else {
					controller._talkPresent._present.SetActive(true);
					Image image = controller._talkPresent._present.GetComponent<Image>();

					if (checks.Contains(presentId + Offsets.PRESENTS)) {
						image.color = Color.white;
					} else {
						image.color = new Color(1, 1, 1, 0.5f);
					}
				}
			}
		} else { // rlh levels
			StarIDEnum star = SelectHirobaManager.CheckTargetStage(selectedStage);
			controller._talkAADisplay.SetAAThumbnail(star);
		}

		// cousins
		if (Plugin.randomizeCousins) {
			int[] cousins = glb.GetStageOujiItoko(selectedStage);
			
			SelectHirobaTalkItokoIcon itokoIcon = controller._itokoIcon;

			// if in rlh, find correct cousin object
			// rlh levels only have at most 1 cousin, so we dont have to worry about the other two spots
			if (controller._aaMode) {
				itokoIcon._icon[0] = controller.transform.FindChild("AASelectDisplay").GetChild(0).GetChild(0)
					.FindChild("TV").FindChild("Foundation").FindChild("Body").FindChild("Item_5").FindChild("Mate_1")
					.gameObject;
			}

			for (int i = 0; i < 3; i++) {
				if (i >= cousins.Length) {
					itokoIcon._icon[i].SetActive(false);
				} else {
					itokoIcon._icon[i].SetActive(true);
					Image image = itokoIcon._icon[i].GetComponent<Image>();

					if (checks.Contains(cousins[i] + Offsets.COUSINS)) {
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

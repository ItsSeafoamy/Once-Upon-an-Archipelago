using App.KatamariSin;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using OnceUponAnArchipelago.Archipelago;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OnceUponAnArchipelago;

public class Patcher {

	// Make all eras available
	[HarmonyPostfix, HarmonyPatch(typeof(SelectMapController), nameof(SelectMapController.GetMapRelease))]
	private static void SelectMapController_GetMapRelease_Postfix(ref bool __result, SelectHirobaEnum.Stage map) {
		__result = true;
	}

	// Select Scroll
	[HarmonyPostfix, HarmonyPatch(typeof(MissionItem), nameof(MissionItem.CheckRelease))]
	private static void MissionItem_CheckRelease_Postfix(ref bool __result, MissionItem __instance) {
		if ((int) __instance._eStageID < 1) __result = true;
		else __result = Plugin.levels.Contains((int) __instance._eStageID);
	}

	[HarmonyPostfix, HarmonyPatch(typeof(SubjectListDataSet), nameof(SubjectListDataSet.SetMyList))]
	private static void SubjectListDataSet_SetMyList_Postfix(SubjectListDataSet __instance, MissionItem item) {
		if (!item._isRelease || (int) item._eStageID < 1) return;

		if (Plugin.randomizePresents) {
			if (ArchipelagoClient.ServerData.CheckedLocations.Contains(Plugin.PRESENT_ID_OFFSET + item._presentID)) {
				__instance._presentImages.color = Color.white;
			} else {
				__instance._presentImages.color = new Color(1, 1, 1, 0.5f);
			}

			__instance._presentCanvas.SetActive(true);
		}

		if (Plugin.randomizeCousins) {
			for (int i = 0; i < item._itokoID.Length; i++) {
				int itokoId = item._itokoID[i] - 1;
				Image image = __instance._itokoImages[i];

				if (itokoId == 98) {
					image.enabled = false;
				} else {
					image.enabled = true;

					if (ArchipelagoClient.ServerData.CheckedLocations.Contains(Plugin.COUSIN_ID_OFFSET + itokoId)) {
						Sprite sprite = __instance._listData.GetCustomSpritesData(itokoId + 1);
						image.sprite = sprite;
						image.color = Color.white;
					} else {
						Sprite sprite = __instance._listData.GetSpriteIcon;
						image.sprite = sprite;
						image.color = new Color(1, 1, 1, 0.5f);
					}
				}
			}

			__instance._itokoCanvas.SetActive(true);
		}
	}

	// Reorder stage list so unlocked levels appear before locked ones
	[HarmonyPostfix, HarmonyPatch(typeof(SelectHirobaObjectBase), nameof(SelectHirobaObjectBase.CheckMapObjectRelease))]
	private static void SelectHirobaObjectBase_CheckMapObjectRelease_Postfix(SelectHirobaObjectBase __instance) {
		Il2CppStructArray<int> stages = __instance.GetStagesID;

		List<int> reordered = [];
		foreach (int i in stages) {
			if (Plugin.levels.Contains(i)) {
				reordered.Add(i);
			}
		}

		reordered.Sort();

		foreach (int i in stages) {
			if (!Plugin.levels.Contains(i)) {
				reordered.Add(i);
			}
		}

		__instance._stageIndex = reordered.ToArray();
		__instance._stagesID = reordered.ToArray();

		Plugin.fansToStages[__instance.StageID] = reordered;

		foreach (int i in __instance.GetStagesID) {
			if (stages.Contains(i)) {
				__instance.SetReleaseActiveNormal();
				return;
			}
		}

		__instance._isRelease = false;
	}

	[HarmonyPostfix, HarmonyPatch(typeof(SelectHirobaObjectBase), nameof(SelectHirobaObjectBase.Start))]
	private static void SelectHirobaObjectBase_Start_Postfix(SelectHirobaObjectBase __instance) {
		foreach (int i in __instance.GetStagesID) {
			if (Plugin.levels.Contains(i)) {
				__instance._isRelease = true;
				return;
			}
		}

		__instance._isRelease = false;
	}

	[HarmonyPrefix, HarmonyPatch(typeof(SelectHirobaObjectBase), nameof(SelectHirobaObjectBase.SetRelease))]
	private static bool SelectHirobaObjectBase_SetRelease_Prefix(SelectHirobaObjectBase __instance) {
		foreach (int i in __instance.GetStagesID) {
			if (Plugin.levels.Contains(i))
				return true;
		}

		return false;
	}

	[HarmonyPrefix, HarmonyPatch(typeof(SelectHirobaObjectBase), nameof(SelectHirobaObjectBase.SetReleaseActiveNormal))]
	private static bool SelectHirobaObjectBase_SetReleaseActiveNormal_Prefix(SelectHirobaObjectBase __instance) {
		foreach (int i in __instance.GetStagesID) {
			if (Plugin.levels.Contains(i))
				return true;
		}

		return false;
	}

	// allows skipping the tutorial
	// makes it possible to permanently lose access to the cousin check
/*	[HarmonyPostfix, HarmonyPatch(typeof(MainGameUIManager), nameof(MainGameUIManager.IsTutorialStep7))]
	private static void MainGameUIManager_IsTutorialStep7_Postfix(ref bool __result) {
		__result = true;
	}*/

	// disables plaza blocks
	[HarmonyPrefix, HarmonyPatch(typeof(SelectHirobaBlockEventObject), nameof(SelectHirobaBlockEventObject.Start))]
	private static void SelectHirobaBlockEventObject_Start_Prefix(SelectHirobaBlockEventObject __instance) {
		__instance._type = SelectHirobaBlockEventObject.Type.Crown;
		__instance._crownIndex = 0;
	}

	// unlocked cousins
	[HarmonyPostfix, HarmonyPatch(typeof(GlobalSaveData), nameof(GlobalSaveData.CheckOujiItokoGet))]
	private static void GlobalSaveData_CheckOujiItokoGet_Postfix(ref bool __result, int ItokoID) {
		if (Plugin.randomizeCousins) {
			__result = Plugin.cousins.Contains(ItokoID);
		}
	}

	// unlocked presents
	[HarmonyPostfix, HarmonyPatch(typeof(GlobalSaveData), nameof(GlobalSaveData.CheckPresentFlag))]
	private static void GlobalSaveData_CheckPresentFlag_Postfix(ref bool __result, int PresentID) {
		if (Plugin.randomizePresents) {
			__result = Plugin.presents.Contains(PresentID);
		}
	}

	// detects crown collection
	[HarmonyPostfix, HarmonyPatch(typeof(MainGameCollectiveItem), nameof(MainGameCollectiveItem.Collected))]
	private static void MainGameCollectiveItem_Collected_Postfix(MainGameCollectiveItem __instance) {
		Plugin.Logger.LogInfo($"Collected Crown: {__instance.name} ({__instance.CollectID})");

		if (Plugin.randomizeCrowns) {
			Plugin.archipelagoClient.SendCheck(__instance.CollectID + Plugin.CROWN_ID_OFFSET);
		}
	}

	// detects freebie collection
	[HarmonyPostfix, HarmonyPatch(typeof(MainGameItemBox), nameof(MainGameItemBox.Collected))]
	private static void MainGameItemBox_Collected_Postfix(MainGameItemBox __instance) {
		Plugin.Logger.LogInfo($"Collected ItemBox: {__instance.name}");
	}

	// detects present collection
	[HarmonyPostfix, HarmonyPatch(typeof(MainGameManager), nameof(MainGameManager.RequestPresentMessage))]
	private static void MainGameManager_RequestPresentMessage_Postfix(MainGameManager __instance) {
		Plugin.Logger.LogInfo($"Collected Present: {__instance._presentRolled}");

		if (Plugin.randomizePresents) {
			Plugin.archipelagoClient.SendCheck(__instance._presentRolled + Plugin.PRESENT_ID_OFFSET);
		}
	}

	// detects cousin collection
	[HarmonyPostfix, HarmonyPatch(typeof(MainGameManager), nameof(MainGameManager.SetItokoRolled))]
	private static void MainGameManager_SetItokoRolled_Postfix(int ItokoID) {
		Plugin.Logger.LogInfo($"Collected cousin: {ItokoID}");

		if (Plugin.randomizeCousins) {
			Plugin.archipelagoClient.SendCheck(ItokoID + Plugin.COUSIN_ID_OFFSET);
		}
	}

	// detects stage clear
	[HarmonyPostfix, HarmonyPatch(typeof(MainGameManager), nameof(MainGameManager.MakeStageCleared))]
	private static void MainGameManager_MakeStageCleared_Postfix(MainGameManager __instance) {
		Plugin.Logger.LogInfo($"Cleared stage: {__instance.StageIdx}");

		if (__instance.StageIdx == 51) {
			Plugin.archipelagoClient.Goal();
		} else {
			Plugin.archipelagoClient.SendCheck(__instance.StageIdx + Plugin.LEVEL_ID_OFFSET);

			if (Plugin.planetsOnClear) {
				Plugin.archipelagoClient.SendCheck(__instance.StageIdx + Plugin.PLANET_ID_OFFSET);
			}
		}
	}

	// make sure the final stage can always be completed once you have it
	[HarmonyPostfix, HarmonyPatch(typeof(MainGameManager), nameof(MainGameManager.AddKatamariSize))]
	private static void MainGameManager_AddKatamariSize_Postfix(MainGameManager __instance) {
		if (__instance.StageIdx == 51 && __instance.KatamariSize < 30000 && Plugin.easyFinale) {
			__instance._katamariSize = 30000;
		}
	}

	// mark levels as cleared/uncleared to determine which levels are unlocked
	[HarmonyPostfix, HarmonyPatch(typeof(GlobalSaveData), nameof(GlobalSaveData.IsClearTargetStage))]
	private static void GlobalSaveData_IsClearTargetStage_Postfix(ref bool __result, StarIDEnum starID) {
		int stageId = SelectHirobaManager.ConvertStageID(starID);

		if (stageId == 4 || stageId == 19) __result = Plugin.levels.Contains(4) && Plugin.levels.Contains(19);
		else if (stageId == 11 || stageId == 31) __result = Plugin.levels.Contains(11) && Plugin.levels.Contains(31);
		else if (stageId == 5 || stageId == 21) __result = Plugin.levels.Contains(5) && Plugin.levels.Contains(21);
		else if (stageId == 15 || stageId == 24) __result = Plugin.levels.Contains(15) && Plugin.levels.Contains(24);
		else if (stageId == 34 || stageId == 36) __result = Plugin.levels.Contains(34) && Plugin.levels.Contains(36);
		else if (stageId == 56 || stageId == 61) __result = Plugin.levels.Contains(56) && Plugin.levels.Contains(61);
		else if (stageId == 6 || stageId == 42) __result = Plugin.levels.Contains(6) && Plugin.levels.Contains(42);
		else if (stageId == 53 || stageId == 54) __result = Plugin.levels.Contains(53) && Plugin.levels.Contains(54);
		else if (stageId == 26 || stageId == 29 || stageId == 30) {
			if (Plugin.levels.Contains(26) && Plugin.levels.Contains(29) && Plugin.levels.Contains(30)) __result = true;
			else if (Plugin.levels.Contains(26) && (Plugin.levels.Contains(29) || Plugin.levels.Contains(30))) {
				__result = stageId == 26;
			} else if (Plugin.levels.Contains(29) && Plugin.levels.Contains(30)) {
				__result = stageId == 29;
			} else {
				__result = false;
			}
		}
	}

	// loads the correct stage
	[HarmonyPostfix, HarmonyPatch(typeof(SelectHirobaKingController), nameof(SelectHirobaKingController.Update))]
	private static void SelectHirobaKingController_Update_Postfix(SelectHirobaKingController __instance) {
		if (__instance._stages != null && Plugin.fansToStages.ContainsKey(__instance._myStageID)) {
			__instance._stages = Plugin.fansToStages[__instance._myStageID].ToArray();
		}
	}

	// allows you to pick up any cousin in the same playthrough
	[HarmonyPrefix, HarmonyPatch(typeof(MainGameMonoBase), nameof(MainGameMonoBase.PermaDelete))]
	private static bool MainGameMonoBase_PermaDelete_Prefix() {
		return false;
	}

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

	// runs when the tutorial first plays
	[HarmonyPostfix, HarmonyPatch(typeof(UITutorialManager), nameof(UITutorialManager.Start))]
	private static void UITutorialManager_Start_Postfix(ref UITutorialManager __instance) {
		GlobalSaveData data = __instance.globalMan.glbSave;

		data._progression = 34; // how far along the story you are. not sure if this is actually needed anymore
		data.Big1Start1st = true; // allows leaving ALAP1 on first playthrough
		data._firstSelectHiroba = true; // sets the SS Prince as having already been repaired
		data.FirstCharaCustom = true; // skips the forced go check out the customization
		data._stageIndex = (int) SelectHirobaEnum.Stage.EDO; // makes the first era you go to after the tutorial Edo Japan

		// marks all events as having already been done
		// prevents the king forcing you to go into certain eras on a whim
		// also skips the repair the S.S. Prince event
		// and more importantly, prevents softlocks that can happen when you receive certain/too many levels at the wrong/same time
		for (int i = 0; i < data._messageEvent.Count; i++) {
			data._messageEvent[i] = true;
		}
	}

	[HarmonyPostfix, HarmonyPatch(typeof(MainGameManager), nameof(MainGameManager.Update))]
	private static void MainGameManager_Update_Postfix(MainGameManager __instance) {
		// don't interfere with the tutorial or that hole...
		if (__instance.StageIdx == 20 || __instance.StageIdx == 51) return;

		// Handle items
		if (__instance.IsPlayerInitiated && __instance.GetInventoryItem() == eInstageItemType.NoItem && Plugin.items.Count > 0) {
			eInstageItemType item = Plugin.items.Dequeue();
			__instance.SetInventoryItem(item);
		}

		// Handle deathlink
		Plugin.archipelagoClient.CheckDeathLink(__instance);
	}

	[HarmonyPrefix, HarmonyPatch(typeof(MainGameManager), nameof(MainGameManager.EndGame))]
	private static void MainGameManager_EndGame_Prefix(MainGameManager __instance, ref bool force) {
		// force is true when killed by deathlink
		if (force) {
			force = false;
		} else if (!__instance.IsStageClear()) {
			Plugin.archipelagoClient.SendDeathLink();
		}
	}
}
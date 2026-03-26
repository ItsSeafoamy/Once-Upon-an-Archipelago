using App.KatamariSin;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using OnceUponAnArchipelago.Archipelago;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OnceUponAnArchipelago.Patcher;

public class SelectHirobaPatcher {

	// Make all eras available
	[HarmonyPostfix, HarmonyPatch(typeof(SelectMapController), nameof(SelectMapController.GetMapRelease))]
	private static void SelectMapController_GetMapRelease_Postfix(ref bool __result, SelectHirobaEnum.Stage map) {
		__result = true;
	}

	// Select Scroll
	[HarmonyPostfix, HarmonyPatch(typeof(MissionItem), nameof(MissionItem.CheckRelease))]
	private static void MissionItem_CheckRelease_Postfix(ref bool __result, MissionItem __instance) {
		if ((int)__instance._eStageID < 1) __result = true;
		else __result = Plugin.levels.Contains((int)__instance._eStageID);
	}

	// show what checks you've done in the select scroll
	[HarmonyPostfix, HarmonyPatch(typeof(SubjectListDataSet), nameof(SubjectListDataSet.SetMyList))]
	private static void SubjectListDataSet_SetMyList_Postfix(SubjectListDataSet __instance, MissionItem item) {
		item._isRelease = item.CheckRelease();

		if (!item._isRelease || (int)item._eStageID < 1 || (int)item._eStageID == 51) return;

		if (Plugin.randomizePresents && item._presentID >= 0) {
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

		if (Plugin.randomizeCrowns) {
			int stageId = (int)item._eStageID;
			int[] crownIds = GlobalManager.instance.GetStageCollective(stageId);

			for (int i = 0; i < 3; i++) {
				Image image = __instance._collectiveImages[i];

				if (ArchipelagoClient.ServerData.CheckedLocations.Contains(Plugin.CROWN_ID_OFFSET + crownIds[i])) {
					image.enabled = true;
				} else {
					image.enabled = false;
				}
			}

			__instance._collectiveCanvas.SetActive(true);
		}

		Image clearImage = __instance._itokoImages[3];
		if (ArchipelagoClient.ServerData.CheckedLocations.Contains(Plugin.LEVEL_ID_OFFSET + (int)item._eStageID)) {
			clearImage.enabled = true;

			clearImage.sprite = Plugin.clearSprite;
			clearImage.color = Color.white;
		} else {
			clearImage.enabled = false;
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

		List<int> specialFans = [28, 32, 34, 40];

		foreach (int i in __instance.GetStagesID) {
			if (stages.Contains(i)) {
				if (specialFans.Contains(__instance.ObjectId)) {
					__instance.SetReleaseActiveNormal();
					return;
				} else {
					__instance.SetRelease();
					return;
				}
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

	// disables plaza blocks
	[HarmonyPrefix, HarmonyPatch(typeof(SelectHirobaBlockEventObject), nameof(SelectHirobaBlockEventObject.Start))]
	private static void SelectHirobaBlockEventObject_Start_Prefix(SelectHirobaBlockEventObject __instance) {
		__instance._type = SelectHirobaBlockEventObject.Type.Crown;
		__instance._crownIndex = 0;
	}

	// mark levels as cleared/uncleared to determine which levels are unlocked
	[HarmonyPostfix, HarmonyPatch(typeof(GlobalSaveData), nameof(GlobalSaveData.IsClearTargetStage))]
	private static void GlobalSaveData_IsClearTargetStage_Postfix(ref bool __result, StarIDEnum starID) {
		int stageId = SelectHirobaManager.ConvertStageID(starID);

		if (stageId == 4 || stageId == 19) __result = IsStageMarkedClear(stageId, 4, 19);
		else if (stageId == 5 || stageId == 21) __result = IsStageMarkedClear(stageId, 5, 21);
		else if (stageId == 6 || stageId == 42) __result = IsStageMarkedClear(stageId, 6, 42);
		else if (stageId == 11 || stageId == 31) __result = IsStageMarkedClear(stageId, 11, 31);
		else if (stageId == 15 || stageId == 24) __result = IsStageMarkedClear(stageId, 15, 24);
		else if (stageId == 34 || stageId == 36) __result = IsStageMarkedClear(stageId, 34, 36);
		else if (stageId == 53 || stageId == 54) __result = IsStageMarkedClear(stageId, 53, 54);
		else if (stageId == 56 || stageId == 61) __result = IsStageMarkedClear(stageId, 56, 61);
		else if (stageId == 26 || stageId == 29 || stageId == 30) {
			int owned = 0;
			if (Plugin.levels.Contains(26)) owned++;
			if (Plugin.levels.Contains(29)) owned++;
			if (Plugin.levels.Contains(30)) owned++;

			__result = owned switch {
				3 => true,
				2 => stageId == 26,
				_ => false
			};
		}
	}

	private static bool IsStageMarkedClear(int stageId, int a, int b) {
		if (Plugin.levels.Contains(a) && Plugin.levels.Contains(b)) {
			if (a == 6) {
				GlobalSaveData.instance._forBranching = true; // fix for As AFAP Ghost Ship
			}

			return stageId == a;
		} else return false;
	}

	// sets a fan message for ALAP1
	// in vanilla, it's impossible to not have AFAP1, so no message for ALAP1 exists, which createes a null reference
	[HarmonyPostfix, HarmonyPatch(typeof(SelectHirobaFanMessage), nameof(SelectHirobaFanMessage.SetMessage))]
	private static void SelectHirobaFanMessage_SetMessage_Postfix(SelectHirobaFanMessage __instance, string messageID) {
		if (messageID == "FAN_M02_1_") {
			__instance._fanMessages[0] = __instance._fanMessages[1];
		}
	}

	// loads the correct stage
	[HarmonyPostfix, HarmonyPatch(typeof(SelectHirobaKingController), nameof(SelectHirobaKingController.Update))]
	private static void SelectHirobaKingController_Update_Postfix(SelectHirobaKingController __instance) {
		if (__instance._stages != null && Plugin.fansToStages.ContainsKey(__instance._myStageID)) {
			__instance._stages = Plugin.fansToStages[__instance._myStageID].ToArray();
		}
	}
}
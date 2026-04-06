using App.KatamariSin;
using HarmonyLib;
using UnityEngine;

namespace OnceUponAnArchipelago.Patcher;

public class InGamePatcher {

	private static bool usingMushroom = false;
	private static float spiderTimer = 0f;

	// detects crown collection
	[HarmonyPostfix, HarmonyPatch(typeof(MainGameCollectiveItem), nameof(MainGameCollectiveItem.Collected))]
	private static void MainGameCollectiveItem_Collected_Postfix(MainGameCollectiveItem __instance) {
		Plugin.Logger.LogInfo($"Collected Crown: {__instance.name} ({__instance.CollectID})");

		if (Plugin.randomizeCrowns) {
			Plugin.archipelagoClient.SendCheck(__instance.CollectID + Plugin.CROWN_ID_OFFSET);
		}
	}

	// detects present collection
	[HarmonyPostfix, HarmonyPatch(typeof(MainGameManager), nameof(MainGameManager.RequestPresentMessage))]
	private static void MainGameManager_RequestPresentMessage_Postfix(MainGameManager __instance) {
		Plugin.Logger.LogInfo($"Collected Present: {GlobalManager.instance.AllStageData.list[__instance._stageIdx].PresentID}");

		if (Plugin.randomizePresents) {
			Plugin.archipelagoClient.SendCheck(GlobalManager.instance.AllStageData.list[__instance._stageIdx].PresentID + Plugin.PRESENT_ID_OFFSET);
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

	// fix for mushrooms deleting other freebie pickups
	[HarmonyPrefix, HarmonyPatch(typeof(MainGameItemBox), nameof(MainGameItemBox.Collected))]
	private static bool MainGameItemBox_Collected_Prefix() {
		return GlobalManager.Instance.GameMan._itemEffectTimer <= 0f;
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

	// allows you to pick up any cousin in the same playthrough
	[HarmonyPrefix, HarmonyPatch(typeof(MainGameMonoBase), nameof(MainGameMonoBase.PermaDelete))]
	private static bool MainGameMonoBase_PermaDelete_Prefix() {
		return false;
	}

	[HarmonyPostfix, HarmonyPatch(typeof(MainGameManager), nameof(MainGameManager.Start))]
	private static void MainGameManager_Start_Postfix(MainGameManager __instance) {
		if (Plugin.itemsToSkip == -1) {
			Plugin.LoadArchipelagoData();
		}
	}

	[HarmonyPostfix, HarmonyPatch(typeof(MainGameManager), nameof(MainGameManager.Update))]
	private static void MainGameManager_Update_Postfix(MainGameManager __instance) {
		// make sure we're in a safe state to mess with
		if (__instance.StageIdx == 20 || __instance.StageIdx == 51
			|| __instance.IsInDebug
			|| __instance.IsInDemo
			|| __instance._isInPause
			|| __instance.IsInConfirm
			|| __instance.MainUI.IsTipsActive()
			|| !__instance.isStageLoaded
			|| __instance.isStageLoading
			|| !__instance.IsPlayerInitiated
			|| __instance.IsInDemoCamera
			|| __instance._isInDemoFade
			|| __instance.IsInRestartWait
		) return;

		// Handle items
		if (__instance.GetInventoryItem() == eInstageItemType.NoItem && __instance._itemEffectTimer <= 0f && Plugin.items.Count > 0) {
			eInstageItemType item = Plugin.items.Dequeue();

			if (Plugin.itemsToSkip > 0) {
				Plugin.itemsToSkip--;
			} else {
				__instance.SetInventoryItem(item);

				Plugin.usedItemCount++;
				Plugin.SaveArchipelagoData();
			}
		}

		// Spider trap handler
		if (spiderTimer > 0) {
			spiderTimer -= Time.deltaTime;

			if (spiderTimer <= 0) {
				__instance.RequestSpiderDamageDemo_End();
				spiderTimer = 0f;
			}
		}

		// Handle traps
		if (Plugin.traps.Count > 0 && spiderTimer <= 0) {
			int trapId = Plugin.traps.Dequeue();

			if (Plugin.trapsToSkip > 0) {
				Plugin.trapsToSkip--;
			} else {
				if (trapId == (int)eInstageItemType.Spider) {
					__instance.RequestSpiderDamageDemo_Start();
					spiderTimer = 10f;
				} else if (trapId == (int)eInstageItemType.Tarai) { // washpan
					__instance.RequestTaraiDamageDemo();
					__instance.DebugSubKatamariSize((int)(__instance.KatamariSize * 0.1f));
				}

				Plugin.usedTrapCount++;
				Plugin.SaveArchipelagoData();
			}
		}

		// Handle deathlink
		Plugin.archipelagoClient.CheckDeathLink(__instance);
	}

	// Mushroom Item
	[HarmonyPrefix, HarmonyPatch(typeof(MainGameManager), nameof(MainGameManager.ActivateItemEffect))]
	private static void MainGameManager_ActivateItemEffect_Prefix(MainGameManager __instance) {
		if (__instance.GetInventoryItem() == eInstageItemType.Mushroom) {
			__instance.AddKatamariSize((int)(__instance.KatamariSize * 0.1f));

			usingMushroom = true;
		} else if (__instance.GetInventoryItem() == eInstageItemType.SP_Mushroom) {
			__instance.AddKatamariSize((int)(__instance.KatamariSize * 0.2f));

			usingMushroom = true;
		}
	}

	// Fix for mushroom model immediately disappearing on use
	[HarmonyPostfix, HarmonyPatch(typeof(MainGameManager), nameof(MainGameManager.ActivateItemEffect))]
	private static void MainGameManager_ActivateItemEffect_Postfix(MainGameManager __instance) {
		if (usingMushroom) {
			__instance._itemEffectTimer = 3f;
			usingMushroom = false;
		}
	}

	// Spider speed multiplier
	[HarmonyPostfix, HarmonyPatch(typeof(MainGameManager), nameof(MainGameManager.GetItemMultiplier))]
	private static void MainGameCore_GetItemMultiplier_Postfix(ref float __result, eInstageItemType checkItem) {
		// Game always uses Drink (Rocket) multiplier for player speed, regardless if they're actually using a rocket or not
		if (checkItem == eInstageItemType.Drink && spiderTimer > 0) {
			__result = 0.5f;
		}
	}

	// deathlink
	[HarmonyPrefix, HarmonyPatch(typeof(MainGameManager), nameof(MainGameManager.EndGame))]
	private static void MainGameManager_EndGame_Prefix(MainGameManager __instance, ref bool force) {
		// force is true when killed by deathlink
		if (force) {
			force = false;
		} else if (!__instance.IsStageClear() && !__instance._hasCleared) {
			Plugin.archipelagoClient.SendDeathLink();
		}
	}

	// runs when the tutorial first plays
	[HarmonyPostfix, HarmonyPatch(typeof(UITutorialManager), nameof(UITutorialManager.Start))]
	private static void UITutorialManager_Start_Postfix(ref UITutorialManager __instance) {
		GlobalSaveData data = __instance.globalMan.glbSave;

		data._progression = 34; // how far along the story you are. not sure if this is actually needed anymore
		data.Big1Start1st = true; // allows leaving ALAP1 on first playthrough
		data._firstSelectHiroba = true; // sets the SS Prince as having already been repaired
		data._stageIndex = (int)SelectHirobaEnum.Stage.EDO; // makes the first era you go to after the tutorial Edo Japan

		// marks all events as having already been done
		// prevents the king forcing you to go into certain eras on a whim
		// also skips the repair the S.S. Prince event
		// and more importantly, prevents softlocks that can happen when you receive certain/too many levels at the wrong/same time
		for (int i = 0; i < data._messageEvent.Count; i++) {
			data._messageEvent[i] = true;
		}

		Plugin.DeleteArchipelagoData();
	}
}
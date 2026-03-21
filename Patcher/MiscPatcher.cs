using App.KatamariSin;
using HarmonyLib;

namespace OnceUponAnArchipelago.Patcher;

public class MiscPatcher {

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
}
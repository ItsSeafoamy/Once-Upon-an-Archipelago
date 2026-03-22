using App.KatamariSin;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

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

	// Load the katamari sprite which gets used as the clear icon in the select scroll
	[HarmonyPostfix, HarmonyPatch(typeof(AssetBundle), nameof(AssetBundle.LoadFromFileAsync), argumentTypes: [typeof(string), typeof(uint)])]
	private static void AssetBundle_LoadAssetAsync_Postfix(string path, AssetBundleCreateRequest __result) {
		if (path.Contains("duplicateassetisolation2_assets_all_")) {
			var action = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<AsyncOperation>>((System.Action<AsyncOperation>)((v) => {
				AssetBundle bundle = __result.assetBundle;
				AssetBundleRequest req = bundle.LoadAssetAsync("Assets/GameResource/Miscs/UI/Core/Core_02_Tutorial.png", Il2CppType.Of<Sprite>());
				
				var assetAction = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<AsyncOperation>>((System.Action<AsyncOperation>)((v) => {
					Plugin.Logger.LogInfo(req.asset.name);

					Sprite sprite = req.asset.TryCast<Sprite>();
					Plugin.clearSprite = sprite;
				}));

				req.add_completed(assetAction);
			}));

			__result.add_completed(action);
		}
	}
}
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using App.KatamariSin;
using HarmonyLib;
using OnceUponAnArchipelago.Archipelago;
using System.Collections.Generic;
using BepInEx.Configuration;
using TMPro;
using OnceUponAnArchipelago.Patcher;

namespace OnceUponAnArchipelago;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin {
	internal static ManualLogSource Logger;

	public static ArchipelagoClient archipelagoClient;
	public static UITextSetter apConnectionUI;
	public static string apConnectionText = "<color=red>Archipelago: Not Connected</color>";

	public static TextMeshProUGUI planetsText;

	public static List<int> levels = [];
	public static List<int> cousins = [];
	public static List<int> presents = [];
	public static Queue<eInstageItemType> items = [];

	public static int planets = 0;
	public static int planetsNeeded;
	public static bool planetsOnClear;
	public static bool randomizeCousins;
	public static bool randomizePresents;
	public static bool randomizeCrowns;
	public static bool easyFinale;

	public static Dictionary<int, List<int>> fansToStages = [];
	public static Dictionary<int, string> levelNames = [];

	public const int LEVEL_ID_OFFSET = 0;
	public const int COUSIN_ID_OFFSET = 1_000;
	public const int CROWN_ID_OFFSET = 2_000;
	public const int PRESENT_ID_OFFSET = 3_000;
	public const int PLANET_ID_OFFSET = 4_000;
	public const int FILLER_ID_OFFSET = 5_000;
	public const int FREEBIE_ID_OFFSET = 6_000;

	public override void Load() {
		// Plugin startup logic
		Logger = Log;

		// load config
		ConfigEntry<string> uri = Config.Bind("Archipelago", "serverAddress", "archipelago.gg:12345", "The url to connect to, including port");
		ConfigEntry<string> slotName = Config.Bind("Archipelago", "slotName", "Player1", "The name of the slot to connect to");
		ConfigEntry<string> password = Config.Bind("Archipelago", "password", "", "The server password. Leave blank if there is none");
		easyFinale = Config.Bind("General", "easyFinale", true, "Allows you to plug the hole after rolling up just one item. This removes the requirement to beat lots of stages first. Logic assumes enabled.").Value;

		// connect to archipelago
		archipelagoClient = new ArchipelagoClient();
		ArchipelagoClient.ServerData.Uri = uri.Value;
		ArchipelagoClient.ServerData.SlotName = slotName.Value;
		ArchipelagoClient.ServerData.Password = password.Value;
		archipelagoClient.Connect();

		levelNames[51] = "That Hole...";

		Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

		Harmony.CreateAndPatchAll(typeof(InGamePatcher));
		Harmony.CreateAndPatchAll(typeof(SelectHirobaPatcher));
		Harmony.CreateAndPatchAll(typeof(UIPatcher));
		Harmony.CreateAndPatchAll(typeof(MiscPatcher));
	}

	public static void SetApConnectionText(string text) {
		apConnectionText = text;
		apConnectionUI?.SetText(apConnectionText);
	}

	public static void SetPlanetsText(int planetsOwned, int planetsNeeded) {
		planetsText?.SetText($"Planets: {planetsOwned} / {planetsNeeded}");
	}
}
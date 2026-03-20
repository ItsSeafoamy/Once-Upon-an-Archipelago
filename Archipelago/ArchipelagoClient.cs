using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using App.KatamariSin;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Packets;
namespace OnceUponAnArchipelago.Archipelago;

public class ArchipelagoClient {
	public const string APVersion = "0.6.6";
	private const string Game = "Once Upon a Katamari";

	public static bool Authenticated;
	private bool attemptingConnection;

	public static ArchipelagoData ServerData = new();
	private DeathLinkHandler DeathLinkHandler;
	private ArchipelagoSession session;

	/// <summary>
	/// call to connect to an Archipelago session. Connection info should already be set up on ServerData
	/// </summary>§
	/// <returns></returns>
	public void Connect() {
		if (Authenticated || attemptingConnection) return;

		try {
			session = ArchipelagoSessionFactory.CreateSession(ServerData.Uri);
			SetupSession();
		} catch (Exception e) {
			Plugin.Logger.LogError(e);
		}

		TryConnect();
	}

	/// <summary>
	/// add handlers for Archipelago events
	/// </summary>
	private void SetupSession() {
		session.Items.ItemReceived += OnItemReceived;
		session.Locations.CheckedLocationsUpdated += OnLocationChecked;
		session.Socket.ErrorReceived += OnSessionErrorReceived;
		session.Socket.SocketClosed += OnSessionSocketClosed;
	}

	/// <summary>
	/// attempt to connect to the server with our connection info
	/// </summary>
	private void TryConnect() {
		try {
			HandleConnectResult(
					session.TryConnectAndLogin(
						Game,
						ServerData.SlotName,
						ItemsHandlingFlags.AllItems,
						new Version(APVersion),
						password: ServerData.Password,
						requestSlotData: true // ServerData.NeedSlotData
					));
		} catch (Exception e) {
			Plugin.Logger.LogError(e);
			HandleConnectResult(new LoginFailure(e.ToString()));
			attemptingConnection = false;
		}
	}

	/// <summary>
	/// handle the connection result and do things
	/// </summary>
	/// <param name="result"></param>
	private void HandleConnectResult(LoginResult result) {
		string outText;
		if (result.Successful) {
			var success = (LoginSuccessful) result;

			ServerData.SetupSession(success.SlotData, session.RoomState.Seed);
			Authenticated = true;

			DeathLinkHandler = new(session.CreateDeathLinkService(), ServerData.SlotName, (long) success.SlotData["death_link"] == 1);
			session.Locations.CompleteLocationChecksAsync(ServerData.CheckedLocations.ToArray());
			outText = $"Successfully connected to {ServerData.Uri} as {ServerData.SlotName}!";

			long planetCount = (long) success.SlotData["number_of_planets"];
			long planetRequirement = (long) success.SlotData["planets_requirement"];

			Plugin.planetsNeeded = (int) Math.Max(1, Math.Floor(planetCount * (planetRequirement / 100f)));
			Plugin.planetsOnClear = (long) success.SlotData["planets_on_clear"] == 1;
			Plugin.randomizeCousins = (long) success.SlotData["randomize_cousins"] == 1;
			Plugin.randomizePresents = (long) success.SlotData["randomize_presents"] == 1;
			Plugin.randomizeCrowns = (long) success.SlotData["randomize_crowns"] == 1;

			Plugin.Logger.LogMessage(outText);

			Plugin.SetApConnectionText($"Archipelago: Connected");
		} else {
			var failure = (LoginFailure)result;
			outText = $"Failed to connect to {ServerData.Uri} as {ServerData.SlotName}.";
			outText = failure.Errors.Aggregate(outText, (current, error) => current + $"\n    {error}");

			Plugin.Logger.LogError(outText);

			Authenticated = false;
			Disconnect();
		}

		Plugin.Logger.LogMessage(outText);
		attemptingConnection = false;
	}

	/// <summary>
	/// something went wrong, or we need to properly disconnect from the server. cleanup and re null our session
	/// </summary>
	private void Disconnect() {
		Plugin.Logger.LogDebug("disconnecting from server...");
		session?.Socket.DisconnectAsync();
		session = null;
		Authenticated = false;

		Plugin.SetApConnectionText("<color=red>Archipelago: Not Connected</color>");
	}

	public void SendMessage(string message) {
		session.Socket.SendPacketAsync(new SayPacket { Text = message });
	}

	public void SendCheck(int location) {
		session.Locations.CompleteLocationChecks(location);
	}

	public void Goal() {
		session.SetGoalAchieved();
	}

	public void CheckDeathLink(MainGameManager man) {
		DeathLinkHandler.KillPlayer(man);
	}

	public void SendDeathLink() {
		DeathLinkHandler.SendDeathLink();
	}

	/// <summary>
	/// we received an item so reward it here
	/// </summary>
	/// <param name="helper">item helper which we can grab our item from</param>
	private void OnItemReceived(ReceivedItemsHelper helper) {
		var receivedItem = helper.DequeueItem();

		if (helper.Index <= ServerData.Index) return;

		ServerData.Index++;

		int id = (int) receivedItem.ItemId;
		Plugin.Logger.LogInfo($"Received item: {receivedItem.ItemName} ({id}) from {receivedItem.Player.Name}");

		if (id >= Plugin.FREEBIE_ID_OFFSET) {
			Plugin.items.Enqueue((eInstageItemType)(id - Plugin.FREEBIE_ID_OFFSET));
		} else if (id >= Plugin.FILLER_ID_OFFSET) {
			return;
		} else if (id >= Plugin.PLANET_ID_OFFSET) {
			Plugin.planets++;
			Plugin.SetPlanetsText(Plugin.planets, Plugin.planetsNeeded);

			if (Plugin.planets >= Plugin.planetsNeeded) {
				Plugin.levels.Add(51); // final level (That Hole...)
			}
		} else if (id >= Plugin.PRESENT_ID_OFFSET) {
			Plugin.presents.Add(id - Plugin.PRESENT_ID_OFFSET);
		} else if (id >= Plugin.COUSIN_ID_OFFSET) {
			Plugin.cousins.Add(id - Plugin.COUSIN_ID_OFFSET);
		} else if (id >= Plugin.LEVEL_ID_OFFSET) {
			Plugin.levels.Add(id - Plugin.LEVEL_ID_OFFSET);
			Plugin.levelNames[id - Plugin.LEVEL_ID_OFFSET] = receivedItem.ItemName;
		}
	}

	private void OnLocationChecked(ReadOnlyCollection<long> newCheckedLocations) {
		ServerData.CheckedLocations.AddRange(newCheckedLocations);
		
		foreach (long loc in newCheckedLocations) {
			Plugin.Logger.LogInfo($"Checked location: {loc}");
		}
	}

	/// <summary>
	/// something went wrong with our socket connection
	/// </summary>
	/// <param name="e">thrown exception from our socket</param>
	/// <param name="message">message received from the server</param>
	private void OnSessionErrorReceived(Exception e, string message) {
		if (e is WebSocketException) Disconnect();
		Plugin.Logger.LogError(e);
	}

	/// <summary>
	/// something went wrong closing our connection. disconnect and clean up
	/// </summary>
	/// <param name="reason"></param>
	private void OnSessionSocketClosed(string reason) {
		Plugin.Logger.LogError($"Connection to Archipelago lost: {reason}");
		Disconnect();
	}
}
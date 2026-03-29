using System;
using System.Collections.Generic;
using App.KatamariSin;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using BepInEx;

namespace OnceUponAnArchipelago.Archipelago;

public class DeathLinkHandler {
	private static bool deathLinkEnabled;
	private string slotName;
	private readonly DeathLinkService service;
	private readonly Queue<DeathLink> deathLinks = new();

	/// <summary>
	/// instantiates our death link handler, sets up the hook for receiving death links, and enables death link if needed
	/// </summary>
	/// <param name="deathLinkService">The new DeathLinkService that our handler will use to send and
	/// receive death links</param>
	/// <param name="enableDeathLink">Whether we should enable death link or not on startup</param>
	public DeathLinkHandler(DeathLinkService deathLinkService, string name, bool enableDeathLink = false) {
		service = deathLinkService;
		service.OnDeathLinkReceived += DeathLinkReceived;
		slotName = name;
		deathLinkEnabled = enableDeathLink;

		if (deathLinkEnabled) {
			service.EnableDeathLink();
		}
	}

	/// <summary>
	/// enables/disables death link
	/// </summary>
	public void ToggleDeathLink() {
		deathLinkEnabled = !deathLinkEnabled;

		if (deathLinkEnabled) {
			service.EnableDeathLink();
		} else {
			service.DisableDeathLink();
		}
	}

	/// <summary>
	/// what happens when we receive a deathLink
	/// </summary>
	/// <param name="deathLink">Received Death Link object to handle</param>
	private void DeathLinkReceived(DeathLink deathLink) {
		deathLinks.Enqueue(deathLink);

		Plugin.Logger.LogInfo("Queing deathlink: " + (deathLink.Cause.IsNullOrWhiteSpace() ? $"{deathLink.Source} died" : deathLink.Cause));
	}

	/// <summary>
	/// can be called when in a valid state to kill the player, dequeueing and immediately killing the player with a
	/// message if we have a death link in the queue
	/// </summary>
	public void KillPlayer(MainGameManager man) {
		try {
			if (deathLinks.Count < 1) return;

			DeathLink deathLink = deathLinks.Dequeue();
			deathLinks.Clear();
			string cause = "Triggering deathlink: " + (deathLink.Cause.IsNullOrWhiteSpace() ? $"{deathLink.Source} died" : deathLink.Cause);

			man.EndGame(true);
			Plugin.Logger.LogInfo(cause);
		} catch (Exception e) {
			Plugin.Logger.LogError(e);
		}
	}

	/// <summary>
	/// returns message for the player to see when a death link is received without a cause
	/// </summary>
	/// <param name="deathLink">death link object to get relevant info from</param>
	/// <returns></returns>
	private string GetDeathLinkCause(DeathLink deathLink) {
		return $"{deathLink.Source} died";
	}

	/// <summary>
	/// called to send a death link to the multiworld
	/// </summary>
	public void SendDeathLink() {
		try {
			if (!deathLinkEnabled) return;

			Plugin.Logger.LogMessage("sharing your death...");

			// add the cause here
			var linkToSend = new DeathLink(slotName);

			service.SendDeathLink(linkToSend);
		} catch (Exception e) {
			Plugin.Logger.LogError(e);
		}
	}
}
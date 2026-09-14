using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OnceUponAnArchipelago;

public static class UIManager {

	private static TextMeshProUGUI apConnectionUI;

	public static void Init() {
		GameObject canvasObject = new("Archipelago");
		GameObject.DontDestroyOnLoad(canvasObject);
		canvasObject.hideFlags |= HideFlags.HideAndDontSave;
		canvasObject.layer = 5;
		canvasObject.transform.position = new Vector3(0, 0, 1);

		Canvas canvas = canvasObject.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceCamera;
		canvas.referencePixelsPerUnit = 100;
		canvas.sortingOrder = 20_000;
		canvas.overrideSorting = true;

		CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
		scaler.referenceResolution = new Vector2(1920, 1080);
		scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

		apConnectionUI = canvasObject.AddComponent<TextMeshProUGUI>();
		apConnectionUI.color = Color.green;
		SetApConnectionText("<red>Archipelago: Not Connected</red>");
	}

	public static void SetApConnectionText(string text) {
		apConnectionUI?.SetText(text);
	}
}

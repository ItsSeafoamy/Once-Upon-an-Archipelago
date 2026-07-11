using App.KatamariSin;
using UnityEngine;

namespace OnceUponAnArchipelago;

public class FogTrap {

	private const float DURATION = 30f;
	private static float fogStart = 0.2f;
	private static float fogEnd = 30f;
	private static Color FOG_COLOR = new(0.0588f, 0.0667f, 0.1686f, 1f);

	private static float time;

	public static void Activate() {
		time = DURATION;
	}

	public static void Update(MainGameManager manager) {
		if (time > 0) {
			if (!manager.fogController.fog || RenderSettings.fogColor != FOG_COLOR) {
				fogStart = manager.Cam.CameraPosition.magnitude * 0.5f;
				fogEnd = fogStart * 100f;
				
				manager.fogController.fog = true;

				RenderSettings.fog = true;
				RenderSettings.fogColor = FOG_COLOR;
				RenderSettings.fogDensity = 0.004f;
				RenderSettings.fogStartDistance = 0;
				RenderSettings.fogEndDistance = fogStart;
				RenderSettings.fogMode = FogMode.Linear;
			}

			float progress = 1 - (time / DURATION);
			RenderSettings.fogEndDistance = Mathf.Lerp(fogStart, fogEnd, progress * progress);

			time -= Time.deltaTime;

			if (time <= 0) {
				time = 0;
				manager.fogController.fog = false;
			}
		}
	}
}

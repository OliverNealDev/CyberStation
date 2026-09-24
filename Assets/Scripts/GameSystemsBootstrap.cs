using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Adds the day cycle, directives, events, stats and the runtime HUD to the station scene
// when it loads, the same way SaveManager bootstraps itself. Doing it here rather than in
// the scene file keeps these systems out of a scene that artists and levels also touch,
// and a scene reload (starting a new station) rebuilds them from scratch.
public static class GameSystemsBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only the station scene has a passenger simulation to attach to.
        if (Object.FindAnyObjectByType<PassengerManager>() == null || Object.FindAnyObjectByType<GameSpeedManager>() != null)
        {
            return;
        }

        // The game is mouse-driven. Keyboard navigation would let Space, the pause key,
        // also press whichever button was clicked last.
        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem != null)
        {
            eventSystem.sendNavigationEvents = false;
        }

        GameObject systems = new GameObject("GameSystems");
        systems.AddComponent<GameSpeedManager>();
        systems.AddComponent<StationClockManager>();
        systems.AddComponent<StationStatsTracker>();
        systems.AddComponent<DirectiveManager>();
        systems.AddComponent<StationEventManager>();
        systems.AddComponent<PassengerVoiceCoordinator>();

        new GameObject("StationHUD").AddComponent<StationHudController>();
    }
}

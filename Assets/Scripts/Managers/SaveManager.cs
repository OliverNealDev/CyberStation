using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

// A system that keeps its own state in the save, under its own key, without SaveManager
// having to know its fields. New systems register themselves; nothing here changes.
public interface ISaveParticipant
{
    string SaveKey { get; }
    string CaptureState();
    void RestoreState(string json);

    // Called instead of RestoreState when the save holds nothing under this key.
    // loadedSaveVersion is 0 for a brand-new station and 1 for a save written before
    // versioning existed, which lets a system tell "new player" from "returning player".
    void OnNoSavedState(int loadedSaveVersion);
}

public class SaveManager : MonoBehaviour
{
    private const string SaveFileName = "savegame.json";
    private const float AutoSaveIntervalSeconds = 60f;

    // 1: the original format, which had no version field (it reads back as 0).
    // 2: adds saveVersion and per-system sections.
    public const int CurrentSaveVersion = 2;
    private const int FirstVersionedSave = 2;

    private static SaveManager instance;
    private static readonly List<ISaveParticipant> participants = new List<ISaveParticipant>();

    private readonly Dictionary<string, string> loadedSections = new Dictionary<string, string>(StringComparer.Ordinal);
    private bool hasDeliveredLoadedState;
    private int loadedSaveVersion;

    private readonly Dictionary<string, ObjectBuildable> buildablesById =
        new Dictionary<string, ObjectBuildable>(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Expansion> expansionsById =
        new Dictionary<string, Expansion>(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Train> trainsById =
        new Dictionary<string, Train>(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, StaffMember> staffById =
        new Dictionary<string, StaffMember>(StringComparer.OrdinalIgnoreCase);

    private Coroutine initialLoadRoutine;
    private bool hasAttemptedInitialLoad;
    private bool isApplyingSave;

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void CyberStationSyncFileSystem();
#endif

    // On the web a write only reaches IndexedDB once the filesystem is flushed, so every
    // path that touches the save file has to call this or the save is lost on tab close.
    private static void FlushSaveFileSystem()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            CyberStationSyncFileSystem();
        }
        catch (Exception exception)
        {
            Debug.LogError($"SaveManager failed to flush the web filesystem: {exception}");
        }
#endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        instance = null;
        participants.Clear();
    }

    public static bool HasSaveFile => File.Exists(Path.Combine(Application.persistentDataPath, SaveFileName));

    public static void RegisterParticipant(ISaveParticipant participant)
    {
        if (participant == null || participants.Contains(participant))
        {
            return;
        }

        participants.Add(participant);

        // Systems created after the load has already happened still get their state.
        if (instance != null && instance.hasDeliveredLoadedState)
        {
            instance.DeliverLoadedState(participant);
        }
    }

    public static void UnregisterParticipant(ISaveParticipant participant)
    {
        participants.Remove(participant);
    }

    // Older saves have no version field, which JsonUtility reads back as 0.
    public static int NormalizeSaveVersion(int storedVersion)
    {
        return storedVersion < FirstVersionedSave ? 1 : storedVersion;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject saveManagerObject = new GameObject("SaveManager");
        instance = saveManagerObject.AddComponent<SaveManager>();
        DontDestroyOnLoad(saveManagerObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        StartCoroutine(AutoSaveRoutine());
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (hasAttemptedInitialLoad || initialLoadRoutine != null)
        {
            return;
        }

        initialLoadRoutine = StartCoroutine(LoadInitialStateRoutine());
    }

    private IEnumerator LoadInitialStateRoutine()
    {
        yield return null;

        while (!AreManagersReady())
        {
            yield return null;
        }

        hasAttemptedInitialLoad = true;
        initialLoadRoutine = null;

        if (!File.Exists(SaveFilePath))
        {
            Debug.Log($"SaveManager found no save file at {SaveFilePath}.");
            CompleteLoad(null, 0);
            yield break;
        }

        GameSaveData saveData;
        try
        {
            string json = File.ReadAllText(SaveFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("SaveManager found an empty save file and skipped loading.");
                CompleteLoad(null, 0);
                yield break;
            }

            saveData = JsonUtility.FromJson<GameSaveData>(json);
        }
        catch (Exception exception)
        {
            Debug.LogError($"SaveManager failed to read save file '{SaveFilePath}': {exception}");
            CompleteLoad(null, 0);
            yield break;
        }

        if (saveData == null)
        {
            Debug.LogWarning("SaveManager could not deserialize the save file.");
            CompleteLoad(null, 0);
            yield break;
        }

        isApplyingSave = true;

        ApplyCoreState(saveData);
        RestoreExpansions(saveData);
        RestorePlacedBuildables(saveData);

        yield return null;

        if (BuildController.Instance != null)
        {
            BuildController.Instance.RefreshLoadedBuildables();
        }

        RestoreTrainState(saveData);

        yield return null;

        RestoreStaffState(saveData);

        isApplyingSave = false;
        CompleteLoad(saveData, NormalizeSaveVersion(saveData.saveVersion));
        Debug.Log($"SaveManager loaded save data (version {loadedSaveVersion}) from {SaveFilePath}.");
    }

    private void CompleteLoad(GameSaveData saveData, int saveVersion)
    {
        loadedSections.Clear();
        loadedSaveVersion = saveVersion;

        if (saveData != null && saveData.sections != null)
        {
            for (int i = 0; i < saveData.sections.Count; i++)
            {
                SaveSection section = saveData.sections[i];
                if (section != null && !string.IsNullOrEmpty(section.key))
                {
                    loadedSections[section.key] = section.json;
                }
            }
        }

        hasDeliveredLoadedState = true;

        List<ISaveParticipant> snapshot = new List<ISaveParticipant>(participants);
        for (int i = 0; i < snapshot.Count; i++)
        {
            DeliverLoadedState(snapshot[i]);
        }
    }

    private void DeliverLoadedState(ISaveParticipant participant)
    {
        try
        {
            if (loadedSections.TryGetValue(participant.SaveKey, out string json) && !string.IsNullOrEmpty(json))
            {
                participant.RestoreState(json);
            }
            else
            {
                participant.OnNoSavedState(loadedSaveVersion);
            }
        }
        catch (Exception exception)
        {
            // One broken section must not stop the rest of the station loading.
            Debug.LogError($"SaveManager failed to restore '{participant.SaveKey}': {exception}");
        }
    }

    private IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            while (!CanSaveOrLoad())
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(AutoSaveIntervalSeconds);

            if (CanSaveOrLoad())
            {
                SaveNow();
            }
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveNow();
        }
    }

    private void OnApplicationQuit()
    {
        SaveNow();
    }

    public void SaveNow()
    {
        if (!CanSaveOrLoad())
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            string json = JsonUtility.ToJson(CaptureCurrentState(), true);
            File.WriteAllText(SaveFilePath, json);
            FlushSaveFileSystem();
        }
        catch (Exception exception)
        {
            Debug.LogError($"SaveManager failed to write save file '{SaveFilePath}': {exception}");
        }
    }

    public static void ResetSaveDataAndRestartScene()
    {
        if (instance == null)
        {
            DeleteSaveFile(Path.Combine(Application.persistentDataPath, SaveFileName));
            ReloadActiveScene();
            return;
        }

        instance.ResetSaveDataAndRestartSceneInternal();
    }

    private void ResetSaveDataAndRestartSceneInternal()
    {
        if (initialLoadRoutine != null)
        {
            StopCoroutine(initialLoadRoutine);
            initialLoadRoutine = null;
        }

        isApplyingSave = false;
        hasAttemptedInitialLoad = false;
        hasDeliveredLoadedState = false;
        loadedSections.Clear();

        DeleteSaveFile(SaveFilePath);
        ReloadActiveScene();
    }

    private static void DeleteSaveFile(string saveFilePath)
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
                FlushSaveFileSystem();
                Debug.Log($"SaveManager deleted save data at {saveFilePath}.");
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"SaveManager failed to delete save file '{saveFilePath}': {exception}");
        }
    }

    private static void ReloadActiveScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
        {
            return;
        }

        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(activeScene.name);
        }
    }

    private bool CanSaveOrLoad()
    {
        return hasAttemptedInitialLoad &&
               !isApplyingSave &&
               AreManagersReady();
    }

    private bool AreManagersReady()
    {
        return EconomyManager.Instance != null &&
               ProgressionManager.Instance != null &&
               ExpansionManager.Instance != null &&
               GridManager.Instance != null &&
               TrainManager.Instance != null &&
               StaffManager.Instance != null &&
               PassengerManager.Instance != null;
    }

    private void ApplyCoreState(GameSaveData saveData)
    {
        EconomyManager.Instance.LoadMoney(saveData.money);
        ProgressionManager.Instance.LoadProgress(saveData.currentXp);
    }

    private void RestoreExpansions(GameSaveData saveData)
    {
        if (saveData.builtExpansionIds == null)
        {
            return;
        }

        CacheExpansions();

        for (int i = 0; i < saveData.builtExpansionIds.Count; i++)
        {
            string expansionId = saveData.builtExpansionIds[i];
            Expansion expansion = ResolveExpansion(expansionId);
            if (expansion == null)
            {
                Debug.LogWarning($"SaveManager could not find expansion '{expansionId}' while loading.");
                continue;
            }

            ExpansionManager.Instance.RestoreBuiltExpansion(expansion);
        }
    }

    private void RestorePlacedBuildables(GameSaveData saveData)
    {
        if (saveData.placedBuildables == null)
        {
            return;
        }

        CacheBuildables();

        for (int i = 0; i < saveData.placedBuildables.Count; i++)
        {
            PlacedBuildableSaveData placedData = saveData.placedBuildables[i];
            ObjectBuildable buildable = ResolveBuildable(placedData.buildableId);
            if (buildable == null || buildable.prefab == null)
            {
                Debug.LogWarning($"SaveManager could not find buildable '{placedData.buildableId}' while loading.");
                continue;
            }

            if (!GridManager.Instance.IsAreaWithinBounds(
                    placedData.gridX,
                    placedData.gridY,
                    Mathf.Max(1, placedData.sizeX),
                    Mathf.Max(1, placedData.sizeY)))
            {
                Debug.LogWarning($"SaveManager skipped out-of-bounds buildable '{placedData.buildableId}'.");
                continue;
            }

            if (!GridManager.Instance.IsAreaFree(
                    placedData.gridX,
                    placedData.gridY,
                    Mathf.Max(1, placedData.sizeX),
                    Mathf.Max(1, placedData.sizeY)))
            {
                Debug.LogWarning($"SaveManager skipped occupied buildable '{placedData.buildableId}' at {placedData.gridX},{placedData.gridY}.");
                continue;
            }

            GameObject placedObject = Instantiate(
                buildable.prefab,
                placedData.position,
                Quaternion.Euler(0f, placedData.rotationY, 0f));

            PreviewableObject previewableObject = placedObject.GetComponent<PreviewableObject>();
            if (previewableObject != null)
            {
                previewableObject.ExitPreviewModeSilently();
            }

            PlacedBuildable placedBuildable = placedObject.GetComponent<PlacedBuildable>();
            if (placedBuildable == null)
            {
                placedBuildable = placedObject.AddComponent<PlacedBuildable>();
            }

            placedBuildable.Initialize(
                buildable,
                new Vector2Int(placedData.gridX, placedData.gridY),
                new Vector2Int(Mathf.Max(1, placedData.sizeX), Mathf.Max(1, placedData.sizeY)),
                placedData.cost);

            GridManager.Instance.OccupyArea(
                placedData.gridX,
                placedData.gridY,
                Mathf.Max(1, placedData.sizeX),
                Mathf.Max(1, placedData.sizeY));
        }
    }

    private void RestoreTrainState(GameSaveData saveData)
    {
        CacheTrains();

        if (saveData.unlockedTrainIds != null)
        {
            for (int i = 0; i < saveData.unlockedTrainIds.Count; i++)
            {
                string trainId = saveData.unlockedTrainIds[i];
                Train train = ResolveTrain(trainId);
                if (train == null)
                {
                    Debug.LogWarning($"SaveManager could not find train '{trainId}' while loading.");
                    continue;
                }

                TrainManager.Instance.RestoreUnlockedTrain(train);
            }
        }

        if (saveData.trainAssignments == null)
        {
            return;
        }

        for (int i = 0; i < saveData.trainAssignments.Count; i++)
        {
            TrainAssignmentSaveData assignmentData = saveData.trainAssignments[i];
            Train train = ResolveTrain(assignmentData.trainId);
            PlatformController platform = TrainManager.Instance.GetPlatformByNumber(assignmentData.platformNumber);

            if (train == null)
            {
                Debug.LogWarning($"SaveManager could not find assigned train '{assignmentData.trainId}'.");
                continue;
            }

            if (platform == null)
            {
                Debug.LogWarning(
                    $"SaveManager could not find platform {assignmentData.platformNumber} for train '{assignmentData.trainId}'.");
                continue;
            }

            TrainManager.Instance.AssignTrainToPlatformSlot(train, platform, assignmentData.slotIndex);
        }
    }

    private void RestoreStaffState(GameSaveData saveData)
    {
        if (saveData.hiredStaff == null)
        {
            return;
        }

        CacheStaff();

        for (int i = 0; i < saveData.hiredStaff.Count; i++)
        {
            StaffSaveData staffData = saveData.hiredStaff[i];
            StaffMember staffType = ResolveStaff(staffData.staffId);
            if (staffType == null)
            {
                Debug.LogWarning($"SaveManager could not find staff '{staffData.staffId}' while loading.");
                continue;
            }

            StaffManager.Instance.RestoreHiredStaff(
                staffType,
                staffData.position,
                Quaternion.Euler(0f, staffData.rotationY, 0f));
        }
    }

    private GameSaveData CaptureCurrentState()
    {
        GameSaveData saveData = new GameSaveData
        {
            saveVersion = CurrentSaveVersion,
            money = EconomyManager.Instance.money,
            currentXp = ProgressionManager.Instance.CurrentXp
        };

        CaptureSections(saveData);

        for (int i = 0; i < ExpansionManager.Instance.builtExpansions.Count; i++)
        {
            Expansion expansion = ExpansionManager.Instance.builtExpansions[i];
            if (expansion != null)
            {
                saveData.builtExpansionIds.Add(expansion.name);
            }
        }

        PlacedBuildable[] placedBuildables = FindObjectsByType<PlacedBuildable>(FindObjectsSortMode.None);
        for (int i = 0; i < placedBuildables.Length; i++)
        {
            PlacedBuildable placedBuildable = placedBuildables[i];
            if (placedBuildable == null ||
                !placedBuildable.IsRuntimePlaced ||
                placedBuildable.BuildableData == null)
            {
                continue;
            }

            saveData.placedBuildables.Add(new PlacedBuildableSaveData
            {
                buildableId = placedBuildable.BuildableData.name,
                gridX = placedBuildable.gridPos.x,
                gridY = placedBuildable.gridPos.y,
                sizeX = placedBuildable.size.x,
                sizeY = placedBuildable.size.y,
                cost = placedBuildable.cost,
                position = placedBuildable.transform.position,
                rotationY = placedBuildable.transform.eulerAngles.y
            });
        }

        for (int i = 0; i < TrainManager.Instance.unlockedTrains.Count; i++)
        {
            Train train = TrainManager.Instance.unlockedTrains[i];
            if (train != null)
            {
                saveData.unlockedTrainIds.Add(train.name);
            }
        }

        for (int i = 0; i < TrainManager.Instance.activePlatforms.Count; i++)
        {
            PlatformController platform = TrainManager.Instance.activePlatforms[i];
            if (platform == null)
            {
                continue;
            }

            AddTrainAssignment(saveData.trainAssignments, platform.trainInSlot1, platform.platformNumber, 1);
            AddTrainAssignment(saveData.trainAssignments, platform.trainInSlot2, platform.platformNumber, 2);
        }

        for (int i = 0; i < StaffManager.Instance.hiredStaff.Count; i++)
        {
            Staff staff = StaffManager.Instance.hiredStaff[i];
            if (staff == null || staff.staffType == null)
            {
                continue;
            }

            saveData.hiredStaff.Add(new StaffSaveData
            {
                staffId = staff.staffType.name,
                position = staff.transform.position,
                rotationY = staff.transform.eulerAngles.y
            });
        }

        return saveData;
    }

    private void CaptureSections(GameSaveData saveData)
    {
        Dictionary<string, string> sections = new Dictionary<string, string>(loadedSections, StringComparer.Ordinal);

        for (int i = 0; i < participants.Count; i++)
        {
            ISaveParticipant participant = participants[i];
            try
            {
                sections[participant.SaveKey] = participant.CaptureState();
            }
            catch (Exception exception)
            {
                Debug.LogError($"SaveManager failed to capture '{participant.SaveKey}': {exception}");
            }
        }

        // Sections from systems that are not running this session are carried over
        // untouched, so their data is not lost just because this build did not load them.
        foreach (KeyValuePair<string, string> section in sections)
        {
            saveData.sections.Add(new SaveSection { key = section.Key, json = section.Value });
        }
    }

    private void AddTrainAssignment(List<TrainAssignmentSaveData> assignments, Train train, int platformNumber, int slotIndex)
    {
        if (assignments == null || train == null)
        {
            return;
        }

        assignments.Add(new TrainAssignmentSaveData
        {
            trainId = train.name,
            platformNumber = platformNumber,
            slotIndex = slotIndex
        });
    }

    private void CacheBuildables()
    {
        if (buildablesById.Count > 0)
        {
            return;
        }

        ObjectBuildable[] buildables = Resources.LoadAll<ObjectBuildable>("BuildItems");
        for (int i = 0; i < buildables.Length; i++)
        {
            ObjectBuildable buildable = buildables[i];
            if (buildable != null && !string.IsNullOrEmpty(buildable.name))
            {
                buildablesById[buildable.name] = buildable;
            }
        }
    }

    private void CacheExpansions()
    {
        if (expansionsById.Count > 0)
        {
            return;
        }

        Expansion[] expansions = Resources.LoadAll<Expansion>("Expansions");
        for (int i = 0; i < expansions.Length; i++)
        {
            Expansion expansion = expansions[i];
            if (expansion != null && !string.IsNullOrEmpty(expansion.name))
            {
                expansionsById[expansion.name] = expansion;
            }
        }
    }

    private void CacheTrains()
    {
        if (trainsById.Count > 0)
        {
            return;
        }

        Train[] trains = Resources.LoadAll<Train>("Trains");
        for (int i = 0; i < trains.Length; i++)
        {
            Train train = trains[i];
            if (train != null && !string.IsNullOrEmpty(train.name))
            {
                trainsById[train.name] = train;
            }
        }
    }

    private void CacheStaff()
    {
        if (staffById.Count > 0)
        {
            return;
        }

        StaffMember[] staffMembers = Resources.LoadAll<StaffMember>("Staff");
        for (int i = 0; i < staffMembers.Length; i++)
        {
            StaffMember staffMember = staffMembers[i];
            if (staffMember != null && !string.IsNullOrEmpty(staffMember.name))
            {
                staffById[staffMember.name] = staffMember;
            }
        }
    }

    private ObjectBuildable ResolveBuildable(string buildableId)
    {
        if (string.IsNullOrEmpty(buildableId))
        {
            return null;
        }

        CacheBuildables();
        buildablesById.TryGetValue(buildableId, out ObjectBuildable buildable);
        return buildable;
    }

    private Expansion ResolveExpansion(string expansionId)
    {
        if (string.IsNullOrEmpty(expansionId))
        {
            return null;
        }

        CacheExpansions();
        expansionsById.TryGetValue(expansionId, out Expansion expansion);
        return expansion;
    }

    private Train ResolveTrain(string trainId)
    {
        if (string.IsNullOrEmpty(trainId))
        {
            return null;
        }

        CacheTrains();
        trainsById.TryGetValue(trainId, out Train train);
        return train;
    }

    private StaffMember ResolveStaff(string staffId)
    {
        if (string.IsNullOrEmpty(staffId))
        {
            return null;
        }

        CacheStaff();
        staffById.TryGetValue(staffId, out StaffMember staffMember);
        return staffMember;
    }

    [Serializable]
    private class GameSaveData
    {
        public int saveVersion;
        public int money;
        public int currentXp;
        public List<string> builtExpansionIds = new List<string>();
        public List<PlacedBuildableSaveData> placedBuildables = new List<PlacedBuildableSaveData>();
        public List<string> unlockedTrainIds = new List<string>();
        public List<TrainAssignmentSaveData> trainAssignments = new List<TrainAssignmentSaveData>();
        public List<StaffSaveData> hiredStaff = new List<StaffSaveData>();
        public List<SaveSection> sections = new List<SaveSection>();
    }

    [Serializable]
    private class SaveSection
    {
        public string key;
        public string json;
    }

    [Serializable]
    private class PlacedBuildableSaveData
    {
        public string buildableId;
        public int gridX;
        public int gridY;
        public int sizeX;
        public int sizeY;
        public int cost;
        public Vector3 position;
        public float rotationY;
    }

    [Serializable]
    private class TrainAssignmentSaveData
    {
        public string trainId;
        public int platformNumber;
        public int slotIndex;
    }

    [Serializable]
    private class StaffSaveData
    {
        public string staffId;
        public Vector3 position;
        public float rotationY;
    }
}

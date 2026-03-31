using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum SoundEffectId
{
    ButtonHover,
    ButtonClick,
    BuildPlaced,
    BuildInvalid,
    Demolish,
    TierUp,
    TrainApproaching,
    BillCharged,
    HireAndroid
}

public class SoundEffectController : MonoBehaviour
{
    private const float ButtonScanInterval = 0.1f;

    public static SoundEffectController Instance { get; private set; }

    private readonly HashSet<SoundEffectId> warnedMissingClips = new HashSet<SoundEffectId>();

    [Header("UI")]
    [SerializeField] private AudioClip buttonHoverClip;
    [SerializeField, Range(0f, 1f)] private float buttonHoverVolume = 0.45f;
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField, Range(0f, 1f)] private float buttonClickVolume = 0.7f;

    [Header("Build")]
    [SerializeField] private AudioClip buildPlacedClip;
    [SerializeField, Range(0f, 1f)] private float buildPlacedVolume = 1f;
    [SerializeField] private AudioClip buildInvalidClip;
    [SerializeField, Range(0f, 1f)] private float buildInvalidVolume = 1f;
    [SerializeField] private AudioClip demolishClip;
    [SerializeField, Range(0f, 1f)] private float demolishVolume = 1f;

    [Header("Progression")]
    [SerializeField] private AudioClip tierUpClip;
    [SerializeField, Range(0f, 1f)] private float tierUpVolume = 1f;

    [Header("Station")]
    [SerializeField] private AudioClip trainApproachingClip;
    [SerializeField, Range(0f, 1f)] private float trainApproachingVolume = 1f;
    [SerializeField] private AudioClip billChargedClip;
    [SerializeField, Range(0f, 1f)] private float billChargedVolume = 0.85f;
    [SerializeField] private AudioClip hireAndroidClip;
    [SerializeField, Range(0f, 1f)] private float hireAndroidVolume = 1f;

    private AudioSource audioSource;
    private Coroutine buttonScanCoroutine;

    public static void Play(SoundEffectId effectId)
    {
        if (Instance == null)
        {
            return;
        }

        Instance.PlayInternal(effectId);
    }

    private void Awake()
    {
        Instance = this;

        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        Instance = this;

        if (buttonScanCoroutine == null)
        {
            buttonScanCoroutine = StartCoroutine(AttachButtonHooksLoop());
        }
    }

    private void Start()
    {
        AttachHooksToButtons();
    }

    private void OnDisable()
    {
        if (buttonScanCoroutine != null)
        {
            StopCoroutine(buttonScanCoroutine);
            buttonScanCoroutine = null;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private IEnumerator AttachButtonHooksLoop()
    {
        while (true)
        {
            AttachHooksToButtons();
            yield return new WaitForSeconds(ButtonScanInterval);
        }
    }

    private void AttachHooksToButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || button.TryGetComponent(out UIButtonSoundHook _))
            {
                continue;
            }

            button.gameObject.AddComponent<UIButtonSoundHook>();
        }
    }

    private void PlayInternal(SoundEffectId effectId)
    {
        if (!TryGetClip(effectId, out AudioClip clip))
        {
            return;
        }

        audioSource.PlayOneShot(clip, GetVolume(effectId));
    }

    private bool TryGetClip(SoundEffectId effectId, out AudioClip clip)
    {
        clip = GetClip(effectId);
        if (clip != null)
        {
            return true;
        }

        if (warnedMissingClips.Add(effectId))
        {
            Debug.LogWarning(
                $"SoundEffectController: Assign a clip for {effectId} on the SoundEffectController inspector.");
        }

        return false;
    }

    private AudioClip GetClip(SoundEffectId effectId)
    {
        switch (effectId)
        {
            case SoundEffectId.ButtonHover:
                return buttonHoverClip;
            case SoundEffectId.ButtonClick:
                return buttonClickClip;
            case SoundEffectId.BuildPlaced:
                return buildPlacedClip;
            case SoundEffectId.BuildInvalid:
                return buildInvalidClip;
            case SoundEffectId.Demolish:
                return demolishClip;
            case SoundEffectId.TierUp:
                return tierUpClip;
            case SoundEffectId.TrainApproaching:
                return trainApproachingClip;
            case SoundEffectId.BillCharged:
                return billChargedClip;
            case SoundEffectId.HireAndroid:
                return hireAndroidClip;
            default:
                return null;
        }
    }

    private float GetVolume(SoundEffectId effectId)
    {
        switch (effectId)
        {
            case SoundEffectId.ButtonHover:
                return Mathf.Clamp01(buttonHoverVolume);
            case SoundEffectId.ButtonClick:
                return Mathf.Clamp01(buttonClickVolume);
            case SoundEffectId.BuildPlaced:
                return Mathf.Clamp01(buildPlacedVolume);
            case SoundEffectId.BuildInvalid:
                return Mathf.Clamp01(buildInvalidVolume);
            case SoundEffectId.Demolish:
                return Mathf.Clamp01(demolishVolume);
            case SoundEffectId.TierUp:
                return Mathf.Clamp01(tierUpVolume);
            case SoundEffectId.TrainApproaching:
                return Mathf.Clamp01(trainApproachingVolume);
            case SoundEffectId.BillCharged:
                return Mathf.Clamp01(billChargedVolume);
            case SoundEffectId.HireAndroid:
                return Mathf.Clamp01(hireAndroidVolume);
            default:
                return 1f;
        }
    }
}

[RequireComponent(typeof(Button))]
public class UIButtonSoundHook : MonoBehaviour, IPointerEnterHandler
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        EnsureButtonReference();

        if (button != null)
        {
            button.onClick.AddListener(HandleButtonClicked);
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button == null || !button.IsInteractable())
        {
            return;
        }

        SoundEffectController.Play(SoundEffectId.ButtonHover);
    }

    private void HandleButtonClicked()
    {
        if (button == null || !button.IsInteractable())
        {
            return;
        }

        SoundEffectController.Play(SoundEffectId.ButtonClick);
    }

    private void EnsureButtonReference()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }
}

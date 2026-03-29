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
    [SerializeField] private AudioClip buttonClickClip;

    [Header("Build")]
    [SerializeField] private AudioClip buildPlacedClip;
    [SerializeField] private AudioClip buildInvalidClip;
    [SerializeField] private AudioClip demolishClip;

    [Header("Progression")]
    [SerializeField] private AudioClip tierUpClip;

    [Header("Station")]
    [SerializeField] private AudioClip trainApproachingClip;
    [SerializeField] private AudioClip billChargedClip;
    [SerializeField] private AudioClip hireAndroidClip;

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
                return 0.45f;
            case SoundEffectId.ButtonClick:
                return 0.7f;
            case SoundEffectId.BillCharged:
                return 0.85f;
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

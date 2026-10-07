using UnityEngine;
using DG.Tweening;

/// <summary>
/// Purely visual office character. Seated at a workstation it mirrors the OfficeWorker state:
/// idle (breathing, looking around) -> receives paper (nod + hop) -> works (typing arms) -> paper processed (stamp).
/// A worker left without papers now and then dozes off (head drops, small "Zz" bubble) and wakes the moment work arrives.
/// Without a worker it is a decorative colleague (receptionist waves at the player, manager alternates writing and thinking).
/// No pathfinding and no gameplay logic: everything here is a handful of sin waves and tweens.
/// </summary>
public class OfficeAvatar : MonoBehaviour
{
    public enum Role { Worker, Receptionist, Manager }

    [SerializeField] private Role role = Role.Worker;
    [Header("Parts")]
    [SerializeField] private Transform torso;
    [SerializeField] private Transform head;
    [SerializeField] private Transform armLeft;   // pivots sit at the shoulders; arms hang along -Y
    [SerializeField] private Transform armRight;

    [Header("Tuning")]
    [SerializeField] private float typingSpeed = 17f;
    [SerializeField] private float waveDistance = 3.4f;

    [Header("Idle Personality (workers only)")]
    [Tooltip("Small 'Zz' bubble above the head, shown now and then while the worker has nothing to do.")]
    [SerializeField] private Transform sleepBubble;
    [SerializeField] private TMPro.TMP_Text sleepText;
    [Tooltip("Seconds of true idleness before the worker first dozes off.")]
    [SerializeField] private float sleepDelay = 6f;
    [Tooltip("How long one doze (bubble visible) lasts.")]
    [SerializeField] private float sleepDuration = 4.5f;
    [Tooltip("Awake pause between two dozes, so the bubble never stays on screen permanently.")]
    [SerializeField] private float sleepCooldown = 7f;

    private static readonly string[] ZzzFrames = { "z", "zZ", "zZz" };
    private Vector3 bubbleBaseLocalPos, bubbleBaseScale;
    private Quaternion cameraRotation = Quaternion.Euler(45f, 0f, 0f);
    private float idleTime, nextDozeAt, dozeEnd;
    private bool dozing;
    private float sleepBlend;
    private int zzzFrame = -1;
    private float cheerEnd;
    private float cheer;
    private bool revealing;   // hidden until the freshly built desk stands; nothing may cut the pop-in short

    private OfficeWorker worker;
    private Transform player;
    private Vector3 baseScale;
    private Vector3 torsoBaseScale;
    private Vector3 rootBaseLocalPos;

    private bool working;
    private float workBlend;          // 0 idle pose .. 1 typing pose
    private float stamp;              // decaying arm slam, kicked by PaperProcessed
    private float lookYaw, lookTarget, nextLookTime;
    private float wave;               // 0..1 blend for the receptionist wave
    private float nextPlayerCheck;
    private bool playerNear;
    private float nextManagerSwitch;
    private float phase;

    private void Awake()
    {
        baseScale = transform.localScale;
        rootBaseLocalPos = transform.localPosition;
        if (torso != null) torsoBaseScale = torso.localScale;
        phase = Random.value * 10f;
        worker = GetComponentInParent<OfficeWorker>(true);
        if (sleepBubble != null)
        {
            bubbleBaseLocalPos = sleepBubble.localPosition;
            bubbleBaseScale = sleepBubble.localScale;
            sleepBubble.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (role == Role.Worker && worker != null)
        {
            worker.PaperReceived += OnPaperReceived;
            worker.WorkingChanged += OnWorkingChanged;
            worker.PaperProcessed += OnPaperProcessed;
            worker.BuildStarted += OnBuildStarted;
            UpgradeManager.onLevelChanged += OnUpgradeChanged;
        }
        transform.localScale = baseScale;
        nextLookTime = Time.time + Random.Range(1f, 3f);
        ResetIdle();
        if (Camera.main != null) cameraRotation = Camera.main.transform.rotation; // the follow camera never rotates
    }

    private void OnDisable()
    {
        if (worker != null)
        {
            worker.PaperReceived -= OnPaperReceived;
            worker.WorkingChanged -= OnWorkingChanged;
            worker.PaperProcessed -= OnPaperProcessed;
            worker.BuildStarted -= OnBuildStarted;
        }
        UpgradeManager.onLevelChanged -= OnUpgradeChanged;
        transform.DOKill();
        if (head != null) head.DOKill();
        transform.localScale = baseScale;
        transform.localPosition = rootBaseLocalPos;
        HideBubble();
        cheer = 0f;
        cheerEnd = 0f;
        revealing = false;
    }

    private void Start()
    {
        if (role == Role.Receptionist)
        {
            var pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) player = pm.transform;
        }
    }

    // ---- worker feedback ------------------------------------------------------------------------------------------

    private void OnPaperReceived()
    {
        ResetIdle(); // work arrived: the Zz bubble goes away at once
        if (revealing) return;

        // idle -> receive: a quick nod and a small hop
        if (head != null)
        {
            head.DOKill(true);
            head.DOPunchRotation(new Vector3(24f, 0f, 0f), 0.35f, 6, 0.6f);
        }
        transform.DOKill(true);
        transform.localPosition = rootBaseLocalPos;
        transform.DOPunchPosition(transform.parent != null ? transform.parent.InverseTransformVector(Vector3.up * 0.18f) : Vector3.up * 0.18f, 0.3f, 5, 0.5f);
    }

    private void OnWorkingChanged(bool isWorking) { working = isWorking; }

    private void OnPaperProcessed()
    {
        working = true;
        stamp = 1f;
    }

    /// <summary>The desk rises during the purchase build-in; the employee pops in once it stands, then cheers.</summary>
    private void OnBuildStarted(float deskReadyDelay)
    {
        ResetIdle();
        revealing = true;
        transform.DOKill();
        transform.localScale = Vector3.zero;
        transform.DOScale(baseScale, 0.3f).SetEase(Ease.OutBack, 2f).SetDelay(Mathf.Max(0f, deskReadyDelay))
            .OnKill(() => revealing = false)
            .OnComplete(() => Cheer(0.5f));
    }

    /// <summary>Worker Speed bought: every worker gives a small happy hop.</summary>
    private void OnUpgradeChanged(UpgradeManager.UpgradeType type, int level)
    {
        if (type != UpgradeManager.UpgradeType.WorkerSpeed || !isActiveAndEnabled || revealing) return;
        ResetIdle();
        Cheer(0.4f);
    }

    /// <summary>Arms up and a hop. Purely visual, safe to call at any time.</summary>
    private void Cheer(float duration)
    {
        cheerEnd = Time.time + duration;
        transform.DOKill(true);
        transform.localPosition = rootBaseLocalPos;
        transform.DOPunchPosition(transform.parent != null ? transform.parent.InverseTransformVector(Vector3.up * 0.22f) : Vector3.up * 0.22f, 0.35f, 4, 0.5f);
    }

    // ---- idle personality -----------------------------------------------------------------------------------------

    private void ResetIdle()
    {
        idleTime = 0f;
        nextDozeAt = sleepDelay;
        HideBubble();
    }

    private void HideBubble()
    {
        dozing = false;
        if (sleepBubble == null) return;
        sleepBubble.DOKill();
        sleepBubble.gameObject.SetActive(false);
    }

    private void UpdateIdle(float dt)
    {
        if (role != Role.Worker || worker == null || sleepBubble == null) return;

        if (!worker.IsIdle || working)
        {
            if (idleTime > 0f || dozing) ResetIdle();
            return;
        }

        idleTime += dt;
        if (!dozing && idleTime >= nextDozeAt)
        {
            dozing = true;
            dozeEnd = idleTime + sleepDuration;
            zzzFrame = -1;
            sleepBubble.gameObject.SetActive(true);
            sleepBubble.DOKill();
            sleepBubble.localScale = Vector3.zero;
            sleepBubble.DOScale(bubbleBaseScale, 0.3f).SetEase(Ease.OutBack).SetTarget(sleepBubble);
        }
        else if (dozing && idleTime >= dozeEnd)
        {
            // dozed long enough: the bubble shrinks away and the worker stays awake for a while
            dozing = false;
            nextDozeAt = idleTime + sleepCooldown;
            sleepBubble.DOKill();
            sleepBubble.DOScale(0f, 0.2f).SetEase(Ease.InBack).SetTarget(sleepBubble)
                .OnComplete(() => sleepBubble.gameObject.SetActive(false));
        }
    }

    private void LateUpdate()
    {
        if (sleepBubble == null || !sleepBubble.gameObject.activeSelf) return;

        // faces the camera and floats gently; the Zs build up "z", "zZ", "zZz"
        sleepBubble.localPosition = bubbleBaseLocalPos + Vector3.up * (Mathf.Sin((Time.time + phase) * 2.2f) * 0.06f);
        sleepBubble.rotation = cameraRotation;
        int frame = (int)(Time.time / 0.45f) % ZzzFrames.Length;
        if (frame != zzzFrame && sleepText != null)
        {
            zzzFrame = frame;
            sleepText.text = ZzzFrames[frame];
        }
    }

    // ---- per-frame pose -------------------------------------------------------------------------------------------

    private void Update()
    {
        float dt = Time.deltaTime;
        float t = Time.time + phase;

        UpdateRoleState(dt);
        UpdateIdle(dt);

        workBlend = Mathf.MoveTowards(workBlend, working ? 1f : 0f, dt * 6f);
        stamp = Mathf.MoveTowards(stamp, 0f, dt * 5f);
        sleepBlend = Mathf.MoveTowards(sleepBlend, dozing ? 1f : 0f, dt * (dozing ? 1.5f : 8f)); // nods off slowly, wakes instantly
        cheer = Mathf.MoveTowards(cheer, Time.time < cheerEnd ? 1f : 0f, dt * 9f);

        // breathing (slower and deeper while dozing)
        if (torso != null)
        {
            float breathe = 1f + Mathf.Sin(t * Mathf.Lerp(2.2f, 1.2f, sleepBlend)) * Mathf.Lerp(0.015f, 0.03f, sleepBlend);
            torso.localScale = new Vector3(torsoBaseScale.x, torsoBaseScale.y * breathe, torsoBaseScale.z);
            // lean toward the desk while typing
            torso.localRotation = Quaternion.Euler(Mathf.Lerp(0f, 9f, workBlend), 0f, 0f);
        }

        // look around when idle, face the work when busy
        if (Time.time >= nextLookTime)
        {
            lookTarget = Random.Range(-35f, 35f);
            nextLookTime = Time.time + Random.Range(2f, 4.5f);
        }
        float yaw = Mathf.Lerp(lookTarget, 0f, workBlend);
        if (role == Role.Receptionist && playerNear && player != null)
        {
            Vector3 to = transform.InverseTransformPoint(player.position);
            yaw = Mathf.Clamp(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, -60f, 60f);
        }
        yaw *= 1f - sleepBlend;
        lookYaw = Mathf.Lerp(lookYaw, yaw, 1f - Mathf.Exp(-6f * dt));
        // dozing: the head drops forward with a slow nod
        float dozePitch = sleepBlend * (24f + Mathf.Sin(t * 1.4f) * 5f);
        if (head != null && !DOTween.IsTweening(head))
            head.localRotation = Quaternion.Euler(Mathf.Lerp(0f, 12f, workBlend) + dozePitch, lookYaw, 0f);

        PoseArms(t);
    }

    private void UpdateRoleState(float dt)
    {
        if (role == Role.Manager)
        {
            // alternates between writing and thinking; nothing smarter than a timer
            if (Time.time >= nextManagerSwitch)
            {
                working = !working;
                nextManagerSwitch = Time.time + (working ? Random.Range(3f, 5f) : Random.Range(2f, 4f));
            }
        }
        else if (role == Role.Receptionist && player != null && Time.time >= nextPlayerCheck)
        {
            nextPlayerCheck = Time.time + 0.2f;
            playerNear = (player.position - transform.position).sqrMagnitude < waveDistance * waveDistance;
        }

        wave = Mathf.MoveTowards(wave, (role == Role.Receptionist && playerNear) ? 1f : 0f, dt * 5f);
    }

    private void PoseArms(float t)
    {
        if (armLeft == null || armRight == null) return;

        // rest: hands loosely forward on the lap/desk; work: typing, alternating
        float restPitch = -35f + Mathf.Sin(t * 1.3f) * 3f;
        float speed = typingSpeed * UpgradeManager.Instance.WorkSpeedFactor;   // the Worker Speed upgrade types faster too
        float typeL = -62f + Mathf.Sin(t * speed) * 13f;
        float typeR = -62f + Mathf.Sin(t * speed + 2.4f) * 13f;
        float pitchL = Mathf.Lerp(restPitch, typeL, workBlend);
        float pitchR = Mathf.Lerp(restPitch, typeR, workBlend) - stamp * 28f;   // right hand slams the paper

        float rollR = 0f;
        if (wave > 0f)
        {
            pitchR = Mathf.Lerp(pitchR, -150f, wave);
            rollR = Mathf.Sin(Time.time * 9f) * 22f * wave;
        }

        // dozing: arms drop to the lap; cheering: both arms up
        pitchL = Mathf.Lerp(pitchL, -12f, sleepBlend);
        pitchR = Mathf.Lerp(pitchR, -12f, sleepBlend);
        if (cheer > 0f)
        {
            float shake = Mathf.Sin(Time.time * 22f) * 10f;
            pitchL = Mathf.Lerp(pitchL, -165f + shake, cheer);
            pitchR = Mathf.Lerp(pitchR, -165f - shake, cheer);
        }

        armLeft.localRotation = Quaternion.Euler(pitchL, 0f, -4f);
        armRight.localRotation = Quaternion.Euler(pitchR, 0f, 4f + rollR);
    }
}

using UnityEngine;
using DG.Tweening;

/// <summary>
/// Purely visual office character. Seated at a workstation it mirrors the OfficeWorker state:
/// idle (breathing, looking around) -> receives paper (nod + hop) -> works (typing arms) -> paper processed (stamp).
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
    }

    private void OnEnable()
    {
        if (role == Role.Worker && worker != null)
        {
            worker.PaperReceived += OnPaperReceived;
            worker.WorkingChanged += OnWorkingChanged;
            worker.PaperProcessed += OnPaperProcessed;
            worker.BuildStarted += OnBuildStarted;
        }
        transform.localScale = baseScale;
        nextLookTime = Time.time + Random.Range(1f, 3f);
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
        transform.DOKill();
        if (head != null) head.DOKill();
        transform.localScale = baseScale;
        transform.localPosition = rootBaseLocalPos;
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

    /// <summary>The desk rises during the purchase build-in; the worker pops in just as it lands.</summary>
    private void OnBuildStarted(float impactDelay)
    {
        transform.DOKill();
        transform.localScale = Vector3.zero;
        transform.DOScale(baseScale, 0.35f).SetEase(Ease.OutBack, 2f).SetDelay(Mathf.Max(0f, impactDelay * 0.8f));
    }

    // ---- per-frame pose -------------------------------------------------------------------------------------------

    private void Update()
    {
        float dt = Time.deltaTime;
        float t = Time.time + phase;

        UpdateRoleState(dt);

        workBlend = Mathf.MoveTowards(workBlend, working ? 1f : 0f, dt * 6f);
        stamp = Mathf.MoveTowards(stamp, 0f, dt * 5f);

        // breathing
        if (torso != null)
        {
            float breathe = 1f + Mathf.Sin(t * 2.2f) * 0.015f;
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
        lookYaw = Mathf.Lerp(lookYaw, yaw, 1f - Mathf.Exp(-6f * dt));
        if (head != null && !DOTween.IsTweening(head))
            head.localRotation = Quaternion.Euler(Mathf.Lerp(0f, 12f, workBlend), lookYaw, 0f);

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

        armLeft.localRotation = Quaternion.Euler(pitchL, 0f, -4f);
        armRight.localRotation = Quaternion.Euler(pitchR, 0f, 4f + rollR);
    }
}

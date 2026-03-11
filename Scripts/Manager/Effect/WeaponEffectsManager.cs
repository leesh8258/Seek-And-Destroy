using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class WeaponEffectsManager : MonoBehaviour
{
    private const int HitKindNone = 0;
    private const int HitKindEnvironment = 1;

    public static WeaponEffectsManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform effectPool;

    [Header("Optional Prewarm")]
    [Min(0), SerializeField] private int prewarmTrailCountPerPrefab = 0;
    [Min(0), SerializeField] private int prewarmImpactCountPerPrefab = 0;
    [Min(0), SerializeField] private int prewarmMuzzleCountPerPrefab = 0;

    [Header("Environment Impact Sound")]
    [SerializeField] private float environmentSoundProbeRadius = 0.25f;
    [SerializeField] private LayerMask environmentSoundProbeMask;
    [SerializeField] private int environmentSoundProbeMax = 3;

    private Collider[] environmentProbeColliders;
    private PlayerFovController viewerFovController;

    private readonly Dictionary<GameObject, Queue<GameObject>> trailPools = new Dictionary<GameObject, Queue<GameObject>>();
    private readonly Dictionary<GameObject, Queue<GameObject>> impactPools = new Dictionary<GameObject, Queue<GameObject>>();
    private readonly Dictionary<GameObject, Queue<GameObject>> muzzlePools = new Dictionary<GameObject, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        environmentProbeColliders = new Collider[Mathf.Max(1, environmentSoundProbeMax)];
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayMuzzle(Transform muzzle, in WeaponShotVfx weaponShotVfx)
    {
        if (!enabled) return;
        if (muzzle == null) return;
        if (weaponShotVfx.muzzlePrefab == null) return;

        Prewarm(muzzlePools, weaponShotVfx.muzzlePrefab, prewarmMuzzleCountPerPrefab);

        GameObject instance = GetOrCreate(muzzlePools, weaponShotVfx.muzzlePrefab);
        if (instance == null) return;

        PrepareSpawnedEffect(instance);

        instance.transform.SetParent(muzzle, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        instance.SetActive(true);

        ApplyImmediateVisibility(instance);

        float delay = Mathf.Max(0.0f, weaponShotVfx.muzzleReturnDelaySeconds);
        StartCoroutine(ReturnToPoolDelay(muzzlePools, weaponShotVfx.muzzlePrefab, instance, delay));
    }

    public void PlayShotTrailsAndImpacts(Vector3 origin, Vector3[] endPoints, int[] hitKinds, Vector3[] hitNormals, WeaponShotVfx weaponShotVfx)
    {
        if (!enabled) return;
        if (endPoints == null) return;

        if (weaponShotVfx.trailPrefab != null)
        {
            Prewarm(trailPools, weaponShotVfx.trailPrefab, prewarmTrailCountPerPrefab);
        }

        if (weaponShotVfx.impactPrefab != null)
        {
            Prewarm(impactPools, weaponShotVfx.impactPrefab, prewarmImpactCountPerPrefab);
        }

        int count = endPoints.Length;
        if (count <= 0) return;

        StartCoroutine(PlayBurstRoutine(origin, endPoints, hitKinds, hitNormals, weaponShotVfx));
    }

    private void Prewarm(Dictionary<GameObject, Queue<GameObject>> pools, GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;

        Queue<GameObject> pool = GetOrCreatePool(pools, prefab);
        while (pool.Count < count)
        {
            GameObject instance = Instantiate(prefab, effectPool);
            instance.SetActive(false);
            pool.Enqueue(instance);
        }
    }

    private IEnumerator PlayBurstRoutine(Vector3 origin, Vector3[] endPoints, int[] hitKinds, Vector3[] hitNormals, WeaponShotVfx weaponShotVfx)
    {
        int count = endPoints.Length;

        GameObject[] trailInstances = null;
        TrailRenderer[] trailRenderers = null;

        if (weaponShotVfx.trailPrefab != null)
        {
            trailInstances = new GameObject[count];
            trailRenderers = new TrailRenderer[count];
        }

        float[] travelTimes = new float[count];
        float[] elapsedTimes = new float[count];

        bool[] spawnImpacts = new bool[count];
        Quaternion[] impactRotations = new Quaternion[count];

        // 1) 준비: 각 탄환별 travelTime, impact rot, trail instance 준비
        for (int i = 0; i < count; i++)
        {
            Vector3 end = endPoints[i];
            float distance = Vector3.Distance(origin, end);
            float travelTime = CalculateTravelTime(distance, weaponShotVfx.shotRange, weaponShotVfx.timeToMaxRange);

            int kind = HitKindNone;
            if (hitKinds != null && i < hitKinds.Length)
            {
                kind = hitKinds[i];
            }

            Vector3 normal = Vector3.zero;
            if (hitNormals != null && i < hitNormals.Length)
            {
                normal = hitNormals[i];
            }

            bool isEnvironment = kind == HitKindEnvironment;
            bool spawnImpact = isEnvironment && weaponShotVfx.impactPrefab != null;
            Quaternion impactRotation = Quaternion.identity;

            if (spawnImpact)
            {
                Vector3 toShooter = origin - end;
                if (toShooter.sqrMagnitude > 0.00001f)
                {
                    impactRotation = Quaternion.LookRotation(toShooter.normalized);
                }

                else if (normal.sqrMagnitude > 0.00001f)
                {
                    impactRotation = Quaternion.LookRotation(normal);
                }
            }

            travelTimes[i] = travelTime;
            elapsedTimes[i] = 0.0f;
            spawnImpacts[i] = spawnImpact;
            impactRotations[i] = impactRotation;

            if (weaponShotVfx.trailPrefab != null)
            {
                GameObject trailInstance = GetOrCreate(trailPools, weaponShotVfx.trailPrefab);
                if (trailInstance != null)
                {
                    PrepareSpawnedEffect(trailInstance);

                    trailInstance.transform.SetParent(effectPool, false);
                    trailInstance.transform.position = origin;
                    trailInstance.transform.rotation = Quaternion.identity;
                    trailInstance.SetActive(true);

                    ApplyImmediateVisibility(trailInstance);

                    TrailRenderer tr = trailInstance.GetComponent<TrailRenderer>();
                    if (tr != null)
                    {
                        tr.Clear();
                        tr.emitting = true;

                        float t = travelTime;
                        float minTime = Mathf.Max(0.0f, weaponShotVfx.minTrailTime);
                        if (t < minTime)
                        {
                            t = minTime;
                        }

                        tr.time = t;
                    }

                    trailInstances[i] = trailInstance;
                    trailRenderers[i] = tr;
                }
            }
        }

        // 2) 업데이트: 하나의 while 루프로 모든 탄환 trail 이동
        bool anyAlive = true;
        while (anyAlive)
        {
            anyAlive = false;

            for (int i = 0; i < count; i++)
            {
                float travelTime = travelTimes[i];
                if (elapsedTimes[i] >= travelTime)
                {
                    continue;
                }

                anyAlive = true;
                elapsedTimes[i] += Time.deltaTime;

                float a = (travelTime > 0.0001f) ? Mathf.Clamp01(elapsedTimes[i] / travelTime) : 1.0f;
                Vector3 p = Vector3.Lerp(origin, endPoints[i], a);

                GameObject trailInstance = trailInstances != null ? trailInstances[i] : null;
                if (trailInstance != null)
                {
                    trailInstance.transform.position = p;
                }
            }

            yield return null;
        }

        // 3) 완료: end 고정 + impact 생성 + trail 반환 예약
        for (int i = 0; i < count; i++)
        {
            Vector3 end = endPoints[i];

            GameObject trailInstance = trailInstances != null ? trailInstances[i] : null;
            TrailRenderer tr = trailRenderers != null ? trailRenderers[i] : null;

            if (trailInstance != null)
            {
                trailInstance.transform.position = end;
            }

            if (spawnImpacts[i])
            {
                GameObject impactInstance = GetOrCreate(impactPools, weaponShotVfx.impactPrefab);
                if (impactInstance != null)
                {
                    PrepareSpawnedEffect(impactInstance);

                    impactInstance.transform.SetParent(effectPool, false);
                    impactInstance.transform.position = end;
                    impactInstance.transform.rotation = impactRotations[i];
                    impactInstance.SetActive(true);

                    ApplyImmediateVisibility(impactInstance);
                    PlayEnvironmentImpactSound(end);

                    float delay = Mathf.Max(0.0f, weaponShotVfx.impactReturnDelaySeconds);
                    StartCoroutine(ReturnToPoolDelay(impactPools, weaponShotVfx.impactPrefab, impactInstance, delay));
                }
            }

            if (tr != null && trailInstance != null)
            {
                tr.emitting = false;
                float delay = Mathf.Max(0.0f, tr.time);
                StartCoroutine(ReturnToPoolDelay(trailPools, weaponShotVfx.trailPrefab, trailInstance, delay));
            }

            else if (trailInstance != null)
            {
                ReturnToPool(trailPools, weaponShotVfx.trailPrefab, trailInstance);
            }
        }
    }

    private Queue<GameObject> GetOrCreatePool(Dictionary<GameObject, Queue<GameObject>> pools, GameObject prefab)
    {
        if (pools.TryGetValue(prefab, out Queue<GameObject> existing))
        {
            return existing;
        }

        Queue<GameObject> created = new Queue<GameObject>();
        pools.Add(prefab, created);
        return created;
    }

    private GameObject GetOrCreate(Dictionary<GameObject, Queue<GameObject>> pools, GameObject prefab)
    {
        if (prefab == null) return null;

        Queue<GameObject> pool = GetOrCreatePool(pools, prefab);
        while (pool.Count > 0)
        {
            GameObject instance = pool.Dequeue();
            if (instance != null)
            {
                return instance;
            }
        }

        GameObject created = Instantiate(prefab, effectPool);
        created.SetActive(false);
        return created;
    }

    private IEnumerator ReturnToPoolDelay(Dictionary<GameObject, Queue<GameObject>> pools, GameObject prefab, GameObject instance, float delay)
    {
        if (instance == null) yield break;
        if (prefab == null) yield break;

        if (delay > 0.0f)
        {
            yield return new WaitForSeconds(delay);
        }

        ReturnToPool(pools, prefab, instance);
    }

    private void ReturnToPool(Dictionary<GameObject, Queue<GameObject>> pools, GameObject prefab, GameObject instance)
    {
        if (instance == null) return;
        if (prefab == null) return;

        FovVisibilityTarget target = instance.GetComponentInChildren<FovVisibilityTarget>(true);
        if (target != null)
        {
            target.ResetVisibilityState();
        }

        TrailRenderer[] trails = instance.GetComponentsInChildren<TrailRenderer>(true);
        for (int i = 0; i < trails.Length; i++)
        {
            TrailRenderer trail = trails[i];
            if (trail == null) continue;
            trail.emitting = false;
            trail.Clear();
        }

        instance.SetActive(false);
        instance.transform.SetParent(effectPool, false);

        Queue<GameObject> pool = GetOrCreatePool(pools, prefab);
        pool.Enqueue(instance);
    }

    private float CalculateTravelTime(float distance, float range, float timeToMaxRange)
    {
        if (range <= 0.0001f)
        {
            return 0.0f;
        }

        float clampedDistance = distance;
        if (clampedDistance < 0.0f)
        {
            clampedDistance = 0.0f;
        }

        float t = Mathf.Clamp01(clampedDistance / range);
        float travelTime = t * timeToMaxRange;
        return travelTime;
    }

    private void PlayEnvironmentImpactSound(Vector3 position)
    {
        if (SoundManager.Instance == null) return;

        int count = Physics.OverlapSphereNonAlloc(
            position,
            environmentSoundProbeRadius,
            environmentProbeColliders,
            environmentSoundProbeMask,
            QueryTriggerInteraction.Ignore
        );

        if (count <= 0) return;

        MapEnvironmentSoundHandler bestHandler = null;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            Collider col = environmentProbeColliders[i];
            if (col == null) continue;

            if (!col.TryGetComponent(out MapEnvironmentSoundHandler handler)) continue;
            if (handler.HitSound == null) continue;

            float sqr = (col.transform.position - position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                bestHandler = handler;
            }
        }

        if (bestHandler == null) return;

        float volume = Mathf.Max(0f, bestHandler.VolumeMultiplier);

        SoundManager.Instance.Play3DSound(bestHandler.HitSound, position, volume);
    }

    public void SetViewerFovController(PlayerFovController controller)
    {
        viewerFovController = controller;
    }

    private void PrepareSpawnedEffect(GameObject instance)
    {
        if (instance == null) return;

        FovVisibilityTarget target = instance.GetComponentInChildren<FovVisibilityTarget>(true);
        if (target != null)
        {
            target.RefreshManagedRenderersFromChildren();
            target.ResetVisibilityState();
        }
    }

    private void ApplyImmediateVisibility(GameObject instance)
    {
        if (instance == null) return;
        if (viewerFovController == null) return;

        FovVisibilityTarget target = instance.GetComponentInChildren<FovVisibilityTarget>(true);
        if (target == null) return;

        if (viewerFovController.IsTargetVisibleNow(target))
        {
            viewerFovController.ReportImmediateVisibility(target);
        }
    }
}

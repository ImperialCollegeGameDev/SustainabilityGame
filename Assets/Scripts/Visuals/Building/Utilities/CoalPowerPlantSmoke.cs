using UnityEngine;

public class CoalPowerPlantSmoke : MonoBehaviour
{
    [SerializeField] private ParticleSystem smoke;

    [SerializeField] private Transform chimneyA;
    [SerializeField] private Transform chimneyB;

    [Header("Emission")]
    [Tooltip("Local-space offset (x,y,z) applied to the emission position. Interpreted relative to the ParticleSystem's transform.")]
    [SerializeField] private Vector3 emissionOffset = Vector3.zero;

    [Tooltip("Upward speed applied to emitted particles (units/sec).")]
    [SerializeField] private float upwardSpeed = 1f;

    [SerializeField] private float emitRate = 5f; // particles per second

    private float timer;
    private ParticleSystem.EmitParams emitParams;
    private bool simulationSpaceIsLocal;

    private void Awake()
    {
        if (smoke == null)
        {
            Debug.LogWarning($"[{nameof(CoalPowerPlantSmoke)}] ParticleSystem reference is null on '{name}'. Smoke will not emit until a ParticleSystem is assigned.");
            return;
        }

        emitParams = new ParticleSystem.EmitParams();
        simulationSpaceIsLocal = smoke.main.simulationSpace == ParticleSystemSimulationSpace.Local;

        // Ensure the system is playing so emitted particles are visible immediately
        if (!smoke.isPlaying) smoke.Play();
    }

    private void Update()
    {
        if (smoke == null) return;
        if (emitRate <= 0f) return;

        timer += Time.deltaTime;
        float interval = 1f / emitRate;

        while (timer >= interval)
        {
            EmitRandomChimney();
            timer -= interval;
        }
    }

    private void EmitRandomChimney()
    {
        // Build list of available chimneys
        if (chimneyA == null && chimneyB == null) return;

        Transform[] available;
        if (chimneyA != null && chimneyB != null)
        {
            available = new[] { chimneyA, chimneyB };
        }
        else if (chimneyA != null)
        {
            available = new[] { chimneyA };
        }
        else // chimneyB != null
        {
            available = new[] { chimneyB };
        }

        int idx = Random.Range(0, available.Length); // pseudorandom selection
        EmitFrom(available[idx].position);
    }

    private void EmitFrom(Vector3 worldChimneyPosition)
    {
        if (smoke == null) return;

        // Apply configured emissionOffset. emissionOffset is specified in the ParticleSystem's local space,
        // so convert it to world-space offset before adding to the chimney world position.
        Vector3 worldOffset = smoke.transform.TransformVector(emissionOffset);
        Vector3 finalWorldPos = worldChimneyPosition + worldOffset;

        // EmitParams.position must be given in the particle system's simulation space:
        if (simulationSpaceIsLocal)
            emitParams.position = smoke.transform.InverseTransformPoint(finalWorldPos);
        else
            emitParams.position = finalWorldPos;

        // Set emitted particle velocity to point upward.
        // Velocity must also be in the same space as the simulationSpace setting.
        if (simulationSpaceIsLocal)
            emitParams.velocity = smoke.transform.InverseTransformDirection(Vector3.up) * upwardSpeed;
        else
            emitParams.velocity = Vector3.up * upwardSpeed;

        smoke.Emit(emitParams, 1);
    }

    // Convenience: auto-assign a child ParticleSystem in the editor if none is set.
    private void OnValidate()
    {
        if (smoke == null)
        {
            smoke = GetComponentInChildren<ParticleSystem>();
        }
    }
}

using UnityEngine;
using WaterSystem;
using Unity.Mathematics;
using Unity.Collections;

namespace WaterSystem
{
    /// <summary>
    /// This controls the logic for the wind surfer
    /// </summary>
    public class WindsurferManager : MonoBehaviour
    {
        public Transform[] surfers;
        private NativeArray<float3> _points; // point to sample wave height
        private float3[] _heights; // height sameple from water system
        private float3[] _normals; // height sameple from water system
        private Vector3[] _smoothPositions; // the smoothed position
        private int _guid; // the objects GUID for wave height lookup

        // Use this for initialization
        private void Start()
        {
            _guid = gameObject.GetInstanceID();

            _heights = new float3[surfers.Length];
            _normals = new float3[surfers.Length];
            _smoothPositions = new Vector3[surfers.Length];

            for (var i = 0; i < surfers.Length; i++)
            {
                _smoothPositions[i] = surfers[i].position;
            }
            _points = new NativeArray<float3>(surfers.Length, Allocator.Persistent);
        }

        private void OnDisable()
        {
            _points.Dispose();
        }

        // TODO - need to validate logic here (not smooth at all in demo)
        private void Update()
        {
            //GerstnerWaves.UpdateSamplePoints(ref _points, _guid);
           // GerstnerWaves.GetData(_guid, ref _heights, ref _normals);

            for (int i = 0; i < surfers.Length; i++)
            {
                _smoothPositions[i] = surfers[i].position;
                // Sample the water height at the current position
                _points[0] = _smoothPositions[i];
                if (_heights[0].y > _smoothPositions[i].y)
                    _smoothPositions[i].y += Time.deltaTime;
                else
                    _smoothPositions[i].y -= Time.deltaTime * 0.25f;
#if !STATIC_EVERYTHING
                surfers[i].position = _smoothPositions[i];
#endif
            }
        }
    }
}


/* What ChatGPT said about updating lines 44 and 45:

The two implementations both compute Gerstner‐wave displacements in a Burst‐compiled job, but they live in completely different pipelines and data layouts. In short:

1. **Old (`GerstnerWavesJobs`)**

   * A **static**, standalone helper.
   * You manually call `UpdateSamplePoints(ref NativeArray<float3> samplePoints, int guid)` to copy each object’s world‐space positions into a big `_positions` array, keyed by that `guid`.
   * Then you call `UpdateHeights()`, which schedules a single `HeightJob` over all registered positions, writing out `_wavePos` and `_waveNormal` into flat `NativeArray<float3>` buffers.
   * Finally, any code can call `GetData(int guid, ref float3[] outPos, ref float3[] outNorm)` to copy that GUID’s slice of `_wavePos/_waveNormal` back into ordinary float3 arrays.
   * In other words, you drive everything by hand: (1) push object‐positions in → (2) schedule the HeightJob → (3) pull wave‐results back out.

2. **New (`GerstnerWaves : WaterModifier<…>`)**

   * An **instance‐based modifier** that plugs into the newer `WaterPhysics`/`WaterModifier<T>` pipeline.
   * You never call `UpdateSamplePoints` or `GetData` yourself. Instead, each “buoyant query” or “mesh‐vertex query” registers its sample‐positions as `WaterSample` entries in a shared `NativeArray<WaterSample>`.
   * Every frame, `WaterPhysics` gathers all `WaterSample` positions (with their instance IDs) and then calls your `GerstnerWaves.EnqueueJob(...)`. Your `HeightJob` reads from `NativeArray<WaterSample>` and writes per‐sample results into `NativeArray<WaterSurface>`.
   * Down‐stream code can call `WaterQuery.GetData(...)` to copy only that query’s slice of `WaterSurface` back to CPU. But you never manage a giant static `_positions` array or do explicit GUID lookups yourself. The `WaterModifier` base class handles offsets, instance IDs, and slicing for you.

---

### Key functional differences

| Aspect                     | Old (`GerstnerWavesJobs`)                                                                                                   | New (`GerstnerWaves : WaterModifier`)                                                                                                                                 |
| -------------------------- | --------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Registration of samples    | You call `UpdateSamplePoints(ref NativeArray<float3>, guid)` manually                                                       | Your `WaterQuery` implementation calls into `WaterPhysics`, which handles sample registration for you                                                                 |
| Data layout                | Flat `NativeArray<float3> _positions` and parallel arrays for wave output (`_wavePos`, `_waveNormal`) keyed by GUID offsets | `NativeArray<WaterSample>` holds (pos + instanceID); output is written into `NativeArray<WaterSurface>`                                                               |
| How you schedule jobs      | You call a static `UpdateHeights()`, which schedules a single `HeightJob` over all registered samples–any frame you choose  | `WaterPhysics.LateUpdate()` will schedule your `HeightJob` automatically as part of its pipeline, chaining dependencies to other water‐modifier jobs                  |
| Fetching results           | You call `GetData(guid, ref float3[] outPos, ref float3[] outNorm)` to copy wave positions and normals back for that GUID   | You call `WaterQuery.GetData(ref WaterSurface[] out)` to copy only your query‐slice of `WaterSurface` back                                                            |
| Coupling with physics/mesh | Completely detached from any “water body” concept; you manage GUIDs yourself                                                | Tightly integrated: each `GerstnerWaves` modifier is tied to a specific `WaterBody` via `dataset.WaterBodyId`, so it only runs on that body’s samples                 |
| Code complexity            | You manually manage `Registry[g] = (start,end)` indices, plus global NativeArrays, and call `.Dispose()` in cleanup         | The base `WaterModifier` class handles index offsets, NativeArray lifetimes (disposing when the body goes away), and hashing‐based recook when wave parameters change |

---

## “Can I just rename my old calls to use the new class?”

No. In your old code you wrote:

```csharp
GerstnerWavesJobs.UpdateSamplePoints(ref _points, _guid);
GerstnerWavesJobs.GetData(_guid, ref _heights, ref _normals);
```

Those methods do not exist on the new `GerstnerWaves` class, because:

1. **`GerstnerWaves` is not static**—it inherits from `WaterModifier<Data, JobData>`. You never call `GerstnerWaves.UpdateSamplePoints(...)`. Instead, all sample‐registration happens inside `WaterPhysics`, which invokes your `GerstnerWaves.EnqueueJob(...)`.
2. There is no public `GetData(int, ref float3[], ref float3[])` on the new class. Output lives in `WaterSurface[]`, and consumers call `WaterQuery.GetData(...)` to read from that.

To migrate, you must:

1. **Convert any custom “push‐point, fetch‐results” logic** into a proper `WaterQuery` implementation. That query registers sample‐positions in `SetQueryPositions(...)` and then retrieves each frame’s `WaterSurface` data in `GetQueryResults(...)`.
2. **Remove direct calls to `GerstnerWavesJobs.UpdateSamplePoints`/`GetData`**. Instead, let the `WaterModifier` system handle the job scheduling, and query your results from the native `WaterSurface` slice.

---

### Bottom line

* The old `GerstnerWavesJobs` is a self-contained static helper; you always manually push positions and pull wave results.
* The new `GerstnerWaves` plugs into URP’s `WaterPhysics` pipeline, so you no longer drive it with `UpdateSamplePoints`/`GetData`.
* You **cannot** simply rename `GerstnerWavesJobs` → `GerstnerWaves` and expect the same calls to exist. Instead, you must rewrite any code that used those two static calls into the new `WaterQuery`/`WaterModifier` pattern.



*/

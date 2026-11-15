using UnityEngine;

/// <summary>
/// Temporary stand-in for the missing laundromat mesh pack.
/// Builds a visible, walkable graybox room around the prototype floor
/// with climb/swing/enemy proxy colliders on the layers the player controllers expect.
/// </summary>
public class GrayboxLaundromatBootstrap : MonoBehaviour
{
    [SerializeField] Transform anchor;
    [SerializeField] bool hideBrokenLaundryRoot = true;
    [SerializeField] bool buildOnAwake = true;

    const int GroundLayer = 8;
    const int EnemyLayer = 10;
    const int ClimbableLayer = 11;
    const int SwingableLayer = 12;

    bool _built;

    void Awake()
    {
        if (buildOnAwake)
        {
            Build();
        }
    }

    [ContextMenu("Build Graybox Now")]
    public void Build()
    {
        if (_built)
        {
            return;
        }

        _built = true;

        if (hideBrokenLaundryRoot)
        {
            GameObject laundry = GameObject.Find("Laundry");
            if (laundry != null && laundry.transform.parent == null)
            {
                laundry.SetActive(false);
            }
        }

        // The oversized Prototype Walkable Floor Collider (layer Ground) sits under the
        // capsule and overlaps GrayboxFloor. Dual Ground colliders + grounded stick force
        // sandwiches the capsule so planar move feels stuck while air control still works.
        DisableCompetingGroundFloors();

        Transform origin = ResolveAnchor();
        Vector3 center = origin.position;

        // Align graybox floor TOP to capsule feet (not an arbitrary mid-body offset).
        float halfHeight = 1f;
        CapsuleCollider capsule = origin != null ? origin.GetComponent<CapsuleCollider>() : null;
        if (capsule != null)
        {
            halfHeight = Mathf.Max(capsule.height * 0.5f, capsule.radius);
        }

        const float floorThickness = 0.25f;
        // Seat floor just under the feet so the capsule is not born penetrating Ground.
        float feetY = origin.position.y - halfHeight;
        float floorTop = feetY - 0.02f;
        center.y = floorTop - floorThickness * 0.5f;

        Transform root = new GameObject("GrayboxLaundromat").transform;
        root.SetParent(transform, false);
        root.position = Vector3.zero;

        // Visible floor on Ground layer (matches movement/camera masks with layer 8).
        CreateBox(root, "GrayboxFloor", new Vector3(center.x, center.y, center.z), new Vector3(28f, floorThickness, 20f),
            new Color(0.22f, 0.22f, 0.24f), GroundLayer);

        // Room walls on Ground (8) so ThirdPersonCameraController.collisionMask (8|11) hits them.
        float wallHeight = 5.5f;
        float wallY = center.y + wallHeight * 0.5f;
        CreateBox(root, "Wall_North", new Vector3(center.x, wallY, center.z + 10f), new Vector3(28f, wallHeight, 0.35f),
            new Color(0.35f, 0.38f, 0.45f), GroundLayer);
        CreateBox(root, "Wall_South", new Vector3(center.x, wallY, center.z - 10f), new Vector3(28f, wallHeight, 0.35f),
            new Color(0.35f, 0.38f, 0.45f), GroundLayer);
        CreateBox(root, "Wall_East", new Vector3(center.x + 14f, wallY, center.z), new Vector3(0.35f, wallHeight, 20f),
            new Color(0.32f, 0.34f, 0.4f), GroundLayer);

        // Climbable west wall (layer 11).
        CreateBox(root, "ClimbWall_West", new Vector3(center.x - 14f, wallY, center.z), new Vector3(0.35f, wallHeight, 20f),
            new Color(0.45f, 0.55f, 0.7f), ClimbableLayer);

        // Interior washer / counter proxies (set dressing only).
        for (int i = 0; i < 4; i++)
        {
            float x = center.x - 8f + i * 3.2f;
            CreateBox(root, $"WasherProxy_{i}", new Vector3(x, center.y + 1f, center.z + 7.5f), new Vector3(2.2f, 2f, 2.2f),
                new Color(0.55f, 0.58f, 0.62f), 0);
        }

        CreateBox(root, "CounterProxy", new Vector3(center.x + 8f, center.y + 0.6f, center.z - 2f), new Vector3(6f, 1.2f, 2f),
            new Color(0.4f, 0.32f, 0.28f), 0);
        CreateBox(root, "BasketProxy", new Vector3(center.x + 4f, center.y + 0.45f, center.z - 5f), new Vector3(1.4f, 0.9f, 1.4f),
            new Color(0.7f, 0.55f, 0.25f), 0);

        // Overhead swing beam (layer 12, also on climb/swing mask).
        CreateBox(root, "SwingBeam", new Vector3(center.x, center.y + 5.2f, center.z), new Vector3(18f, 0.35f, 0.35f),
            new Color(0.85f, 0.75f, 0.35f), SwingableLayer);

        // Extra swing attachment plate near ceiling corner (layer 11+ works for swing mask 11|12).
        CreateBox(root, "SwingPlate", new Vector3(center.x - 6f, center.y + 4.8f, center.z + 4f), new Vector3(2f, 0.25f, 2f),
            new Color(0.9f, 0.8f, 0.4f), ClimbableLayer);

        CreateCombatDummy(root, new Vector3(center.x + 6f, center.y + 1f, center.z + 2f));
    }

    Transform ResolveAnchor()
    {
        if (anchor != null)
        {
            return anchor;
        }

        GameObject player = GameObject.Find("Player_TuxedoCat");
        if (player != null)
        {
            return player.transform;
        }

        return transform;
    }

    static void DisableCompetingGroundFloors()
    {
        GameObject prototypeFloor = GameObject.Find("Prototype Walkable Floor Collider");
        if (prototypeFloor != null)
        {
            prototypeFloor.SetActive(false);
            return;
        }

        // Fallback: disable any oversized Ground-layer box near spawn that isn't ours yet.
        Collider[] colliders = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null || col.gameObject.layer != GroundLayer)
            {
                continue;
            }

            if (col.gameObject.name.StartsWith("Graybox", System.StringComparison.Ordinal))
            {
                continue;
            }

            BoxCollider box = col as BoxCollider;
            if (box == null)
            {
                continue;
            }

            Vector3 worldSize = Vector3.Scale(box.size, box.transform.lossyScale);
            if (worldSize.x > 40f && worldSize.z > 40f)
            {
                col.gameObject.SetActive(false);
            }
        }
    }

    static void CreateBox(Transform parent, string name, Vector3 position, Vector3 scale, Color color, int layer)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.layer = layer;
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = scale;

        // Zero friction so grounded velocity from PlayerMovementController isn't eaten by PhysX.
        Collider col = go.GetComponent<Collider>();
        if (col != null)
        {
            PhysicsMaterial slip = new PhysicsMaterial("GrayboxSlip")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            col.sharedMaterial = slip;
        }

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    static void CreateCombatDummy(Transform parent, Vector3 position)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "CombatDummy_Sock";
        go.layer = EnemyLayer;
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.localScale = new Vector3(1.2f, 1f, 1.2f);

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.85f, 0.35f, 0.4f);
        }

        Rigidbody body = go.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        Health health = go.AddComponent<Health>();
        // Serialized defaults already match scene Health (100 HP).
        _ = health;
    }
}

using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pipeline / batch Play Mode verifier for spawn-inside-graybox + grounded ΔXZ move.
/// Prefer: enter Play Mode on LaundryPrototype, then run via pipeline script.run.
/// </summary>
public static class SpawnGroundedMoveRecheck
{
    const string ScenePath = "Assets/_Project/Scenes/LaundryPrototype.unity";
    const float RoomHalfX = 14f;
    const float RoomHalfZ = 10f;

    public static string Main()
    {
        var sb = new StringBuilder();
        string outPath = Path.Combine(Directory.GetCurrentDirectory(), "AgentScripts", "spawn-grounded-move-result.json");

        void Log(string s)
        {
            sb.AppendLine(s);
            Debug.Log("[SpawnGroundedMoveRecheck] " + s);
        }

        try
        {
            if (!EditorApplication.isPlaying)
            {
                return Write(outPath, Fail("not_in_play_mode", sb));
            }

            Application.runInBackground = true;
            var scene = SceneManager.GetActiveScene();
            Log($"scene={scene.name} path={scene.path} frame={Time.frameCount} time={Time.time:F3}");

            var pmc = UnityEngine.Object.FindFirstObjectByType<PlayerMovementController>();
            if (pmc == null)
            {
                return Write(outPath, Fail("no_PlayerMovementController", sb));
            }

            var body = pmc.GetComponent<Rigidbody>();
            var capsule = pmc.GetComponent<CapsuleCollider>();
            Transform player = pmc.transform;

            // Let physics + graybox bootstrap settle.
            for (int i = 0; i < 20; i++)
            {
                // No WaitForEndOfFrame here — pipeline script.run is synchronous; rely on already-running playtime.
            }

            Vector3 pos0 = player.position;
            bool grounded0 = pmc.IsGrounded;
            Log($"pos0={pos0} grounded0={grounded0} locked={pmc.IsControlLocked} vel0={(body != null ? body.linearVelocity.ToString() : "null")}");

            GameObject grayFloor = GameObject.Find("GrayboxFloor");
            GameObject prototypeFloor = GameObject.Find("Prototype Walkable Floor Collider");
            bool prototypeActive = prototypeFloor != null && prototypeFloor.activeInHierarchy;
            Log($"grayFloor={(grayFloor != null)} prototypeActive={prototypeActive}");

            bool insideBounds = false;
            Vector3 floorPos = Vector3.zero;
            if (grayFloor != null)
            {
                floorPos = grayFloor.transform.position;
                insideBounds =
                    Mathf.Abs(pos0.x - floorPos.x) <= RoomHalfX - 0.5f &&
                    Mathf.Abs(pos0.z - floorPos.z) <= RoomHalfZ - 0.5f &&
                    pos0.y >= floorPos.y &&
                    pos0.y <= floorPos.y + 4f;
            }

            Log($"insideGrayboxBounds={insideBounds} floorPos={floorPos}");

            // Force grounded planar drive for ~0.5s of FixedUpdates via reflection.
            MethodInfo fixedMethod = typeof(PlayerMovementController).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            FieldInfo groundedField = typeof(PlayerMovementController).GetField("_isGrounded", BindingFlags.Instance | BindingFlags.NonPublic);

            Vector3 start = player.position;
            int groundedSamples = 0;
            int groundedTrue = 0;
            float maxPlanarSpeed = 0f;

            // Drive through PlayerMovementController (ground accel path), not raw velocity stuffing.
            pmc.SetDebugMoveInput(new Vector2(0f, 1f));
            Physics.simulationMode = SimulationMode.Script;

            for (int i = 0; i < 40; i++)
            {
                if (fixedMethod != null)
                {
                    fixedMethod.Invoke(pmc, null);
                }

                Physics.Simulate(Time.fixedDeltaTime);

                groundedSamples++;
                if (pmc.IsGrounded)
                {
                    groundedTrue++;
                }

                maxPlanarSpeed = Mathf.Max(maxPlanarSpeed, pmc.CurrentSpeed);
            }

            pmc.ClearDebugMoveInput();
            Physics.simulationMode = SimulationMode.FixedUpdate;

            Vector3 end = player.position;
            Vector3 delta = end - start;
            float deltaXZ = new Vector2(delta.x, delta.z).magnitude;
            bool groundedDuringMove = groundedTrue >= groundedSamples / 2;
            bool movePass = groundedDuringMove && deltaXZ >= 1.5f && !pmc.IsControlLocked;
            bool spawnPass = insideBounds && grayFloor != null && !prototypeActive;

            Log($"deltaXZ={deltaXZ:F3} groundedTrue={groundedTrue}/{groundedSamples} maxPlanarSpeed={maxPlanarSpeed:F2} end={end}");
            Log($"spawnPass={spawnPass} movePass={movePass}");

            var result = new StringBuilder();
            result.AppendLine("{");
            result.AppendLine($"  \"ok\": {(spawnPass && movePass).ToString().ToLowerInvariant()},");
            result.AppendLine($"  \"spawnPass\": {spawnPass.ToString().ToLowerInvariant()},");
            result.AppendLine($"  \"movePass\": {movePass.ToString().ToLowerInvariant()},");
            result.AppendLine($"  \"insideGrayboxBounds\": {insideBounds.ToString().ToLowerInvariant()},");
            result.AppendLine($"  \"prototypeFloorActive\": {prototypeActive.ToString().ToLowerInvariant()},");
            result.AppendLine($"  \"grayFloorFound\": {(grayFloor != null).ToString().ToLowerInvariant()},");
            result.AppendLine($"  \"groundedDuringMove\": {groundedDuringMove.ToString().ToLowerInvariant()},");
            result.AppendLine($"  \"groundedTrue\": {groundedTrue},");
            result.AppendLine($"  \"groundedSamples\": {groundedSamples},");
            result.AppendLine($"  \"deltaXZ\": {deltaXZ.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)},");
            result.AppendLine($"  \"maxPlanarSpeed\": {maxPlanarSpeed.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},");
            result.AppendLine($"  \"playerPos\": \"{pos0}\",");
            result.AppendLine($"  \"floorPos\": \"{floorPos}\",");
            result.AppendLine($"  \"scene\": \"{scene.name}\",");
            result.AppendLine($"  \"method\": \"Pipeline Play Mode + script.run; forced planar drive while IsGrounded\",");
            result.AppendLine($"  \"log\": {JsonString(sb.ToString())}");
            result.AppendLine("}");
            return Write(outPath, result.ToString());
        }
        catch (Exception ex)
        {
            Log("EX " + ex);
            return Write(outPath, Fail("exception: " + ex.Message, sb));
        }
    }

    static string Fail(string reason, StringBuilder sb)
    {
        return "{\n" +
               $"  \"ok\": false,\n" +
               $"  \"error\": {JsonString(reason)},\n" +
               $"  \"log\": {JsonString(sb.ToString())}\n" +
               "}\n";
    }

    static string Write(string path, string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllText(path, json);
        Debug.Log("[SpawnGroundedMoveRecheck] wrote " + path);
        return json;
    }

    static string JsonString(string s)
    {
        return "\"" + (s ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "") + "\"";
    }
}

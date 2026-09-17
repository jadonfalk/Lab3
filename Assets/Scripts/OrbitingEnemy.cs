using UnityEngine;

public class OrbitingEnemy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform orbitCenter;

    [Header("Orbit")]
    [SerializeField] private bool clockwise = true;

    // Angular speeds in degrees per second.
    [SerializeField] private float farSpeed = 25f;
    [SerializeField] private float nearSpeed = 90f;

    // How strongly the enemy returns to its original orbit radius.
    [SerializeField] private float orbitReturnStrength = 2f;

    [Header("Distance Thresholds")]
    [SerializeField] private float nearDistance = 2f;
    [SerializeField] private float farDistance = 8f;

    [Header("Player Avoidance")]
    [SerializeField] private float playerAvoidanceDistance = 2f;
    [SerializeField] private float playerAvoidanceStrength = 12f;

    // Minimum center-to-center distance.
    // Set this to approximately the player radius + enemy radius.
    [SerializeField] private float playerMinimumDistance = 1f;

    [Header("Facing")]
    // 90 for sprites that point downward at zero rotation.
    [SerializeField] private float facingOffset = 90f;

    private float orbitRadius;
    private float startingZ;

    private void Start()
    {
        if (player == null || orbitCenter == null)
        {
            Debug.LogWarning(
                "Assign a Player and Orbit Center to this enemy.", this);

            enabled = false;
            return;
        }

        // Calculate the starting orbit radius on the XY plane.
        Vector3 offset = transform.position - orbitCenter.position;
        offset.z = 0f;

        orbitRadius = Mathf.Max(offset.magnitude, 0.1f);
        startingZ = transform.position.z;
    }

    private void LateUpdate()
    {
        if (player == null || orbitCenter == null)
            return;

        // Move after the player's Update has finished.
        Vector3 position = transform.position;

        // Calculate squared distance to the player.
        Vector3 toPlayer = player.position - position;
        toPlayer.z = 0f;

        // Keep distance thresholds valid.
        float near = Mathf.Max(0f, nearDistance);
        float far = Mathf.Max(near + 0.01f, farDistance);

        // 0 when near the player, 1 when far away.
        float distanceRatio = Mathf.Clamp01(
            (toPlayer.sqrMagnitude - near * near) /
            (far * far - near * near)
        );

        float angularSpeed = Mathf.Lerp(
            nearSpeed, farSpeed, distanceRatio
        );

        // Find the outward direction from the orbit center.
        Vector3 radial = position - orbitCenter.position;
        radial.z = 0f;

        float currentRadius = radial.magnitude;

        Vector3 outward = currentRadius > 0.0001f
            ? radial / currentRadius
            : Vector3.right;

        // A perpendicular vector points along the orbit.
        Vector3 tangent = clockwise
            ? new Vector3(outward.y, -outward.x, 0f)
            : new Vector3(-outward.y, outward.x, 0f);

        // Convert angular speed into movement speed.
        float moveSpeed =
            angularSpeed * Mathf.Deg2Rad * orbitRadius;

        Vector3 orbitVelocity = tangent * moveSpeed;

        // Return toward the original radius after dodging the player.
        Vector3 returnVelocity = outward *
            (orbitRadius - currentRadius) * orbitReturnStrength;

        // Only avoid the player. Other enemies are ignored.
        Vector3 avoidanceVelocity = GetPlayerAvoidance(
            position, outward
        );

        // Combine the vectors to choose a movement direction.
        Vector3 steering =
            orbitVelocity + returnVelocity + avoidanceVelocity;

        // Fall back to the orbit direction if the vectors cancel out.
        Vector3 moveDirection = steering.sqrMagnitude > 0.0001f
            ? steering.normalized
            : tangent;

        // Preserve the speed calculated from distance to the player.
        position += moveDirection * moveSpeed * Time.deltaTime;

        // Enforce minimum separation from the player.
        Vector3 awayFromPlayer = position - player.position;
        awayFromPlayer.z = 0f;

        float minimumDistance = Mathf.Max(
            0f, playerMinimumDistance
        );

        if (awayFromPlayer.sqrMagnitude <
            minimumDistance * minimumDistance)
        {
            Vector3 separationDirection =
                awayFromPlayer.sqrMagnitude > 0.0001f
                    ? awayFromPlayer.normalized
                    : outward;

            // Place the enemy on the edge of the player's safety area.
            position = player.position +
                separationDirection * minimumDistance;
        }

        // Keep movement on the original XY plane.
        position.z = startingZ;
        transform.position = position;

        // Face the player after movement and separation.
        toPlayer = player.position - position;
        toPlayer.z = 0f;

        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            float angle =
                Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

            transform.rotation = Quaternion.Euler(
                0f, 0f, angle + facingOffset
            );
        }
    }

    private Vector3 GetPlayerAvoidance(
        Vector3 position,
        Vector3 fallbackDirection)
    {
        if (playerAvoidanceDistance <= 0f)
            return Vector3.zero;

        Vector3 away = position - player.position;
        away.z = 0f;

        float distanceSquared = away.sqrMagnitude;
        float safeDistanceSquared =
            playerAvoidanceDistance * playerAvoidanceDistance;

        // No steering is needed outside the avoidance range.
        if (distanceSquared >= safeDistanceSquared)
            return Vector3.zero;

        // Handle matching player and enemy positions.
        if (distanceSquared < 0.0001f)
            return fallbackDirection * playerAvoidanceStrength;

        // Increase avoidance as the enemy approaches the player.
        float weight =
            1f - distanceSquared / safeDistanceSquared;

        return away.normalized * playerAvoidanceStrength * weight;
    }
}
using UnityEngine;

/// <summary>
/// Simple script to make camera follow the player without the rotation that comes with parenting
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private float cameraHeight;
    [SerializeField] private Vector3 adjustedPosition;

    /// <summary>
    /// We're putting the logic in late update *for now* to make sure it happens after the player moves but before the next frame, since the player moves in update
    /// TODO: Move camera from player or god script?
    /// </summary>
    private void LateUpdate()
    {
        adjustedPosition = player.transform.position;
        adjustedPosition.z += cameraHeight;
        transform.position = adjustedPosition;
    }
}

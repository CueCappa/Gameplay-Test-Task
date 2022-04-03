using UnityEngine;

/// <summary>
/// Simple script to make camera follow the player without the rotation that comes with parenting
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private float cameraHeight;
    [SerializeField] private Vector3 adjustedPosition;

    // I'm putting the logic for camera movement in late update to make sure it happens after the player moves but before the next frame, since the player moves in update
    private void LateUpdate()
    {
        adjustedPosition = player.transform.position;
        adjustedPosition.z += cameraHeight;
        transform.position = adjustedPosition;
    }
}

using UnityEngine;
using UnityEngine.Serialization;

public class LogoFollower : MonoBehaviour
{
    [FormerlySerializedAs("target")] public Transform Target; // Reference to the main camera
    [FormerlySerializedAs("smoothness")] public float Smoothness = 0.1f; // Smoothing factor for the logo movement
    [FormerlySerializedAs("followDelay")] public float FollowDelay = 0.5f; // Delay between the logo and the main camera

    private Vector3 _initialOffset; // Initial offset between the logo and the camera
    private Vector3 _velocity = Vector3.zero; // Velocity for smoothing the movement

    private void Start()
    {
        // Calculate and store the initial offset between the logo and the camera
        _initialOffset = transform.position - Target.position;
    }

    private void LateUpdate()
    {
        // Calculate the target position with a delay
        var targetPosition = Target.position + Target.forward * FollowDelay;

        // Calculate the center position of the camera's view
        var cameraCenterPosition = targetPosition;

        // Calculate the final position of the logo based on the center position and the initial offset
        var logoPosition = cameraCenterPosition +
                           Quaternion.Euler(Target.eulerAngles.x, Target.eulerAngles.y, 0f) * _initialOffset;

        // Smoothly move the logo towards the final position
        transform.position = Vector3.SmoothDamp(transform.position, logoPosition, ref _velocity, Smoothness);

        // Rotate the logo to face the camera
        transform.LookAt(Target);
        transform.Rotate(0f, 0f, 180f);
    }
}

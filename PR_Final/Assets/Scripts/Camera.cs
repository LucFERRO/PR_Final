using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public Transform targetTransform; // Le joueur � suivre
    public float smoothTime = 0.1f; // Temps de lissage pour la position
    public float rotationSpeed = 5f; // Vitesse de rotation de la cam�ra
    public float lookSpeed = 2f; // Vitesse de la souris pour la rotation
    public float pitchClamp = 85f; // Limiter la rotation verticale

    private Vector3 startOffset; // Offset initial entre la cam�ra et le joueur
    private Vector3 velocity = Vector3.zero;
    private float currentPitch = 0f; // Rotation verticale actuelle
    private float currentYaw = 0f; // Rotation horizontale actuelle

    private void Start()
    {
        startOffset = transform.position - targetTransform.position; // Calcul de l'offset initial
        Cursor.lockState = CursorLockMode.Locked; // Verrouille le curseur au centre de l'�cran
    }

    private void Update()
    {
        RotateCamera(); // Appel de la m�thode pour faire tourner la cam�ra avec la souris
    }

    private void FixedUpdate()
    {
        Vector3 targetPosition = targetTransform.position + startOffset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

        // S'assurer que la cam�ra suit la rotation du joueur
        Quaternion playerRotation = targetTransform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, playerRotation, Time.deltaTime * rotationSpeed);
    }

    private void RotateCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed; // R�cup�rer la position de la souris
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

        currentYaw += mouseX; // Ajouter le mouvement horizontal
        currentPitch -= mouseY; // Ajouter le mouvement vertical
        currentPitch = Mathf.Clamp(currentPitch, -pitchClamp, pitchClamp); // Limiter la rotation verticale

        // Appliquer la rotation
        transform.localRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
    }

    public void RotateCameraBasedOnCollision(Vector3 collisionWallNormal)
    {
        // Calculez la rotation de la cam�ra en fonction de la direction du mur
        float rotationAngle = Vector3.Dot(transform.right, collisionWallNormal) > 0 ? 90f : -90f;

        // Appliquer la rotation instantan�ment pour une r�ponse imm�diate
        Quaternion wallRotation = Quaternion.Euler(0f, rotationAngle, 0f) * transform.rotation;
        transform.rotation = wallRotation;
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;

public class Character : MonoBehaviour
{
    private CharacterController characterController;
    public static Vector3 positionJoueur;

    public float forwardSpeed = 5f; // la vitesse constante genre en avant
    public float lateralSpeed = 5f; // la vitesse de gauche � droite
    public float movementSpeed = 5f;

    public float jumpSpeed = 20f; // hauteur saut
    public float ySpeed = 0f; // 
    public float gravite = 4f; // la gravit� (doit faire � peu pr�s 1/4 de la jumpspeed apr�s avoir test�)

    public FollowPlayer cameraScript;

    public float speedCap = 50f;
    public float exponentialDecay = 0.0005f;

    public float logGrowth = 50f;
    public float logHolder = 0.1f; // initialSpeed x2 apr�s 6.5s, x3 apr�s 17s, x4 apr�s 34s, donc trop rapide?

    //public float logGrowth = 100f;
    //public float logHolder = 0.05f; // initialSpeed x2 apr�s 6s, x3 apr�s 12s, x4 apr�s 22s, donc trop rapide?

    private Vector3 lastPosition; //pour TP
    private float totalDistance = 0f; //pour compter distance 
    private float distanceSinceLastLog = 0f;

    private float[] logIntervals = new float[] { 100f, 250f, 500f, 750f, 1000f, 1500f, 2000f }; //A MODIFIER ABSOLUMENT SINON ON VA PAS S'EN SORTIR
    //private int lastLogIndex = 0;

    public UIHandler uiHandler;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        //lastPosition = transform.position;

        if (uiHandler == null)
        {
            uiHandler = FindObjectOfType<UIHandler>();
            Debug.Log("Quoicoubeh fr�re");
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {

        if (hit.gameObject.CompareTag("obstacle"))
        {
            RestartScene();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // V�rifier si le joueur entre en collision avec un mur
        if (collision.gameObject.CompareTag("wallJump"))
        {
            Debug.Log("test");

            // R�cup�rer la normale du mur touch�
            Vector3 wallNormal = collision.contacts[0].normal;

            // Appeler une fonction dans le script de la cam�ra pour qu'elle s'oriente
            
        }
    }



    private void RestartScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    private void Update()
    {

        float verticalMove = Input.GetAxis("Vertical") * movementSpeed; // z
        float horizontalMove = Input.GetAxis("Horizontal") * movementSpeed; // x


        //Vector3 move = new Vector3(lateralMove, ySpeed, accelerationFunctionLog(Time.timeSinceLevelLoad));
        Vector3 move = new Vector3(verticalMove, 0, horizontalMove);

        //Avec acc�l�ration
        //Vector3 move = new Vector3(lateralMove, ySpeed, accelerationFunctionLog(Time.timeSinceLevelLoad));


        //Debug.Log("Time: "+Time.timeSinceLevelLoad);
        //Debug.Log(accelerationFunctionLog(Time.timeSinceLevelLoad));

        characterController.Move(move * Time.deltaTime);


        if (characterController.isGrounded)
        {

            if (Input.GetKeyDown(KeyCode.Space))
            {
                ySpeed = jumpSpeed; //fait le saut
            }
        }
        else
        {

            ySpeed += Physics.gravity.y * gravite * Time.deltaTime; // LE * GRAVITE EST ABSOLUMENT NECESSAIRE POUR UNE BELLE GRAV, PAS TOUCHER 

        }

        Vector3 currentPosition = transform.position;
        float distanceMoved = Vector3.Distance(lastPosition, currentPosition);

        totalDistance += distanceMoved;
        distanceSinceLastLog += distanceMoved;

        //if (lastLogIndex < logIntervals.Length && totalDistance >= logIntervals[lastLogIndex])
        //{
        //    Debug.Log($"{logIntervals[lastLogIndex]} m�tres parcourus !");
        //    uiHandler.ShowDistanceMessage(logIntervals[lastLogIndex]); 
        //    lastLogIndex++;
        //}
        lastPosition = currentPosition;
        positionJoueur = transform.position;

        //Debug.Log(Mathf.Floor(transform.position.z % 250));
        if (Mathf.Floor(transform.position.z % 250) == 0)
        {
            uiHandler.ShowDistanceMessage(milestonesAnnouncer());
        }
    }
    private float accelerationFunctionExp(float x) // Atteint le cap bien trop vite m�me avec exponentialDecay = 0.0005
    {
        return forwardSpeed + speedCap * (1 - Mathf.Exp(-exponentialDecay * x));
    }

    private float accelerationFunctionLog(float x)
    {
        return forwardSpeed + logGrowth * Mathf.Log(logHolder * x + 1);
    }

    public float milestonesAnnouncer()
    {



        Debug.Log($"{Mathf.Floor(transform.position.z)} m�tres parcourus !");
        return Mathf.Floor(transform.position.z);


    }
    //public void AddDistance(float distance) 
    //{
    //    totalDistance += distance;
    //    distanceSinceLastLog += distance;

    //    if (lastLogIndex < logIntervals.Length && totalDistance >= logIntervals[lastLogIndex])
    //    {
    //        Debug.Log($"{logIntervals[lastLogIndex]} m�tres parcourus !");
    //        uiHandler.ShowDistanceMessage(logIntervals[lastLogIndex]);
    //        lastLogIndex++;
    //    }
    //}
}


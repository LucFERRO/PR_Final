using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System;
using UnityEngine.TextCore.Text;

namespace Fragsurf.Movement
{

    /// <summary>
    /// Easily add a surfable character to the scene
    /// </summary>
    [AddComponentMenu("Fragsurf/Surf Character")]
    public class SurfCharacter : MonoBehaviour, ISurfControllable
    {

        public enum ColliderType
        {
            Capsule,
            Box
        }

        float horizontalInput;
        float verticalInput;
        public Camera cam;
        public Transform orientation;
        public float smoothStopMultiplier = 5f;
        public float groundDrag;

        public RaycastHit clippedWall;
        private float wallRunSpeed;

        public LayerMask whatIsGround;
        public LayerMask whatIsWall;

        private Ray ray;
        public float speedPenaltyCoef;


        //Grounded watcher
        private bool wasGrounded;
        public bool grounded;
        private bool groundedPublic
        {
            get
            {
                return grounded;
            }
            set
            {
                wasGrounded = grounded;
                grounded = value;
                if (!value && wasGrounded)
                {
                    //Debug.Log("jumped!");
                }
                if (value && !wasGrounded)
                {
                    //Debug.Log("landed!");
                    BunnySpeedPunish();
                }
            }
        }

        //Wallrun watcher
        private bool wasWallRunning;
        public bool wallRunning;
        private bool wallRunningPublic
        {
            get
            {
                return wallRunning;
            }
            set
            {
                wasWallRunning = wallRunning;
                wallRunning = value;
                //if (!value && wasWallRunning && Input.GetKeyUp(KeyCode.Space))
                if (!value && wasWallRunning && !Input.GetButton("Jump"))
                {
                    WallJump();
                    savedVelocity = 0;
                }
                if (value && !wasWallRunning && Input.GetButton("Jump"))
                {
                    savedVelocity = _moveData.velocity.magnitude;
                }
            }
        }

        //Current wall watcher
        private GameObject prevTouchedWall;
        private GameObject lastTouchedWall;
        private GameObject lastTouchedWallPublic
        {
            get
            {
                return lastTouchedWall;
            }
            set
            {
                prevTouchedWall = lastTouchedWall;
                lastTouchedWall = value;
                if (value == prevTouchedWall)
                {
                    prevTouchedWall.tag = "lastGrabbedWall";
                    Debug.Log("SAME WALL");
                }
                else
                {
                    //Debug.Log("DIFF WALL");
                }
            }
        }

        public float doubleJumpForce;

        [Header("Variations")]
        public bool redirectionVelocity;
        public bool redirectionVelocityWithoutZ;
        public bool canDoubleWallGrab;
        public float witchTime;
        public float maxWallrunDuration;
        public bool proportionnalWalljump;
        public float proportionnalWalljumpBoost1;
        public float proportionnalWalljumpBoost2;
        public bool fixedVelocityOnWallrun;
        public int percentage;
        public float wallDetectionRadius;

        [Header("DebugOverlay")]
        [SerializeField] private float savedVelocity;
        [SerializeField] public float currentSpeed;
        [SerializeField] private float currentWallrunDuration;
        [SerializeField] private float currentWitchTimePreparation;

        [Header("Teleportation Indicator")]
        public GameObject teleportationIndicatorPrefab;
        public GameObject currentIndicator;
        public MeshRenderer indicatorMesh;
        public float baseTeleportDistance = 25f;
        private float maxTeleportDistance;

        ///// Fields /////
        [Header("Physics Settings")]
        [HideInInspector] public Vector3 colliderSize = new Vector3(1f, 2f, 1f);
        [HideInInspector] public ColliderType collisionType { get { return ColliderType.Box; } }
        [HideInInspector] public float weight = 75f;
        [HideInInspector] public float rigidbodyPushForce = 2f;
        [HideInInspector] public bool solidCollider = false;

        [Header("View Settings")]
        public Transform viewTransform;
        public Transform playerRotationTransform;

        [Header("Crouching setup")]
        [HideInInspector] public float crouchingHeightMultiplier = 0.5f;
        [HideInInspector] public float crouchingSpeed = 10f;
        float defaultHeight;
        bool allowCrouch = false;

        [Header("Features")]
        [HideInInspector] public bool crouchingEnabled = false;
        [HideInInspector] public bool slidingEnabled = false;
        [HideInInspector] public bool laddersEnabled = true;
        [HideInInspector] public bool supportAngledLadders = true;

        [Header("Step offset")]
        [HideInInspector] public bool useStepOffset = false;
        [HideInInspector] public float stepOffset = 0.35f;

        [Header("Movement Config")]
        [SerializeField]
        [HideInInspector] public MovementConfig _movementConfig;

        private GameObject _groundObject;
        private Vector3 _baseVelocity;
        private Collider _collider;
        private Vector3 _angles;
        private Vector3 _startPosition;
        private GameObject _colliderObject;
        private GameObject _cameraWaterCheckObject;
        private CameraWaterCheck _cameraWaterCheck;

        [HideInInspector] public MoveData _moveData = new MoveData();
        private SurfController _controller = new SurfController();

        private Rigidbody rb;

        private List<Collider> triggers = new List<Collider>();
        private int numberOfTriggers = 0;

        private bool underwater = false;

        ///// Properties /////

        public MoveType moveType { get { return MoveType.Walk; } }
        public MovementConfig moveConfig { get { return _movementConfig; } }
        public MoveData moveData { get { return _moveData; } }
        public new Collider collider { get { return _collider; } }

        public GameObject groundObject
        {

            get { return _groundObject; }
            set { _groundObject = value; }

        }

        public Vector3 baseVelocity { get { return _baseVelocity; } }

        public Vector3 forward { get { return viewTransform.forward; } }
        public Vector3 right { get { return viewTransform.right; } }
        public Vector3 up { get { return viewTransform.up; } }

        Vector3 prevPosition;

        ///// Methods /////

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, colliderSize);
        }

        private void Awake()
        {

            _controller.playerTransform = playerRotationTransform;

            if (viewTransform != null)
            {

                _controller.camera = viewTransform;
                _controller.cameraYPos = viewTransform.localPosition.y;

            }

        }

        private void Start()
        {


            rb = gameObject.GetComponent<Rigidbody>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();

            rb.freezeRotation = true;

            _colliderObject = new GameObject("PlayerCollider");
            _colliderObject.layer = gameObject.layer;
            _colliderObject.transform.SetParent(transform);
            _colliderObject.transform.rotation = Quaternion.identity;
            _colliderObject.transform.localPosition = Vector3.zero;
            _colliderObject.transform.SetSiblingIndex(0);

            // Water check
            _cameraWaterCheckObject = new GameObject("Camera water check");
            _cameraWaterCheckObject.layer = gameObject.layer;
            _cameraWaterCheckObject.transform.position = viewTransform.position;

            SphereCollider _cameraWaterCheckSphere = _cameraWaterCheckObject.AddComponent<SphereCollider>();
            _cameraWaterCheckSphere.radius = 0.1f;
            _cameraWaterCheckSphere.isTrigger = true;

            Rigidbody _cameraWaterCheckRb = _cameraWaterCheckObject.AddComponent<Rigidbody>();
            _cameraWaterCheckRb.useGravity = false;
            _cameraWaterCheckRb.isKinematic = true;

            _cameraWaterCheck = _cameraWaterCheckObject.AddComponent<CameraWaterCheck>();

            prevPosition = transform.position;

            if (viewTransform == null)
                viewTransform = Camera.main.transform;

            if (playerRotationTransform == null && transform.childCount > 0)
                playerRotationTransform = transform.GetChild(0);

            _collider = gameObject.GetComponent<Collider>();

            if (_collider != null)
                GameObject.Destroy(_collider);

            allowCrouch = crouchingEnabled;

            //rb.isKinematic = true;
            // rb.useGravity = false;
            // rb.angularDrag = 0f;
            // rb.drag = 0f;
            rb.mass = 0;

            currentWallrunDuration = maxWallrunDuration;
            currentWitchTimePreparation = witchTime;

            switch (collisionType)
            {

                // Box collider
                case ColliderType.Box:

                    _collider = _colliderObject.AddComponent<BoxCollider>();

                    var boxc = (BoxCollider)_collider;
                    boxc.size = colliderSize;

                    defaultHeight = boxc.size.y;

                    break;

                // Capsule collider
                case ColliderType.Capsule:

                    _collider = _colliderObject.AddComponent<CapsuleCollider>();

                    var capc = (CapsuleCollider)_collider;
                    capc.height = colliderSize.y;
                    capc.radius = colliderSize.x / 2f;

                    defaultHeight = capc.height;

                    break;

            }

            _moveData.slopeLimit = _movementConfig.slopeLimit;

            _moveData.rigidbodyPushForce = rigidbodyPushForce;

            _moveData.slidingEnabled = slidingEnabled;
            _moveData.laddersEnabled = laddersEnabled;
            _moveData.angledLaddersEnabled = supportAngledLadders;

            _moveData.playerTransform = transform;
            _moveData.viewTransform = viewTransform;
            _moveData.viewTransformDefaultLocalPos = viewTransform.localPosition;

            _moveData.defaultHeight = defaultHeight;
            _moveData.crouchingHeight = crouchingHeightMultiplier;
            _moveData.crouchingSpeed = crouchingSpeed;

            _collider.isTrigger = !solidCollider;
            _moveData.origin = transform.position;
            _startPosition = transform.position;

            _moveData.useStepOffset = useStepOffset;
            _moveData.stepOffset = stepOffset;

        }

        private void Update()
        {

            _colliderObject.transform.rotation = Quaternion.identity;

            if (grounded)
            {
                _moveData.canDoubleJump = false;
                _moveData.hasDoubleJumpedSinceLastLanding = false;
                _moveData.hasTeleportedSinceLastLanding = false;
                ResetLastGrabbedWallFunction();
                ResetCurrentWallrunDuration();
            }
            else
            {
                _moveData.canDoubleJump = true;
            }

            if (!_moveData.hasTeleportedSinceLastLanding)
            {
                TpPreview();
            }

            CheckForWall();
            SimpleCheckGround();
            if (witchTime > 0)
            {
                checkForWitchTime();
            }

            ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            WallRunningMovement();
            AdjustTpDistance();
            //Reset velocity to fix the leftover Impulse dropping wallrunning
            ResetMovement();

            currentSpeed = _moveData.velocity.magnitude;


            //WALLRUN OLD
            //if (!Input.GetButtonDown("Jump") || (!_moveData.playerNearWallL && !_moveData.playerNearWallR && !_moveData.playerNearWallL && !_moveData.playerNearWallR))
            //{
            //    wallRunningPublic = false;
            //    _moveData.wallRunning = false;
            //}

            if (_moveData.wallRunning)
            {
                wallRunningPublic = true;
                HandleWallDuration();
            }
            else
            {
                percentage = 0;
                ResetCurrentWallrunDuration();
                wallRunningPublic = false;
                //Block movement while wallrunning
                UpdateMoveData();
            }

            if ((!grounded && !wallRunning && _moveData.canDoubleJump && !_moveData.hasDoubleJumpedSinceLastLanding && Input.GetButtonDown("Jump")))
            {
                DoubleJump();
                _moveData.hasDoubleJumpedSinceLastLanding = true;
                //PreventDoubleJump();
            }



            // DEBUG AREA

            //Debug.Log("wallrunPublic "+wallRunningPublic);
            //Debug.Log("wallrunning "+ wallRunning);

            Debug.Log(currentWallrunDuration);

            // DEBUG AREA



            // Previous movement code
            Vector3 positionalMovement = transform.position - prevPosition;
            transform.position = prevPosition;
            moveData.origin += positionalMovement;

            // Triggers
            if (numberOfTriggers != triggers.Count)
            {
                numberOfTriggers = triggers.Count;

                underwater = false;
                triggers.RemoveAll(item => item == null);
                foreach (Collider trigger in triggers)
                {

                    if (trigger == null)
                    {
                        continue;
                    }

                    if (trigger.GetComponentInParent<Water>())
                    {
                        underwater = true;
                    }
                }

            }

            _moveData.cameraUnderwater = _cameraWaterCheck.IsUnderwater();
            _cameraWaterCheckObject.transform.position = viewTransform.position;
            moveData.underwater = underwater;

            if (allowCrouch)
            {
                _controller.Crouch(this, _movementConfig, Time.deltaTime);
            }

            _controller.ProcessMovement(this, _movementConfig, Time.deltaTime);

            transform.position = moveData.origin;
            prevPosition = transform.position;

            _colliderObject.transform.rotation = Quaternion.identity;

        }

        private void HandleWallDuration()
        {

            if (maxWallrunDuration != 0)
            {
                currentWallrunDuration -= Time.deltaTime;
            }

            //if (proportionnalWalljump)
            //{
            percentage = Convert.ToInt32(Mathf.Round((maxWallrunDuration - currentWallrunDuration) / maxWallrunDuration * 100));
            //}
        }

        private void ResetCurrentWallrunDuration()
        {
            currentWallrunDuration = maxWallrunDuration;
        }

        private void ResetCurrentWitchTimePreparation()
        {
            currentWitchTimePreparation = witchTime;
        }

        private void ResetLastGrabbedWallFunction()
        {
            GameObject[] grabbedWalls = GameObject.FindGameObjectsWithTag("lastGrabbedWall");
            if (grabbedWalls.Length != 0)
            {
                foreach (GameObject wall in grabbedWalls)
                {
                    wall.tag = "Untagged";
                }
            }

            if (lastTouchedWall != null && !grounded)
            {
                lastTouchedWall.tag = "lastGrabbedWall";
            }
        }

        private void BunnySpeedPunish()
        {
            if (_moveData.velocity.magnitude > _movementConfig.walkSpeed + 2)
            {
                _moveData.velocity = _moveData.velocity.magnitude * 1f * _moveData.velocity.normalized;
            }
        }

        private void WallJump()
        {
            if (redirectionVelocity || redirectionVelocityWithoutZ)
                RedirectVelocity();
            //_moveData.velocity = _moveData.velocity * _moveData.wallJumpForce;

            if (proportionnalWalljump && maxWallrunDuration > 0)
            {

                //Debug.Log(currentWallrunDuration + " " + maxWallrunDuration + " " +percentage);
                if (percentage > 50 && percentage < 75)
                {
                    _moveData.velocity = _moveData.velocity * proportionnalWalljumpBoost1;
                }
                if (percentage > 75)
                {
                    _moveData.velocity = _moveData.velocity * proportionnalWalljumpBoost2;
                }
            }

            ResetLastGrabbedWallFunction();
            ResetCurrentWallrunDuration();


            _moveData.hasDoubleJumpedSinceLastLanding = false;
            _moveData.hasTeleportedSinceLastLanding = false;
        }

        private void DoubleJump()
        {
            Vector3 yTruncatedVel = Vector3.Dot(_moveData.velocity, orientation.forward) * orientation.forward + Vector3.Dot(_moveData.velocity, orientation.right) * orientation.right;
            yTruncatedVel += Vector3.up * doubleJumpForce;
            _moveData.velocity = yTruncatedVel;
        }

        private void WallRunningMovement()
        {
            if (!Input.GetButton("Jump") || !_moveData.playerNearWall)
            {
                _moveData.wallRunning = false;
                return;
            }

            RaycastHit wallHit = _moveData.nearestWallHit;
            Vector3 wallNormal = _moveData.nearestWallHit.normal;
            Vector3 wallHitPoint = _moveData.nearestWallHit.point;
            _moveData.wallDist = _moveData.nearestWallHit.distance;

            if (wallHit.collider.gameObject.tag == "lastGrabbedWall" && !canDoubleWallGrab)
            {
                _moveData.wallRunning = false;
                return;
            }
            if (currentWallrunDuration < 0f)
            {
                _moveData.wallRunning = false;
                return;
            }

            if (lastTouchedWallPublic != wallHit.collider.gameObject)
            {
                lastTouchedWallPublic = wallHit.collider.gameObject;
            }

            if (savedVelocity > 0)
            {
                if (!fixedVelocityOnWallrun)
                {
                    savedVelocity -= speedPenaltyCoef * Time.deltaTime;
                }
                _moveData.velocity = _moveData.velocity.normalized * savedVelocity;
            }
            // Can wallrun

            //OLD ?
            //_moveData.playerNearWall = Physics.Raycast(wallHitPoint, wallNormal, out clippedWall, 1f, whatIsWall);
            _moveData.wallRunning = true;

            Vector3 wallForward = Vector3.Cross(wallNormal, wallHit.transform.up);

            if ((_moveData.velocity - wallForward).magnitude > (_moveData.velocity - -wallForward).magnitude)
            {
                wallForward = -wallForward;
            }
            wallRunSpeed = _moveData.velocity.magnitude;

            _moveData.velocity = wallRunSpeed * wallForward;

            if (wallHit.collider.gameObject.GetComponent<MovingBlock>() != null)
            {
                _moveData.origin += wallHit.collider.gameObject.GetComponent<MovingBlock>().deltaPos;
            }
        }
        private void WallRunningMovementOLD()
        {
            if (Input.GetButton("Jump") && ((_moveData.leftWallHit.distance < 1f && _moveData.leftWallHit.distance != 0) || (_moveData.rightWallHit.distance < 1f && _moveData.rightWallHit.distance != 0) || (_moveData.frontWallHit.distance < 1f && _moveData.frontWallHit.distance != 0) || (_moveData.backWallHit.distance < 1f && _moveData.backWallHit.distance != 0)))
            {
                RaycastHit wallHit = _moveData.rightWallHit;
                Vector3 wallNormal = _moveData.rightWallHit.normal;
                Vector3 wallHitPoint = _moveData.rightWallHit.point;
                _moveData.wallDist = _moveData.rightWallHit.distance;

                if ((_moveData.leftWallHit.distance < 1f && _moveData.leftWallHit.distance != 0))
                {
                    wallHit = _moveData.leftWallHit;
                    wallNormal = _moveData.leftWallHit.normal;
                    wallHitPoint = _moveData.leftWallHit.point;
                    _moveData.wallDist = _moveData.leftWallHit.distance;
                }
                if ((_moveData.frontWallHit.distance < 1f && _moveData.frontWallHit.distance != 0))
                {
                    wallHit = _moveData.frontWallHit;
                    wallNormal = _moveData.frontWallHit.normal;
                    wallHitPoint = _moveData.frontWallHit.point;
                    _moveData.wallDist = _moveData.frontWallHit.distance;
                }
                if ((_moveData.backWallHit.distance < 1f && _moveData.backWallHit.distance != 0))
                {
                    wallHit = _moveData.backWallHit;
                    wallNormal = _moveData.backWallHit.normal;
                    wallHitPoint = _moveData.backWallHit.point;
                    _moveData.wallDist = _moveData.backWallHit.distance;
                }

                if (lastTouchedWallPublic != wallHit.collider.gameObject)
                {
                    lastTouchedWallPublic = wallHit.collider.gameObject;
                }

                if (savedVelocity > 0)
                {
                    if (!fixedVelocityOnWallrun)
                    {
                        savedVelocity -= speedPenaltyCoef * Time.deltaTime;
                    }
                    _moveData.velocity = _moveData.velocity.normalized * savedVelocity;
                }

                if (wallHit.collider.gameObject.tag != "lastGrabbedWall" || canDoubleWallGrab)
                {

                    if (currentWallrunDuration < 0f)
                    {
                        _moveData.wallRunning = false;
                        return;
                    }
                    else
                    {
                        //can wallrun
                        _moveData.playerNearWall = Physics.Raycast(wallHitPoint, wallNormal, out clippedWall, 1f, whatIsWall);
                        _moveData.wallRunning = true;

                        Vector3 wallForward = Vector3.Cross(wallNormal, wallHit.transform.up);

                        if ((_moveData.velocity - wallForward).magnitude > (_moveData.velocity - -wallForward).magnitude)
                        {
                            wallForward = -wallForward;
                        }

                        wallRunSpeed = _moveData.velocity.magnitude;

                        _moveData.velocity = wallRunSpeed * wallForward;
                        _moveData.playerNearWallR = Vector3.Distance(transform.position, _moveData.rightWallHit.point) < 100f;
                        _moveData.playerNearWallL = Vector3.Distance(transform.position, _moveData.leftWallHit.point) < 100f;
                        _moveData.playerNearWallF = Vector3.Distance(transform.position, _moveData.frontWallHit.point) < 100f;
                        _moveData.playerNearWallB = Vector3.Distance(transform.position, _moveData.backWallHit.point) < 100f;

                        if (maxWallrunDuration != 0)
                        {
                            currentWallrunDuration -= Time.deltaTime;
                        }

                        if (proportionnalWalljump)
                        {
                            percentage = Convert.ToInt32(Mathf.Round((maxWallrunDuration - currentWallrunDuration) / maxWallrunDuration * 100));
                        }
                    }
                    if (wallHit.collider.gameObject.GetComponent<MovingBlock>() != null)
                    {
                        _moveData.origin += wallHit.collider.gameObject.GetComponent<MovingBlock>().deltaPos;
                    }
                }
            }
        }

        private bool IsPlayerNearWall(bool[] boolArray)
        {
            for (int i = 0; i < boolArray.Length; i++)
                if (boolArray[i])
                {
                    return true;
                }
            return false;
        }
        private bool SideToTiltTo()
        {
            for (int i = 0; i < _moveData.checkForWallBoolArray.Length; i++)
            {
                if (_moveData.checkForWallBoolArray[i])
                {
                    return i % 2 == 0;
                }
            }
            return false;
        }

        private void CheckForWall()
        {

            Collider[] detectedWalls = Physics.OverlapSphere(transform.position, wallDetectionRadius, LayerMask.GetMask("whatIsWall"));
            if (detectedWalls.Length == 0)
            {
                _moveData.playerNearWall = false;
                return;
            }

            _moveData.playerNearWall = true;

            Vector3[] orientationVectors = new Vector3[] {
            orientation.right,
            -orientation.right,
            orientation.forward + orientation.right,
            orientation.forward - orientation.right,
            -orientation.forward + orientation.right,
            -orientation.forward - orientation.right,
            //orientation.forward,
            //-orientation.forward
            };

            for (int i = 0; i < _moveData.checkForWallBoolArray.Length; i++)
            {
                //if (i == 0)
                //{
                //    Debug.Log("start");
                //}
                bool isThereAWallThere = Physics.Raycast(transform.position, Vector3.ProjectOnPlane(orientationVectors[i], Vector3.up), out _moveData.raycastHitArray[i], _moveData.wallCheckDistance, whatIsWall);
                _moveData.checkForWallBoolArray[i] = isThereAWallThere;
                if (isThereAWallThere)
                {
                    _moveData.nearestWallHit = _moveData.raycastHitArray[i];
                    _moveData.tiltRightOrLeft = i % 2 == 0;
                }
                //if (i % 2 == 0)
                //{

                //    Debug.Log("right side " + _moveData.checkForWallBoolArray[i]);
                //}
                //else
                //{
                //    Debug.Log("left side " + _moveData.checkForWallBoolArray[i]);

                //}
            }
        }
        private void CheckForWallOLD()
        {
            //Facilement factorisable, a voir plus tard
            _moveData.wallRight = Physics.Raycast(transform.position, Vector3.ProjectOnPlane(orientation.right, Vector3.up), out _moveData.rightWallHit, _moveData.wallCheckDistance, whatIsWall);
            _moveData.wallLeft = Physics.Raycast(transform.position, Vector3.ProjectOnPlane(-orientation.right, Vector3.up), out _moveData.leftWallHit, _moveData.wallCheckDistance, whatIsWall);
            _moveData.wallFront = Physics.Raycast(transform.position, Vector3.ProjectOnPlane(orientation.forward, Vector3.up), out _moveData.frontWallHit, _moveData.wallCheckDistance, whatIsWall);
            _moveData.wallBack = Physics.Raycast(transform.position, Vector3.ProjectOnPlane(-orientation.forward, Vector3.up), out _moveData.backWallHit, _moveData.wallCheckDistance, whatIsWall);
        }
        private void checkForWitchTime()
        {
            if (currentWitchTimePreparation < 0)
            {
                BeginWitchTime();
            }
            else
            {
                EndWitchTime();
            }
        }
        private void SimpleCheckGround()
        {
            groundedPublic = Physics.Raycast(transform.position, Vector3.down, _moveData.defaultHeight - 0.000005f, whatIsGround) || Physics.Raycast(transform.position, Vector3.down, _moveData.defaultHeight, whatIsWall);
        }
        private void ResetMovement()
        {
            if (horizontalInput == 0 && verticalInput == 0)
            {
                rb.velocity = Vector3.zero;
            }
        }

        private void ResetDoubleJump()
        {
            _moveData.canDoubleJump = true;
        }

        private void PreventDoubleJump()
        {
            _moveData.canDoubleJump = false;
        }

        private void ToggleTpPreview()
        {
            indicatorMesh.enabled = !indicatorMesh.enabled;
        }

        private void AdjustTpDistance()
        {
            maxTeleportDistance = Mathf.Max(_moveData.velocity.magnitude * 1.5f, baseTeleportDistance);
        }

        private void BeginWitchTime()
        {
            Time.timeScale = 0.05f;
        }
        private void EndWitchTime()
        {
            Time.timeScale = 1f;
        }

        private void TpPreview()
        {
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxTeleportDistance))
            {
                currentIndicator.transform.position = hit.point + hit.normal * 0.5f;
            }
            else
            {
                Vector3 newPos = ray.direction.normalized * maxTeleportDistance;
                currentIndicator.transform.position = transform.position + newPos;
            }

            //Reworked teleport inputs
            if (Input.GetMouseButton(0) && !Input.GetMouseButton(1))
            {
                if (witchTime > 0)
                    currentWitchTimePreparation -= Time.deltaTime;

                indicatorMesh.enabled = true;
            }
            else
            {
                if (witchTime > 0)
                    ResetCurrentWitchTimePreparation();
                indicatorMesh.enabled = false;
            }

            if (Input.GetMouseButtonDown(1) && indicatorMesh.enabled)
            {
                if (witchTime > 0)
                    ResetCurrentWitchTimePreparation();
                indicatorMesh.enabled = false;
            }

            if (Input.GetMouseButtonUp(0) && !Input.GetMouseButton(1))
            {
                if (witchTime > 0)
                    ResetCurrentWitchTimePreparation();
                TpFunction();
            }

            rb.drag = (grounded && horizontalInput == 0 && verticalInput == 0) ? groundDrag : 0;
        }

        private void RedirectVelocity()
        {
            Vector3 prevVel = _moveData.velocity;
            _moveData.velocity = prevVel.magnitude * ray.direction.normalized;
        }

        private void RedirectVelocityWithoutZ()
        {
            Vector3 prevVel = _moveData.velocity;
            Vector3 fragmentedVel = Vector3.Dot(_moveData.velocity, Vector3.forward) * Vector3.forward + Vector3.Dot(_moveData.velocity, Vector3.up) * Vector3.up + Vector3.Dot(_moveData.velocity, Vector3.right) * Vector3.right;

            fragmentedVel.y = 0;

            Vector3 rayWithoutZ = Vector3.Dot(ray.direction, Vector3.forward) * Vector3.forward + Vector3.Dot(ray.direction, Vector3.right) * Vector3.right;

            _moveData.velocity = fragmentedVel.magnitude * rayWithoutZ.normalized;

        }

        private void TpFunction()
        {
            transform.position = currentIndicator.transform.position;

            if (redirectionVelocity)
                RedirectVelocity();

            //Core mech later
            if (redirectionVelocityWithoutZ)
                RedirectVelocityWithoutZ();

            _moveData.hasTeleportedSinceLastLanding = true;

            //Fix inertie au tp?
            rb.AddForce(orientation.transform.forward * rb.velocity.magnitude * 2f, ForceMode.Impulse);

            //ResetDoubleJump();
            //ToggleTpPreview();
        }

        private void UpdateTestBinds()
        {
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                ResetPosition();
            }
        }

        private void ResetPosition()
        {
            moveData.velocity = Vector3.zero;
            moveData.origin = _startPosition;
        }

        private void UpdateMoveData()
        {
            _moveData.verticalAxis = Input.GetAxisRaw("Vertical");
            _moveData.horizontalAxis = Input.GetAxisRaw("Horizontal");

            _moveData.sprinting = Input.GetButton("Sprint");

            if (Input.GetButtonDown("Crouch"))
                _moveData.crouching = true;

            if (!Input.GetButton("Crouch"))
                _moveData.crouching = false;

            bool moveLeft = _moveData.horizontalAxis < 0f;
            bool moveRight = _moveData.horizontalAxis > 0f;
            bool moveFwd = _moveData.verticalAxis > 0f;
            bool moveBack = _moveData.verticalAxis < 0f;
            bool jump = Input.GetButton("Jump");

            if (!moveLeft && !moveRight)
            {
                _moveData.sideMove = 0f;
            }
            else if (moveLeft)
            {
                _moveData.sideMove = -moveConfig.acceleration;
            }
            else if (moveRight)
            {
                _moveData.sideMove = moveConfig.acceleration;
            }

            if (!moveFwd && !moveBack)
            {
                _moveData.forwardMove = 0f;
            }
            else if (moveFwd)
            {
                _moveData.forwardMove = moveConfig.acceleration;
            }
            else if (moveBack)
            {
                _moveData.forwardMove = -moveConfig.acceleration;
            }

            if (Input.GetButtonDown("Jump"))
            {
                _moveData.wishJump = true;
            }

            if (!Input.GetButton("Jump"))
            {
                _moveData.wishJump = false;
            }

            _moveData.viewAngles = _angles;

        }
        private void DisableInput()
        {
            _moveData.verticalAxis = 0f;
            _moveData.horizontalAxis = 0f;
            _moveData.sideMove = 0f;
            _moveData.forwardMove = 0f;
            _moveData.wishJump = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static float ClampAngle(float angle, float from, float to)
        {

            if (angle < 0f)
            {
                angle = 360 + angle;
            }

            if (angle > 180f)
            {
                return Mathf.Max(angle, 360 + from);
            }

            return Mathf.Min(angle, to);

        }
        private void OnTriggerEnter(Collider other)
        {

            if (!triggers.Contains(other))
            {
                triggers.Add(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {

            if (triggers.Contains(other))
            {
                triggers.Remove(other);
            }
        }
        private void OnCollisionStay(Collision collision)
        {
            if (collision.rigidbody == null)
            {
                return;
            }

            Vector3 relativeVelocity = collision.relativeVelocity * collision.rigidbody.mass / 50f;
            Vector3 impactVelocity = new Vector3(relativeVelocity.x * 0.0025f, relativeVelocity.y * 0.00025f, relativeVelocity.z * 0.0025f);

            float maxYVel = Mathf.Max(moveData.velocity.y, 10f);
            Vector3 newVelocity = new Vector3(moveData.velocity.x + impactVelocity.x, Mathf.Clamp(moveData.velocity.y + Mathf.Clamp(impactVelocity.y, -0.5f, 0.5f), -maxYVel, maxYVel), moveData.velocity.z + impactVelocity.z);

            newVelocity = Vector3.ClampMagnitude(newVelocity, Mathf.Max(moveData.velocity.magnitude, 30f));
            moveData.velocity = newVelocity;
        }
    }
}


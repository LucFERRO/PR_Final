using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Fragsurf.Movement
{
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
                }
                if (value && !wasGrounded)
                {
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
                }
            }
        }


        public float doubleJumpForce;
        public int doubleJumpClampMin;

        [Header("Variations")]
        public bool redirectionVelocity;
        public bool redirectionVelocityWithoutZ;
        public bool canDoubleWallGrab;
        public float witchTime;
        public float maxWallrunDuration;
        public bool proportionnalWalljump;
        public float[] proportionnalJumpBoosts;
        public int percentage;
        public float speedPenaltyCoef;
        public float bunnyPenaltyCoef;
        public float wallDetectionRadius;
        public float verticalTweakFloat;
        public float fixGravityFloat;

        [Header("FieldOfView")]
        public float baseFov;
        public float highFov;
        public float maxFov;
        public float fovChangeSpeed;
        public float[] wallrunFovBoosts;

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

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, wallDetectionRadius);
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
            {
                viewTransform = Camera.main.transform;
            }

            if (playerRotationTransform == null && transform.childCount > 0)
            {
                playerRotationTransform = transform.GetChild(0);
            }

            _collider = gameObject.GetComponent<Collider>();

            if (_collider != null)
            {
                GameObject.Destroy(_collider);
            }

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
                ResetCurrentWallrunDuration();
                ResetLastGrabbedWallFunction();

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
            ResetMovement();

            currentSpeed = _moveData.velocity.magnitude;

            if (_moveData.wallRunning)
            {
                //if (maxWallrunDuration > 0)
                //{
                HandleWallDuration();
                //}
                wallRunningPublic = true;
            }
            else
            {
                savedVelocity = 0;
                wallRunningPublic = false;
                percentage = 0;
                UpdateMoveData();
            }

            if ((!grounded && !wallRunning && _moveData.canDoubleJump && !_moveData.hasDoubleJumpedSinceLastLanding && Input.GetButtonDown("Jump")))
            {
                DoubleJump();
                _moveData.hasDoubleJumpedSinceLastLanding = true;
            }

            HandleFieldOfView();

            // Previous movement code
            Vector3 positionalMovement = transform.position - prevPosition;
            transform.position = prevPosition;
            moveData.origin += positionalMovement;

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

        private void HandleFieldOfView()
        {
            float rawFov = Mathf.SmoothDamp(cam.fieldOfView, (highFov - baseFov) * 0.02f * currentSpeed + baseFov, ref fovChangeSpeed, 15f * Time.deltaTime);
            cam.fieldOfView = Mathf.Clamp(rawFov, 90, maxFov);
        }
        private void ResetToBaseFieldOfView()
        {
            if (cam.fieldOfView <= 92)
            {
                cam.fieldOfView = 90;
            }
        }
        private void HandleWallDuration()
        {
            //if (currentWallrunDuration <= 0 && lastTouchedWall != null && !grounded)
            //Debug.Log(currentWallrunDuration);
            if (currentWallrunDuration <= 0.05f)
            {
                Debug.Log("wallduration expanded");
                if (lastTouchedWall != null)
                {
                    Debug.Log("lasttouchedwall not null");

                    //UpdateLastGrabbedWall();
                    lastTouchedWallPublic.tag = "lastGrabbedWall";
                    Debug.Log("OUT OF TIME");
                    wallRunningPublic = false;
                    return;
                }
            }
            {
            }
            currentWallrunDuration -= Time.deltaTime;
            percentage = Convert.ToInt32(Mathf.Round((maxWallrunDuration - currentWallrunDuration) / maxWallrunDuration * 100));
        }
        private void ResetCurrentWallrunDuration()
        {
            currentWallrunDuration = maxWallrunDuration;
        }

        private void ResetCurrentWitchTimePreparation()
        {
            currentWitchTimePreparation = witchTime;
        }
        private void UpdateLastGrabbedWall()
        {

            if (lastTouchedWallPublic != null && !grounded)
            {
                lastTouchedWallPublic.tag = "lastGrabbedWall";
            }
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
        }

        private void BunnySpeedPunish()
        {
            if (_moveData.velocity.magnitude > _movementConfig.walkSpeed + 2)
            {
                float speedCoef = 1 - bunnyPenaltyCoef * 0.01f;
                _moveData.velocity = _moveData.velocity.magnitude * speedCoef * _moveData.velocity.normalized;
            }
        }

        private void WallJump()
        {
            if (redirectionVelocity || redirectionVelocityWithoutZ)
            {
                RedirectVelocity();
            }

            if (proportionnalWalljump && maxWallrunDuration > 0)
            {
                int chosenIndex = 0;
                float boostValue = 0;

                if (percentage > 25 && percentage < 50)
                {
                    chosenIndex = 1;
                }
                if (percentage > 50 && percentage < 85)
                {
                    chosenIndex = 2;
                }
                if (percentage > 85)
                {
                    chosenIndex = 3;
                }
                boostValue = 1 + proportionnalJumpBoosts[chosenIndex] * 0.01f;
                _moveData.velocity = _moveData.velocity * boostValue;
                //cam.fieldOfView = wallrunFovBoosts[chosenIndex];
            }
            ResetCurrentWallrunDuration();
            ResetLastGrabbedWallFunction();
            UpdateLastGrabbedWall();
        }

        private void DoubleJump()
        {
            Vector3 yTruncatedVel = Vector3.Dot(_moveData.velocity, orientation.forward) * orientation.forward + Vector3.Dot(_moveData.velocity, orientation.right) * orientation.right;
            float doubleJumpClampedForce = Mathf.Clamp(currentSpeed, doubleJumpClampMin, 100);
            yTruncatedVel += Vector3.up * doubleJumpForce * 0.01f * doubleJumpClampedForce;
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

            if (wallHit.collider == null)
            {
                return;
            }

            if (wallHit.collider.gameObject.tag == "lastGrabbedWall")
            {
                Debug.Log("lastGrabbedWall");
                _moveData.wallRunning = false;
                return;
            }
            if (currentWallrunDuration < 0f)
            {
                //Debug.Log("no more wallrun duration");
                _moveData.wallRunning = false;
                return;
            }

            if (lastTouchedWallPublic != wallHit.collider.gameObject)
            {
                lastTouchedWallPublic = wallHit.collider.gameObject;
            }

            if (savedVelocity > 0)
            {
                savedVelocity -= speedPenaltyCoef * Time.deltaTime;
                _moveData.velocity = _moveData.velocity.normalized * savedVelocity;

            }
            // Can wallrun

            _moveData.wallRunning = true;
            _moveData.hasDoubleJumpedSinceLastLanding = false;
            _moveData.hasTeleportedSinceLastLanding = false;

            //PROJECT ON PLANE

            Vector3 wallForward = Vector3.Cross(wallNormal, wallHit.transform.up);

            if ((_moveData.velocity - wallForward).magnitude > (_moveData.velocity - -wallForward).magnitude)
            {
                wallForward = -wallForward;
            }
            wallRunSpeed = _moveData.velocity.magnitude;

            _moveData.velocity = wallRunSpeed * wallForward;

            Vector3 fixGravity = fixGravityFloat * Vector3.up * Time.deltaTime;
            _moveData.origin += fixGravity;

            if (wallHit.collider.gameObject.GetComponent<MovingBlock>() != null)
            {
                _moveData.origin += wallHit.collider.gameObject.GetComponent<MovingBlock>().deltaPos;
            }
        }

        private void DebugFunction()
        {
            //Debug.Log("playerNearWall" + _moveData.playerNearWall);
            //RaycastHit wallHit = _moveData.nearestWallHit;
            //Vector3 wallNormal = _moveData.nearestWallHit.normal;
            //Vector3 wallHitPoint = _moveData.nearestWallHit.point;
            //_moveData.wallDist = _moveData.nearestWallHit.distance;

            //Debug.Log(wallHit.collider.bounds);
            if (lastTouchedWallPublic != null)
            {
                //Debug.Log(lastTouchedWallPublic.gameObject.name);
            }
        }
        private void CheckForWall()
        {
            Collider[] detectedWalls = Physics.OverlapSphere(transform.position + Vector3.up, wallDetectionRadius, LayerMask.GetMask("whatIsWall"));
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
                bool isThereAWallThere = Physics.Raycast(transform.position, Vector3.ProjectOnPlane(orientationVectors[i], Vector3.up), out _moveData.raycastHitArray[i], _moveData.wallCheckDistance, whatIsWall);
                _moveData.checkForWallBoolArray[i] = isThereAWallThere;
                if (isThereAWallThere && _moveData.raycastHitArray[i].collider.tag != "lastGrabbedWall")
                {
                    _moveData.nearestWallHit = _moveData.raycastHitArray[i];
                    _moveData.tiltRightOrLeft = i % 2 == 0;
                }
            }
            DebugFunction();
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
            Vector3 tweakedDirection = ray.direction.normalized + verticalTweakFloat * Vector3.up;
            _moveData.velocity = prevVel.magnitude * tweakedDirection.normalized;
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
            {
                RedirectVelocity();
            }

            //Core mech later
            if (redirectionVelocityWithoutZ)
            {
                RedirectVelocityWithoutZ();
            }

            _moveData.hasTeleportedSinceLastLanding = true;
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

            // Disable Z & S while airborne
            //if (!grounded)
            //{
            //    _moveData.verticalAxis = 0;
            //}

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
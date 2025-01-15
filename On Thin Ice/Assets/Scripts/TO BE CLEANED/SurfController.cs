using UnityEngine;
using Fragsurf.TraceUtil;

namespace Fragsurf.Movement
{
    public class SurfController
    {
        [HideInInspector] public Transform playerTransform;
        private ISurfControllable _surfer;
        private MovementConfig _config;
        private float _deltaTime;

        public bool jumping = false;
        public bool crouching = false;
        public float speed = 0f;

        public Transform camera;
        public float cameraYPos = 0f;

        private float slideSpeedCurrent = 0f;
        private Vector3 slideDirection = Vector3.forward;

        //private bool sliding = false;
        private bool wasSliding = false;
        private float slideDelay = 0f;

        private bool uncrouchDown = false;
        private float crouchLerp = 0f;

        private float frictionMult = 1f;

        Vector3 groundNormal = Vector3.up;

        public void ProcessMovement(ISurfControllable surfer, MovementConfig config, float deltaTime)
        {
            _surfer = surfer;
            _config = config;
            _deltaTime = deltaTime;

            if (_surfer.moveData.velocity.y <= 0f)
            {
                jumping = false;
            }

            // apply gravity
            if (_surfer.groundObject == null)
            {
                _surfer.moveData.velocity.y -= (_surfer.moveData.gravityFactor * _config.gravity * _deltaTime);
                _surfer.moveData.velocity.y += _surfer.baseVelocity.y * _deltaTime;
            }

            // input velocity, check for ground
            CheckGrounded();
            CalculateMovementVelocity();

            ////}
            //if (_surfer.moveData.wallRunning)
            //{
            //    // Wallrunning logic
            //    //_surfer.moveData.velocity.x = 0;
            //    _surfer.moveData.velocity.y = 0;

            //} else
            //{
            //    //wallRunningPublic = false;
            //}
            float yVel = _surfer.moveData.velocity.y;
            _surfer.moveData.velocity.y = 0f;
            _surfer.moveData.velocity = Vector3.ClampMagnitude(_surfer.moveData.velocity, _config.maxVelocity);
            speed = _surfer.moveData.velocity.magnitude;
            _surfer.moveData.velocity.y = yVel;

            if (_surfer.moveData.velocity.sqrMagnitude == 0f)
            {
                // Do collisions while standing still
                SurfPhysics.ResolveCollisions(_surfer.collider, ref _surfer.moveData.origin, ref _surfer.moveData.velocity, _surfer.moveData.rigidbodyPushForce, 1f, _surfer.moveData.stepOffset, _surfer);
            }
            else
            {

                float maxDistPerFrame = 0.2f;
                Vector3 velocityThisFrame = _surfer.moveData.velocity * _deltaTime;
                float velocityDistLeft = velocityThisFrame.magnitude;
                float initialVel = velocityDistLeft;
                while (velocityDistLeft > 0f)
                {

                    float amountThisLoop = Mathf.Min(maxDistPerFrame, velocityDistLeft);
                    velocityDistLeft -= amountThisLoop;

                    Vector3 velThisLoop = velocityThisFrame * (amountThisLoop / initialVel);
                    _surfer.moveData.origin += velThisLoop;

                    //Walls collisions
                    SurfPhysics.ResolveCollisions(_surfer.collider, ref _surfer.moveData.origin, ref _surfer.moveData.velocity, _surfer.moveData.rigidbodyPushForce, amountThisLoop / initialVel, _surfer.moveData.stepOffset, _surfer);
                }
            }
            _surfer.moveData.groundedTemp = _surfer.moveData.grounded;
            _surfer = null;
        }

        private void CalculateMovementVelocity()
        {
            switch (_surfer.moveType)
            {
                case MoveType.Walk:

                    if (_surfer.groundObject == null)
                    {
                        /*
                        // AIR MOVEMENT
                        */

                        wasSliding = false;

                        // apply movement from input
                        _surfer.moveData.velocity += AirInputMovement();

                        SurfPhysics.Reflect(ref _surfer.moveData.velocity, _surfer.collider, _surfer.moveData.origin, _deltaTime);
                    }
                    else
                    {
                        /*
                        //  GROUND MOVEMENT
                        */

                        // Sliding
                        if (!wasSliding)
                        {
                            slideDirection = new Vector3(_surfer.moveData.velocity.x, 0f, _surfer.moveData.velocity.z).normalized;
                            slideSpeedCurrent = Mathf.Max(_config.maximumSlideSpeed, new Vector3(_surfer.moveData.velocity.x, 0f, _surfer.moveData.velocity.z).magnitude);
                        }

                        //sliding = false;
                        if (_surfer.moveData.velocity.magnitude > _config.minimumSlideSpeed && _surfer.moveData.slidingEnabled && _surfer.moveData.crouching && slideDelay <= 0f)
                        {

                            if (!wasSliding)
                            {
                                slideSpeedCurrent = Mathf.Clamp(slideSpeedCurrent * _config.slideSpeedMultiplier, _config.minimumSlideSpeed, _config.maximumSlideSpeed);
                            }

                            //sliding = true;
                            wasSliding = true;
                            SlideMovement();
                            return;

                        }
                        else
                        {

                            if (slideDelay > 0f)
                            {
                                slideDelay -= _deltaTime;
                            }

                            if (wasSliding)
                            {
                                slideDelay = _config.slideDelay;
                            }

                            wasSliding = false;
                        }

                        float fric = crouching ? _config.crouchFriction : _config.friction;
                        float accel = crouching ? _config.crouchAcceleration : _config.acceleration;
                        float decel = crouching ? _config.crouchDeceleration : _config.deceleration;

                        // Get movement directions
                        Vector3 forward = Vector3.Cross(groundNormal, -playerTransform.right);
                        Vector3 right = Vector3.Cross(groundNormal, forward);

                        float speed = _surfer.moveData.sprinting ? _config.sprintSpeed : _config.walkSpeed;
                        if (crouching)
                        {
                            speed = _config.crouchSpeed;
                        }

                        Vector3 _wishDir;

                        // Jump and friction
                        if (_surfer.moveData.wishJump)
                        {
                            ApplyFriction(0.0f, true, true);
                            Jump();
                            return;
                        }
                        else
                        {
                            ApplyFriction(1.0f * frictionMult, true, true);
                        }

                        float forwardMove = _surfer.moveData.verticalAxis;
                        float rightMove = _surfer.moveData.horizontalAxis;

                        _wishDir = forwardMove * forward + rightMove * right;
                        _wishDir.Normalize();
                        Vector3 moveDirNorm = _wishDir;

                        Vector3 forwardVelocity = Vector3.Cross(groundNormal, Quaternion.AngleAxis(-90, Vector3.up) * new Vector3(_surfer.moveData.velocity.x, 0f, _surfer.moveData.velocity.z));

                        // Set the target speed of the player
                        float _wishSpeed = _wishDir.magnitude;
                        _wishSpeed *= speed;

                        // Accelerate
                        float yVel = _surfer.moveData.velocity.y;
                        Accelerate(_wishDir, _wishSpeed, accel * Mathf.Min(frictionMult, 1f), false);

                        float maxVelocityMagnitude = _config.maxVelocity;
                        _surfer.moveData.velocity = Vector3.ClampMagnitude(new Vector3(_surfer.moveData.velocity.x, 0f, _surfer.moveData.velocity.z), maxVelocityMagnitude);
                        _surfer.moveData.velocity.y = yVel;

                        // Calculate how much slopes should affect movement
                        float yVelocityNew = forwardVelocity.normalized.y * new Vector3(_surfer.moveData.velocity.x, 0f, _surfer.moveData.velocity.z).magnitude;

                        // Apply the Y-movement from slopes
                        _surfer.moveData.velocity.y = yVelocityNew * (_wishDir.y < 0f ? 1.2f : 1.0f);
                        float removableYVelocity = _surfer.moveData.velocity.y - yVelocityNew;
                    }
                    break;
            }
        }

        private void LadderCheck(Vector3 colliderScale, Vector3 direction)
        {
            if (_surfer.moveData.velocity.sqrMagnitude <= 0f)
            {
                return;
            }

            bool foundLadder = false;

            RaycastHit[] hits = Physics.BoxCastAll(_surfer.moveData.origin, Vector3.Scale(_surfer.collider.bounds.size * 0.5f, colliderScale), Vector3.Scale(direction, new Vector3(1f, 0f, 1f)), Quaternion.identity, direction.magnitude, SurfPhysics.groundLayerMask, QueryTriggerInteraction.Collide);
            foreach (RaycastHit hit in hits)
            {
                Ladder ladder = hit.transform.GetComponentInParent<Ladder>();
                if (ladder != null)
                {
                    bool allowClimb = true;
                    float ladderAngle = Vector3.Angle(Vector3.up, hit.normal);
                    if (_surfer.moveData.angledLaddersEnabled)
                    {
                        if (hit.normal.y < 0f)
                        {
                            allowClimb = false;
                        }
                        else
                        {
                            if (ladderAngle <= _surfer.moveData.slopeLimit)
                            {
                                allowClimb = false;
                            }
                        }
                    }
                    else if (hit.normal.y != 0f)
                        allowClimb = false;

                    if (allowClimb)
                    {
                        foundLadder = true;
                        if (_surfer.moveData.climbingLadder == false)
                        {
                            _surfer.moveData.climbingLadder = true;
                            _surfer.moveData.ladderNormal = hit.normal;
                            _surfer.moveData.ladderDirection = -hit.normal * direction.magnitude * 2f;

                            if (_surfer.moveData.angledLaddersEnabled)
                            {
                                Vector3 sideDir = hit.normal;
                                sideDir.y = 0f;
                                sideDir = Quaternion.AngleAxis(-90f, Vector3.up) * sideDir;

                                _surfer.moveData.ladderClimbDir = Quaternion.AngleAxis(90f, sideDir) * hit.normal;
                                _surfer.moveData.ladderClimbDir *= 1f / _surfer.moveData.ladderClimbDir.y; // Make sure Y is always 1
                            }
                            else
                            {
                                _surfer.moveData.ladderClimbDir = Vector3.up;
                            }
                        }
                    }
                }
            }

            if (!foundLadder)
            {
                _surfer.moveData.ladderNormal = Vector3.zero;
                _surfer.moveData.ladderVelocity = Vector3.zero;
                _surfer.moveData.climbingLadder = false;
                _surfer.moveData.ladderClimbDir = Vector3.up;
            }
        }

        private void Accelerate(Vector3 wishDir, float wishSpeed, float acceleration, bool yMovement)
        {
            // Initialise variables
            float _addSpeed;
            float _accelerationSpeed;
            float _currentSpeed;

            _currentSpeed = Vector3.Dot(_surfer.moveData.velocity, wishDir);
            _addSpeed = wishSpeed - _currentSpeed;

            if (_addSpeed <= 0)
            {
                return;
            }

            _accelerationSpeed = Mathf.Min(acceleration * _deltaTime * wishSpeed, _addSpeed);

            // Add the velocity.
            _surfer.moveData.velocity.x += _accelerationSpeed * wishDir.x;
            if (yMovement)
            {
                _surfer.moveData.velocity.y += _accelerationSpeed * wishDir.y;
            }
            _surfer.moveData.velocity.z += _accelerationSpeed * wishDir.z;
        }

        private void ApplyFriction(float t, bool yAffected, bool grounded)
        {
            // Initialise variables
            Vector3 _vel = _surfer.moveData.velocity;
            float _speed;
            float _newSpeed;
            float _control;
            float _drop;

            //QUICK FIX Set Y to 0, set speed to the magnitude of movement and drop to 0
            _vel.y = 0.0f;
            _speed = _vel.magnitude;
            _drop = 0.0f;

            float fric = crouching ? _config.crouchFriction : _config.friction;
            float accel = crouching ? _config.crouchAcceleration : _config.acceleration;
            float decel = crouching ? _config.crouchDeceleration : _config.deceleration;

            // Only apply friction if the player is grounded
            if (grounded)
            {
                _vel.y = _surfer.moveData.velocity.y;
                _control = _speed < decel ? decel : _speed;
                _drop = _control * fric * _deltaTime * t;
            }

            _newSpeed = Mathf.Max(_speed - _drop, 0f);
            if (_speed > 0.0f)
            {
                _newSpeed /= _speed;
            }

            // Set the end-velocity
            _surfer.moveData.velocity.x *= _newSpeed;
            if (yAffected == true) { _surfer.moveData.velocity.y *= _newSpeed; }
            _surfer.moveData.velocity.z *= _newSpeed;

        }
        private Vector3 AirInputMovement()
        {
            Vector3 wishVel, wishDir;
            float wishSpeed;
            GetWishValues(out wishVel, out wishDir, out wishSpeed);

            if (_config.clampAirSpeed && (wishSpeed != 0f && (wishSpeed > _config.maxSpeed)))
            {
                wishVel = wishVel * (_config.maxSpeed / wishSpeed);
                wishSpeed = _config.maxSpeed;
            }
            return SurfPhysics.AirAccelerate(_surfer.moveData.velocity, wishDir, wishSpeed, _config.airAcceleration, _config.airCap, _deltaTime);
        }
        private void GetWishValues(out Vector3 wishVel, out Vector3 wishDir, out float wishSpeed)
        {
            wishVel = Vector3.zero;
            wishDir = Vector3.zero;
            wishSpeed = 0f;

            Vector3 forward = _surfer.forward, right = _surfer.right;

            forward[1] = 0;
            right[1] = 0;
            forward.Normalize();
            right.Normalize();

            for (int i = 0; i < 3; i++)
            {
                wishVel[i] = forward[i] * _surfer.moveData.forwardMove + right[i] * _surfer.moveData.sideMove;
            }

            wishVel[1] = 0;
            wishSpeed = wishVel.magnitude;
            wishDir = wishVel.normalized;
        }

        private void Jump()
        {
            if (!_config.autoBhop)
            {
                _surfer.moveData.wishJump = false;
            }

            _surfer.moveData.velocity.y += _config.jumpForce;
            jumping = true;
        }
        private bool CheckGrounded()
        {
            _surfer.moveData.surfaceFriction = 1f;
            var movingUp = _surfer.moveData.velocity.y > 0f;
            var trace = TraceToFloor();
            float groundSteepness = Vector3.Angle(Vector3.up, trace.planeNormal);

            if (trace.hitCollider == null || groundSteepness > _config.slopeLimit || (jumping && _surfer.moveData.velocity.y > 0f))
            {
                SetGround(null);
                if (movingUp && _surfer.moveType != MoveType.Noclip)
                {
                    _surfer.moveData.surfaceFriction = _config.airFriction;
                }

                return false;
            }
            else
            {
                groundNormal = trace.planeNormal;
                SetGround(trace.hitCollider.gameObject);
                return true;
            }
        }
        private void SetGround(GameObject obj)
        {
            if (obj != null)
            {
                _surfer.groundObject = obj;
                _surfer.moveData.velocity.y = 0;
            }
            else
            {
                _surfer.groundObject = null;
            }
        }

        private Trace TraceBounds(Vector3 start, Vector3 end, int layerMask)
        {
            return Tracer.TraceCollider(_surfer.collider, start, end, layerMask);
        }
        private Trace TraceToFloor()
        {
            Vector3 down = _surfer.moveData.origin;
            down.y -= 0.15f;
            return Tracer.TraceCollider(_surfer.collider, _surfer.moveData.origin, down, SurfPhysics.groundLayerMask);

        }
        public void Crouch(ISurfControllable surfer, MovementConfig config, float deltaTime)
        {
            _surfer = surfer;
            _config = config;
            _deltaTime = deltaTime;

            if (_surfer == null)
            {
                return;
            }

            if (_surfer.collider == null)
            {
                return;
            }

            bool grounded = _surfer.groundObject != null;
            bool wantsToCrouch = _surfer.moveData.crouching;
            float crouchingHeight = Mathf.Clamp(_surfer.moveData.crouchingHeight, 0.01f, 1f);
            float heightDifference = _surfer.moveData.defaultHeight - _surfer.moveData.defaultHeight * crouchingHeight;

            if (grounded)
            {
                uncrouchDown = false;
            }
            // Crouching input
            if (grounded)
            {
                crouchLerp = Mathf.Lerp(crouchLerp, wantsToCrouch ? 1f : 0f, _deltaTime * _surfer.moveData.crouchingSpeed);
            }
            else if (!grounded && !wantsToCrouch && crouchLerp < 0.95f)
            {
                crouchLerp = 0f;
            }
            else if (!grounded && wantsToCrouch)
            {
                crouchLerp = 1f;
            }

            // Collider and position changing
            if (crouchLerp > 0.9f && !crouching)
            {
                // Begin crouching
                crouching = true;
                if (_surfer.collider.GetType() == typeof(BoxCollider))
                {
                    // Box collider
                    BoxCollider boxCollider = (BoxCollider)_surfer.collider;
                    boxCollider.size = new Vector3(boxCollider.size.x, _surfer.moveData.defaultHeight * crouchingHeight, boxCollider.size.z);
                }
                else if (_surfer.collider.GetType() == typeof(CapsuleCollider))
                {
                    // Capsule collider
                    CapsuleCollider capsuleCollider = (CapsuleCollider)_surfer.collider;
                    capsuleCollider.height = _surfer.moveData.defaultHeight * crouchingHeight;
                }

                // Move position and stuff
                _surfer.moveData.origin += heightDifference / 2 * (grounded ? Vector3.down : Vector3.up);
                foreach (Transform child in playerTransform)
                {
                    if (child == _surfer.moveData.viewTransform)
                    {
                        continue;
                    }
                    child.localPosition = new Vector3(child.localPosition.x, child.localPosition.y * crouchingHeight, child.localPosition.z);
                }
                uncrouchDown = !grounded;
            }
            else if (crouching)
            {
                // Check if the player can uncrouch
                bool canUncrouch = true;
                if (_surfer.collider.GetType() == typeof(BoxCollider))
                {
                    // Box collider
                    BoxCollider boxCollider = (BoxCollider)_surfer.collider;
                    Vector3 halfExtents = boxCollider.size * 0.5f;
                    Vector3 startPos = boxCollider.transform.position;
                    Vector3 endPos = boxCollider.transform.position + (uncrouchDown ? Vector3.down : Vector3.up) * heightDifference;
                    Trace trace = Tracer.TraceBox(startPos, endPos, halfExtents, boxCollider.contactOffset, SurfPhysics.groundLayerMask);

                    if (trace.hitCollider != null)
                    {
                        canUncrouch = false;
                    }
                }
                else if (_surfer.collider.GetType() == typeof(CapsuleCollider))
                {
                    // Capsule collider
                    CapsuleCollider capsuleCollider = (CapsuleCollider)_surfer.collider;
                    Vector3 point1 = capsuleCollider.center + Vector3.up * capsuleCollider.height * 0.5f;
                    Vector3 point2 = capsuleCollider.center + Vector3.down * capsuleCollider.height * 0.5f;
                    Vector3 startPos = capsuleCollider.transform.position;
                    Vector3 endPos = capsuleCollider.transform.position + (uncrouchDown ? Vector3.down : Vector3.up) * heightDifference;
                    Trace trace = Tracer.TraceCapsule(point1, point2, capsuleCollider.radius, startPos, endPos, capsuleCollider.contactOffset, SurfPhysics.groundLayerMask);

                    if (trace.hitCollider != null)
                    {
                        canUncrouch = false;
                    }
                }
                // Uncrouch
                if (canUncrouch && crouchLerp <= 0.9f)
                {
                    crouching = false;
                    if (_surfer.collider.GetType() == typeof(BoxCollider))
                    {
                        // Box collider
                        BoxCollider boxCollider = (BoxCollider)_surfer.collider;
                        boxCollider.size = new Vector3(boxCollider.size.x, _surfer.moveData.defaultHeight, boxCollider.size.z);
                    }
                    else if (_surfer.collider.GetType() == typeof(CapsuleCollider))
                    {
                        // Capsule collider
                        CapsuleCollider capsuleCollider = (CapsuleCollider)_surfer.collider;
                        capsuleCollider.height = _surfer.moveData.defaultHeight;
                    }

                    // Move position and stuff
                    _surfer.moveData.origin += heightDifference / 2 * (uncrouchDown ? Vector3.down : Vector3.up);
                    foreach (Transform child in playerTransform)
                    {
                        child.localPosition = new Vector3(child.localPosition.x, child.localPosition.y / crouchingHeight, child.localPosition.z);
                    }
                }
                if (!canUncrouch)
                {
                    crouchLerp = 1f;
                }
            }
        }
        void SlideMovement()
        {
            // Gradually change direction
            slideDirection += new Vector3(groundNormal.x, 0f, groundNormal.z) * slideSpeedCurrent * _deltaTime;
            slideDirection = slideDirection.normalized;

            // Set direction
            Vector3 slideForward = Vector3.Cross(groundNormal, Quaternion.AngleAxis(-90, Vector3.up) * slideDirection);

            // Set the velocity
            slideSpeedCurrent -= _config.slideFriction * _deltaTime;
            slideSpeedCurrent = Mathf.Clamp(slideSpeedCurrent, 0f, _config.maximumSlideSpeed);
            slideSpeedCurrent -= (slideForward * slideSpeedCurrent).y * _deltaTime * _config.downhillSlideSpeedMultiplier; // Accelerate downhill (-y = downward, - * - = +)
            _surfer.moveData.velocity = slideForward * slideSpeedCurrent;

            // Jump
            if (_surfer.moveData.wishJump && slideSpeedCurrent < _config.minimumSlideSpeed * _config.slideSpeedMultiplier)
            {
                Jump();
                return;
            }
        }
    }
}
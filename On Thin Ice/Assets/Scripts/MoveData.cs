﻿using System.Linq;
using UnityEngine;

namespace Fragsurf.Movement {

    public enum MoveType {
        None,
        Walk
    }

    public class MoveData {
        
        public Transform playerTransform;
        public Transform viewTransform;
        public Vector3 viewTransformDefaultLocalPos;
        
        public Vector3 origin;
        public Vector3 viewAngles;
        public Vector3 velocity;

        public float forwardMove;
        public float sideMove;
        public float upMove;
        public float surfaceFriction = 1f;
        public float gravityFactor = 1f;
        public float walkFactor = 1f;
        public float verticalAxis = 0f;
        public float horizontalAxis = 0f;
        public bool wishJump = false;
        public bool crouching = false;
        public bool sprinting = false;
        public float slopeLimit = 45f;
        public float rigidbodyPushForce = 1f;

        //Wallrun
        public bool wallRunning;
        public float wallCheckDistance = 3f;
        public float minJumpHeight;
        public RaycastHit rightWallHit;
        public RaycastHit leftWallHit;        
        public RaycastHit frontWallHit;
        public RaycastHit backWallHit;
        public bool wallRight;
        public bool wallLeft;        
        public bool wallFront;
        public bool wallBack;

        // Check for wall facto
        public RaycastHit[] raycastHitArray = Enumerable.Repeat<RaycastHit>(new RaycastHit(), 6).ToArray();
        public bool[] checkForWallBoolArray = Enumerable.Repeat<bool>(false,6).ToArray();
        public RaycastHit nearestWallHit;
        public bool tiltRightOrLeft;

        public bool playerNearWall;

        public bool playerNearWallR;
        public bool playerNearWallL;        
        public bool playerNearWallF;
        public bool playerNearWallB;
        public float wallDist;

        public bool canDoubleJump;
        public bool hasDoubleJumpedSinceLastLanding;
        public bool hasTeleportedSinceLastLanding;
        public float wallJumpForce = 20f;

        public float defaultHeight = 2f;
        public float crouchingHeight = 1f;
        public float crouchingSpeed = 10f;
        public bool toggleCrouch = false;

        public bool slidingEnabled = false;
        public bool laddersEnabled = false;
        public bool angledLaddersEnabled = false;
        
        public bool climbingLadder = false;
        public Vector3 ladderNormal = Vector3.zero;
        public Vector3 ladderDirection = Vector3.forward;
        public Vector3 ladderClimbDir = Vector3.up;
        public Vector3 ladderVelocity = Vector3.zero;

        public bool underwater = false;
        public bool cameraUnderwater = false;

        public bool grounded = false;
        public bool groundedTemp = false;
        public float fallingVelocity = 0f;

        public bool useStepOffset = false;
        public float stepOffset = 0f; 
    }
}
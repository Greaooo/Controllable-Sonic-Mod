using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Mod;
using Sonic;
using CharApi;
using UnityEngine.Events;

namespace Tails
{
    public class TailsController : MonoBehaviour
    {
        #region Components.
        public Rigidbody2D rb { get; private set; }
        public SpriteRenderer rend { get; private set; }

        // Movement properties
        public float groundSpeed { get; private set; }
        public float acceleration { get; set; } = 15;

        public float decceleration { get; set; } = 30;
        public float friction { get; set; } = 45;
        public float topSpeed { get; set; } = 30;
        public float jumpForce { get; private set; } = 8f;
        public float flyForce { get; private set; } = 75f;
        public float spinDashForce { get; private set; } = 0;

        public float dampAmount = 0.9f;

        private float canMoveCooldown = 0;

        private float crouchTime = 1;

        private float xAxis;
        private float yAxis;

        private float jumpCooldown;

        public CharacterType characterType { get; private set; }

        public bool canMove { get; private set; } = true;

        private TailsAnimationController animator;

        #endregion

        // State management
        public MovementState movementState { get; private set; }

        //Theres definitely some unnecessary voids in this script that i should get rid of.

        void Awake()
        {
            
        }

        void Start()
        {
            var changeSpeedButton = new ContextMenuButton(() => true, "Change max speed",
                "Change Max Speed",
                "Changes the maximum speed tails can move.",
                new UnityEngine.Events.UnityAction[1]
                {
                    () =>
                    {
                        Utils.OpenFloatInputDialog(topSpeed, this, (sonic, input) => sonic.SetMaxSpeed(input), "Set New Max Speed", "Input Here:");
                    }
                });

            var changeFlyForce = new ContextMenuButton(() => true, "Change fly force",
                "Change Flying Force",
                "Changes the amount of force that flying uses.",
                new UnityEngine.Events.UnityAction[1]
                {
                    () =>
                    {
                        Utils.OpenFloatInputDialog(topSpeed, this, (sonic, input) => sonic.SetFlyForce(input), "Set New Max Speed", "Input Here:");
                    }
                });

            var phys = GetComponent<PhysicalBehaviour>();

            phys.ContextMenuOptions.Buttons.Add(changeSpeedButton);
            phys.ContextMenuOptions.Buttons.Add(changeFlyForce);

            rb = GetComponent<Rigidbody2D>();
            rb.useAutoMass = false;
            rb.mass = 3;

            animator = GetComponent<TailsAnimationController>();

            rb.sharedMaterial = new PhysicsMaterial2D()
            {
                friction = 0f,
                bounciness = 0
            };
        }

        void FixedUpdate()
        {
            if (Global.main.Paused) { return; }
            HandleGroundAcceleration();
            ApplyMovement();

            if (movementState == MovementState.FLYING)
            {
                Fly();
            }
        }

        void ApplyMovement()
        {
            rb.velocity = new Vector2(groundSpeed, rb.velocity.y);
        }

        // HAVE to clean this up sometime soon because this all is so messy
        void Update()
        {
            animator.SetAnimationState(movementState);

            if (Global.main.Paused) { return; }

            GetComponent<PhysicalBehaviour>().Temperature = 36.67f;
            GeneralMovement();
            GetInputAxis();
            HandleMovementState();

            if (CanJump() && Input.GetKeyDown(KeyCode.W))
            {
                Jump();
            }

            if (Input.GetKeyDown(KeyCode.W) && !CanJump())
            {
                SwitchMovementState(MovementState.FLYING);
            }

            if(movementState != MovementState.FLYING)
            {
                rb.gravityScale = 1;
            }

            canMoveCooldown -= Time.deltaTime;

            if (movementState == MovementState.CROUCHING)
            {
                Crouching();
                groundSpeed = Mathf.Lerp(groundSpeed, 0, Time.deltaTime);
            }
            else
            {
                crouchTime = 1;
            }

            if (movementState == MovementState.SPINNING)
            {
                Spinning();
            }

            if (movementState == MovementState.ROLLING && xAxis != 0)
            {
                canMoveCooldown -= Time.deltaTime;
            }

            if (canMoveCooldown <= 0 && movementState == MovementState.ROLLING)
            {
                canMove = true;
                SwitchMovementState(MovementState.WALKING);
            }
        }

        #region Movement Functions

        void Fly()
        {
            rb.gravityScale = 0.4f;
            rb.AddForce(Vector2.up * yAxis * flyForce);
        }

        void GeneralMovement()
        {
            if (!CanJump())
            {
                canMove = true;
            }

            if (!canMove) { return; }

            if (movementState == MovementState.SWITCHING)
            {
                if (groundSpeed < 1f && groundSpeed > -1f) { SwitchMovementState(MovementState.IDLE); }
            }

            if (CanJump())
            {
                rb.sharedMaterial.friction = 0.8f;

                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 5);
                if (hit && movementState != MovementState.JUMPING)
                {
                    rb.AddForce(Vector2.down * 35);
                }
            }
            else
            {
                rb.sharedMaterial.friction = 0.0f;
            }
        }

        void HandleGroundAcceleration()
        {
            if (!canMove) { return; }

            //if pressing left
            if (xAxis < 0)
            {
                if (groundSpeed > 0)
                {
                    groundSpeed -= decceleration * Time.fixedDeltaTime;
                }
                else if (groundSpeed > -topSpeed)
                {
                    groundSpeed -= acceleration * Time.fixedDeltaTime;
                }
            }

            //if pressing right
            if (xAxis > 0)
            {
                if (groundSpeed < 0)
                {
                    groundSpeed += decceleration * Time.fixedDeltaTime;
                }
                else if (groundSpeed < topSpeed)
                {
                    groundSpeed += acceleration * Time.fixedDeltaTime;
                }
            }

            if (xAxis == 0)
            {
                groundSpeed *= friction * Time.fixedDeltaTime;
                if (Mathf.Abs(groundSpeed) < 0.2f)
                {
                    groundSpeed = 0;
                }
            }
        }

        void Crouching()
        {

            crouchTime -= Time.deltaTime;

            canMove = false;

            canMoveCooldown = 0.1f;

            if (Input.GetKeyUp(KeyCode.LeftShift) && movementState == MovementState.CROUCHING)
            {
                canMove = true;
            }

            if (crouchTime <= 0)
            {
                Spinning();
            }

        }

        void Spinning()
        {
            SwitchMovementState(MovementState.SPINNING);

            spinDashForce += Time.deltaTime * 15;

            if (spinDashForce > topSpeed)
            {
                spinDashForce = topSpeed;
            }

            groundSpeed = Mathf.Lerp(groundSpeed, 0, Time.deltaTime * 3);

            canMoveCooldown = 1 + Time.deltaTime * 3;

            canMoveCooldown = Mathf.Clamp(canMoveCooldown, 1, 4);

            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                SpinDash();
            }
        }

        void SpinDash()
        {
            SwitchMovementState(MovementState.ROLLING);

            GroundSpeedBurst();

            spinDashForce = 0;
        }

        void GroundSpeedBurst()
        {
            AddForce((int)Mathf.Sign(transform.localScale.x), spinDashForce);
        }

        void GetInputAxis()
        {
            if (!canMove) { xAxis = 0; return; }

            xAxis = Input.GetAxisRaw("Horizontal");

            if (Input.GetKey(KeyCode.S))
            {
                yAxis = -1;
            }

            if (Input.GetKey(KeyCode.W))
            {
                yAxis = 1;
            }

            if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S))
            {
                yAxis = 0;
            }

            UpdateCharacterDirection();
        }

        void UpdateCharacterDirection()
        {
            if (IsSwitching()) { return; }

            if (xAxis < 0)
            {
                transform.localScale = new Vector3(-1.3f, 1.3f, 0);
            }
            else if (xAxis > 0)
            {
                transform.localScale = new Vector3(1.3f, 1.3f, 0);
            }
        }

        public void Jump()
        {
            SwitchMovementState(MovementState.JUMPING);

            jumpCooldown = 0.3f;

            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        public void AddForce(int axis, float force)
        {
            groundSpeed += force * axis;
        }

        public bool CanJump()
        {
            return Physics2D.Raycast(transform.position, -Vector2.up, (transform.localScale.y / 2) + 0.2f);
        }

        bool IsSwitching()
        {
            return (xAxis > 0 && groundSpeed < 0 && Mathf.Abs(groundSpeed) >= 1) || (xAxis < 0 && groundSpeed > 0 && Mathf.Abs(groundSpeed) <= 1);
        }

        #endregion

        #region State Management

        void HandleMovementState()
        {
            jumpCooldown -= Time.deltaTime;

            if (movementState == MovementState.ROLLING) { if (canMoveCooldown <= 0) { canMove = true; } return; }

            if (!canMove) { return; }

            if (jumpCooldown > 0 || movementState == MovementState.SWITCHING) { return; }

            if (CanJump() && xAxis == 0)
            {
                SwitchMovementState(MovementState.IDLE);
            }

            if (CanJump() && xAxis != 0)
            {
                if (groundSpeed >= 30 || groundSpeed <= -30)
                {
                    SwitchMovementState(MovementState.RUNNING);
                }
                else
                {
                    SwitchMovementState(MovementState.WALKING);
                }
            }

            if (IsSwitching() && CanJump())
            {
                SwitchMovementState(MovementState.SWITCHING);
            }

            if (Input.GetKey(KeyCode.LeftShift) && CanJump())
            {
                SwitchMovementState(MovementState.CROUCHING);
                canMove = false;
            }
        }

        void SwitchMovementState(MovementState newState)
        {
            movementState = newState;
        }

        #endregion

        public void SetSpriteRenderer(SpriteRenderer sr)
        {
            rend = sr;
        }

        public void SetMaxSpeed(float newSpeed)
        {
            topSpeed = newSpeed;
            ModAPI.Notify("Changed sonic's max speed to " + newSpeed);
        }

        public void SetFlyForce(float force)
        {
            flyForce = force;
        }

        public void SetCharacterType(CharacterType type)
        {
            characterType = type;
        }

        void OnCollisionEnter2D(Collision2D coll)
        {
            coll.transform.TryGetComponent<LimbBehaviour>(out LimbBehaviour limb);

            if (limb && movementState == MovementState.ROLLING && limb.IsAndroid == false)
            {
                limb.Crush();
            }else
            {
                coll.transform.GetComponent<Rigidbody2D>().AddForce((coll.transform.position - transform.position)*5, ForceMode2D.Impulse);
            }
        }
    }

    public class TailsAnimationController : MonoBehaviour
    {
        private TailsController controller;
        private SpriteRenderer rend;
        private Sprite curSprite;

        private MovementState animState;
        public IAnimation currentAnimation { get; private set; }
        public IAnimation wantedAnimation { get; private set; }

        public AnimationSprites animSprites { get; private set; }
        public event Action<IAnimation> animationChanged;

        private TailsWalk _walkAnimation;
        private TailsIdle _idleAnimation;
        private TailsRun _runAnimation;
        private TailsSkid _skidAnimation;
        private TailsJump _jumpAnimation;
        private TailsSpinDash _spinDashAnimation;
        private TailsCrouch _crouchAnimation;

        void Start()
        {
            InitAnimations();
        }

        public void InitAnimations()
        {
            _walkAnimation = new TailsWalk(this);
            _idleAnimation = new TailsIdle(this);
            _runAnimation = new TailsRun(this);
            _skidAnimation = new TailsSkid(this);
            _crouchAnimation = new TailsCrouch(this);
            _jumpAnimation = new TailsJump(this);
            _spinDashAnimation = new TailsSpinDash(this);

            currentAnimation = _idleAnimation;
            currentAnimation.Enter();
        }

        public void SetController(TailsController script)
        {
            controller = script;
            rend = controller.rend;
        }

        void Update()
        {
            UpdateAnimation();
        }

        private void UpdateAnimation()
        {
            HandleAnimationState();

            currentAnimation?.Update();

            if (currentAnimation != wantedAnimation)
            {
                currentAnimation.Exit();

                currentAnimation = wantedAnimation;

                currentAnimation.Enter();
            }
        }

        public void SetAnimationState(MovementState newState)
        {
            animState = newState;
        }

        private void HandleAnimationState()
        {
            switch (animState)
            {
                case MovementState.SWITCHING:
                    wantedAnimation = _skidAnimation;
                    break;

                case MovementState.IDLE:
                    wantedAnimation = _idleAnimation;
                    break;

                case MovementState.WALKING:
                    wantedAnimation = _walkAnimation;
                    break;

                case MovementState.RUNNING:
                    wantedAnimation = _runAnimation;
                    break;

                case MovementState.CROUCHING:
                    wantedAnimation = _crouchAnimation;
                    break;

                case MovementState.JUMPING:
                    wantedAnimation = _jumpAnimation;
                    break;

                case MovementState.SPINNING:
                    wantedAnimation = _spinDashAnimation;
                    break;

                case MovementState.ROLLING:
                    wantedAnimation = _jumpAnimation;
                    break;
            }
        }

        public void SetCurSprite(Sprite sprite)
        {
            curSprite = sprite;
            rend.sprite = curSprite;

            GetComponent<PhysicalBehaviour>().RefreshOutline();
        }

        public void SetAnimationSprites(AnimationSprites newSprites)
        {
            animSprites = newSprites;
        }

        public AnimationSprites GetAnimationSprites()
        {
            return animSprites;
        }

        public TailsController GetController()
        {
            return controller;
        }
    }

    #region animations

    public class TailsIdle : IAnimation
    {
        TailsAnimationController animator;

        Sprite[] idleSprites;

        float frameLength = .3f;
        int currentFrame;
        int maxFrames = 3;

        public TailsIdle(TailsAnimationController anim)
        {
            animator = anim;
        }

        public void Enter()
        {
            idleSprites = animator.GetAnimationSprites().idleSprites;
        }

        public void Update()
        {
            frameLength -= Time.deltaTime;

            if (frameLength <= 0)
            {
                currentFrame++;
                frameLength = .4f;
            }

            if (currentFrame > idleSprites.Length - 1)
            {
                currentFrame = 0;
            }


            animator.SetCurSprite(idleSprites[currentFrame]);
        }

        public void Exit()
        {

        }
    }

    public class TailsCrouch : IAnimation
    {
        TailsAnimationController animator;

        Sprite[] crouchSprites;

        float frameLength = 0.05f;
        int currentFrame = 0;
        int maxFrames = 1;

        public TailsCrouch(TailsAnimationController anim)
        {
            animator = anim;
        }

        public void Enter()
        {
            crouchSprites = animator.GetAnimationSprites().crouchSprites;
        }

        public void Update()
        {
            animator.SetCurSprite(crouchSprites[0]);
        }

        public void Exit()
        {
            currentFrame = 0;
            frameLength = 0.05f;
        }
    }

    public class TailsWalk : IAnimation
    {
        TailsAnimationController animator;

        Sprite[] walkSprites;

        float frameLength;
        int currentFrame;

        public TailsWalk(TailsAnimationController anim)
        {
            animator = anim;
        }

        public void Enter()
        {
            walkSprites = animator.GetAnimationSprites().walkSprites;
        }

        public void Update()
        {
            frameLength -= Time.deltaTime * Mathf.Abs(animator.GetController().rb.velocity.x);

            if (frameLength <= 0)
            {
                currentFrame++;
                frameLength = 1;
            }

            if (currentFrame > walkSprites.Length - 1)
            {
                currentFrame = 0;
            }

            animator.SetCurSprite(walkSprites[currentFrame]);
        }

        public void Exit()
        {

        }
    }

    public class TailsRun : IAnimation
    {
        TailsAnimationController animator;

        Sprite[] runSprites;

        float frameLength;
        int currentFrame;

        public TailsRun(TailsAnimationController anim)
        {
            animator = anim;
        }

        public void Enter()
        {
            runSprites = animator.GetAnimationSprites().runSprites;
        }

        public void Update()
        {
            frameLength -= Time.deltaTime;

            if (frameLength <= 0)
            {
                currentFrame++;
                frameLength = .3f;
            }

            if (currentFrame > runSprites.Length-1)
            {
                currentFrame = 0;
            }

            animator.SetCurSprite(runSprites[currentFrame]);
        }

        public void Exit()
        {

        }
    }

    public class TailsSpinDash : IAnimation
    {
        TailsAnimationController animator;

        Sprite[] spinSprites;

        float frameLength;
        int currentFrame;

        public TailsSpinDash(TailsAnimationController anim)
        {
            animator = anim;
        }

        public void Enter()
        {
            spinSprites = animator.GetAnimationSprites().spinSprites;
        }

        public void Update()
        {
            frameLength -= animator.GetController().spinDashForce*Time.deltaTime;

            if (frameLength <= 0)
            {
                currentFrame++;
                frameLength = 1;
            }

            if (currentFrame > spinSprites.Length - 1)
            {
                currentFrame = 0;
            }

            animator.SetCurSprite(spinSprites[currentFrame]);
        }

        public void Exit()
        {

        }
    }

    public class TailsJump : IAnimation
    {
        TailsAnimationController animator;

        Sprite[] jumpSprites;

        float frameLength;
        int currentFrame;
        int maxFrames = 3;

        public TailsJump(TailsAnimationController anim)
        {
            animator = anim;
        }

        public void Enter()
        {
            jumpSprites = animator.GetAnimationSprites().jumpSprites;
        }

        public void Update()
        {
            frameLength -= Time.deltaTime;

            if (frameLength <= 0)
            {
                currentFrame++;
                frameLength = .1f;
            }

            if (currentFrame > jumpSprites.Length-1)
            {
                currentFrame = 0;
            }

            animator.SetCurSprite(jumpSprites[currentFrame]);
        }

        public void Exit()
        {

        }
    }

    public class TailsSkid : IAnimation
    {
        TailsAnimationController animator;

        Sprite[] skidSprites;

        float frameLength = .3f;
        int currentFrame;
        int maxFrames = 3;

        public TailsSkid(TailsAnimationController anim)
        {
            animator = anim;
        }

        public void Enter()
        {
            skidSprites = animator.GetAnimationSprites().switching;
        }

        public void Update()
        {
            frameLength -= Time.deltaTime;

            if (frameLength <= 0)
            {
                currentFrame++;
                frameLength = .3f;
            }

            if (currentFrame > skidSprites.Length - 1)
            {
                currentFrame = skidSprites.Length - 1;
            }

            animator.SetCurSprite(skidSprites[currentFrame]);
        }

        public void Exit()
        {
            currentFrame = 0;
            frameLength = .1f;
        }
    }

    #endregion
}
// Originally uploaded by Greao. Do not reupload this mod please. That would NOT be nice >:(

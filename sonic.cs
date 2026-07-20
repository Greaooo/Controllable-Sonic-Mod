using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Mod;
using CharApi;
using UnityEngine.Events;
using AnimatorComponents;

namespace Sonic
{
    public interface IAnimation
    {
        void Enter();
        void Update();
        void Exit();
    }

    public enum MovementState
    {
        IDLE,
        WALKING,
        RUNNING,
        JUMPING,
        SWITCHING,
        CROUCHING,
        SPINNING,
        ROLLING,
        FLYING
    }

    public enum CharacterType
    {
        SONIC,
        TAILS,
        KNUCKLES
    }

    public struct AnimationSprites
    {
        public Sprite[] idleSprites;
        public Sprite bounced;
        public Sprite[] switching;
        public Sprite[] crouchSprites;
        public Sprite[] jumpSprites;
        public Sprite[] runSprites;
        public Sprite[] walkSprites;
        public Sprite[] spinSprites;
    }

    public class SonicController : MonoBehaviour
    {
        // Components
        public Rigidbody2D rb { get; private set; }
        public SpriteRenderer rend { get; private set; }

        // Movement properties
        public float groundSpeed { get; private set; }
        public float acceleration { get; set; } = 15;

        public float decceleration { get; set; } = 30;
        public float friction { get; set; } = 45;
        public float topSpeed { get; set; } = 30;
        public float jumpForce { get; private set; } = 8;
        public float spinDashForce { get; private set; } = 0;

        public float dampAmount = 0.9f;

        private float canMoveCooldown = 0;

        private float crouchTime = 1;

        private float xAxis;

        private float jumpCooldown;

        public CharacterType characterType { get; private set; }

        public bool canMove { get; private set; } = true;

        private SonicAnimationController animator;

        // State management
        public MovementState movementState { get; private set; }

        public UnityAction<MovementState, MovementState> onMovementStateChange;

        //Theres definitely some unnecessary voids in this script that i should get rid of.

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.useAutoMass = false;
            rb.mass = 3;
        }

        void Start()
        {
            var changeSpeedButton = new ContextMenuButton(() => true, "Change max speed",
                "Change Max Speed",
                "Changes the maximum speed sonic can move.",
                new UnityEngine.Events.UnityAction[1]
                {
                    () =>
                    {
                        Utils.OpenFloatInputDialog(topSpeed, this, (sonic, input) => sonic.SetMaxSpeed(input), "Set New Max Speed", "Input Here:");
                    }
                });

            GetComponent<PhysicalBehaviour>().ContextMenuOptions.Buttons.Add(changeSpeedButton);

            animator = GetComponent<SonicAnimationController>();

            ChangePhysicalMaterial(new PhysicsMaterial2D()
            {
                friction = 0f,
                bounciness = 0
            });
        }

        void FixedUpdate()
        {
            if (Global.main.Paused) { return; }
            HandleGroundAcceleration();
            ApplyMovement();
        }

        void ApplyMovement()
        {
            rb.velocity = new Vector2(groundSpeed, rb.velocity.y);
        }

        void ChangePhysicalMaterial(PhysicsMaterial2D physicalBehaviour)
        {
            rb.sharedMaterial = physicalBehaviour;
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

            if (CanJump() && InputSystem.Down("jump"))
            {
                Jump();
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

            if (Mathf.Abs(groundSpeed) <= 2 && movementState == MovementState.ROLLING)
            {
                canMove = true;
                SwitchMovementState(MovementState.WALKING);
            }
        }

        #region Movement Functions

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
                ChangePhysicalMaterial(new PhysicsMaterial2D()
                {
                    friction = .8f,
                    bounciness = 0
                });

                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 5);
                if (hit && movementState != MovementState.JUMPING)
                {
                    rb.AddForce(Vector2.down * 35);
                }
            }
            else
            {
                ChangePhysicalMaterial(new PhysicsMaterial2D()
                {
                    friction = 0f,
                    bounciness = 0
                });
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

            if (movementState == MovementState.CROUCHING)
            {
                groundSpeed += decceleration * Time.fixedDeltaTime;
            }

            if (xAxis == 0)
            {
                groundSpeed *= friction * Time.fixedDeltaTime;
                
                if (Mathf.Abs(groundSpeed) < 0.1f)
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

            groundSpeed = Mathf.Lerp(groundSpeed, 0, Time.deltaTime * 5);

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
            return (xAxis > 0 && groundSpeed < 0 && Mathf.Abs(groundSpeed) >= 1) || 
                (xAxis < 0 && groundSpeed > 0 && Mathf.Abs(groundSpeed) >= 1);
        }

        #endregion

        #region State Management

        void HandleMovementState()
        {
            jumpCooldown -= Time.deltaTime;

            if (movementState == MovementState.ROLLING) { if (Mathf.Abs(groundSpeed) <= 1f) { canMove = true; } return; }

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
            if(newState == movementState) { return; }

            onMovementStateChange?.Invoke(movementState, newState);

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

    public class SonicAnimationController : MonoBehaviour
    {
        private SonicController SonicController;
        private SpriteRenderer rend;
        private Sprite curSprite;

        private MovementState animState;
        public IAnimation currentAnimation { get; private set; }
        public IAnimation wantedAnimation { get; private set; }

        public AnimationSprites animSprites { get; private set; }
        public event Action<IAnimation> animationChanged;

        private WalkAnimation _walkAnimation;
        private IdleAnimation _idleAnimation;
        private RunAnimation _runAnimation;
        private SkidAnimation _skidAnimation;
        private JumpAnimation _jumpAnimation;
        private SpinDashAnimation _spinDashAnimation;
        private CrouchAnimation _crouchAnimation;

        

        void Start()
        {

            //InitAnimations();
        }

        public void InitAnimations()
        {
            _walkAnimation = new WalkAnimation(this);
            _idleAnimation = new IdleAnimation(this);
            _runAnimation = new RunAnimation(this);
            _skidAnimation = new SkidAnimation(this);
            _crouchAnimation = new CrouchAnimation(this);
            _jumpAnimation = new JumpAnimation(this);
            _spinDashAnimation = new SpinDashAnimation(this);

            currentAnimation = _idleAnimation;
            currentAnimation.Enter();
        }

        public void SetController(SonicController script)
        {
            SonicController = script;
            rend = SonicController.rend;
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

        public SonicController GetSonicController()
        {
            return SonicController;
        }

        public void OnAnimationFrameChanged(Sprite newSprite)
        {
            throw new NotImplementedException();
        }
    }

    #region Animations

    public class IdleAnimation : IAnimation
    {
        SonicAnimationController animator;

        Sprite[] idleSprites;

        float frameLength = .3f;
        int currentFrame;
        int maxFrames = 3;

        public IdleAnimation(SonicAnimationController anim)
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

    public class CrouchAnimation : IAnimation
    {
        SonicAnimationController animator;

        Sprite[] crouchSprites;

        float frameLength = 0.05f;
        int currentFrame = 0;
        int maxFrames = 1;

        public CrouchAnimation(SonicAnimationController anim)
        {
            animator = anim;
        }

        public void Enter()
        {
            crouchSprites = animator.GetAnimationSprites().crouchSprites;
        }

        public void Update()
        {
            frameLength -= Time.deltaTime;

            if (frameLength <= 0 && currentFrame < maxFrames)
            {
                currentFrame++;
                frameLength = 0.03f;
            }

            animator.SetCurSprite(crouchSprites[currentFrame]);
        }

        public void Exit()
        {
            currentFrame = 0;
            frameLength = 0.05f;
        }
    }

    public class WalkAnimation : IAnimation
    {
        SonicAnimationController animator;

        Sprite[] walkSprites;

        float frameLength;
        int currentFrame;
        int maxFrames = 7;

        public WalkAnimation(SonicAnimationController anim)
        {
            animator = anim;
        }

        public void Enter()
        {
            walkSprites = animator.GetAnimationSprites().walkSprites;
        }

        public void Update()
        {
            frameLength -= Time.deltaTime * Mathf.Abs(animator.GetSonicController().rb.velocity.x);

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

    public class RunAnimation : IAnimation
    {
        SonicAnimationController animator;

        Sprite[] runSprites;

        float frameLength;
        int currentFrame;
        int maxFrames = 3;

        public RunAnimation(SonicAnimationController anim)
        {
            animator = anim;
        }

        public void Enter()
        {
            runSprites = animator.GetAnimationSprites().runSprites;
        }

        public void Update()
        {
            frameLength -= Time.deltaTime * Mathf.Abs(animator.GetSonicController().rb.velocity.x);

            if (frameLength <= 0)
            {
                currentFrame++;
                frameLength = 1;
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

    public class SpinDashAnimation : IAnimation
    {
        SonicAnimationController animator;

        Sprite[] spinSprites;

        float frameLength;
        int currentFrame;
        int maxFrames = 4;

        public SpinDashAnimation(SonicAnimationController anim)
        {
            animator = anim;
        }

        public void Enter()
        {
            spinSprites = animator.GetAnimationSprites().spinSprites;
        }

        public void Update()
        {
            frameLength -= animator.GetSonicController().spinDashForce*Time.deltaTime;

            if (frameLength <= 0)
            {
                currentFrame++;
                frameLength = 1;
            }

            if (currentFrame > maxFrames)
            {
                currentFrame = 0;
            }

            animator.SetCurSprite(spinSprites[currentFrame]);
        }

        public void Exit()
        {

        }
    }

    public class JumpAnimation : IAnimation
    {
        SonicAnimationController animator;

        Sprite[] jumpSprites;

        float frameLength;
        int currentFrame;
        int maxFrames = 3;

        public JumpAnimation(SonicAnimationController anim)
        {
            animator = anim;
        }

        public void Enter()
        {
            jumpSprites = animator.GetAnimationSprites().jumpSprites;
        }

        public void Update()
        {
            frameLength -= Time.deltaTime * Mathf.Abs(animator.GetSonicController().rb.velocity.x);

            frameLength = Mathf.Clamp(frameLength, -10, .5f);

            if (frameLength <= 0)
            {
                currentFrame++;
                frameLength = 1;
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

    public class SkidAnimation : IAnimation
    {
        SonicAnimationController animator;

        Sprite[] skidSprites;

        float frameLength = .3f;
        int currentFrame;
        int maxFrames = 3;

        public SkidAnimation(SonicAnimationController anim)
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
// Originally uploaded by 'Greao'. Do not reupload without their explicit permission. Please :)

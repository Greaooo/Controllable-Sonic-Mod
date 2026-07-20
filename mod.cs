using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Sonic;
using Tails;
using WorldChunks;
using AnimatorComponents;

namespace Mod
{
    public static class GlobalPaths
    {
        public const string Sonic = "sprites/sonic";
        public const string Tails = "sprites/tails";

        public static class SonicPaths
        {
            public const string Crouch = Sonic + "/crouch_sprites";
            public const string Idle = Sonic + "/idle_sprites";
            public const string Jump = Sonic + "/jump_sprites";
            public const string Misc = Sonic + "/misc_sprites";
            public const string Pushing = Sonic + "/pushing_sprites";
            public const string Walk = Sonic + "/walk_sprites";
            public const string Run = Sonic + "run_sprites";
            public const string Skid = Sonic + "skid_sprites";
            public const string SpinDash = Sonic + "spindash_sprites";
        }

        public static class TailsPaths
        {
            public const string Crouch = Tails + "/crouch_sprites";
            public const string Idle = Tails + "/idle_sprites";
            public const string Jump = Tails + "/jump_sprites";
            public const string Walk = Tails + "/walk_sprites";
            public const string Run = Tails + "run_sprites";
            public const string Skid = Tails + "skid_sprites";
            public const string SpinDash = Tails + "spindash_sprites";
        }
    }


    public class Mod : MonoBehaviour
    {

        public static void OnLoad()
        {
            ModAPI.RegisterInput("left", "left", KeyCode.A);
            ModAPI.RegisterInput("right", "right", KeyCode.D);
            ModAPI.RegisterInput("jump", "jump", KeyCode.W);
            ModAPI.RegisterInput("crouch", "crouch", KeyCode.LeftShift);
        }

        public static void Main()
        {
            ModAPI.RegisterCategory("Sonic Entities", "All of the entities in Sonic", ModAPI.LoadSprite("sprites/sonic/preview.png"));

            //Sonic
            ModAPI.Register(new Modification
            {
                NameOverride = "Sonic The Hedgehog",
                DescriptionOverride = "The one and only blue hedgehog. Can also run fast ig.",
                OriginalItem = ModAPI.FindSpawnable("Metal Cube"),
                ThumbnailOverride = ModAPI.LoadSprite("sprites/sonic/preview.png"),
                CategoryOverride = ModAPI.FindCategory("Sonic Entities"),

                AfterSpawn = (Instance) =>
                {
                    AnimationSprites anim = new AnimationSprites()
                    {
                        idleSprites = new Sprite[]
                        {
                            ModAPI.LoadSprite("sonic/idle_sprites/idle0.png")
                        },
                        walkSprites = new Sprite[]
                        {
                            ModAPI.LoadSprite("sonic/walk_sprites/walk0.png"),
                            ModAPI.LoadSprite("sonic/walk_sprites/walk1.png"),
                            ModAPI.LoadSprite("sonic/walk_sprites/walk2.png"),
                            ModAPI.LoadSprite("sonic/walk_sprites/walk3.png"),
                            ModAPI.LoadSprite("sonic/walk_sprites/walk4.png"),
                            ModAPI.LoadSprite("sonic/walk_sprites/walk5.png"),
                            ModAPI.LoadSprite("sonic/walk_sprites/walk6.png"),
                            ModAPI.LoadSprite("sonic/walk_sprites/walk7.png")
                        },
                        jumpSprites = new Sprite[]
                        {
                            ModAPI.LoadSprite("sonic/jump_sprites/jump0.png"),
                            ModAPI.LoadSprite("sonic/jump_sprites/jump1.png"),
                            ModAPI.LoadSprite("sonic/jump_sprites/jump2.png"),
                            ModAPI.LoadSprite("sonic/jump_sprites/jump3.png"),
                        },
                        runSprites = new Sprite[]
                        {
                            ModAPI.LoadSprite("sonic/run_sprites/run0.png"),
                            ModAPI.LoadSprite("sonic/run_sprites/run1.png"),
                            ModAPI.LoadSprite("sonic/run_sprites/run2.png"),
                            ModAPI.LoadSprite("sonic/run_sprites/run3.png"),
                        },
                        switching = new Sprite[]
                        {
                            ModAPI.LoadSprite("sonic/skid_sprites/skid0.png"),
                            ModAPI.LoadSprite("sonic/skid_sprites/skid1.png"),
                            ModAPI.LoadSprite("sonic/skid_sprites/skid2.png"),
                            ModAPI.LoadSprite("sonic/skid_sprites/skid3.png")
                        },
                        spinSprites = new Sprite[]
                        {
                            ModAPI.LoadSprite("sonic/spindash_sprites/0.png"),
                            ModAPI.LoadSprite("sonic/spindash_sprites/1.png"),
                            ModAPI.LoadSprite("sonic/spindash_sprites/2.png"),
                            ModAPI.LoadSprite("sonic/spindash_sprites/3.png"),
                            ModAPI.LoadSprite("sonic/spindash_sprites/4.png"),
                            ModAPI.LoadSprite("sonic/spindash_sprites/5.png"),
                        },
                        crouchSprites = new Sprite[]
                        {
                            ModAPI.LoadSprite("sonic/crouch_sprites/crouch0.png"),
                            ModAPI.LoadSprite("sonic/crouch_sprites/crouch1.png"),
                        },
                    };

                    Instance.transform.localScale = new Vector3(1.3f, 1.3f, 0);

                    SonicController c = Instance.AddComponent<SonicController>();

                    Instance.GetComponent<SonicController>().SetSpriteRenderer(Instance.GetComponent<SpriteRenderer>());

                    Instance.GetComponent<Rigidbody2D>().mass = 2;
                    Instance.GetComponent<Rigidbody2D>().freezeRotation = true;

                    Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Soft");
                    Instance.GetComponent<PhysicalBehaviour>().PlaySliderSound = false;

                    Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("sprites/sonic/misc_sprites/idle.png");
                    Instance.GetComponent<PhysicalBehaviour>().HasOutline = false;

                    

                    SonicAnimationController a = Instance.AddComponent<SonicAnimationController>();
                    a.SetAnimationSprites(anim);
                    a.SetController(c);

                    Instance.GetComponent<Rigidbody2D>().sharedMaterial = new PhysicsMaterial2D()
                    {
                        friction = 0,
                        bounciness = 0
                    };

                    Destroy(Instance.GetComponent<BoxCollider2D>());
                    Instance.gameObject.AddComponent<CapsuleCollider2D>();
                    Instance.GetComponent<CapsuleCollider2D>().size = new Vector2(0.5f, 1.18f);
                }
            });

            //Tails
            ModAPI.Register(new Modification
            {
                NameOverride = "Miles 'Tails' Prower",
                DescriptionOverride = "Miles 'Tails' Prower. Thats his name.",
                OriginalItem = ModAPI.FindSpawnable("Metal Cube"),
                ThumbnailOverride = ModAPI.LoadSprite("sprites/tails/preview.png"),
                CategoryOverride = ModAPI.FindCategory("Sonic Entities"),

                AfterSpawn = (Instance) =>
                {
                    Instance.transform.localScale = new Vector3(1.3f, 1.3f, 0);

                    TailsController c = Instance.AddComponent<TailsController>();

                    Instance.GetComponent<Rigidbody2D>().mass = 2;
                    Instance.GetComponent<Rigidbody2D>().freezeRotation = true;

                    Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Soft");
                    Instance.GetComponent<PhysicalBehaviour>().PlaySliderSound = false;

                    Instance.GetComponent<TailsController>().SetSpriteRenderer(Instance.GetComponent<SpriteRenderer>());

                    Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("sprites/sonic/misc_sprites/idle.png");
                    Instance.GetComponent<PhysicalBehaviour>().HasOutline = false;

                    AnimationSprites anim = new AnimationSprites()
                    {
                        idleSprites = new Sprite[]
                        {
                            ModAPI.LoadSprite("tails/idle_sprites/idle0.png")
                        },
                        walkSprites = new Sprite[]
                        {
                            ModAPI.LoadSprite("tails/walk_sprites/walk0.png"),
                            ModAPI.LoadSprite("tails/walk_sprites/walk1.png"),
                            ModAPI.LoadSprite("tails/walk_sprites/walk2.png"),
                            ModAPI.LoadSprite("tails/walk_sprites/walk3.png"),
                            ModAPI.LoadSprite("tails/walk_sprites/walk4.png"),
                            ModAPI.LoadSprite("tails/walk_sprites/walk5.png"),
                            ModAPI.LoadSprite("tails/walk_sprites/walk6.png"),
                            ModAPI.LoadSprite("tails/walk_sprites/walk7.png")
                        },
                        jumpSprites = new Sprite[]
                        {
                            ModAPI.LoadSprite("tails/jump_sprites/jump0.png"),
                            ModAPI.LoadSprite("tails/jump_sprites/jump1.png"),
                            ModAPI.LoadSprite("tails/jump_sprites/jump2.png"),
                            ModAPI.LoadSprite("tails/jump_sprites/jump3.png"),
                        },
                        runSprites = new Sprite[]
                        {
                            ModAPI.LoadSprite("tails/run_sprites/run0.png"),
                            ModAPI.LoadSprite("tails/run_sprites/run1.png"),
                            ModAPI.LoadSprite("tails/run_sprites/run2.png"),
                            ModAPI.LoadSprite("tails/run_sprites/run3.png"),
                        },
                        switching = new Sprite[]
                        {
                            ModAPI.LoadSprite("tails/skid_sprites/skid0.png"),
                            ModAPI.LoadSprite("tails/skid_sprites/skid1.png"),
                            ModAPI.LoadSprite("tails/skid_sprites/skid2.png"),
                            ModAPI.LoadSprite("tails/skid_sprites/skid3.png")
                        },
                        spinSprites = new Sprite[]
                        {
                            ModAPI.LoadSprite("tails/spindash_sprites/0.png"),
                            ModAPI.LoadSprite("tails/spindash_sprites/1.png"),
                            ModAPI.LoadSprite("tails/spindash_sprites/2.png"),
                            ModAPI.LoadSprite("tails/spindash_sprites/3.png"),
                            ModAPI.LoadSprite("tails/spindash_sprites/4.png"),
                            ModAPI.LoadSprite("tails/spindash_sprites/5.png"),
                        },
                        crouchSprites = new Sprite[]
                        {
                            ModAPI.LoadSprite("tails/crouch_sprites/crouch0.png"),
                            ModAPI.LoadSprite("tails/crouch_sprites/crouch1.png"),
                        },
                    };

                    TailsAnimationController a = Instance.AddComponent<TailsAnimationController>();
                    a.SetAnimationSprites(anim);
                    a.SetController(c);

                    Instance.GetComponent<Rigidbody2D>().sharedMaterial = new PhysicsMaterial2D()
                    {
                        friction = 0,
                        bounciness = 0
                    };

                    Destroy(Instance.GetComponent<BoxCollider2D>());
                    Instance.gameObject.AddComponent<CapsuleCollider2D>();
                    Instance.GetComponent<CapsuleCollider2D>().size = new Vector2(0.5f, 1.18f);
                }
            });
        }
    }
}
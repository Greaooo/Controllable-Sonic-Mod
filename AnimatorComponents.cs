using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AnimatorComponents
{
    /// <summary>
    /// 
    /// Animation system that can play and switch between multiple "animation sequences," or animation clips,
    /// that hold frame data, or an array of "AnimationFrame".
    /// 
    /// The length used in "AnimationFrame" is the amount of frames the sprite should last on screen.
    /// The framerate of an animation is by default 60, but can be changed when constructing an
    /// animation system.
    /// 
    /// Therefore the length of an AnimationFrame is the frames the sprite lasts, divided by the animation
    /// system framerate. (For instance, if a sprite is supposed to last 5 frames: 5/60)
    /// 
    /// The subsequent float is then used for the duration of the sprite.
    /// 
    /// </summary>

    public class AnimationSystem : MonoBehaviour
    {
        private IAnimatable user;

        public int frame_rate = 60;

        public UnityAction<AnimationSequence> on_animation_changed;

        public Dictionary<string, AnimationSequence> cached_animations = new Dictionary<string, AnimationSequence>();

        public bool Is_Paused { get; private set; } = false;
        public bool Is_Debug { get; private set; } = false;

        public AnimationSequence current_playing_animation { get; private set; }

        public void InitSystem(IAnimatable animatable, int new_frame_rate = 60, bool isInDebug = false)
        {
            user = animatable;
            frame_rate = new_frame_rate;

            Is_Debug = isInDebug;
        }

        public void Update()
        {
            if (current_playing_animation != null)
            {
                current_playing_animation.TickAnimation();
            }
        }

        public void PlayAnimation(string animation_name)
        {
            if(!cached_animations.ContainsKey(animation_name))
            {
                ModAPI.Notify($"Couldn't find animation with name: {animation_name}!");
                return;
            }

            if(current_playing_animation != null)
            {
                if (current_playing_animation.animation_name == animation_name)
                {
                    if (Is_Debug)
                    {
                        //ModAPI.Notify($"Already playing animation {animation_name}!");
                    }
                    return;
                }
            }

            if (Is_Debug)
            {
                ModAPI.Notify($"Started playing animation {animation_name}!");
            }

            on_animation_changed?.Invoke(cached_animations[animation_name]);
            current_playing_animation = cached_animations[animation_name];
        }

        public void OnFrameChange(AnimationFrame new_frame)
        {
            try
            {
                ModAPI.Notify($"Frame now changed to: {new_frame.frame_sprite}");

                user.OnAnimationFrameChanged(new_frame.frame_sprite);
            }
            catch(Exception ex)
            {
                ModAPI.Notify(ex);
            }
        }

        public void AddAnimation(AnimationSequence new_animation)
        {
            if (cached_animations.ContainsKey(new_animation.animation_name))
            {
                ModAPI.Notify($"Could not add {new_animation.animation_name}! Animation already exists.");
                return;
            }

            if (Is_Debug)
            {
                ModAPI.Notify($"Created new animation with name {new_animation.animation_name} " +
                    $"containing {new_animation.animation_frames.Length}, " +
                    "and cached it.");
            }
            
            cached_animations.Add(new_animation.animation_name, new_animation);
        }

        public void SetPauseAnimationSystem(bool new_pause)
        {
            Is_Paused = new_pause;

            if (Is_Debug)
            {
                ModAPI.Notify($"Is paused set to: {Is_Paused}");
            }
        }
    }

    public struct AnimationFrame
    {
        public Sprite frame_sprite;
        public float frame_length;

        public AnimationFrame(Sprite new_frame_sprite, AnimationSystem system, int frames_on_screen)
        {
            frame_sprite = new_frame_sprite;
            frame_length = (float)frames_on_screen / system.frame_rate;
        }
    }

    public class AnimationSequence
    {
        private AnimationSystem animation_system;

        public string animation_name;
        public AnimationFrame[] animation_frames;

        private float current_frame_time;
        private int current_frame_index;

        public AnimationSequence(AnimationSystem new_animation_system, string new_name, AnimationFrame[] new_animation_frames)
        {
            animation_system = new_animation_system;
            animation_name = new_name;
            animation_frames = new_animation_frames;
        }

        public void TickAnimation()
        {
            current_frame_time -= 1f/animation_system.frame_rate;

            if(current_frame_time <= 0)
            {
                IterateFrame();
            }
        }

        private void IterateFrame()
        {
            try
            {
                current_frame_index ++;

                if (current_frame_index >= animation_frames.Length)
                {
                    current_frame_index = 0;
                }

                current_frame_time = animation_frames[current_frame_index].frame_length;

                animation_system.OnFrameChange(animation_frames[current_frame_index]);
            }
            catch (Exception e)
            {
                ModAPI.Notify(e.ToString());
            }
        }
    }

    public interface IAnimatable
    {
        void OnAnimationFrameChanged(Sprite newSprite);
    }

    public class AnimationSystemTester : MonoBehaviour, IAnimatable
    {
        private AnimationSystem system;

        private SpriteRenderer rend;

        public const string Sprite_Path = "sprites/debug/possibleteaser";

        public struct AnimationSprites
        {
            public Sprite[] leftSprites;
            public Sprite[] rightSprites;
            public Sprite[] upSprites;
            public Sprite[] downSprites;
        }

        public AnimationSprites anim_sprites;

        private const string animWalkDown = "walk_down";

        private void Start()
        {
            try
            {
                system = gameObject.AddComponent<AnimationSystem>();
                system.InitSystem(this, 30, true);

                AnimationFrame[] frames =
                {
                    new AnimationFrame(anim_sprites.downSprites[0], system, 155),
                    new AnimationFrame(anim_sprites.downSprites[1], system, 155),
                    new AnimationFrame(anim_sprites.downSprites[2], system, 155),
                    new AnimationFrame(anim_sprites.downSprites[3], system, 155),
                };

                system.AddAnimation(new AnimationSequence(system, animWalkDown, frames));
                rend = GetComponent<SpriteRenderer>();
            }
            catch (Exception e)
            {
                ModAPI.Notify(e.ToString());
            }
        }

        void Update()
        {
            try
            {
                system.PlayAnimation(animWalkDown);
            }
            catch(Exception e)
            {
                ModAPI.Notify(e.ToString());
            }
            
        }

        public void OnAnimationFrameChanged(Sprite newSprite)
        {
            rend.sprite = newSprite;
        }
    }
}

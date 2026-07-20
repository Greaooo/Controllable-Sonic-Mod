using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Mod;




namespace CharUtils
{
    public struct CharacterUtilities
    {
        public static Sprite[] GetSprites(string sprite_path, int amount_of_sprites)
        {
            Sprite[] grabbed_sprites = new Sprite[amount_of_sprites];

            for (int i = 0; i < amount_of_sprites; i++)
            {
                grabbed_sprites[i] = ModAPI.LoadSprite(sprite_path + i + ".png");
                ModAPI.Notify(grabbed_sprites[i].name);
            }

            return grabbed_sprites;
        }
    }
}
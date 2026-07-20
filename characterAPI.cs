using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Mod;




namespace CharApi
{
    public struct CharacterApi
    {
        public static Sprite[] GetSprites(string sprite_path, int amount_of_sprites)
        {
            Sprite[] grabbed_sprites = new Sprite[amount_of_sprites];

            for (int i = 0; i < amount_of_sprites; i++)
            {
                if(ModAPI.LoadSprite(sprite_path + i + ".png") == null) { return grabbed_sprites; }

                grabbed_sprites[i] = ModAPI.LoadSprite(sprite_path + i + ".png");
            }

            return grabbed_sprites;
        }

        //find all files in a folder and load them at runtime, useful for animations.
        public static Sprite[] GetAllSpritesFromFolder(string folder_path)
        {
            List<Sprite> found_sprites = new List<Sprite>();

            string[] found_files = GetSubFilePaths(folder_path);
            string meta_location = $"{ModAPI.Metadata.MetaLocation}/";

            for (int i = 0; i < found_files.Length; i++)
            {
                found_files[i] = found_files[i].Substring(meta_location.Length);

                Sprite grabbed_sprite = ModAPI.LoadSprite(found_files[i]);

                if(grabbed_sprite == null)
                {
                    ModAPI.Notify($"Couldnt get sprite at: {found_files[i]}");
                    continue;
                }

                found_sprites.Add(grabbed_sprite);
            }

            ModAPI.Notify($"Finished locating sprites at: {folder_path}!");
            return found_sprites.ToArray();
        }

        //get all child files in folder path
        internal static string[] GetSubFilePaths(string folder_path)
        {
            string meta_location = ModAPI.Metadata.MetaLocation;
            string directory_path = $"{meta_location}/{folder_path}".Replace("\\", "/");

            if (!Directory.Exists(directory_path))
            {
                ModAPI.Notify($"{directory_path} could not be found!");
                return Array.Empty<string>();
            }

            string[] found_files = Directory.GetFiles(directory_path);

            if (found_files.Length <= 0)
            {
                ModAPI.Notify($"No files found at file path {directory_path}!");
                return Array.Empty<string>();
            }
            
            return found_files;
        }
    }
}
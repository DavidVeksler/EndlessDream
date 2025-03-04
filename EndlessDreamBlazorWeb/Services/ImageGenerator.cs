﻿using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System.Text.Json;
using Image = SixLabors.ImageSharp.Image;

namespace EndlessDreamBlazorWeb.Services
{
    public class ImageGenerator
    {
        private static readonly HttpClient client = new();
        private static readonly string url = "http://192.168.1.250:8080";
        private JsonElement error;
        internal ExifProfile? exifProfile;
        private const string MODEL_HASH = "c8df560d29";

        public async Task<Image?> RenderStableDiffusionImage(string prompt, int steps, int seed = -1, bool IsPhoto = true)
        {
            if (IsPhoto)
            {
                prompt = "photo of " + prompt;
            }

            var payload = new
            {
                prompt,
                negative_prompt = "easynegative, CyberRealistic_Negative-neg, epiCNegative, ac_neg1",
                steps,
                cfgScale = 7,
                seed,
                modelHash = MODEL_HASH,
                ensd = 31337,
                tokenMergingRatio = 0.5,
                faceRestoration = "CodeFormer",
                restore_faces = true,
                save_images = false,
                skip_clip=2,
                width= 800, height= 800,
               
            };

            StringContent content = new(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync($"{url}/sdapi/v1/txt2img", content);

            string jsonString = await response.Content.ReadAsStringAsync();
            JsonElement data = JsonSerializer.Deserialize<JsonElement>(jsonString);

            if (data.TryGetProperty("error", out error))
            {
                Console.WriteLine(error.ToString());
                throw new Exception(error.ToString());
            }

            foreach (JsonElement i in data.GetProperty("images").EnumerateArray())
            {
                byte[] imageBytes = Convert.FromBase64String(i.GetString().Split(",", 2)[0]);
                using MemoryStream ms = new(imageBytes);
                Image image = Image.Load(ms);

                var pngPayload = new { image = "data:image/png;base64," + i.GetString() };
                StringContent pngContent = new(JsonSerializer.Serialize(pngPayload), System.Text.Encoding.UTF8, "application/json");
                HttpResponseMessage response2 = await client.PostAsync($"{url}/sdapi/v1/png-info", pngContent);
                string jsonString2 = await response2.Content.ReadAsStringAsync();
                JsonElement data2 = JsonSerializer.Deserialize<JsonElement>(jsonString2);

                // Add parameters as EXIF metadata
                string? pngInfo = data2.GetProperty("info").GetString();

                exifProfile = new ExifProfile();
                exifProfile.SetValue(ExifTag.UserComment, pngInfo);
                image.Metadata.ExifProfile = exifProfile;

                return image;
            }

            return null;
        }
    }
}
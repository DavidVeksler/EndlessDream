using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System.Text.Json;
using Image = SixLabors.ImageSharp.Image;

namespace EndlessDreamBlazorWeb.Services
{
    public class ImageGenerator
    {
        private static readonly HttpClient client = new();
        private static readonly string url = "http://127.0.0.1:8080";
        private JsonElement error;
        internal ExifProfile? exifProfile;


        public async Task<Image?> RenderStableDiffusionImage(string prompt, int steps, int seed = -1, bool IsPhoto = true)
        {
            if (IsPhoto)
            {
                prompt = "photo of " + prompt;
            }

            var payload = new
            {
                prompt,
                negative_prompt = "easynegative, Asian-Less-Neg, nsfw, ugly, blurry, distorted, nude",
                steps,
                cfgScale = 7,
                seed,
                modelHash = "c249d7853b",
                model = "dreamshaper_6BakedVae",
                ensd = 31337,
                tokenMergingRatio = 0.5,
                faceRestoration = "CodeFormer",
                restore_faces = true,
                save_images = false,
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
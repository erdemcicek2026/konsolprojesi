using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace konsolprojesi;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        
        // Padding (Kenar boşluğu) Thickness nesnesi ile verilir
        this.Padding = new Thickness(10, 20, 10, 20);
    }

    private async void OnKameraClicked(object sender, EventArgs e)
    {
        if (!MediaPicker.Default.IsCaptureSupported)
        {
            await this.DisplayAlertAsync("Hata", "Bu cihazda kamera desteklenmiyor.", "Tamam");
            return;
        }

        try
        {
            FileResult photo = await MediaPicker.Default.CapturePhotoAsync();

            if (photo != null)
            {
                string LocalFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                
                using (Stream sourceStream = await photo.OpenReadAsync())
                using (FileStream localFileStream = File.OpenWrite(LocalFilePath))
                {
                    await sourceStream.CopyToAsync(localFileStream);
                }
                
                ImgFoto.Source = ImageSource.FromFile(LocalFilePath);
                LblSonuc.Text = "Fotoğraf alındı, Gemini'ye gönderiliyor...";

                // ---- YENİ EKLENEN KISIM: ZEKAYI TETİKLE ----
                
                // Resmi Base64 (Yazı) formatına çeviriyoruz ki internetten gidebilsin
                byte[] imageBytes = File.ReadAllBytes(LocalFilePath);
                string base64Image = Convert.ToBase64String(imageBytes);

                // API Key'ini buraya yazmalısın
                string apiKey = "AQ.Ab8RN6J_cFPRebc_e91ELXznC_jNbNlzIZH6_8DwpCLtxC1qKQ"; 
                
                // Gemini'den cevabı bekle
                string aiCevabi = await GeminiAnalizEt(base64Image, apiKey);
                
                // Gelen cevabı ekrana yaz
                LblSonuc.Text = aiCevabi;
            }
        }
        catch (Exception ex)
        {
            await this.DisplayAlertAsync("Hata", $"Bir sorun oluştu: {ex.Message}", "Tamam");
            LblSonuc.Text = "Analiz başarısız.";
        }
    }

    // Gemini API'sine resmi ve soruyu gönderen özel metot
    private async Task<string> GeminiAnalizEt(string base64Resim, string apiKey)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                // Kullanacağımız model: Gemini 1.5 Flash (Hızlı ve görsel işleyebilen model)
                string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

                // Gemini'ye göndereceğimiz paketi (JSON) hazırlıyoruz
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = "Bu buzdolabı fotoğrafındaki malzemelere bak. Bana bu malzemelerle yapabileceğim pratik, tek bir yemek tarifi ver. Türkçe cevapla." },
                                new { inline_data = new { mime_type = "image/jpeg", data = base64Resim } }
                            }
                        }
                    }
                };

                // Paketi JSON formatına çevirip HTTP post ile yolluyoruz
                string jsonContent = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Gelen karmaşık JSON datasının içinden sadece bize lazım olan "text" kısmını cımbızlıyoruz
                    JObject jsonResponse = JObject.Parse(responseString);
                    string tarif = jsonResponse["candidates"][0]["content"]["parts"][0]["text"].ToString();
                    return tarif;
                }
                else
                {
                    return $"API Hatası: {response.StatusCode}";
                }
            }
        }
        catch (Exception ex)
        {
            return $"Bağlantı Hatası: {ex.Message}";
        }
    }
}
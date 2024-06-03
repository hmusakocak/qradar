using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;



while (true)
{
    Console.WriteLine("Lütfen işlem yapmak istediğiniz seçeneği seçin:");
    Console.WriteLine("1. Dosyadaki URL'leri QRadar'a yükle");
    Console.WriteLine("2. QRadar'daki referans veri koleksiyonlarını listele");
    Console.WriteLine("3. USOM URL listesini indir ve kaydet");
    Console.WriteLine("4. Çıkış");

    string option = Console.ReadLine();

    switch (option)
    {
        case "1":
            await UploadURLsToQRadar();
            break;
        case "2":
            await ListReferenceDataCollections();
            break;
        case "3":
            await DownloadUSOMURLList();
            break;
        case "4":
            return;
        default:
            Console.WriteLine("Geçersiz seçenek! Lütfen tekrar deneyin.");
            break;
    }
}


static async System.Threading.Tasks.Task UploadURLsToQRadar()
{
    Console.WriteLine("QRadar'a yüklemek için dosya yolu girin:");
    string filePath = Console.ReadLine();
    Console.WriteLine("QRadar'a yüklemek için host girin:");
    string host = Console.ReadLine();
    Console.WriteLine("QRadar'a yüklemek için Reference Set ID girin:");
    int ref_id = Convert.ToInt32(Console.ReadLine());
    Console.WriteLine("QRadar'a yüklemek için kullanıcı adı girin:");
    string username = Console.ReadLine();
    Console.WriteLine("QRadar'a yüklemek için şifre girin:");
    string password = Console.ReadLine();

    try
    {

        var url = "https://" + host + "/api/reference_data_collections/set_entries";
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
        try
        {
            using (var httpClient = new HttpClient(handler))
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}")));

                string _filePath = filePath;
                using (StreamReader sr = new StreamReader(_filePath))
                {
                    string line;
                    string data = "";
                    var counter = 0;
                    while ((line = sr.ReadLine()) != null)
                    {

                        data += $"{{\"collection_id\": \"{ref_id}\", \"value\": \"{line}\"}},";
                        counter++;

                        if (counter == 5000)
                        {
                            data = "[" + data + "]";

                            var content = new StringContent(data, Encoding.UTF8, "application/json");

                            var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
                            {
                                Content = content
                            };
                            var response = await httpClient.SendAsync(request);
                            counter = 0;
                            data = "";
                        }
                    }

                    if (counter > 0)
                    {
                        data = "[" + data + "]";
                        var content = new StringContent(data, Encoding.UTF8, "application/json");

                        var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
                        {
                            Content = content
                        };
                        var response = await httpClient.SendAsync(request);
                        counter = 0;
                        data = "";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
    catch (Exception excep)
    {
        Console.WriteLine(excep.Message.ToString());
    }

    Console.WriteLine("URL'ler başarıyla QRadar'a yüklendi!");
}

static async System.Threading.Tasks.Task ListReferenceDataCollections()
{
    Console.WriteLine("QRadar adresini girin:");
    string host = Console.ReadLine();

    Console.WriteLine("Kullanıcı adını girin:");
    string username = Console.ReadLine();

    Console.WriteLine("Şifreyi girin:");
    string password = Console.ReadLine();
    string data = "";

    try
    {
        string url = "https://" + host + "/api/reference_data_collections/sets";
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;


        using (HttpClient httpClient = new HttpClient(handler))
        {

            try
            {

                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}")));

                HttpResponseMessage response = await httpClient.GetAsync(url);

                string responseBody = await response.Content.ReadAsStringAsync();

                try
                {
                    var items = JsonDocument.Parse(responseBody).RootElement.EnumerateArray();
                    foreach (var item in items)
                    {
                        string name = item.GetProperty("name").GetString();
                        int id = item.GetProperty("id").GetInt32();
                        string namespaceValue = item.GetProperty("namespace").GetString();

                        data += ($"Name: {name}, ID: {id}, Namespace: {namespaceValue}{Environment.NewLine}");
                    }
                    
                    Console.WriteLine(data);
                    Console.WriteLine("Referans veri koleksiyonları başarıyla listelendi!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
            catch (HttpRequestException ex)
            {
                data = "Hata: " + ex.Message;
            }
        }
    }
    catch (Exception excep)
    {
        Console.WriteLine(excep.Message.ToString());
    }


}

static async System.Threading.Tasks.Task DownloadUSOMURLList()
{
    string url = "https://www.usom.gov.tr/url-list.txt";
    string localPath = "url-list.txt";

    using (HttpClient client = new HttpClient())
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync();

            File.WriteAllText(localPath, content);

            Console.WriteLine("Dosya başarıyla indirildi ve kaydedildi.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Bir hata oluştu: {ex.Message}");
        }
    }
}


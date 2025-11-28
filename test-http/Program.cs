using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

// Simple test program to test OpenCode HTTP API
class Program
{
    static async Task Main(string[] args)
    {
        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var openCodeUrl = "http://localhost:36277";
        var text = "Test z C# konzolové aplikace";
        
        Console.WriteLine($"🚀 Testing OpenCode HTTP API");
        Console.WriteLine($"📡 URL: {openCodeUrl}");
        Console.WriteLine($"📝 Text: {text}");
        
        try
        {
            // Step 1: Append text
            var appendEndpoint = $"{openCodeUrl}/tui/append-prompt";
            Console.WriteLine($"🔍 Endpoint: {appendEndpoint}");
            
            var payload = new { text };
            var json = JsonSerializer.Serialize(payload);
            Console.WriteLine($"🔍 Payload: {json}");
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            Console.WriteLine("📡 Sending POST request...");
            var response = await httpClient.PostAsync(appendEndpoint, content);
            
            Console.WriteLine($"📡 Response status: {response.StatusCode}");
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"📡 Response body: {responseBody}");
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Text sent successfully!");
                
                // Step 2: Submit prompt
                Console.WriteLine("📡 Submitting prompt...");
                await Task.Delay(100);
                
                var submitEndpoint = $"{openCodeUrl}/tui/submit-prompt";
                var submitResponse = await httpClient.PostAsync(submitEndpoint, null);
                
                Console.WriteLine($"📡 Submit response status: {submitResponse.StatusCode}");
                var submitBody = await submitResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"📡 Submit response body: {submitBody}");
                
                if (submitResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ Prompt submitted successfully!");
                }
                else
                {
                    Console.WriteLine($"❌ Submit failed with status {submitResponse.StatusCode}");
                }
            }
            else
            {
                Console.WriteLine($"❌ Request failed with status {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"❌ HTTP Request Exception: {ex.Message}");
            Console.WriteLine($"   Inner Exception: {ex.InnerException?.Message}");
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"❌ Timeout: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"   Stack: {ex.StackTrace}");
        }
    }
}

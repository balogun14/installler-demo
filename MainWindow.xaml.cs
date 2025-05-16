using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows;
using System.Security.Cryptography;
using System.Net.Http;
using System.Net.Sockets;

namespace authtest
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _client = new();
        private const string ApiBaseUrl = "http://localhost:13255"; // Replace with your API URL

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Step 1: Get a random unused port for redirect URI
                var port = GetRandomUnusedPort();
                var redirectUri = $"http://127.0.0.1:{port}/";
                var deviceId = "42c0cb37-f80d-4ab1-95b4-d19466e1fd68";

                // Step 2: Call google-signin endpoint
                var response = await _client.GetAsync($"{ApiBaseUrl}/api/InstallerAuth/google-signin?deviceId={deviceId}&redirectUri={Uri.EscapeDataString(redirectUri)}");
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                var authUrl = result["authorizationUrl"];
                var codeVerifier = result["codeVerifier"];
                var returnedRedirectUri = result["redirectUri"];

                // Step 3: Start HttpListener to capture redirect
                var http = new HttpListener();
                http.Prefixes.Add(redirectUri);
                http.Start();

                // Step 4: Open browser
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(authUrl) { UseShellExecute = true });

                // Step 5: Wait for OAuth redirect
                var context = await http.GetContextAsync();
                this.Activate(); // Bring app to foreground

                // Step 6: Send response to browser
                var responseString = "<html><body>Please return to the app.</body></html>";
                var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                var httpResponse = context.Response; 
                httpResponse.ContentLength64 = buffer.Length; // Set ContentLength64 on HttpListenerResponse
                await httpResponse.OutputStream.WriteAsync(buffer, 0, buffer.Length); // Write to OutputStream of HttpListenerResponse
                httpResponse.OutputStream.Close(); // Close the OutputStream
                http.Stop();

                // Step 7: Extract code and state
                var query = context.Request.QueryString;
                if (query["error"] != null)
                {
                    MessageBox.Show($"OAuth error: {query["error"]}");
                    return;
                }
                if (query["code"] == null || query["state"] == null)
                {
                    MessageBox.Show("Malformed authorization response");
                    return;
                }

                var code = query["code"];
                var state = query["state"];

                // Step 8: Call google-callback endpoint
                var callbackResponse = await _client.PostAsJsonAsync($"{ApiBaseUrl}/api/InstallerAuth/google-callback", new
                {
                    Code = code,
                    State = state,
                    CodeVerifier = codeVerifier,
                    RedirectUri = returnedRedirectUri 
                });
                //TODO: Fix the parsing of token
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var some = await callbackResponse.Content.ReadAsStringAsync();
                var authResult = JsonSerializer.Deserialize<AuthResult>(some, options);

                Console.WriteLine(authResult.ToString());
                MessageBox.Show($"Sign-in successful! Token: {authResult.Token}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }

  
}

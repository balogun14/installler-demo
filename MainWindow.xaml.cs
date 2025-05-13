using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace authtest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private readonly HttpClient _client = new HttpClient();
        private const string ApiBaseUrl = "http://localhost:13255"; // Replace with your API URL

        private async void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Step 1: Get OAuth URL from google-signin
                var DeviceId = Guid.NewGuid();
                var response = await _client.GetAsync($"{ApiBaseUrl}/api/InstallerAuth/google-signin?deviceId={DeviceId}");
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                var authUrl = result["authorizationUrl"];

                // Step 2: Open browser
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(authUrl) { UseShellExecute = true });

                // Step 3: Prompt user to paste the code
                var code = Microsoft.VisualBasic.Interaction.InputBox("Enter the Google authorization code:", "Google Sign-In");
                if (string.IsNullOrEmpty(code)) return;

                // Step 4: Send code and state to google-callback
                var state = JsonSerializer.Serialize(new { guid = Guid.NewGuid().ToString(), deviceId = DeviceId.ToString() }); // Use same state as in google-signin
                var callbackResponse = await _client.PostAsJsonAsync($"{ApiBaseUrl}/api/InstallerAuth/google-callback", new
                {
                    Code = code,
                    State = state
                });

                var authResult = JsonSerializer.Deserialize<AuthResult>(await callbackResponse.Content.ReadAsStringAsync());
                MessageBox.Show($"Sign-in successful! Token: {authResult.Token}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
}

public record AuthResult(string Token, string UserType, string FullName);
}
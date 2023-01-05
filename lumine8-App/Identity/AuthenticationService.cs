using Grpc.Core;
using lumine8.Aes;
using lumine8_GrpcService;
using lumine8_maui;
using Newtonsoft.Json;

namespace lumine8.Client.Identity
{
    public partial class AuthenticationService
    {
        MainProto.MainProtoClient MainClient;
        public Metadata headers = new();

        public AuthenticationService(MainProto.MainProtoClient MainClient)
        {
            this.MainClient = MainClient;
        }

        public bool isAuthenticated { get; private set; } = false;
        public string Message { get; private set; } = string.Empty;
        public LoginUser loginUser { get; private set; } = new();
        public KeyFile keyFile = null;
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "lumine8");

        public void Reset()
        {
            isAuthenticated = false;
            loginUser = new();
        }

        public Task InitializeAuthenticate()
        {
            var cred = Path.Combine(path, "Current.json");

            if (!Directory.Exists(path) && OperatingSystem.IsAndroid())
                Directory.CreateDirectory(path);
            if (!File.Exists(cred))
                using (FileStream fs = File.Create(cred)) { }

            using StreamReader rc = new StreamReader(cred);
            string json = rc.ReadToEnd();
            string jsrc = json.ToString();

            loginUser = JsonConvert.DeserializeObject<LoginUser>(jsrc);
            if (loginUser == null)
                loginUser = new();
            else
            {
                var key = Path.Combine(path, loginUser.Username, "Key.json");

                if (!File.Exists(key))
                    File.Create(key);

                byte[] keyBytes = File.ReadAllBytes(key);

                CAes aes = new CAes(loginUser.Password, loginUser.PrivateKey);
                var ke = aes.Decrypt(keyBytes, aes.iv, aes.key);
                keyFile = JsonConvert.DeserializeObject<KeyFile>(aes.Decrypt(keyBytes, aes.iv, aes.key));
                loginUser.Password = keyFile.Password;
                loginUser.PrivateKey = keyFile.PrivateKey;
            }

            if (loginUser != null && !string.IsNullOrWhiteSpace(loginUser?.Username) && !string.IsNullOrWhiteSpace(loginUser?.PrivateKey))
            {
                return Task.Run(async () =>
                {
                    var b = await MainClient.AuthenticateAsync(loginUser);
                    headers = new()
                    {
                        { "Username", loginUser.Username },
                        { "Password", loginUser.Password },
                        { "PrivateKey", loginUser.PrivateKey }
                    };
                    isAuthenticated = b.IsAuthenticated;
                });
            }

            return Task.CompletedTask;
        }
    }
}

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
        public string Message { get; private set; } = "";
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
            //var key = Path.Combine(path, username, "Key.l8");
            var cred = Path.Combine(path, "Current.l8");

            //if (!File.Exists(key))
            //    File.Create(key);

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
                var key = Path.Combine(path, loginUser.Username, "Key.l8");

                if (!File.Exists(key))
                    File.Create(key);

                byte[] keyBytes = File.ReadAllBytes(key);

                CAes aes = new CAes(loginUser.Username, loginUser.Password);
                keyFile = JsonConvert.DeserializeObject<KeyFile>(aes.Decrypt(keyBytes, aes.iv, aes.key));
                loginUser.PrivateKey = keyFile.PrivateKey;
                loginUser.Mnemonic = keyFile.Mnemonic;
            }

            if (loginUser != null && !string.IsNullOrWhiteSpace(loginUser?.Username) && !string.IsNullOrWhiteSpace(loginUser?.PrivateKey))
            {
                return Task.Run(async () =>
                {
                    var b = await MainClient.AuthenticateAsync(loginUser);
                    headers = new()
                    {
                        { "Username", loginUser.Username },
                        { "PrivateKey", loginUser.PrivateKey }
                    };
                    isAuthenticated = b.IsAuthenticated;
                });
            }

            return Task.CompletedTask;
        }
    }
}

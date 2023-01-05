using lumine8.Aes;
using lumine8_GrpcService;
using lumine8_maui;
using Newtonsoft.Json;

namespace lumine8.Client.Identity
{
    public class AccountManager
    {
        MainProto.MainProtoClient MainClient;
        AuthenticationService authService;

        public LoginUser loginUser = new();
        public string privateKey = string.Empty;
        public CreateAccountResponse response = null;

        public string Message = "";
        public string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "lumine8");

        public AccountManager(MainProto.MainProtoClient MainClient, AuthenticationService authService)
        {
            this.MainClient = MainClient;
            this.authService = authService;
        }

        public async Task<bool> SignInAsync(LoginUser loginUser)
        {
            if (loginUser != null)
            {
                var b = await MainClient.AuthenticateAsync(loginUser);

                if (b.IsAuthenticated)
                {
                    await authService.InitializeAuthenticate();

                    var cred = Path.Combine(path, "Current.json");
                    File.WriteAllText(cred, JsonConvert.SerializeObject(loginUser));

                    return authService.isAuthenticated;
                }
            }

            Message = "User not authorized";
            return false;
        }

        public async Task<bool> SetupAccountAsync(LoginUser loginUser)
        {
            if (loginUser != null)
            {
                var b = await MainClient.AuthenticateAsync(loginUser);

                if (b.IsAuthenticated)
                {
                    var key = Path.Combine(path, loginUser.Username, "Key.json");
                    var dir = Path.Combine(path, loginUser.Username);
                    var cred = Path.Combine(path, "Current.json");

                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                        using (FileStream fs = File.Create(key)) { }
                    }

                    File.WriteAllText(cred, JsonConvert.SerializeObject(loginUser));

                    CAes aes = new CAes(loginUser.Password, loginUser.PrivateKey);
                    byte[] encrypt = aes.Encrypt(JsonConvert.SerializeObject(new KeyFile { Password = loginUser.Password, PrivateKey = loginUser.PrivateKey }), aes.iv, aes.key);
                    File.WriteAllBytes(key, encrypt);

                    await authService.InitializeAuthenticate();

                    return authService.isAuthenticated;
                }
            }

            Message = "User not authorized";
            return false;
        }

        public async Task SignOut(LoginUser loginUser)
        {
            if (File.Exists(Path.Combine(path, "Current.json")))
                File.Delete(Path.Combine(path, "Current.json"));

            this.loginUser = new();

            authService.Reset();
            await authService.InitializeAuthenticate();
        }

        public async Task<bool> SwitchAccountAsync(LoginUser loginUser)
        {
            var current = Path.Combine(path, "Current.json");

            if (!File.Exists(current))
                File.Create(current);

            CAes aes = new CAes(loginUser.Username, loginUser.Password);
            var f = aes.Decrypt(File.ReadAllBytes(Path.Combine(path, loginUser.Username, "Key.json")), aes.ConvertStringToKeyOrIV(loginUser.Username), aes.ConvertStringToKeyOrIV(loginUser.Password));

            var key = JsonConvert.DeserializeObject<KeyFile>(f);
            loginUser.Password = key.Password;
            loginUser.PrivateKey = key.PrivateKey;

            var b = await SignInAsync(loginUser);
            if (b)
            {
                loginUser.Password = string.Empty;
                loginUser.PrivateKey = string.Empty;
                File.WriteAllText(current, JsonConvert.SerializeObject(loginUser));
            }

            return b;
        }
    }
}

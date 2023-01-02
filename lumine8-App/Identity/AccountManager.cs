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

                    var cred = Path.Combine(path, "Current.l8");
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
                if (response != null)
                {
                    loginUser.Mnemonic = response.Mnemonic;
                    loginUser.PrivateKey = response.PrivateKey;
                }

                var b = await MainClient.AuthenticateAsync(loginUser);

                if (b.IsAuthenticated)
                {
                    var key = Path.Combine(path, loginUser.Username, "Key.l8");
                    var dir = Path.Combine(path, loginUser.Username);
                    var cred = Path.Combine(path, "Current.l8");

                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                        using (FileStream fs = File.Create(key)) { }
                    }

                    loginUser.Mnemonic = string.Empty;
                    loginUser.PrivateKey = string.Empty;
                    File.WriteAllText(cred, JsonConvert.SerializeObject(loginUser));

                    CAes aes = new CAes(loginUser.Username, loginUser.Password);
                    byte[] encrypt = aes.Encrypt(JsonConvert.SerializeObject(new KeyFile { PrivateKey = response.PrivateKey, Mnemonic = response.Mnemonic }), aes.iv, aes.key);
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
            if (File.Exists(Path.Combine(path, "Current.l8")))
                File.Delete(Path.Combine(path, "Current.l8"));

            this.loginUser = new();

            authService.Reset();
            await authService.InitializeAuthenticate();
        }

        public async Task<bool> SwitchAccountAsync(LoginUser loginUser)
        {
            var current = Path.Combine(path, "Current.l8");

            if (!File.Exists(current))
                File.Create(current);

            CAes aes = new CAes(loginUser.Username, loginUser.Password);
            var f = aes.Decrypt(File.ReadAllBytes(Path.Combine(path, loginUser.Username, "Key.l8")), aes.ConvertStringToKeyOrIV(loginUser.Username), aes.ConvertStringToKeyOrIV(loginUser.Password));

            var key = JsonConvert.DeserializeObject<KeyFile>(f);
            loginUser.Mnemonic = key.Mnemonic;
            loginUser.PrivateKey = key.PrivateKey;

            var b = await SignInAsync(loginUser);
            if (b)
            {
                loginUser.Mnemonic = string.Empty;
                loginUser.PrivateKey = string.Empty;
                File.WriteAllText(current, JsonConvert.SerializeObject(loginUser));
            }

            return b;
        }
    }
}

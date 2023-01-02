using Microsoft.JSInterop;

namespace lumine8.Client.Pages
{
    public class Mobile
    {
        private readonly IJSRuntime jSRuntime;
        public static bool mMobile;

        public Mobile(IJSRuntime jSRuntime)
        {
            this.jSRuntime = jSRuntime;
        }

        public bool isMobile()
        {
            return mMobile;
        }

        public async ValueTask Initialize()
        {
            if(OperatingSystem.IsBrowser())
                await jSRuntime.InvokeVoidAsync("CheckMobile");
            else
                mMobile = !(OperatingSystem.IsWindows() || OperatingSystem.IsMacOS());
        }

        [JSInvokable] public static void IsMobile(bool mobile) => mMobile = mobile;
    }
}

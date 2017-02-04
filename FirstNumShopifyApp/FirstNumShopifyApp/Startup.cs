using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(FirstNumShopifyApp.Startup))]
namespace FirstNumShopifyApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}

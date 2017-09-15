using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(VideoStreamingAspMvc.Startup))]
namespace VideoStreamingAspMvc
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}

using Autofac;
using CodeLocks.Locks;
using CodeLocks.UI;
using OpenMod.API.Plugins;

namespace CodeLocks
{
    public class ContainerConfigurator : IPluginContainerConfigurator
    {
        public void ConfigureContainer(IPluginServiceConfigurationContext context)
        {
            context.ContainerBuilder.RegisterType<ObjectManager>()
                .AsSelf()
                .SingleInstance();

            context.ContainerBuilder.RegisterType<CodeLockManager>()
                .AsSelf()
                .SingleInstance();

            context.ContainerBuilder.RegisterType<UIManager>()
                .AsSelf()
                .SingleInstance();
        }
    }
}

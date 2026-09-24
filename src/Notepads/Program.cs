// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Notepads.Services;
    using Notepads.Settings;
    using Windows.ApplicationModel;
    using Windows.ApplicationModel.Activation;

    public static class Program
    {
        static void Main(string[] args)
        {
            LoggingService.SafeLog("Program.Main entered. Args: " + (args != null ? string.Join(" ", args) : "none"));
            try
            {
                Task.Run(LoggingService.InitializeFileSystemLoggingAsync);

                IActivatedEventArgs activatedArgs = AppInstance.GetActivatedEventArgs();
                LoggingService.SafeLog("activatedArgs: " + (activatedArgs != null ? activatedArgs.GetType().FullName : "null"));

                if (activatedArgs is FileActivatedEventArgs)
                {
                    RedirectOrCreateNewInstance();
                }
                else if (activatedArgs is CommandLineActivatedEventArgs)
                {
                    RedirectOrCreateNewInstance();
                }
                else if (activatedArgs is ProtocolActivatedEventArgs protocolActivatedEventArgs)
                {
                    LoggingService.SafeLog($"[{nameof(Main)}] [ProtocolActivated] Protocol: {protocolActivatedEventArgs.Uri}");
                    var protocol = NotepadsProtocolService.GetOperationProtocol(protocolActivatedEventArgs.Uri, out _);
                    if (protocol == NotepadsOperationProtocol.OpenNewInstance)
                    {
                        OpenNewInstance();
                    }
                    else
                    {
                        RedirectOrCreateNewInstance();
                    }
                }
                else if (activatedArgs is LaunchActivatedEventArgs launchActivatedEventArgs)
                {
                    bool handled = false;

                    if (!string.IsNullOrEmpty(launchActivatedEventArgs.Arguments) && Uri.TryCreate(launchActivatedEventArgs.Arguments, UriKind.Absolute, out var parsedUri))
                    {
                        var protocol = NotepadsProtocolService.GetOperationProtocol(parsedUri, out _);
                        if (protocol == NotepadsOperationProtocol.OpenNewInstance)
                        {
                            handled = true;
                            OpenNewInstance();
                        }
                    }

                    if (!handled)
                    {
                        RedirectOrCreateNewInstance();
                    }
                }
                else
                {
                    RedirectOrCreateNewInstance();
                }
            }
            catch (Exception ex)
            {
                LoggingService.SafeLog("CRITICAL EXCEPTION IN Program.Main: " + ex);
                throw;
            }
        }

        private static void OpenNewInstance()
        {
            LoggingService.SafeLog("OpenNewInstance starting Application for App");
            AppInstance.FindOrRegisterInstanceForKey(App.InstanceId.ToString());
            Windows.UI.Xaml.Application.Start(p => new App());
        }

        private static void RedirectOrCreateNewInstance()
        {
            var instance = (GetLastActiveInstance() ?? AppInstance.FindOrRegisterInstanceForKey(App.InstanceId.ToString()));
            LoggingService.SafeLog($"RedirectOrCreateNewInstance: instance.IsCurrentInstance = {instance.IsCurrentInstance}");

            if (instance.IsCurrentInstance)
            {
                LoggingService.SafeLog("RedirectOrCreateNewInstance: calling Application.Start");
                Windows.UI.Xaml.Application.Start(p => new App());
            }
            else
            {
                // open new instance if user prefers to
                if (ApplicationSettingsStore.Read(SettingsKey.AlwaysOpenNewWindowBool) is bool alwaysOpenNewWindowBool && alwaysOpenNewWindowBool)
                {
                    OpenNewInstance();
                }
                else
                {
                    LoggingService.SafeLog("RedirectOrCreateNewInstance: redirecting to existing instance");
                    instance.RedirectActivationTo();
                }
            }
        }

        private static AppInstance GetLastActiveInstance()
        {
            var instances = AppInstance.GetInstances();

            if (instances.Count == 0)
            {
                return null;
            }
            else if (instances.Count == 1)
            {
                return instances.FirstOrDefault();
            }

            if (!(ApplicationSettingsStore.Read(SettingsKey.ActiveInstanceIdStr) is string activeInstance))
            {
                return null;
            }

            foreach (var appInstance in instances)
            {
                if (appInstance.Key == activeInstance)
                {
                    return appInstance;
                }
            }

            // activeInstance might be closed already, let's return the first instance in this case
            return instances.FirstOrDefault();
        }
    }
}
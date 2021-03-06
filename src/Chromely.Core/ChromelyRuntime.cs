using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Chromely.Core
{
    /// <summary>
    /// This class provides operating system and runtime information
    /// used to host the application.
    /// </summary>
    public static class ChromelyRuntime
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public static int GetExpectedChromiumBuildNumber(ChromelyCefWrapper wrapper)
        {
            try
            {
                switch (wrapper)
                {
                    case ChromelyCefWrapper.CefGlue:
                        return GetExpectedChromiumBuildNumberCefGlue(GetWrapperAssemblyName(wrapper));
                
                    case ChromelyCefWrapper.CefSharp:
                        return GetExpectedChromiumBuildNumberCefSharp(GetWrapperAssemblyName(wrapper));
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                // ignore
            }
            return 0;
        }

        private static int GetExpectedChromiumBuildNumberCefGlue(string dllName)
        {
            // for CefGlue use common assembly
            dllName = dllName
                .Replace(".CefGlue.Gtk.", ".CefGlue.")      
                .Replace(".CefGlue.Winapi.", ".CefGlue.");

            var assembly = Assembly.LoadFrom(dllName);
            var types = assembly.GetTypes();
            var type = types.FirstOrDefault(t => t.Name == "CefRuntime");
            var versionProperty = type?.GetProperty("ChromeVersion");
            var version = versionProperty?.GetValue(null).ToString();
            if (!string.IsNullOrEmpty(version)
                && int.TryParse(version.Split('.')[2], out var build))
            {
                return build;
            }
            return 0;
        }

        private static int GetExpectedChromiumBuildNumberCefSharp(string dllName)
        {
            var arch = RuntimeInformation.ProcessArchitecture.ToString();
            dllName = dllName
                .Replace("Chromely.CefSharp.Winapi", Path.Combine(arch, "CefSharp.Core"));

            var assembly = Assembly.LoadFrom(dllName);
            var types = assembly.GetTypes();
            var type = types.FirstOrDefault(t => t.Name == "Cef");
            var versionProperty = type?.GetProperty("CefVersion");
            var version = versionProperty?.GetValue(null).ToString();
            if (!string.IsNullOrEmpty(version)
                && int.TryParse(version.Split('.')[1], out var build))
            {
                return build;
            }
            return 0;
        }

        /// <summary>
        /// Gets the runtime the application is running on.
        /// </summary>
        public static ChromelyPlatform Platform
        {
            get
            {
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.MacOSX:
                        return ChromelyPlatform.MacOSX;
                    
                    case PlatformID.Unix:
                    case (PlatformID)128:   // Framework (1.0 and 1.1) didn't include any PlatformID value for Unix, so Mono used the value 128.
                        return IsRunningOnMac()
                        ? ChromelyPlatform.MacOSX
                        : ChromelyPlatform.Linux;

                    case PlatformID.Win32NT:
                    case PlatformID.Win32S:
                    case PlatformID.Win32Windows:
                    case PlatformID.WinCE:
                    case PlatformID.Xbox:
                        return ChromelyPlatform.Windows;

                    default:
                        return ChromelyPlatform.NotSupported;
                }
                
            }
        }
        

        private static bool IsRunningOnMac()
        {
            var osName = Environment.OSVersion.VersionString;
            return osName.ToLower().Contains("darwin");
        }

        private static string DefaultWrapperApi
        {
            get
            {
                switch (Platform)
                {
                    case ChromelyPlatform.Windows:
                        return "Winapi";
                    case ChromelyPlatform.Linux:
                        return "Gtk";
                    case ChromelyPlatform.MacOSX:
                        return "Libui";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Returns the name of the assembly dll
        /// containing wrapper specific implementation
        /// of 
        /// </summary>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public static string GetWrapperAssemblyName(ChromelyCefWrapper wrapper)
        {
            var coreAssembly = typeof(ChromelyRuntime).Assembly;
            var path = Path.GetDirectoryName(coreAssembly.Location) ?? ".";

            var wrapperApi = (wrapper == ChromelyCefWrapper.CefSharp)
                ? "Winapi"
                : DefaultWrapperApi;
            
            var dllName = Path.Combine(path, $"Chromely.{wrapper}.{wrapperApi}.dll");
            return dllName;
        }
        
        
        
    }
}
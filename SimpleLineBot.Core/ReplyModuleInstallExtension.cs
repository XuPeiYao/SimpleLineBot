using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Async;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SimpleLineBot.Core {
    public static class ReplyModuleInstallExtension {
        public static void InstallModules(
            this IServiceCollection services,
            string contentRootPath) {
            LineBotService.ContentRootPath = contentRootPath;

            #region Internal Module
            List<Type> result = new List<Type>();

            IMvcBuilder mvc = services.AddMvc();

            foreach (var assembly in Assembly
                .GetEntryAssembly()
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .Concat(new Assembly[] { Assembly.GetEntryAssembly() })) {

                mvc = mvc.AddApplicationPart(assembly);

                Type[] types = null;
                try {
                    types = assembly.GetTypes();

                } catch (ReflectionTypeLoadException e) {
                    Console.WriteLine("[FAIL LOAD]: " + assembly.FullName + ";" + e.InnerException);
                    types = e.Types;
                }
                result.AddRange(types);
            }

            LineBotService.ReplyModules = new List<(Type, bool)>();
            foreach (var type in result) {
                if (!type.GetInterfaces().Contains(typeof(ILineReplyModule))) continue;
                LineBotService.ReplyModules.Add((type, true));
                services.AddScoped(type);
            }
            #endregion

            var pluginsPath = Path.Combine(contentRootPath, "plugins");

            Console.WriteLine($"PluginsPath: {pluginsPath}");
            if (!Directory.Exists(pluginsPath)) {
                Console.WriteLine("建立plugins目錄");
                Directory.CreateDirectory(pluginsPath);
            }

            // plugin
            Directory.GetDirectories(pluginsPath).ParallelForEachAsync(async module => {
                Console.WriteLine("Install:" + module);
                foreach (var dll in Directory.GetFiles(module, "*.dll", SearchOption.AllDirectories)) {
                    System.Runtime.Loader.AssemblyLoadContext.Default
                        .LoadFromAssemblyPath(dll);
                }
                foreach (var dll in Directory.GetFiles(module, "*.dll", SearchOption.AllDirectories)) {
                    InstallReplayProcessFromDll(services, mvc, dll);
                }
            }).GetAwaiter().GetResult();

            mvc = mvc.AddControllersAsServices();

            // 停用模組
            using (var file = File.Open("disableModules.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read)) {
                var moduleNames = new StreamReader(file).ReadToEnd()?.Split(',') ?? new string[0];
                moduleNames = moduleNames.Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
                foreach (var moduleName in moduleNames) {
                    var moduleIndex = LineBotService.ReplyModules.FindIndex(x => x.type.Name.ToLower() == moduleName.ToLower());
                    LineBotService.ReplyModules[moduleIndex] = (LineBotService.ReplyModules[moduleIndex].type, false);
                }
            }
        }

        private static void InstallReplayProcessFromDll(IServiceCollection services, IMvcBuilder mvc, string dllPath) {
            var assembly = Assembly.LoadFile(dllPath);

            mvc = mvc.AddApplicationPart(assembly);

            foreach (var type in assembly.GetTypes()) {
                if (!type.GetInterfaces().Contains(typeof(ILineReplyModule))) continue;

                lock (LineBotService.ReplyModules) {
                    LineBotService.ReplyModules.Add((type, true));
                    services.AddScoped(type);
                }
            }
        }
    }
}

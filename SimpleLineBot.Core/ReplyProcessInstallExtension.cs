﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SimpleLineBot.Core {
    public static class ReplyProcessInstallExtension {
        public static void InstallReplyProcess(
            this IServiceCollection services,
            string contentRootPath) {
            #region Internal Module
            List<Type> result = new List<Type>();

            foreach (var assembly in Assembly
                .GetEntryAssembly()
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .Concat(new Assembly[] { Assembly.GetEntryAssembly() })) {
                Type[] types = null;
                try {
                    types = assembly.GetTypes();

                } catch (ReflectionTypeLoadException e) {
                    Console.WriteLine("[FAIL LOAD]: " + assembly.FullName + ";" + e.InnerException);
                    types = e.Types;
                }
                result.AddRange(types);
            }

            LineBotService.ReplyProcesses = new List<Type>();
            foreach (var type in result) {
                if (!type.GetInterfaces().Contains(typeof(ILineReplyProcess))) continue;
                LineBotService.ReplyProcesses.Add(type);
                services.AddScoped(type);
            }
            #endregion

            var pluginsPath = Path.Combine(contentRootPath, "plugins");

            if (!Directory.Exists(pluginsPath)) {
                Directory.CreateDirectory(pluginsPath);
            }

            // plugin
            foreach (var dll in Directory.GetFiles(pluginsPath, "*.dll", SearchOption.AllDirectories)) {
                InstallReplayProcessFromDll(services, dll);
            }
        }

        private static void InstallReplayProcessFromDll(IServiceCollection services, string dllPath) {
            foreach (var type in Assembly.LoadFile(dllPath).GetTypes()) {
                if (!type.GetInterfaces().Contains(typeof(ILineReplyProcess))) continue;

                LineBotService.ReplyProcesses.Add(type);
                services.AddScoped(type);
            }
        }
    }
}
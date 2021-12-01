﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.Application.Config;
using PackageUploader.Application.Extensions;
using PackageUploader.Application.Operations;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using PackageUploader.FileLogger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;

namespace PackageUploader.Application
{
    internal class Program
    {
        private const string LogTimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";

        // Options
        private static readonly Option<bool> VerboseOption = new(new[] { "-v", "--Verbose" }, "Log verbose messages such as http calls");
        private static readonly Option<FileInfo> LogFileOption = new(new[] { "-l", "--LogFile" }, "Location of the log file");
        private static readonly Option<string> ClientSecretOption = new(new[] { "-s", "--ClientSecret" }, "Client secret of the AAD app (only for AppSecret)");
        internal static readonly Option<FileInfo> ConfigFileOption = new Option<FileInfo>(new[] { "-c", "--ConfigFile" }, "Location of the config file").Required();
        private static readonly Option<ConfigFileFormat> ConfigFileFormatOption = new(new[] { "-f", "--ConfigFileFormat" }, () => ConfigFileFormat.Json, "Format of the config file");
        private static readonly Option<IngestionExtensions.AuthenticationMethod> AuthenticationMethodOption = new(new[] { "-a", "--Authentication" }, () => IngestionExtensions.AuthenticationMethod.AppSecret, "Authentication method");
        internal static readonly Option<bool> OverwriteOption = new(new[] { "-o", "--Overwrite" }, "Overwrite file");
        private static readonly Command NewCommand = new Command("New", "Generate config template file") { OverwriteOption, }.AddOperationHandler<GenerateConfigTemplateOperation>();
        private static readonly Command ValidateConfigCommand = new Command("ValidateConfig", "Validate Json config file against the schema") { ConfigFileOption, }.AddOperationHandler<ValidateConfigOperation>();

        internal enum ConfigFileFormat { Json, Xml, Ini, }

        private static async Task<int> Main(string[] args)
        {
            return await BuildCommandLine()
                .UseHost(hostBuilder => hostBuilder
                    .ConfigureLogging(ConfigureLogging)
                    .ConfigureServices(ConfigureServices)
                    .ConfigureAppConfiguration(ConfigureAppConfiguration)
                )
                .UseDefaults()
                .Build()
                .InvokeAsync(args)
                .ConfigureAwait(false);
        }

        private static void ConfigureLogging(HostBuilderContext context, ILoggingBuilder logging)
        {
            var invocationContext = context.GetInvocationContext();
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Warning);
            logging.AddFilter("PackageUploader", invocationContext.GetOptionValue(VerboseOption) ? LogLevel.Trace : LogLevel.Information);
            logging.AddSimpleFile(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = LogTimestampFormat;
            }, file =>
            {
                var logFile = invocationContext.GetOptionValue(LogFileOption);
                file.Path = logFile?.FullName ?? Path.Combine(Path.GetTempPath(), $"PackageUploader_{DateTime.Now:yyyyMMddHHmmss}.log");
                file.Append = true;
            });
            logging.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = LogTimestampFormat;
            });
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            var invocationContext = context.GetInvocationContext();

            services.AddLogging();
            services.AddPackageUploaderService(context.Configuration, invocationContext.GetOptionValue(AuthenticationMethodOption));

            services.AddOperation<GetProductOperation, GetProductOperationConfig>(context);
            services.AddOperation<UploadUwpPackageOperation, UploadUwpPackageOperationConfig>(context);
            services.AddOperation<UploadXvcPackageOperation, UploadXvcPackageOperationConfig>(context);
            services.AddOperation<RemovePackagesOperation, RemovePackagesOperationConfig>(context);
            services.AddOperation<ImportPackagesOperation, ImportPackagesOperationConfig>(context);
            services.AddOperation<PublishPackagesOperation, PublishPackagesOperationConfig>(context);
            services.AddOperation<GenerateConfigTemplateOperation>();
            services.AddOperation<ValidateConfigOperation>();
        }

        private static void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder builder)
        {
            var invocationContext = context.GetInvocationContext();

            var command = invocationContext.ParseResult.CommandResult.Command;
            if (command != ValidateConfigCommand)
            {
                var configFile = invocationContext.GetOptionValue(ConfigFileOption);
                if (configFile is not null)
                {
                    var configFileFormat = invocationContext.GetOptionValue(ConfigFileFormatOption);
                    builder.AddConfigFile(configFile, configFileFormat);
                }
            }

            var authenticationMethod = invocationContext.GetOptionValue(AuthenticationMethodOption);
            if (authenticationMethod is IngestionExtensions.AuthenticationMethod.AppSecret)
            {
                var clientSecret = invocationContext.GetOptionValue(ClientSecretOption);
                var inMemoryValues = new Dictionary<string, string>();
                inMemoryValues.TryAdd($"{AadAuthInfo.ConfigName}:{nameof(AzureApplicationSecretAuthInfo.ClientSecret)}", clientSecret);
                builder.AddInMemoryCollection(inMemoryValues);
            }
        }

        private static CommandLineBuilder BuildCommandLine()
        {
            var rootCommand = new RootCommand
            {
                new Command(OperationName.GetProduct.ToString(), "Gets metadata of the product")
                {
                    ConfigFileOption, ConfigFileFormatOption, ClientSecretOption, AuthenticationMethodOption, NewCommand,
                }.AddOperationHandler<GetProductOperation>(),
                new Command(OperationName.UploadUwpPackage.ToString(), "Uploads Uwp game package")
                {
                    ConfigFileOption, ConfigFileFormatOption, ClientSecretOption, AuthenticationMethodOption, NewCommand,
                }.AddOperationHandler<UploadUwpPackageOperation>(),
                new Command(OperationName.UploadXvcPackage.ToString(), "Uploads Xvc game package and assets")
                {
                    ConfigFileOption, ConfigFileFormatOption, ClientSecretOption, AuthenticationMethodOption, NewCommand,
                }.AddOperationHandler<UploadXvcPackageOperation>(),
                new Command(OperationName.RemovePackages.ToString(), "Removes all game packages and assets from a branch")
                {
                    ConfigFileOption, ConfigFileFormatOption, ClientSecretOption, AuthenticationMethodOption, NewCommand,
                }.AddOperationHandler<RemovePackagesOperation>(),
                new Command(OperationName.ImportPackages.ToString(), "Imports all game packages from a branch to a destination branch")
                {
                    ConfigFileOption, ConfigFileFormatOption, ClientSecretOption, AuthenticationMethodOption, NewCommand,
                }.AddOperationHandler<ImportPackagesOperation>(),
                new Command(OperationName.PublishPackages.ToString(), "Publishes all game packages from a branch or flight to a destination sandbox or flight")
                {
                    ConfigFileOption, ConfigFileFormatOption, ClientSecretOption, AuthenticationMethodOption, NewCommand,
                }.AddOperationHandler<PublishPackagesOperation>(),
                //ValidateConfigCommand,
            };
            rootCommand.AddGlobalOption(VerboseOption);
            rootCommand.AddGlobalOption(LogFileOption);
            rootCommand.Description = "Application that enables game developers to upload Xbox and PC game packages to Partner Center";
            return new CommandLineBuilder(rootCommand);
        }
    }
}
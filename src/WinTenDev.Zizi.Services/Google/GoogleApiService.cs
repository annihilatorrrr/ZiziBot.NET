﻿using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Cloud.Vision.V1;
using Microsoft.Extensions.Options;
using Serilog;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Utils.IO;

namespace WinTenDev.Zizi.Services.Google;

/// <summary>
/// This class is used for Google Apis services
/// </summary>
public class GoogleApiService
{
    private readonly GoogleCloudConfig _googleCloudConfig;

    /// <summary>
    /// Constructor for GoogleApiService
    /// </summary>
    /// <param name="googleCloudConfig"></param>
    public GoogleApiService(
        IOptionsSnapshot<GoogleCloudConfig> googleCloudConfig
    )
    {
        _googleCloudConfig = googleCloudConfig.Value;
    }

    /// <summary>
    /// This function is used to create a new instance of ImageAnnotatorClient
    /// </summary>
    /// <returns></returns>
    public ImageAnnotatorClient CreateImageAnnotatorClient()
    {
        var credPath = _googleCloudConfig.CredentialsPath;

        Log.Information("Instantiates a client, cred {CredPath}", credPath);
        var clientBuilder = new ImageAnnotatorClientBuilder
        {
            CredentialsPath = credPath
        };

        var client = clientBuilder.Build();

        return client;
    }

    /// <summary>
    /// This function is used to create a new instance of Drive client service
    /// </summary>
    /// <returns></returns>
    public async Task<DriveService> CreateDriveClientAsync()
    {
        var driveAuth = _googleCloudConfig.DriveAuth;
        var credPath = Path.Combine("Storage", "Common", "gdrive-auth-token-store").SanitizeSlash().EnsureDirectory();
        var secrets = await GoogleClientSecrets.FromFileAsync(driveAuth);

        var fileDataStore = new FileDataStore(credPath, true);
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
        secrets.Secrets,
        new[]
        {
            DriveService.Scope.Drive,
            DriveService.Scope.DriveFile
        },
        "user",
        CancellationToken.None,
        fileDataStore);

        Log.Debug("Credential saved to {CredPath}", credPath);

        Log.Debug("Initializing Drive service");
        var service = new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "ZiziBot"
        });

        return service;
    }
}
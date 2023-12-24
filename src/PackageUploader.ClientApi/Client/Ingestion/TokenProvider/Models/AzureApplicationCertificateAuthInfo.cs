// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;

[OptionsValidator]
internal partial class AzureApplicationCertificateAuthInfoValidator : IValidateOptions<AzureApplicationCertificateAuthInfo>
{ }

public sealed class AzureApplicationCertificateAuthInfo : AadAuthInfo
{
    [Required]
    public string CertificateStore { get; set; } = "My";

    [Required]
    public StoreLocation CertificateLocation { get; set; } = StoreLocation.CurrentUser;

    [Required]
    public string CertificateThumbprint { get; set; }
}
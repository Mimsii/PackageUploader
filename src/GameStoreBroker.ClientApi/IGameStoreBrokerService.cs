﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion.Models;
using GameStoreBroker.ClientApi.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi
{
    public interface IGameStoreBrokerService
    {
        Task<GameProduct> GetProductByBigIdAsync(string bigId, CancellationToken ct);
        Task<GameProduct> GetProductByProductIdAsync(string productId, CancellationToken ct);
        Task<GamePackageBranch> GetPackageBranchByFriendlyNameAsync(GameProduct product, string branchFriendlyName, CancellationToken ct);
        Task<GamePackageBranch> GetPackageBranchByFlightNameAsync(GameProduct product, string flightName, CancellationToken ct);
        Task<GamePackage> UploadGamePackageAsync(GameProduct product, GamePackageBranch packageBranch, string marketGroupId, string packageFilePath, GameAssets gameAssets, int minutesToWaitForProcessing, CancellationToken ct);
        Task RemovePackagesAsync(GameProduct product, GamePackageBranch packageBranch, string marketGroupId, CancellationToken ct);
        Task SetXvcAvailabilityDateAsync(GameProduct product, GamePackageBranch packageBranch, GamePackage gamePackage, string marketGroupId, DateTime? availabilityDate, CancellationToken ct);
        Task SetXvcAvailabilityDateAsync(GameProduct product, GamePackageBranch packageBranch, string marketGroupId, DateTime? availabilityDate, CancellationToken ct);
        Task SetUwpAvailabilityDateAsync(GameProduct product, GamePackageBranch packageBranch, string marketGroupId, DateTime? availabilityDate, CancellationToken ct);
        Task SetUwpMandatoryDateAsync(GameProduct product, GamePackageBranch packageBranch, string marketGroupId, DateTime? mandatoryDate, CancellationToken ct);
        Task ImportPackagesAsync(GameProduct product, GamePackageBranch originPackageBranch, GamePackageBranch destinationPackageBranch, string marketGroupId, bool overwrite, CancellationToken ct);
    }
}
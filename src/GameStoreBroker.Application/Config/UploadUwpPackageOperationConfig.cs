// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion.Models;
using GameStoreBroker.ClientApi.Models;

namespace GameStoreBroker.Application.Config
{
    internal class UploadUwpPackageOperationConfig : UploadPackageOperationConfig, IGameConfiguration
    {
        internal override string GetOperationName() => "UploadUwpPackage";

        public GamePackageDate MandatoryDate { get; set; }
        public GameGradualRolloutInfo GradualRollout { get; set; }
    }
}
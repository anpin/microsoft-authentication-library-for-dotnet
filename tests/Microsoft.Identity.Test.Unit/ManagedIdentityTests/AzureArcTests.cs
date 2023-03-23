﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    [DeploymentItem("Resources\\ManagedIdentityAzureArcSecret.txt")]
    public class AzureArcTests : TestBase
    {
        private const string AzureArc = "Azure Arc";

        [DataTestMethod]
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)]
        [DataRow("resourceId", UserAssignedIdentityId.ResourceId)]
        public async Task AzureArcUserAssignedManagedIdentityNotSupportedAsync(string userAssignedClientId, UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySourceType.AzureArc, ManagedIdentityTests.AzureArcEndpoint);

                IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder.Create(userAssignedClientId)
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .Build();

                MsalClientException ex = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                    await mi.AcquireTokenForManagedIdentity("scope")
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.UserAssignedManagedIdentityNotSupported, ex.ErrorCode);
                Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityUserAssignedNotSupported, AzureArc), ex.Message);
            }
        }

        [TestMethod]
        public async Task AzureArcAuthHeaderMissingAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySourceType.AzureArc, ManagedIdentityTests.AzureArcEndpoint);

                IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder.Create()
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .Build();

                httpManager.AddManagedIdentityWSTrustMockHandler(ManagedIdentityTests.AzureArcEndpoint);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity("scope")
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.ManagedIdentityNoChallengeError, ex.Message);
            }
        }

        [TestMethod]
        public async Task AzureArcAuthHeaderInvalidAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySourceType.AzureArc, ManagedIdentityTests.AzureArcEndpoint);

                IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder.Create()
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .Build();

                httpManager.AddManagedIdentityWSTrustMockHandler(ManagedIdentityTests.AzureArcEndpoint, "somevalue=filepath");

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity("scope")
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.ManagedIdentityInvalidChallenge, ex.Message);
            }
        }

        [TestMethod]
        public async Task AzureArcInvalidEndpointAsync()
        {
            using(new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySourceType.AzureArc, "localhost/token");

                IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder.Create()
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .Build();

                MsalClientException ex = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.InvalidManagedIdentityEndpoint, ex.ErrorCode);
                Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityEndpointInvalidUriError, "IDENTITY_ENDPOINT", "localhost/token", AzureArc), ex.Message);
            }
        }
    }
}
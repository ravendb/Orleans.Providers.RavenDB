﻿using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using Orleans.Providers.RavenDB.StorageProviders;
using Orleans.TestingHost;
using Raven.Client.Documents;
using Raven.Client.ServerWide.Operations;
using Raven.Embedded;
using TestExtensions;
using TestExtensions.Runners;
using UnitTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests
{
    //[TestCategory("Persistence"), TestCategory("RavenDB")]
    public class PersistenceGrainTests_RavenDBGrainStorage : OrleansTestingBase, IClassFixture<RavenDbStorageFixture>
    {
        public const string TestDatabaseName = "TestDatabase";
        private readonly GrainPersistenceTestsRunner _runner;


        public PersistenceGrainTests_RavenDBGrainStorage(ITestOutputHelper output, RavenDbStorageFixture fixture, string grainNamespace = "UnitTests.Grains")
        {
            fixture.EnsurePreconditionsMet();

            _runner = new GrainPersistenceTestsRunner(output, fixture, grainNamespace);
        }


        [SkippableFact, TestCategory("Functional")]
        public async Task Grain_RavenGrainStorage_Delete() => await _runner.Grain_GrainStorage_Delete();

        [SkippableFact, TestCategory("Functional")]
        public async Task Grain_RavenGrainStorage_Read() => await _runner.Grain_GrainStorage_Read();

        [SkippableFact, TestCategory("Functional")]
        public async Task Grain_GuidKey_RavenGrainStorage_Read_Write() => await _runner.Grain_GuidKey_GrainStorage_Read_Write();

        [SkippableFact, TestCategory("Functional")]
        public async Task Grain_LongKey_RavenGrainStorage_Read_Write() => await _runner.Grain_LongKey_GrainStorage_Read_Write();

        [SkippableFact, TestCategory("Functional")]
        public async Task Grain_LongKeyExtended_RavenGrainStorage_Read_Write() => await _runner.Grain_LongKeyExtended_GrainStorage_Read_Write();

        [SkippableFact, TestCategory("Functional")]
        public async Task Grain_GuidKeyExtended_RavenGrainStorage_Read_Write() => await _runner.Grain_GuidKeyExtended_GrainStorage_Read_Write();

        [SkippableFact, TestCategory("Functional")]
        public async Task Grain_Generic_RavenGrainStorage_Read_Write() => await _runner.Grain_Generic_GrainStorage_Read_Write();

        [SkippableFact, TestCategory("Functional")]
        public async Task Grain_NestedGeneric_RavenGrainStorage_Read_Write() => await _runner.Grain_NestedGeneric_GrainStorage_Read_Write();

        [SkippableFact, TestCategory("Functional")]
        public async Task Grain_Generic_RavenGrainStorage_DiffTypes() => await _runner.Grain_Generic_GrainStorage_DiffTypes();

        [SkippableFact, TestCategory("Functional")]
        public async Task Grain_RavenGrainStorage_SiloRestart() => await _runner.Grain_GrainStorage_SiloRestart();

    }

}
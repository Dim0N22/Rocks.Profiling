﻿using System.Threading.Tasks;
using NSubstitute;
using Ploeh.AutoFixture;
using Rocks.Profiling.Internal;
using Rocks.Profiling.Internal.Implementation;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Rocks.Profiling.Tests.Internal.Implementation
{
    public class CompletedSessionsProcessorQueueTests
    {
        [Fact]
        public async Task Add_ProcessCompletedSession()
        {
            // arrange
            var fixture = new FixtureBuilder().Build();

            var completed_session_info = fixture.Create<CompletedSessionInfo>();

            var processor_service = fixture.Freeze<ICompletedSessionProcessorService>();
            processor_service.ShouldProcess(null).ReturnsForAnyArgs(true);


            // act
            fixture.Create<CompletedSessionsProcessorQueue>().Add(completed_session_info);


            // assert
            await Task.Delay(100).ConfigureAwait(false); // wait background processing task
            processor_service.Received(1).Process(completed_session_info);
        }
    }
}
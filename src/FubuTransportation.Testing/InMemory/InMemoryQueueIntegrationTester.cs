﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FubuCore;
using FubuTransportation.Configuration;
using FubuTransportation.InMemory;
using FubuTransportation.Runtime;
using FubuTransportation.Scheduling;
using NUnit.Framework;
using System.Linq;
using FubuTestingSupport;
using TaskScheduler = FubuTransportation.Scheduling.TaskScheduler;

namespace FubuTransportation.Testing.InMemory
{
    [TestFixture]
    public class InMemoryQueueIntegrationTester
    {
        [SetUp]
        public void SetUp()
        {
            InMemoryQueueManager.ClearAll();
        }

        [TearDown]
        public void Teardown()
        {
            InMemoryQueueManager.ClearAll();
        }

        [Test]
        public void can_round_trip_an_envelope_through_the_queue()
        {
            var envelope = new EnvelopeToken();
            envelope.CorrelationId = Guid.NewGuid().ToString();
            envelope.Headers["Foo"] = "Bar";
            envelope.Data = new byte[]{1,2,3,4,5};

            var queue = InMemoryQueueManager.QueueFor(new Uri("memory://foo"));

            var receiver = new RecordingReceiver();
            var task = Task.Factory.StartNew(() => queue.Receive(receiver));

            queue.Enqueue(envelope);

            Wait.Until(() => receiver.Received.Any(), timeoutInMilliseconds:2000);

            var received = receiver.Received.Single();

            received.CorrelationId.ShouldEqual(envelope.CorrelationId);
            received.ContentType.ShouldEqual(envelope.ContentType);
            received.Data.ShouldEqual(envelope.Data);
            task.SafeDispose();
        }

        [Test]
        public void create_from_graph_and_run_through_the_channel()
        {
            using (var graph = new ChannelGraph())
            {
                var node = graph.ChannelFor<BusSettings>(x => x.Outbound);

                node.Uri = new Uri("memory://foo");

                var transport = new InMemoryTransport();
                transport.OpenChannels(graph);
                node.Channel.ShouldNotBeNull();

                var envelope = new Envelope();
                envelope.CorrelationId = Guid.NewGuid().ToString();
                envelope.Headers["Foo"] = "Bar";
                envelope.Data = new byte[] {1, 2, 3, 4, 5};

                var receiver = new RecordingReceiver();

                node.StartReceiving(receiver);

                node.Channel.Send(envelope.Data, envelope.Headers);

                Wait.Until(() => receiver.Received.Any(), timeoutInMilliseconds: 2000);

                var received = receiver.Received.Single();

                received.CorrelationId.ShouldEqual(envelope.CorrelationId);
                received.ContentType.ShouldEqual(envelope.ContentType);
                received.Data.ShouldEqual(envelope.Data);

            }
        }
    }

    
}
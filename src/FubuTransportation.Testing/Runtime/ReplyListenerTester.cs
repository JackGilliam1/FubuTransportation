﻿using System;
using System.Linq;
using System.Threading.Tasks;
using FubuCore;
using FubuTransportation.Events;
using FubuTransportation.Logging;
using FubuTransportation.Runtime;
using NUnit.Framework;
using Rhino.Mocks;
using FubuTestingSupport;

namespace FubuTransportation.Testing.Runtime
{
    [TestFixture]
    public class ReplayListener_expiration_logic_Tester
    {
        [Test]
        public void uses_the_expiration_time()
        {
            var listener = new ReplyListener<Events.Message1>(null, Guid.NewGuid().ToString(), 10.Minutes());

            listener.IsExpired.ShouldBeFalse();

            listener.ExpiresAt.ShouldNotBeNull();
            (listener.ExpiresAt > DateTime.UtcNow.AddMinutes(9)).ShouldBeTrue();
            (listener.ExpiresAt < DateTime.UtcNow.AddMinutes(11)).ShouldBeTrue();
        }
    }

    [TestFixture]
    public class when_receiving_a_matching_failure_ack
    {
        private IEventAggregator theEvents;
        public readonly string correlationId = Guid.NewGuid().ToString();
        private ReplyListener<Events.Message1> theListener;

        [SetUp]
        public void SetUp()
        {
            theEvents = MockRepository.GenerateMock<IEventAggregator>();

            theListener = new ReplyListener<Events.Message1>(theEvents, correlationId, 10.Minutes());

            var envelope = new EnvelopeToken
            {
                Message = new FailureAcknowledgement
                {
                    CorrelationId = correlationId,
                    Message = "No soup for you!"
                }
            };

            envelope.Headers[Envelope.ResponseIdKey] = correlationId;

            theListener.Handle(new EnvelopeReceived
            {
                Envelope = envelope
            });
        }

        [Test]
        public void the_listener_should_set_a_failure_on_the_task()
        {
            theListener.Task.Exception
                .Flatten()
                .InnerExceptions.Single()
                .ShouldBeOfType<ReplyFailureException>()
                .Message.ShouldEqual("No soup for you!");
        }

        [Test]
        public void should_remove_itself_from_the_event_aggregator()
        {
            theEvents.AssertWasCalled(x => x.RemoveListener(theListener));
        }

        [Test]
        public void should_be_expired()
        {
            theListener.IsExpired.ShouldBeTrue();
        }
    }

    [TestFixture]
    public class when_receiving_a_failure_ack_that_does_not_match
    {
        private IEventAggregator theEvents;
        public readonly string correlationId = Guid.NewGuid().ToString();
        private ReplyListener<Events.Message1> theListener;

        [SetUp]
        public void SetUp()
        {
            theEvents = MockRepository.GenerateMock<IEventAggregator>();

            theListener = new ReplyListener<Events.Message1>(theEvents, correlationId, 10.Minutes());

            var envelope = new EnvelopeToken
            {
                Message = new FailureAcknowledgement
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    Message = "No soup for you!"
                }
            };

            envelope.Headers[Envelope.ResponseIdKey] = correlationId;

            theListener.Handle(new EnvelopeReceived
            {
                Envelope = envelope
            });
        }

        [Test]
        public void the_listener_should_not_be_expired()
        {
            theListener.IsExpired.ShouldBeFalse();
        }

        [Test]
        public void the_listener_should_not_complete_the_task_in_any_way()
        {
            theListener.Task.IsCompleted.ShouldBeFalse();
            theListener.Task.IsFaulted.ShouldBeFalse();
        }

        [Test]
        public void should_NOT_remove_itself_from_the_event_aggregator()
        {
            theEvents.AssertWasNotCalled(x => x.RemoveListener(theListener));
        }
    }


    [TestFixture]
    public class when_receiving_the_matching_reply
    {
        private IEventAggregator theEvents;
        public readonly string correlationId = Guid.NewGuid().ToString();
        private ReplyListener<Events.Message1> theListener;
        private Events.Message1 theMessage;

        [SetUp]
        public void SetUp()
        {
            theEvents = MockRepository.GenerateMock<IEventAggregator>();

            theListener = new ReplyListener<Events.Message1>(theEvents, correlationId, 10.Minutes());

            theMessage = new Events.Message1();
            
            var envelope = new EnvelopeToken
            {
                Message = theMessage
            };

            envelope.Headers[Envelope.ResponseIdKey] = correlationId;

            theListener.Handle(new EnvelopeReceived
            {
                Envelope = envelope
            });
        }

        [Test]
        public void should_set_the_completion_value()
        {
            theListener.Task.Result.ShouldBeTheSameAs(theMessage);
        }

        [Test]
        public void should_remove_itself_from_the_event_aggregator()
        {
            theEvents.AssertWasCalled(x => x.RemoveListener(theListener));
        }
    }

    [TestFixture]
    public class ReplyListenerMatchesTester
    {
        private IEventAggregator theEvents;
        public readonly string correlationId = Guid.NewGuid().ToString();
        private ReplyListener<Events.Message1> theListener;

        [SetUp]
        public void SetUp()
        {
            theEvents = MockRepository.GenerateMock<IEventAggregator>();
            theListener = new ReplyListener<Events.Message1>(theEvents, correlationId, 10.Minutes());
        }

        [Test]
        public void matches_if_type_is_right_and_correlation_id_matches()
        {
            theListener.Matches(new EnvelopeToken
            {
                ResponseId = correlationId,
                Message = new Events.Message1()
            }).ShouldBeTrue();
        }

        [Test]
        public void does_not_match_if_correlation_id_is_wrong()
        {
            theListener.Matches(new EnvelopeToken
            {
                ResponseId = Guid.NewGuid().ToString(),
                Message = new Events.Message1()
            }).ShouldBeFalse();
        }

        [Test]
        public void does_not_match_if_the_message_type_is_wrong()
        {
            theListener.Matches(new EnvelopeToken
            {
                ResponseId = correlationId,
                Message = new Events.Message2()
            }).ShouldBeFalse();
        }
    }
}
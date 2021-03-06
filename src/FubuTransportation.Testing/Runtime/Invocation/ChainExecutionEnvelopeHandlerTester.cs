﻿using System;
using FubuTestingSupport;
using FubuTransportation.Async;
using FubuTransportation.Configuration;
using FubuTransportation.Registration.Nodes;
using FubuTransportation.Runtime;
using FubuTransportation.Runtime.Invocation;
using FubuTransportation.Runtime.Serializers;
using FubuTransportation.Testing.Events;
using NUnit.Framework;
using Rhino.Mocks;

namespace FubuTransportation.Testing.Runtime.Invocation
{
    [TestFixture]
    public class when_the_chain_does_not_exist : InteractionContext<ChainExecutionEnvelopeHandler>
    {
        private Envelope theEnvelope;
        private IChainInvoker theInvoker;

        protected override void beforeEach()
        {
            theEnvelope = ObjectMother.Envelope();
            theInvoker = MockFor<IChainInvoker>();

            theInvoker.Stub(x => x.FindChain(theEnvelope))
                      .Return(null);
        }

        [Test]
        public void should_not_return_any_continuation()
        {
            ClassUnderTest.Handle(theEnvelope).ShouldBeNull();
        }
    }


    [TestFixture]
    public class when_there_is_a_chain : InteractionContext<ChainExecutionEnvelopeHandler>
    {
        private Envelope theEnvelope;
        private IChainInvoker theInvoker;
        private HandlerChain theChain;

        protected override void beforeEach()
        {
            theEnvelope = ObjectMother.Envelope();
            theInvoker = MockFor<IChainInvoker>();
            theChain = new HandlerChain();

            theInvoker.Stub(x => x.FindChain(theEnvelope))
                      .Return(theChain);
        }

        [Test]
        public void if_the_chain_invocation_succeeds_and_there_is_no_explicit_continuation_use_successful_continuation()
        {
            theInvoker.Expect(x => x.ExecuteChain(theEnvelope, theChain))
                      .Return(MockFor<IInvocationContext>());

            MockFor<IInvocationContext>().Stub(x => x.Continuation).Return(null);


            ClassUnderTest.Handle(theEnvelope)
                          .ShouldBeOfType<ChainSuccessContinuation>()
                          .Context.ShouldBeTheSameAs(MockFor<IInvocationContext>());

        }

        [Test]
        public void if_the_chain_invocation_succeeds_and_there_is_an_explicit_continuation()
        {
            theInvoker.Expect(x => x.ExecuteChain(theEnvelope, theChain))
                      .Return(MockFor<IInvocationContext>());

            var explicitContinuation = MockRepository.GenerateMock<IContinuation>();
            MockFor<IInvocationContext>().Stub(x => x.Continuation).Return(explicitContinuation);


            ClassUnderTest.Handle(theEnvelope)
                          .ShouldBeTheSameAs(explicitContinuation);
        }

        [Test]
        public void if_the_chain_invocation_blows_up_return_a_chain_failure_continuation()
        {
            var exception = new NotImplementedException();

            theInvoker.Expect(x => x.ExecuteChain(theEnvelope, theChain))
                      .Throw(exception);

            ClassUnderTest.Handle(theEnvelope)
                          .ShouldBeOfType<ChainFailureContinuation>()
                          .Exception.ShouldBeTheSameAs(exception);

        }

        [Test]
        public void if_the_chain_throws_a_deserialization_continuation()
        {
            var exception = new EnvelopeDeserializationException("I blew up!");

            theInvoker.Expect(x => x.ExecuteChain(theEnvelope, theChain))
                .Throw(exception);

            ClassUnderTest.Handle(theEnvelope)
                .ShouldBeOfType<DeserializationFailureContinuation>()
                .Exception.ShouldBeTheSameAs(exception);
        }
    }



    [TestFixture]
    public class determining_the_continuation_for_an_async_chain : InteractionContext<ChainExecutionEnvelopeHandler>
    {
        private Envelope theEnvelope;
        private IChainInvoker theInvoker;
        private HandlerChain theChain;
        private IContinuation theContinuation;

        protected override void beforeEach()
        {
            theEnvelope = ObjectMother.Envelope();
            theInvoker = MockFor<IChainInvoker>();
            theChain = new HandlerChain();
            theChain.AddToEnd(HandlerCall.For<TaskHandler>(x => x.AsyncHandle(null)));
            theChain.IsAsync.ShouldBeTrue();

            theInvoker.Stub(x => x.FindChain(theEnvelope))
                      .Return(theChain);

            theContinuation = ClassUnderTest.Handle(theEnvelope);
        }

        [Test]
        public void the_continuation_should_be_async()
        {
            theContinuation.ShouldBeOfType<AsyncChainExecutionContinuation>();
        }


    }
}
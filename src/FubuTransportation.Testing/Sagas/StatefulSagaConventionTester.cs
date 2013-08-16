﻿using System;
using FubuMVC.Core.Registration.Nodes;
using FubuTransportation.Configuration;
using FubuTransportation.Registration.Nodes;
using FubuTransportation.Sagas;
using FubuTransportation.Testing.ScenarioSupport;
using NUnit.Framework;
using FubuTestingSupport;

namespace FubuTransportation.Testing.Sagas
{
    [TestFixture]
    public class StatefulSagaConventionTester
    {
        [Test]
        public void is_saga_handler_positive()
        {
            StatefulSagaConvention.IsSagaHandler(HandlerCall.For<SimpleSagaHandler>(x => x.Start(null)))
                .ShouldBeTrue();

            StatefulSagaConvention.IsSagaHandler(HandlerCall.For<SimpleSagaHandler>(x => x.Second(null)))
                .ShouldBeTrue();

            StatefulSagaConvention.IsSagaHandler(HandlerCall.For<SimpleSagaHandler>(x => x.Last(null)))
                .ShouldBeTrue();
        }

        [Test]
        public void is_saga_handler_negative()
        {
            StatefulSagaConvention.IsSagaHandler(HandlerCall.For<SimpleHandler<OneMessage>>(x => x.Handle(null)))
                .ShouldBeFalse();
        }

        [Test]
        public void is_saga_chain_is_false_for_regular_behavior_chain()
        {
            StatefulSagaConvention.IsSagaChain(new BehaviorChain())
                .ShouldBeFalse();
        }

        [Test]
        public void is_saga_chain_is_false_for_handler_chain_with_no_saga_handlers()
        {
            var call = HandlerCall.For<SimpleHandler<OneMessage>>(x => x.Handle(null));

            var chain = new HandlerChain();
            chain.AddToEnd(call);

            StatefulSagaConvention.IsSagaChain(chain)
                .ShouldBeFalse();
        }

        [Test]
        public void is_saga_chain_is_true_for_handler_chain_with_a_saga_handler()
        {
            var call = HandlerCall.For<SimpleSagaHandler>(x => x.Last(null));

            var chain = new HandlerChain();
            chain.AddToEnd(call);

            StatefulSagaConvention.IsSagaChain(chain)
                .ShouldBeTrue();
        }
    }

    public class MySagaState
    {
        
    }

    public class SagaMessageOne
    {
        public Guid CorrelationId { get; set; }
    }

    public class SagaMessageTwo
    {
        public Guid CorrelationId { get; set; }
    }

    public class SagaMessageThree
    {
        public Guid CorrelationId { get; set; }
    }

    public class SimpleSagaHandler : IStatefulSaga<MySagaState>
    {
        public bool IsCompleted()
        {
            throw new NotImplementedException();
        }

        public MySagaState State { get; set; }

        public void Start(SagaMessageOne one)
        {
            
        }

        public void Second(SagaMessageTwo two)
        {
            
        }

        public void Last(SagaMessageThree three)
        {
            
        }
    }
}
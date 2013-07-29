﻿using FubuTestingSupport;
using FubuTransportation.Runtime;
using NUnit.Framework;
using Rhino.Mocks;
using Is = Rhino.Mocks.Constraints.Is;

namespace FubuTransportation.Testing.Runtime
{
    [TestFixture]
    public class EnvelopeSerializerTester : InteractionContext<EnvelopeSerializer>
    {
        private IMessageSerializer[] serializers;
        private Envelope theEnvelope;

        protected override void beforeEach()
        {
            serializers = Services.CreateMockArrayFor<IMessageSerializer>(5);
            for (int i = 0; i < serializers.Length; i++)
            {
                serializers[i].Stub(x => x.ContentType).Return("text/" + i);
            }

            theEnvelope = new Envelope(null)
            {
                Data = new byte[0]
            };
        }

        [Test]
        public void chooses_by_mimetype()
        {
            theEnvelope.ContentType = serializers[3].ContentType;
            var o = new object();
            serializers[3].Stub(x => x.Deserialize(null)).IgnoreArguments().Return(o);

            ClassUnderTest.Deserialize(theEnvelope);

            theEnvelope.Message.ShouldBeTheSameAs(o);
        }

    }
}
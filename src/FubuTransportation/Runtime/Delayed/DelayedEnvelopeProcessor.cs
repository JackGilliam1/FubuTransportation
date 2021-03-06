﻿using System;
using System.Collections.Generic;
using FubuCore.Dates;
using FubuCore.Logging;
using FubuTransportation.Polling;

namespace FubuTransportation.Runtime.Delayed
{
    public class DelayedEnvelopeProcessor : IJob
    {
        private readonly ILogger _logger;
        private readonly ISystemTime _systemTime;
        private readonly IEnumerable<ITransport> _transports;

        public DelayedEnvelopeProcessor(ILogger logger, ISystemTime systemTime, IEnumerable<ITransport> transports)
        {
            _logger = logger;
            _systemTime = systemTime;
            _transports = transports;
        }

        public void Execute()
        {
            var currentTime = _systemTime.UtcNow();
            _transports.Each(transport => {
                DequeuFromTransport(transport, currentTime);
            });
        }

        public void DequeuFromTransport(ITransport transport, DateTime currentTime)
        {
            try
            {
                var envelopes = transport.ReplayDelayed(currentTime);
                envelopes.Each(x => _logger.InfoMessage(() => new DelayedEnvelopeAddedBackToQueue {Envelope = x}));
            }
            catch (Exception e)
            {
                _logger.Error("Error while trying to dequeue delayed messages from " + transport.ToString(), e);
            }
        }
    }
}
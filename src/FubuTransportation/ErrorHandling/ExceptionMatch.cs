﻿using System;
using FubuTransportation.Runtime;

namespace FubuTransportation.ErrorHandling
{
    public class ExceptionMatch : IExceptionMatch
    {
        private readonly Func<Exception, bool> _filter;
        private readonly string _description;

        public ExceptionMatch(Func<Exception, bool> filter, string description)
        {
            _filter = filter;
            _description = description;
        }

        public bool Matches(Envelope envelope, Exception ex)
        {
            return _filter(ex);
        }

        public string Description
        {
            get { return _description; }
        }
    }
}
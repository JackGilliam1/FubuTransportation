﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using FubuCore;
using FubuCore.Reflection;
using FubuCore.Util;
using FubuMVC.Core.Registration;
using System.Linq;
using FubuTransportation.Runtime;
using FubuTransportation.Runtime.Invocation;
using FubuTransportation.Runtime.Serializers;
using FubuTransportation.Scheduling;

namespace FubuTransportation.Configuration
{
    [ApplicationLevel]
    public class ChannelGraph : IEnumerable<ChannelNode>
    {
        private readonly Cache<string, ChannelNode> _channels = new Cache<string, ChannelNode>(key => new ChannelNode{Key = key});

        public ChannelGraph()
        {
            DefaultContentType = new XmlMessageSerializer().ContentType;
        }


        /// <summary>
        /// Used to identify the instance of the running FT node
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The default content type to use for serialization if none is specified at
        /// either the message or channel level
        /// </summary>
        public string DefaultContentType { get; set; }

        public ChannelNode ChannelFor<T>(Expression<Func<T, Uri>> property)
        {
            return ChannelFor(ReflectionHelper.GetAccessor(property));
        }

        public ChannelNode ChannelFor(Accessor accessor)
        {
            var key = ToKey(accessor);
            var channel = _channels[key];
            channel.SettingAddress = accessor;

            return channel;
        }

        // leave it virtual for testing
        public virtual void ReadSettings(IServiceLocator services)
        {
            _channels.Each(x => x.ReadSettings(services));
        }

        public virtual void StartReceiving(IHandlerPipeline pipeline)
        {
            _channels.Where(x => x.Incoming).Each(node =>
            {
                var scheduler = new StartingChannelNodeVisitor(new Receiver(pipeline, this, node));
                node.Accept(scheduler);
            });
        }

        public static string ToKey(Accessor accessor)
        {
            return accessor.OwnerType.Name.Replace("Settings", "") + ":" + accessor.Name;
        }

        public static string ToKey<T>(Expression<Func<T, object>> property)
        {
            return ToKey(property.ToAccessor());
        }

        public IEnumerator<ChannelNode> GetEnumerator()
        {
            return _channels.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(ChannelNode replyNode)
        {
            _channels[replyNode.Key] = replyNode;
        }
    }

}
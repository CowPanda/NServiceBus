﻿namespace NServiceBus.Core.Tests.Transport
{
    using System;
    using System.Transactions;
    using Fakes;
    using NServiceBus.Transports;
    using NUnit.Framework;
    using Settings;
    using Unicast.Transport;
    using TransactionSettings = Unicast.Transport.TransactionSettings;

    public class for_the_transactional_transport
    {
        [SetUp]
        public void SetUp()
        {
            fakeReceiver = new FakeReceiver();

            TransportReceiver = new MainTransportReceiver(new TransactionSettings(true, TimeSpan.FromSeconds(30), IsolationLevel.ReadCommitted, 5, false,false), 
                new DequeueSettings("FakeQueue",1), 0, fakeReceiver, new FakeFailureManager(), new SettingsHolder(), new BusConfiguration().BuildConfiguration(), null);

        }

        protected FakeReceiver fakeReceiver;
        protected MainTransportReceiver TransportReceiver;
    }
}
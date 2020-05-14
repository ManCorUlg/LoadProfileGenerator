﻿using System.Collections.Generic;
using Automation;
using CalculationController.DtoFactories;
using CalculationEngine.OnlineDeviceLogging;
using Common.JSON;
using Common.Tests;
using JetBrains.Annotations;
using NUnit.Framework;
using Xunit;
using Xunit.Abstractions;
using Assert = NUnit.Framework.Assert;

namespace Calculation.Tests.OnlineDeviceLogging
{
    [TestFixture]
    public class ProfileActivationEntryTests : UnitTestBaseClass
    {
        public ProfileActivationEntryTests([NotNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        [Category(UnitTestCategories.BasicTest)]
        public void ProfileActivationEntryTest()
        {
            CalcParameters calcParameters = CalcParametersFactory.MakeGoodDefaults();
            ProfileActivationEntry entry = new ProfileActivationEntry("d1", "p1", "ps1", "lt",calcParameters);
            Dictionary<ProfileActivationEntry.ProfileActivationEntryKey, ProfileActivationEntry> entries =
                new Dictionary<ProfileActivationEntry.ProfileActivationEntryKey, ProfileActivationEntry>
                {
                    { entry.GenerateKey(), entry }
                };
            entry.ActivationCount = 5;
            ProfileActivationEntry entry2 = new ProfileActivationEntry("d1", "p1", "ps1", "lt",calcParameters);
            bool result2 = entries.ContainsKey(entry2.GenerateKey());
            Assert.True(result2);
        }
    }
}
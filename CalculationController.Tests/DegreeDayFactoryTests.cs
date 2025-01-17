﻿using System;
using System.Collections.Generic;
using Automation;
using Automation.ResultFiles;
using CalculationController.CalcFactories;
using Common;
using Common.Enums;
using Common.Tests;
using Database;
using Database.Tables.Houses;
using Database.Tests;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace CalculationController.Tests
{
    public class DegreeDayFactoryTests : UnitTestBaseClass
    {
        [Fact]
        [Trait(UnitTestCategories.Category,UnitTestCategories.BasicTest)]
        public void Run()
        {
            //WorkingDir wd = new WorkingDir(Utili.GetCurrentMethodAndClass());
            using (DatabaseSetup db = new DatabaseSetup(Utili.GetCurrentMethodAndClass()))
            {
                Simulator sim = new Simulator(db.ConnectionString);
                var tempProfile = sim.TemperatureProfiles[0];
                var geoloc = sim.GeographicLocations[0];
                var ht = sim.HouseTypes[0];
                ht.AdjustYearlyEnergy = false;
                ht.HeatingYearlyTotal = 10000;
                if (ht.AdjustYearlyEnergy)
                {
                    throw new LPGException("Invalid test:Adjust energy is turned on");
                }
                House house = new House("house", "desc", tempProfile, geoloc, ht,
                    sim.ConnectionString, EnergyIntensityType.AsOriginal, "source", CreationType.ManuallyCreated,
                    Guid.NewGuid().ToStrGuid());
                HouseholdKey myKey = new HouseholdKey("blub");
                var start = new DateTime(2019, 12, 27);
                var end = new DateTime(2020, 12, 31);
                var ltdict = CalcLoadTypeDtoFactory.MakeLoadTypes(sim.LoadTypes.Items, new TimeSpan(0, 1, 0), LoadTypePriority.All);
                var spaceheating = CalcHouseDtoFactory.CreateSpaceHeatingObject(house, tempProfile, myKey, out var _, start, end, ltdict);
                if (spaceheating == null)
                {
                    throw new LPGException("Spaceheating was null");
                }
                //double sum = 0;
                Dictionary<int, double> sumByYear = new Dictionary<int, double>();
                foreach (var cdd in spaceheating.CalcDegreeDays)
                {
                    if (!sumByYear.ContainsKey(cdd.Year))
                    {
                        sumByYear.Add(cdd.Year, 0);
                    }
                    sumByYear[cdd.Year] += cdd.HeatingAmount;
                }

                foreach (var pair in sumByYear)
                {
                    Logger.Info(pair.Key + ": " + pair.Value);
                }
            }

        }

        public DegreeDayFactoryTests([NotNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }
}

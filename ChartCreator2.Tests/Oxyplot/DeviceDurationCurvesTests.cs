﻿using System;
using System.IO;
using Automation;
using ChartCreator2.OxyCharts;
using Common;
using Common.Tests;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;


namespace ChartCreator2.Tests.Oxyplot {

    public class DeviceDurationCurvesTests : UnitTestBaseClass
    {
        [StaFact]
        [Trait(UnitTestCategories.Category,UnitTestCategories.LongTest5)]
        public void MakePlotTest()
        {
            CleanTestBase.RunAutomatically(false);
            var cs = new OxyCalculationSetup(Utili.GetCurrentMethodAndClass());
            cs.StartHousehold(1, GlobalConsts.CSVCharacter,
                LoadTypePriority.Mandatory, new DateTime(2012, 12, 31),
                x => x.Enable(CalcOption.DurationCurve));
            using (var fft = new FileFactoryAndTracker(cs.DstDir, "1", cs.Wd.InputDataLogger))
            {
                fft.ReadExistingFilesFromSql();
                CalculationProfiler cp = new CalculationProfiler();
                ChartCreationParameters ccps = new ChartCreationParameters(300, 4000,
                    2500, false, GlobalConsts.CSVCharacter, new DirectoryInfo(cs.DstDir));
                var aeupp = new DeviceDurationCurves(ccps, fft, cp);
                Logger.Info("Making picture");
                var di = new DirectoryInfo(cs.DstDir);
                var rfe = cs.GetRfeByFilename("DeviceDurationCurves.Electricity.csv");

                aeupp.MakePlot(rfe);
                Logger.Info("finished picture");
                // OxyCalculationSetup.CopyImage(resultFileEntries[0].FullFileName)
                var imagefiles = FileFinder.GetRecursiveFiles(di, "DeviceDurationCurves.*.png");
                imagefiles.Count.Should().BeGreaterOrEqualTo( 1);
            }
            cs.CleanUp();
            CleanTestBase.RunAutomatically(true);
        }
        /*
        [Fact]
        [Trait(UnitTestCategories.Category,UnitTestCategories.ManualOnly)]
        public void MakePlotTest2()
        {
            const string dstdir = @"E:\unittest\StartHousehold1";
            var fft = new FileFactoryAndTracker(dstdir, "1");
            CalculationProfiler cp = new CalculationProfiler();
            ChartBase.ChartCreationParameterSet ccps = new ChartBase.ChartCreationParameterSet(4000,
                2500, 300, false, fft, GlobalConsts.CSVCharacter, cp);
            var aeupp = new DeviceDurationCurves(ccps);

            Logger.Info("Making picture");
            var di = new DirectoryInfo(dstdir);

            var rfe = ResultFileList.LoadAndGetByFileName(di.FullName, "DeviceDurationCurves.Electricity.csv");
            aeupp.MakePlot(rfe, "dev duration curves", di);
            Logger.Info("finished picture");
        }*/
        public DeviceDurationCurvesTests([JetBrains.Annotations.NotNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }
}
﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Automation;
using Automation.ResultFiles;
using CalcPostProcessor.Steps;
using Common;
using Common.SQLResultLogging;
using Newtonsoft.Json;

namespace CalcPostProcessor.LoadTypeHouseholdSteps {
    public class IndividualHouseholdFlexJsonSumProfileProcessor : HouseholdLoadTypeStepBase
    {
        [JetBrains.Annotations.NotNull] private readonly IFileFactoryAndTracker _fft;

        public IndividualHouseholdFlexJsonSumProfileProcessor([JetBrains.Annotations.NotNull] CalcDataRepository repository,
                                                              [JetBrains.Annotations.NotNull] IFileFactoryAndTracker fft,
                                                              [JetBrains.Annotations.NotNull] ICalculationProfiler
                                                                  calculationProfiler) : base(
            repository,
            AutomationUtili.GetOptionList(CalcOption.JsonHouseholdSumFilesNoFlex),
            calculationProfiler,
            "Individual Household Json Sum Profiles for not flexible devices") =>
            _fft = fft;

        [JetBrains.Annotations.NotNull]
        public override List<CalcOption> NeededOptions => new List<CalcOption> { CalcOption.DetailedDatFiles };

        protected override void PerformActualStep(IStepParameters parameters)
        {
            var p = (HouseholdLoadtypeStepParameters)parameters;

            if (p.Key.HHKey == Constants.GeneralHouseholdKey)
            {
                return;
            }

            var efc = Repository.ReadEnergyFileColumns(p.Key.HHKey);
            var dstLoadType = p.LoadType;

            if (!efc.ColumnCountByLoadType.ContainsKey(dstLoadType))
            {
                return;
            }

            var columns = efc.ColumnEntriesByColumn[dstLoadType].Values
                .Where(entry => entry.HouseholdKey == p.Key.HHKey && entry.CalcDeviceDto.FlexibilityMode == FlexibilityType.NoFlexibility)
                .Select(entry => entry.Column)
                .ToList();
            var hhname = "." + p.Key.HHKey;
            var calcParameters = Repository.CalcParameters;
            var jrf = new JsonSumProfile("Sum profile minus flexible devices", calcParameters.InternalStepsize,
                calcParameters.OfficialStartTime, dstLoadType.Name, dstLoadType.UnitOfSum,
                dstLoadType.ConvertToLoadTypeInformation(), p.Key);
            foreach (var efr in p.EnergyFileRows)
            {
                if (!efr.Timestep.DisplayThisStep)
                {
                    continue;
                }

                jrf.Values.Add(efr.GetSumForCertainCols(columns) * dstLoadType.ConversionFactor);
            }

            var sumfile = _fft.MakeFile<StreamWriter>("Sum.NoFlexDevices." + dstLoadType.FileName + hhname + ".json",
                "Summed up energy profile for all devices for " + dstLoadType.Name + " without any flexible devices", true,
                ResultFileID.JsonHouseholdSumsNoFlexDevices, p.Key.HHKey, TargetDirectory.Results,
                calcParameters.InternalStepsize, CalcOption.JsonHouseholdSumFilesNoFlex,
                dstLoadType.ConvertToLoadTypeInformation());
            sumfile.Write(JsonConvert.SerializeObject(jrf, Formatting.Indented));
            sumfile.Flush();

        }
    }
}
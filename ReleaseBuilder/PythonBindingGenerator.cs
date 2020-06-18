﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Automation;
using Automation.ResultFiles;
using Common;
using Common.Tests;
using Database;
using Database.Tables;
using Database.Tests;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace ReleaseBuilder
{
    public class PythonBindingGenerator: UnitTestBaseClass
    {
        [Fact]
        public void MakePythonData()
        {
            using DatabaseSetup db = new DatabaseSetup(Utili.GetCurrentMethodAndClass());
            Simulator sim = new Simulator(db.ConnectionString);

                StreamWriter sw = new StreamWriter(@"C:\Work\utsp\lpgdata.py");
                //sw.WriteLine("from dataclasses import dataclass, field");
                //sw.WriteLine("from dataclasses_json import dataclass_json  # type: ignore");
                //sw.WriteLine("from typing import List, Optional, Any");
                sw.WriteLine("from lpgadapter import *");
                //sw.WriteLine("from enum import Enum");
                sw.WriteLine();
                WriteNames(sim.LoadTypes.It.Select(x => (DBBase)x).ToList(), sw, "LoadTypes");
                WriteNames(sim.HouseTypes.It.Select(x => (DBBase)x).ToList(), sw, "HouseTypes");
                WriteJsonRefs(sim.ModularHouseholds.It.Select(x => (DBBase)x).ToList(), sw, "Households");
                WriteJsonRefs(sim.GeographicLocations.It.Select(x => (DBBase)x).ToList(), sw, "GeographicLocations");
                WriteJsonRefs(sim.TemperatureProfiles.It.Select(x => (DBBase)x).ToList(), sw, "TemperatureProfiles");
                WriteJsonRefs(sim.TransportationDeviceSets.It.Select(x => (DBBase)x).ToList(), sw, "TransportationDeviceSets");
                WriteJsonRefs(sim.ChargingStationSets.It.Select(x => (DBBase)x).ToList(), sw, "ChargingStationSets");
                WriteJsonRefs(sim.TravelRouteSets.It.Select(x => (DBBase)x).ToList(), sw, "TravelRouteSets");
                WriteJsonRefs(sim.Houses.It.Select(x => (DBBase)x).ToList(), sw, "Houses");
                sw.Close();
        }

        private static void WriteJsonRefs(List<DBBase> items, StreamWriter sw, string classname)
        {
            sw.WriteLine();
            sw.WriteLine("# noinspection PyPep8,PyUnusedLocal");
            sw.WriteLine("class "+classname+ ":");
            foreach (var item in items) {
                sw.WriteLine("    "+CleanPythonName(item.Name) + ": JsonReference = JsonReference(\"" + item.Name + "\",  StrGuid(\"" + item.Guid + "\"))"  );
            }
            sw.WriteLine();
        }

        private static void WriteNames(List<DBBase> items, StreamWriter sw, string classname)
        {
            sw.WriteLine();
            sw.WriteLine("# noinspection PyPep8,PyUnusedLocal");
            sw.WriteLine("class "+classname+":");
            foreach (var item in items) {
                if(item.Name == "None") {
                    continue;
                }
                string pythonName = CleanPythonName(item.Name);
                sw.WriteLine("    " + pythonName + " = \"" +item.Name + "\"" );
            }
            sw.WriteLine();
        }

        private static string CleanPythonName(string name)
        {
            string s1= name.Replace(" ",
                    "_")
                .Replace("(",
                    "")
                .Replace(")",
                    "")
                .Replace("+",
                    "").Replace(",", "")
                .Replace(".", "_")
                .Replace("/", "_")
                .Replace(":", "_")
                .Replace("ü", "ue")
                .Replace("ö", "oe")
                .Replace("ä", "ae")
                .Replace("-", "_");
            while (s1.Contains("__")) {
                s1 = s1.Replace("__", "_");
            }

            return s1;
        }

        [Fact]
        public void MakePythonBindings()
        {

                StreamWriter sw = new StreamWriter("C:\\Work\\utsp\\lpgpythonbindings.py");
                sw.WriteLine("from __future__ import annotations");
                sw.WriteLine("from dataclasses import dataclass, field");
                sw.WriteLine("from dataclasses_json import dataclass_json  # type: ignore");
                sw.WriteLine("from typing import List, Optional, Any");
                sw.WriteLine("from enum import Enum");

                sw.WriteLine();
                var writtenTypes = new List<string>();
                WriteEnum<LoadTypePriority>(sw, writtenTypes);
                WriteEnum<OutputFileDefault>(sw, writtenTypes);
                WriteEnum<EnergyIntensityType>(sw, writtenTypes);
                WriteEnum<CalcOption>(sw, writtenTypes);
                WriteEnum<HouseDefinitionType>(sw, writtenTypes);
                WriteEnum<Gender>(sw, writtenTypes);
                WriteEnum<HouseholdDataSpecificationType>(sw, writtenTypes);
                var encounteredTypes = new List<string>();
                WriteClass<StrGuid>(sw, encounteredTypes, writtenTypes);
                WriteClass<PersonData>(sw, encounteredTypes, writtenTypes);
                WriteClass<JsonReference>(sw, encounteredTypes, writtenTypes);
                WriteClass<TransportationDistanceModifier>(sw, encounteredTypes, writtenTypes);
                WriteClass<JsonCalcSpecification>(sw, encounteredTypes, writtenTypes);
                WriteClass<HouseReference>(sw, encounteredTypes, writtenTypes);
                WriteClass<HouseholdDataPersonSpecification>(sw, encounteredTypes, writtenTypes);
                WriteClass<HouseholdTemplateSpecification>(sw, encounteredTypes, writtenTypes);
                WriteClass<HouseholdNameSpecification>(sw, encounteredTypes, writtenTypes);
                WriteClass<HouseholdData>(sw, encounteredTypes, writtenTypes);
                WriteClass<HouseData>(sw, encounteredTypes, writtenTypes);
                WriteClass<HouseCreationAndCalculationJob>(sw, encounteredTypes, writtenTypes);
                encounteredTypes.Remove("System.String");
                encounteredTypes.Remove("System.Int32");
                encounteredTypes.Remove("System.Double");
                encounteredTypes.Remove("System.Boolean");
                encounteredTypes.Remove("System.DateTime");
                foreach (var encounteredType in encounteredTypes) {
                    if (!writtenTypes.Contains(encounteredType)) {
                        throw new LPGException("Missing Type:" + encounteredType);
                    }
                }

                sw.Close();
        }


        private static void WriteClass<T>(StreamWriter sw, List<string> encounteredTypes, List<string> writtenTypes)
        {
            sw.WriteLine();
            sw.WriteLine("# noinspection PyPep8Naming, PyUnusedLocal");
            sw.WriteLine("@dataclass_json");
            sw.WriteLine("@dataclass");
            var myclass = typeof(T).Name;
            writtenTypes.Add(typeof(T).FullName);
            sw.WriteLine("class " + myclass + ":");
            var props = typeof(T).GetProperties();
            foreach (var info in props) {
                if (info.CanRead) {
                    MethodInfo getAccessor = info.GetMethod;
                    if (!getAccessor.IsPublic) {
                        continue;
                    }
                    if (getAccessor.IsStatic)
                    {
                        continue;
                    }
                }
                sw.WriteLine("    " +GetPropLine(info,encounteredTypes, out var parametertype));
                sw.WriteLine();
                sw.WriteLine("    def set_" + info.Name + "(self, value: "+ parametertype + ") -> " + myclass + ":" );
                sw.WriteLine("        self." + info.Name + " = value");
                sw.WriteLine("        return self");
                sw.WriteLine();
            }
        }

        private static string GetPropLine(PropertyInfo info, List<string> encounteredTypes, out string typename)
        {
            string fulltypename = info.PropertyType.FullName;
            string shorttypename = info.PropertyType.Name;
            if (info.PropertyType.IsGenericType)
            {
                var genericfulltypename = info.PropertyType.GenericTypeArguments[0].FullName;
                var genericshorttypename = info.PropertyType.GenericTypeArguments[0].Name;
                if (!encounteredTypes.Contains(genericfulltypename))
                {
                    encounteredTypes.Add(genericfulltypename);
                }
                if (fulltypename.StartsWith("System.Nullable`1")) {
                    fulltypename = genericfulltypename;
                    shorttypename = genericshorttypename;
                }
            }
            else {
                if (!encounteredTypes.Contains(fulltypename))
                {
                    encounteredTypes.Add(fulltypename);
                }
            }

            if (fulltypename.StartsWith("System.Collections.Generic.List`1[[System.String,")) {
                typename = "List[str]";
                return info.Name +  ": List[str] = field(default_factory=list)";
            }
            if (fulltypename.StartsWith("System.Collections.Generic.List`1[[Automation.CalcOption"))
            {
                typename = "List[str]";
                return info.Name + ": List[str] = field(default_factory=list)";
            }
            if (fulltypename.StartsWith("System.Collections.Generic.List`1[[Automation.PersonData,"))
            {
                typename = "List[PersonData]";
                return info.Name + ": List[PersonData] = field(default_factory=list)";
            }
            if (fulltypename.StartsWith("System.Collections.Generic.List`1[[Automation.TransportationDistanceModifier, ")) {
                typename = "List[TransportationDistanceModifier]";
                return info.Name + ": List[TransportationDistanceModifier] = field(default_factory=list)";
            }
            if (fulltypename.StartsWith("System.Collections.Generic.List`1[[Automation.HouseholdData,")) {
                typename = "List[HouseholdData]";
                return info.Name + ": List[HouseholdData] = field(default_factory=list)";
            }
            switch (fulltypename) {
                case "Automation.HouseData":
                    typename = shorttypename;
                    return info.Name + ": Optional[" + shorttypename + "] = None";
                case "Automation.JsonCalcSpecification":
                    typename = shorttypename;
                    return info.Name + ": Optional[" + shorttypename + "] = None";
                case "Automation.HouseReference":
                    typename = shorttypename;
                    return info.Name + ": Optional[" + shorttypename + "] = None";
                case "Automation.HouseholdDataPersonSpecification":
                    typename = shorttypename;
                    return info.Name + ": Optional[" + shorttypename + "] = None";
                case "Automation.HouseholdTemplateSpecification":
                    typename = shorttypename;
                    return info.Name + ": Optional[" + shorttypename + "] = None";
                case "Automation.HouseholdNameSpecification":
                    typename = shorttypename;
                    return info.Name + ": Optional[" + shorttypename + "] = None";
                case "Automation.JsonReference":
                    typename = shorttypename;
                    return info.Name + ": Optional[" + shorttypename + "] = None";
                case "Automation.TransportationDistanceModifier":
                    typename = shorttypename;
                    return info.Name + ": Optional[" + shorttypename + "] = None";
                case "Automation.PersonData":
                    typename = shorttypename;
                    return info.Name + ": Optional[" + shorttypename + "] = None";
                case "Automation.HouseholdDataSpecificationType":
                    typename = "str";
                    return info.Name + ": Optional[str] = \"\"";
                case "Automation.HouseDefinitionType":
                    typename = "str";
                    return info.Name + ": Optional[str] = HouseDefinitionType." + HouseDefinitionType.HouseData.ToString();
                case "Automation.Gender":
                    typename = "str";
                    return info.Name + ": Optional[str] = \"\"";
                case "System.String":
                    typename = "str";
                    return info.Name + ": Optional[str] = \"\"";
                case "Automation.CalcOption":
                    typename = "str";
                    return info.Name + ": Optional[str] = \"\"";
                case "Automation.OutputFileDefault":
                    typename = "str";
                    return info.Name + ": Optional[str] = \"\"";
                case "Automation.EnergyIntensityType":
                    typename = "str";
                    return info.Name + ": Optional[str] = \"\"";
                case "Automation.LoadTypePriority":
                    typename = "str";
                    return info.Name + ": Optional[str] = \"\"";
                case "System.DateTime":
                    typename = "str";
                    return info.Name + ": Optional[str] = \"\"";
                case "Automation.StrGuid":
                    typename = "StrGuid";
                    return info.Name + ": Optional[StrGuid] = None";
                case "Automation.HouseholdData":
                    typename = shorttypename;
                    return info.Name + ": Optional[" + shorttypename + "] = None";
                case "System.Double":
                    typename = "float";
                    return info.Name + ": float = 0";
                case "System.Int32":
                    typename = "int";
                    return info.Name + ": int = 0";
                case "System.Boolean":
                    typename = "bool";
                    return info.Name + ": bool = False";

                    //"System.Nullable`1[[Automation.StrGuid, Automation, Version=9.6.0.0, Culture=neutral, PublicKeyToken=null]]'"
            }
            throw new LPGException("unknown type: \n" + fulltypename);
        }

        private static void WriteEnum<T>(StreamWriter sw, List<string> writtenTypes) {
            sw.WriteLine();
            var myclass = typeof(T).Name;
            writtenTypes.Add(typeof(T).FullName);
            var enumvals = Enum.GetValues(typeof(T));
            sw.WriteLine("class " + myclass + "(str, Enum):");
            foreach (var val in enumvals) {
                sw.WriteLine("    " + val + " = \""+ val + "\"");
            }
            sw.WriteLine();
        }

        public PythonBindingGenerator([NotNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }
}

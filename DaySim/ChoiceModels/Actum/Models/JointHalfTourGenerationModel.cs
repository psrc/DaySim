﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  


using System;
using System.Collections.Generic;
using System.Linq;
using DaySim.DomainModels.Actum.Wrappers;
using DaySim.DomainModels.Actum.Wrappers.Interfaces;
using DaySim.Framework.ChoiceModels;
using DaySim.Framework.Coefficients;
using DaySim.Framework.Core;

namespace DaySim.ChoiceModels.Actum.Models {
  public class JointHalfTourGenerationModel : ChoiceModel {
    public const string CHOICE_MODEL_NAME = "ActumJointHalfTourGenerationModel";
    private const int TOTAL_ALTERNATIVES = 7;
    private const int TOTAL_NESTED_ALTERNATIVES = 2;
    private const int TOTAL_LEVELS = 2;
    private const int MAX_PARAMETER = 70; 

    public override void RunInitialize(ICoefficientsReader reader = null) {
      Initialize(CHOICE_MODEL_NAME, Global.Configuration.JointHalfTourGenerationModelCoefficients, TOTAL_ALTERNATIVES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
    }

    public int Run(HouseholdDayWrapper householdDay, int nCallsForTour, bool[] available) {
      return Run(householdDay, nCallsForTour, available, Global.Settings.Purposes.NoneOrHome, Global.Settings.Purposes.NoneOrHome);
    }

    public int Run(HouseholdDayWrapper householdDay, int nCallsForTour, bool[] available, int type, int subType) {
      if (householdDay == null) {
        throw new ArgumentNullException("householdDay");
      }

      householdDay.ResetRandom(920 + nCallsForTour);

      int choice = 0;

      if (Global.Configuration.IsInEstimationMode) {

        choice = type == 0 ? 0 : (type - 1) * 3 + subType + 1;

        if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
          return choice;
        }
      }

      ChoiceProbabilityCalculator choiceProbabilityCalculator = _helpers[ParallelUtility.threadLocalAssignedIndex.Value].GetChoiceProbabilityCalculator(((householdDay.Household.Id * 10 + householdDay.Day) * 397) ^ nCallsForTour);

      if (_helpers[ParallelUtility.threadLocalAssignedIndex.Value].ModelIsInEstimationMode) {
        //if (tour.PersonDay.TotalStops > 0) {  // TODO:  maybe the restrictions coming from HH pattern shoudl enter here
        RunModel(choiceProbabilityCalculator, householdDay, nCallsForTour, available, choice);

        choiceProbabilityCalculator.WriteObservation();
        // }
      } else if (Global.Configuration.TestEstimationModelInApplicationMode) {
        Global.Configuration.IsInEstimationMode = false;

        //choice = Math.Min(personDay.BusinessStops, 1) + 2 * Math.Min(personDay.SchoolStops, 1);

        RunModel(choiceProbabilityCalculator, householdDay, nCallsForTour, available);

        ChoiceProbabilityCalculator.Alternative chosenAlternative = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility, householdDay.Household.Id, choice);

        Global.Configuration.IsInEstimationMode = true;
      } else {
        //if (tour.PersonDay.TotalStops > 0) {  // TODO:  maybe the restrictions coming from HH pattern shoudl enter here
        RunModel(choiceProbabilityCalculator, householdDay, nCallsForTour, available);

        ChoiceProbabilityCalculator.Alternative chosenAlternative = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility);
        choice = (int)chosenAlternative.Choice;
        //}
        //else {                                      // TODO:  see above TODO:
        //	choice = Global.Settings.Purposes.NoneOrHome;   // this is returned if the model isn't even run because we know there are no tours
        //                                               which is used by the choicemodelrunneer to break tour generation
        // }
      }

      return choice;
    }

    private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, HouseholdDayWrapper householdDay, int nCallsForTour, bool[] available, int choice = Constants.DEFAULT_VALUE) {
      //var householdDay = (ActumHouseholdDayWrapper)tour.HouseholdDay;
      IActumHouseholdWrapper household = (IActumHouseholdWrapper)householdDay.Household;
      IActumParcelWrapper householdResidenceParcel = (IActumParcelWrapper)household.ResidenceParcel;

      double workTourLogsum = 0;
      double schoolTourLogsum = 0;

      IEnumerable<PersonDayWrapper> orderedPersonDays = householdDay.PersonDays.OrderBy(p => p.GetJointHalfTourParticipationPriority()).ToList().Cast<PersonDayWrapper>();
      foreach (PersonDayWrapper personDay in orderedPersonDays) {

        //Double workTourLogsum;
        if (personDay.Person.UsualWorkParcelId != Constants.DEFAULT_VALUE && personDay.Person.UsualWorkParcelId != Global.Settings.OutOfRegionParcelId) {
          //JLB 201406
          //var nestedAlternative = Global.ChoiceModelSession.Get<WorkTourModeModel>().RunNested(personDay.Person, personDay.Person.Household.ResidenceParcel, personDay.Person.UsualWorkParcel, Global.Settings.Times.EightAM, Global.Settings.Times.FivePM, personDay.Person.Household.HouseholdTotals.DrivingAgeMembers);
          //JLB 201602
          //var nestedAlternative = Global.ChoiceModelSession.Get<WorkTourModeTimeModel>().RunNested(personDay, personDay.Person.Household.ResidenceParcel, personDay.Person.UsualWorkParcel, Global.Settings.Times.EightAM, Global.Settings.Times.FivePM, personDay.Person.Household.HouseholdTotals.DrivingAgeMembers);
          ChoiceProbabilityCalculator.Alternative nestedAlternative = Global.ChoiceModelSession.Get<TourModeTimeModel>().RunNested(personDay, personDay.Person.Household.ResidenceParcel, personDay.Person.UsualWorkParcel, Global.Settings.Times.EightAM, Global.Settings.Times.FivePM, personDay.Person.Household.HouseholdTotals.DrivingAgeMembers, Global.Settings.Purposes.Work);
          //workTourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();  //JB 20190326 replaced to avoid zeroing out logsum
          workTourLogsum = nestedAlternative == null ? workTourLogsum : nestedAlternative.ComputeLogsum();
        } else {
          //workTourLogsum = 0;  // JB 20190326 commented out to avoid zeroing out the logsum 
        }

        if (personDay.Person.UsualSchoolParcelId != Constants.DEFAULT_VALUE && personDay.Person.UsualSchoolParcelId != Global.Settings.OutOfRegionParcelId) {
          //JLB 201406
          //var nestedAlternative = Global.ChoiceModelSession.Get<SchoolTourModeModel>().RunNested(personDay.Person, personDay.Person.Household.ResidenceParcel, personDay.Person.UsualSchoolParcel, Global.Settings.Times.EightAM, Global.Settings.Times.FivePM, personDay.Person.Household.HouseholdTotals.DrivingAgeMembers);
          //JLB 201602
          //var nestedAlternative = Global.ChoiceModelSession.Get<SchoolTourModeTimeModel>().RunNested(personDay, personDay.Person.Household.ResidenceParcel, personDay.Person.UsualSchoolParcel, Global.Settings.Times.EightAM, Global.Settings.Times.FivePM, personDay.Person.Household.HouseholdTotals.DrivingAgeMembers);
          ChoiceProbabilityCalculator.Alternative nestedAlternative = Global.ChoiceModelSession.Get<TourModeTimeModel>().RunNested(personDay, personDay.Person.Household.ResidenceParcel, personDay.Person.UsualSchoolParcel, Global.Settings.Times.EightAM, Global.Settings.Times.FivePM, personDay.Person.Household.HouseholdTotals.DrivingAgeMembers, Global.Settings.Purposes.School);
          schoolTourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
        } else {
          schoolTourLogsum = 0;
        }

      }

      
      int carOwnership =
                        household.VehiclesAvailable == 0
                            ? Global.Settings.CarOwnerships.NoCars
                            : household.VehiclesAvailable < household.HouseholdTotals.DrivingAgeMembers
                                ? Global.Settings.CarOwnerships.LtOneCarPerAdult
                                : Global.Settings.CarOwnerships.OneOrMoreCarsPerAdult;

      int noCarsFlag = FlagUtility.GetNoCarsFlag(carOwnership);
      int carCompetitionFlag = FlagUtility.GetCarCompetitionFlag(carOwnership);

      //int votALSegment = Global.Settings.VotALSegments.Medium;  // TODO:  calculate a VOT segment that depends on household income
      //GV: 01.4.2019 - getting values from MB's memo
      int votALSegment =
        (household.Income <= 450000)
                  ? Global.Settings.VotALSegments.Low
                  : (household.Income <= 900000)
                      ? Global.Settings.VotALSegments.Medium
                      : Global.Settings.VotALSegments.High;

      //int transitAccessSegment = householdResidenceParcel.TransitAccessSegment();
      //GV: 01.4.2019 - getting values from MB's memo
      //OBS - it has to be in km
      int transitAccessSegment =
         household.ResidenceParcel.GetDistanceToTransit() >= 0 && household.ResidenceParcel.GetDistanceToTransit() <= 0.4
            ? 0
            : household.ResidenceParcel.GetDistanceToTransit() > 0.4 && household.ResidenceParcel.GetDistanceToTransit() <= 1.6
                ? 1
                : 2;


      double personalBusinessAggregateLogsum = Global.AggregateLogsums[householdResidenceParcel.ZoneId]
                [Global.Settings.Purposes.PersonalBusiness][carOwnership][votALSegment][transitAccessSegment];
      double shoppingAggregateLogsum = Global.AggregateLogsums[householdResidenceParcel.ZoneId]
                [Global.Settings.Purposes.Shopping][carOwnership][votALSegment][transitAccessSegment];
      double mealAggregateLogsum = Global.AggregateLogsums[householdResidenceParcel.ZoneId]
                [Global.Settings.Purposes.Meal][carOwnership][votALSegment][transitAccessSegment];
      double socialAggregateLogsum = Global.AggregateLogsums[householdResidenceParcel.ZoneId]
                [Global.Settings.Purposes.Social][carOwnership][votALSegment][transitAccessSegment];
      //var compositeLogsum = Global.AggregateLogsums[household.ResidenceZoneId][Global.Settings.Purposes.HomeBasedComposite][carOwnership][votALSegment][transitAccessSegment];
      double compositeLogsum = Global.AggregateLogsums[household.ResidenceZoneId][Global.Settings.Purposes.HomeBasedComposite][Global.Settings.CarOwnerships.NoCars][votALSegment][transitAccessSegment];

      int youngestAge = 999;
      int numberSelfEmpl = 0;
      int numberFTW = 0;
      int numberPTW = 0;
      foreach (PersonWrapper person in household.Persons) {
        // set characteristics here that depend on person characteristics
        if (person.Age < youngestAge) {
          youngestAge = person.Age;
        }
        if (person.OccupationCode == 8) {
          numberSelfEmpl++;
        }
        if (person.IsFulltimeWorker) {
          numberFTW++;
        }
        if (person.IsPartTimeWorker) {
          numberPTW++;
        }
      }
             

      bool hhLivesInCPHCity = false;
      if (household.ResidenceParcel.LandUseCode == 101 || household.ResidenceParcel.LandUseCode == 147) {
        hhLivesInCPHCity = true;
      }
      

      // NONE_OR_HOME

      ChoiceProbabilityCalculator.Alternative alternative = choiceProbabilityCalculator.GetAlternative(Global.Settings.Purposes.NoneOrHome, available[0], choice == Global.Settings.Purposes.NoneOrHome);

      alternative.Choice = Global.Settings.Purposes.NoneOrHome;

      alternative.AddUtilityTerm(1, (nCallsForTour > 1).ToFlag());

      alternative.AddUtilityTerm(2, household.HasChildrenUnder5.ToFlag());
      alternative.AddUtilityTerm(3, household.HasChildrenAge5Through15.ToFlag());

      //GV: 18. feb. 2019 - "AdultsInSharedHomeStay" variable cannotbe used
      //alternative.AddUtilityTerm(4, (household.Size == 2 && householdDay.AdultsInSharedHomeStay == 2).ToFlag());
      //alternative.AddUtilityTerm(5, (householdDay.AdultsInSharedHomeStay == 1 && household.HasChildrenUnder16).ToFlag());
      //alternative.AddUtilityTerm(6, (householdDay.AdultsInSharedHomeStay == 2 && household.HouseholdTotals.FullAndPartTimeWorkers >= 2).ToFlag());
      //alternative.AddUtilityTerm(7, (householdDay.AdultsInSharedHomeStay == 2 && hasAdultEducLevel12 == 1).ToFlag());
      //alternative.AddUtilityTerm(8, (youngestAge >= 40).ToFlag());

      //GV: 1.4.2019
      alternative.AddUtilityTerm(4, (household.Income >= 450000 && household.Income < 900000).ToFlag());
      //alternative.AddUtilityTerm(11, (household.Income >= 600000 && household.Income < 900000).ToFlag());
      alternative.AddUtilityTerm(5, (household.Income >= 900000).ToFlag());

      alternative.AddUtilityTerm(6, (household.VehiclesAvailable >= 1).ToFlag());

      //GV; 18. feb. 2019, Self Employed in the HH 
      alternative.AddUtilityTerm(7, (numberSelfEmpl >= 1).ToFlag());
      alternative.AddUtilityTerm(8, (household.PartTimeWorkers >= 1).ToFlag());
      alternative.AddUtilityTerm(9, (numberFTW >= 1).ToFlag());
      
      //alternative.AddUtilityTerm(7, (household.VehiclesAvailable == 1 && household.Has2Drivers).ToFlag());
      //alternative.AddUtilityTerm(8, (household.VehiclesAvailable >= 2 && household.Has2Drivers).ToFlag());

      alternative.AddUtilityTerm(11, (household.Size == 3).ToFlag());
      alternative.AddUtilityTerm(12, (household.Size >= 4).ToFlag());

      //alternative.AddUtilityTerm(15, householdDay.PrimaryPriorityTimeFlag);

      //alternative.AddUtilityTerm(21, personDay.Person.IsPartTimeWorker.ToFlag()); //GV - Aks John to include peson.Day in the model
      //alternative.AddUtilityTerm(2, personDay.Person.IsFulltimeWorker.ToFlag()); //GV - Aks John to include peson.Day in the model

      //alternative.AddUtilityTerm(22, (personDay.Person.Gender == 1).ToFlag()); //GV - Aks John to include peson.Day in the model
      //alternative.AddUtilityTerm(23, (hasAdultEducLevel12 == 1).ToFlag()); //GV - Aks John to include peson.Day in the model

      //alternative.AddUtilityTerm(24, MandatoryTourDay); //GV - Aks John to include peson.Day in the model
      //alternative.AddUtilityTerm(25, nonMandatoryTourDay); //GV - Aks John to include peson.Day in the model
      //alternative.AddUtilityTerm(26, atHomeDay); //GV - Aks John to include peson.Day in the model


      // FULL PAIRED
      alternative = choiceProbabilityCalculator.GetAlternative(1, available[1], choice == 1);
      alternative.Choice = 1;
      alternative.AddUtilityTerm(15, 1);

      //GV: 1.4.2019 - disaggregate logsum for the rest of GCA
      //alternative.AddUtilityTerm(16, workTourLogsum * (!hhLivesInCPHCity).ToFlag());
      //alternative.AddUtilityTerm(13, schoolTourLogsum);
      //GV: CPH disaggregate logsum - 18. feb 2019 
      //alternative.AddUtilityTerm(17, workTourLogsum * (hhLivesInCPHCity).ToFlag());
      //alternative.AddUtilityTerm(15, schoolTourLogsum * (hhLivesInCPHCity).ToFlag());
      alternative.AddUtilityTerm(17, workTourLogsum);

      //alternative.AddUtilityTerm(13, householdDay.PrimaryPriorityTimeFlag);

      //alternative.AddNestedAlternative(12, 1, 70);

      // FULL HalfTour 1
      alternative = choiceProbabilityCalculator.GetAlternative(2, available[2], choice == 2);
      alternative.Choice = 2;
      alternative.AddUtilityTerm(21, 1);

      //GV: 1.4.2019 - disaggregate logsum for the rest of GCA
      //alternative.AddUtilityTerm(22, workTourLogsum * (!hhLivesInCPHCity).ToFlag());
      //alternative.AddUtilityTerm(23, schoolTourLogsum);
      //GV: CPH disaggregate logsum - 18. feb 2019 - cannot be esimated
      //alternative.AddUtilityTerm(24, workTourLogsum * (hhLivesInCPHCity).ToFlag());
      //alternative.AddUtilityTerm(25, schoolTourLogsum * (hhLivesInCPHCity).ToFlag());
      alternative.AddUtilityTerm(24, workTourLogsum);

      //alternative.AddUtilityTerm(23, householdDay.PrimaryPriorityTimeFlag);

      //alternative.AddNestedAlternative(12, 1, 70);

      // Full HalfTour 2
      alternative = choiceProbabilityCalculator.GetAlternative(3, available[3], choice == 3);
      alternative.Choice = 3;
      alternative.AddUtilityTerm(31, 1);

      //GV: 1.4.2019 - disaggregate logsum for the rest of GCA
      //alternative.AddUtilityTerm(22, workTourLogsum * (!hhLivesInCPHCity).ToFlag());
      //alternative.AddUtilityTerm(23, schoolTourLogsum);
      //GV: CPH disaggregate logsum - 18. feb 2019 - cannot be estimated
      //alternative.AddUtilityTerm(24, workTourLogsum * (hhLivesInCPHCity).ToFlag());
      //alternative.AddUtilityTerm(25, schoolTourLogsum * (hhLivesInCPHCity).ToFlag());
      alternative.AddUtilityTerm(24, workTourLogsum);
      
      //alternative.AddUtilityTerm(33, householdDay.PrimaryPriorityTimeFlag);

      //alternative.AddNestedAlternative(12, 1, 70);

      // PARTIAL PAIRED
      alternative = choiceProbabilityCalculator.GetAlternative(4, available[4], choice == 4); 
      alternative.Choice = 4;
      alternative.AddUtilityTerm(41, 1);

      //GV: 1.4.2019 - diaggregate logsum for the Rest of GCA
      //alternative.AddUtilityTerm(42, workTourLogsum * (!hhLivesInCPHCity).ToFlag());
      //GV: CPH disaggregate logsum - 18. feb 2019 
      //alternative.AddUtilityTerm(43, workTourLogsum * (hhLivesInCPHCity).ToFlag());
      alternative.AddUtilityTerm(43, workTourLogsum);

      //GV: 14. june 2016 - not sign. and wrong sign
      //alternative.AddUtilityTerm(43, schoolTourLogsum);

      alternative.AddUtilityTerm(44, householdDay.PrimaryPriorityTimeFlag);

      //alternative.AddNestedAlternative(12, 1, 70);

      // PARTIAL HalfTour 1
      alternative = choiceProbabilityCalculator.GetAlternative(5, available[5], choice == 5);
      alternative.Choice = 5;
      alternative.AddUtilityTerm(51, 1);

      //GV: 1.4.2019 - disaggregate logsum for the Rest of GCA
      //alternative.AddUtilityTerm(42, workTourLogsum * (!hhLivesInCPHCity).ToFlag());
      //alternative.AddUtilityTerm(43, schoolTourLogsum);
      //GV: CPH diaggregate logsum - 18. feb 2019 
      //alternative.AddUtilityTerm(43, workTourLogsum * (hhLivesInCPHCity).ToFlag()); 
      alternative.AddUtilityTerm(43, workTourLogsum);

      alternative.AddUtilityTerm(54, householdDay.PrimaryPriorityTimeFlag); 

      //alternative.AddNestedAlternative(12, 1, 70);

      // PARTIAL HalfTour 2
      alternative = choiceProbabilityCalculator.GetAlternative(6, available[6], choice == 6);
      alternative.Choice = 6;
      alternative.AddUtilityTerm(61, 1);

      //GV: 1.4.2019 - disaggregate logsum fo the Rest of GCA
      //alternative.AddUtilityTerm(42, workTourLogsum * (!hhLivesInCPHCity).ToFlag());
      //alternative.AddUtilityTerm(43, schoolTourLogsum);
      //GV: CPH disaggregate logsum - 18. feb 2019 
      //alternative.AddUtilityTerm(43, workTourLogsum * (hhLivesInCPHCity).ToFlag());
      alternative.AddUtilityTerm(43, workTourLogsum);
      
      alternative.AddUtilityTerm(64, householdDay.PrimaryPriorityTimeFlag); 

      //alternative.AddNestedAlternative(12, 1, 70);

    }
  }
}

// MIT License
//
// Copyright(c) 2022 Danish Centre for Particle Therapy
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.IO;
using System.Linq;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace Evaluering4D
{
    /// <summary>
    /// A class that creates the files for DVH export. The format is adjusted such that it is nice to import into matlab.
    /// All structures for the main plan are exported in one file.
    /// All structures for each of the phase-plans are exported in separate files.
    /// If there are uncertainty scenarios on the main plan, these are exported in separate files as well.
    /// </summary>
    internal class ExportableDVHs
    {
        public PlanSetup[] AllPlans { get; }
        public string[] AllPlanIds { get; }
        public string Folder { get; }
        public double DvhResolution { get; }
        
        /// <summary>
        /// The constructor for the class. All plans are needed, the folder to save in and the resolution of the DVHs in Gy selected by the user.
        /// </summary>
        /// <param name="allPlans">A vector with all plans to export</param>
        /// <param name="folder">The folder to save the files</param>
        /// <param name="dvhresolution">The resolution of the exported dvh</param>
        public ExportableDVHs(PlanSetup[] allPlans, string folder, double dvhresolution)
        {
            AllPlans = allPlans;
            Folder = folder;
            DvhResolution = dvhresolution;

            int numberPlans = allPlans.Length;
            AllPlanIds = new string[numberPlans];
            for (int i = 0; i < numberPlans; i++) AllPlanIds[i] = allPlans[i].Id;            
        }

        /// <summary>
        /// All DVHs are saved to files
        /// </summary>
        internal void SaveAll()
        {
            //Loopeing over all the plans
            for (int i = 0; i < AllPlanIds.Count(); i++)
            {
                if (!AllPlans[i].IsDoseValid) continue; //If the plans for some reason are not calculated. This can happen if a materials tabel is missing.
               
                string filename = AllPlanIds[i];
                string firstLine = "Plan id: " + AllPlanIds[i] + " calculated on CT: " + AllPlans[i].StructureSet.Image.Id;
                string modalityLine = "Modality: " + AllPlans[i].PlanType.ToString() + ", Dose grid size [cm]: [" + AllPlans[i].Dose.XRes.ToString("0.00") + ", " + AllPlans[i].Dose.YRes.ToString("0.00") + ", " + AllPlans[i].Dose.ZRes.ToString("0.00") + "], Number of fractions: " + AllPlans[i].NumberOfFractions.ToString() + ", Prescribed dose: " + AllPlans[0].TotalDose.Dose.ToString("0.00") + " Gy" + Environment.NewLine;

                //Data is collected in a large matrix and we need to determine the size first, by finding the largest DVH for all structures and the number of structures..
                int largestDVH = FindLargestDVH(AllPlans[i], DvhResolution);
                int numbOfStructs = FindNumberOfStructs(AllPlans[i]);
                double[,] dvhList = new double[largestDVH, numbOfStructs + 1];

                FillValues(dvhList, AllPlans[i], largestDVH, DvhResolution);

                string[] idList = new string[numbOfStructs + 1];
                FillIDs(idList, AllPlans[i]);

                double[] volList = new double[numbOfStructs];

                FillIVols(volList, AllPlans[i]);

                double[] minList = new double[numbOfStructs];
                double[] maxList = new double[numbOfStructs];
                double[] meanList = new double[numbOfStructs];
                double[] DxxList = new double[numbOfStructs];
                double[] DyyList = new double[numbOfStructs];
                double[] DzzList = new double[numbOfStructs];

                // The header consists of the volume of the structure, the min, max and mean dose and D0.05cc, D0.5cc and D1cc DVH'points as they need a high resolution of the extracted DVH which is not provided.
                FillIminmaxmean(minList, maxList, meanList, DxxList,DyyList,DzzList, AllPlans[i], DvhResolution);

                WriteDVHfile(Folder, filename, dvhList, idList, numbOfStructs, largestDVH, firstLine, volList, minList, maxList, meanList,DxxList, DyyList, DzzList, modalityLine); //MULTI
            }

            //We are checking if the nominal plan has uncertainty scenarios. If yes we need to export them as well.
            if (AllPlans[0].PlanUncertainties.Count() != 0)
            {
                string modalityLine = "Modality: " + AllPlans[0].PlanType.ToString() + ", Dose grid size [cm]: [" + AllPlans[0].Dose.XRes.ToString("0.00") + ", " + AllPlans[0].Dose.YRes.ToString("0.00") + ", " + AllPlans[0].Dose.ZRes.ToString("0.00") + "], Number of fractions: " + AllPlans[0].NumberOfFractions.ToString() + ", Prescribed dose: " + AllPlans[0].TotalDose.Dose.ToString("0.00") + " Gy" + Environment.NewLine;

                foreach (var uncert in AllPlans[0].PlanUncertainties)
                {
                    if (uncert.Dose == null) continue;

                    string filename = AllPlans[0].Id.Substring(0, 2) + "_" + uncert.Id;
                    string firstLine = "Uncertainty scenario: " + uncert.DisplayName + " to nominal plan: " + AllPlans[0].Id + " calculated on CT: " + AllPlans[0].StructureSet.Image.Id;

                    //Data is collected in a large matrix and we need to determine the size first.
                    int largestDVH = FindLargestDVH(AllPlans[0], uncert, DvhResolution);
                    int numbOfStructs = FindNumberOfStructs(AllPlans[0]);
                    double[,] dvhList = new double[largestDVH, numbOfStructs + 1];

                    FillValues(dvhList, AllPlans[0], largestDVH, uncert, DvhResolution); // Values for the uncertainty scenario

                    string[] idList = new string[numbOfStructs + 1];
                    FillIDs(idList, AllPlans[0]); // Same value as for the nominal

                    double[] volList = new double[numbOfStructs];
                    FillIVols(volList, AllPlans[0]); // Same value as for the nominal

                    double[] minList = new double[numbOfStructs];
                    double[] maxList = new double[numbOfStructs];
                    double[] meanList = new double[numbOfStructs];
                    double[] DxxList = new double[numbOfStructs];
                    double[] DyyList = new double[numbOfStructs];
                    double[] DzzList = new double[numbOfStructs];


                    FillIminmaxmean(minList, maxList, meanList, DxxList, DyyList,DzzList, AllPlans[0], uncert, DvhResolution); // Values for the uncertainty scenario

                    WriteDVHfile(Folder, filename, dvhList, idList, numbOfStructs, largestDVH, firstLine, volList, minList, maxList, meanList,DxxList,DyyList,DzzList, modalityLine);
                }
            }
        }

        /// <summary>
        /// Calculates the min, max and mean dose values for all structures in a plan and added to three lists with the correct format.
        /// </summary>
        /// <param name="minList">List of min dose for each structure</param>
        /// <param name="maxList">List of max dose for each structure</param>
        /// <param name="meanList">List of mean dose for each structure</param>
        /// <param name="DxxList"> List of D0.05cc values in Gy </param>
        /// <param name="DyyList">List of D0.5cc values in Gy</param>
        /// <param name="DzzList">List of D1.0cc values in Gy</param>
        /// <param name="planSetup">The single plan to extract from</param>
        /// <param name="dvhresolution">The dvh resolution</param>
        private void FillIminmaxmean(double[] minList, double[] maxList, double[] meanList, double[] DxxList, double[] DyyList,double[] DzzList, PlanSetup planSetup, double dvhresolution)
        {
            int countStruct = 0;
            for (int j = 0; j < planSetup.StructureSet.Structures.Count(); j++)
            {
                if (planSetup.StructureSet.Structures.ElementAt(j) == null || planSetup.StructureSet.Structures.ElementAt(j).IsEmpty || planSetup.StructureSet.Structures.ElementAt(j).DicomType.ToLower() == "support")
                {
                    continue;
                }

                DVHData dvhdata = planSetup.GetDVHCumulativeData(planSetup.StructureSet.Structures.ElementAt(j), DoseValuePresentation.Absolute, VolumePresentation.Relative, dvhresolution);
                minList[countStruct] = dvhdata.MinDose.Dose;
                maxList[countStruct] = dvhdata.MaxDose.Dose;
                meanList[countStruct] = dvhdata.MeanDose.Dose;

                //We need a high resolution for the volume datapoints
                DVHData dvhdata2 = planSetup.GetDVHCumulativeData(planSetup.StructureSet.Structures.ElementAt(j), DoseValuePresentation.Absolute, VolumePresentation.AbsoluteCm3, 0.0001);
                DxxList[countStruct] = CalculateDXXcc(dvhdata2.CurveData, 0.05, planSetup.TotalDose.Dose);
                DyyList[countStruct] = CalculateDXXcc(dvhdata2.CurveData, 0.5, planSetup.TotalDose.Dose);
                DzzList[countStruct] = CalculateDXXcc(dvhdata2.CurveData, 1.0, planSetup.TotalDose.Dose);

                countStruct++;
            }
        }

        /// <summary>
        /// Calculates the min, max and mean dose values for all structures in a plan and added to three lists with the correct format.
        /// </summary>
        /// <param name="minList">List of min dose for each structure</param>
        /// <param name="maxList">List of max dose for each structure</param>
        /// <param name="meanList">List of mean dose for each structure</param>
        /// <param name="DxxList"> List of D0.05cc values in Gy </param>
        /// <param name="DyyList">List of D0.5cc values in Gy</param>
        /// <param name="DzzList">List of D1.0cc values in Gy</param>
        /// <param name="planSetup">The plan to extract from</param>
        /// <param name="dvhresolution">The dvh resolution</param>
        private void FillIminmaxmean(double[] minList, double[] maxList, double[] meanList, double[] DxxList, double[] DyyList, double[] DzzList, PlanSetup planSetup, PlanUncertainty uncert, double dvhresolution)
        {
            int countStruct = 0;
            for (int j = 0; j < planSetup.StructureSet.Structures.Count(); j++)
            {
                if (planSetup.StructureSet.Structures.ElementAt(j) == null || planSetup.StructureSet.Structures.ElementAt(j).IsEmpty || planSetup.StructureSet.Structures.ElementAt(j).DicomType.ToLower() == "support")
                {
                    continue;
                }

                DVHData dvhdata = uncert.GetDVHCumulativeData(planSetup.StructureSet.Structures.ElementAt(j), DoseValuePresentation.Absolute, VolumePresentation.Relative, dvhresolution);
                minList[countStruct] = dvhdata.MinDose.Dose;
                maxList[countStruct] = dvhdata.MaxDose.Dose;
                meanList[countStruct] = dvhdata.MeanDose.Dose;

                //We need a high resolution for the volume datapoints
                DVHData dvhdata2 = uncert.GetDVHCumulativeData(planSetup.StructureSet.Structures.ElementAt(j), DoseValuePresentation.Absolute, VolumePresentation.AbsoluteCm3, 0.0001);
                DxxList[countStruct] = CalculateDXXcc(dvhdata2.CurveData,0.05,planSetup.TotalDose.Dose);
                DyyList[countStruct] = CalculateDXXcc(dvhdata2.CurveData, 0.5, planSetup.TotalDose.Dose); 
                DzzList[countStruct] = CalculateDXXcc(dvhdata2.CurveData, 1.0, planSetup.TotalDose.Dose); 
                countStruct++;
            }
        }

        /// <summary>
        /// Calculates the volume of all structures in a plan and add them to the volList with the correct format.
        /// </summary>
        /// <param name="volList">Volume of each structure</param>
        /// <param name="planSetup">The plansetup to extract from</param>
        private void FillIVols(double[] volList, PlanSetup planSetup)
        {

            int countStruct = 0;
            for (int j = 0; j < planSetup.StructureSet.Structures.Count(); j++)
            {
                if (planSetup.StructureSet.Structures.ElementAt(j) == null || planSetup.StructureSet.Structures.ElementAt(j).IsEmpty || planSetup.StructureSet.Structures.ElementAt(j).DicomType.ToLower() == "support")
                {
                    continue;
                }

                volList[countStruct] = planSetup.StructureSet.Structures.ElementAt(j).Volume;
                countStruct++;
            }
        }

        /// <summary>
        /// The file is now written and saved for the given plan or uncertaintyscenario by collecting all the lists for the IDs, DVH values, mean, max, mean doses and volumes.
        /// </summary>
        /// <param name="v">The path to save to</param>
        /// <param name="filename">The file name</param>
        /// <param name="dvhList">DVH values</param>
        /// <param name="idList">The structure ids</param>
        /// <param name="numbOfStructs">Number of structures</param>
        /// <param name="largestDVH">The larges DVH size</param>
        /// <param name="firstLine">A string with the first line of the file</param>
        /// <param name="volList">All volumes</param>
        /// <param name="minList">All minimum doses</param>
        /// <param name="maxList">All maximum doses</param>
        /// <param name="meanList">All mean doses</param>
        /// <param name="DxxList"> All D0.05cc values in Gy </param>
        /// <param name="DyyList">All D0.5cc values in Gy</param>
        /// <param name="DzzList">All D1.0cc values in Gy</param>
        /// <param name="modalityLine">Line descriping modality, resolution of dose and fractionation</param>
        private void WriteDVHfile(string v, string filename, double[,] dvhList, string[] idList, int numbOfStructs, int largestDVH, string firstLine, double[] volList, double[] minList, double[] maxList, double[] meanList, double[] DxxList, double[] DyyList, double[] DzzList,string modalityLine)
        {
            string lines = firstLine + Environment.NewLine;

            lines += modalityLine;

            //Structure IDs are added
            string temp = "";
            for (int p = 0; p < numbOfStructs + 1; p++)
            {
                temp += idList[p] + "\t";
            }
            lines += temp + Environment.NewLine;

            temp = "Volume [cc] \t";
            for (int p = 0; p < numbOfStructs; p++)
            {
                temp += Math.Round(volList[p],3, MidpointRounding.AwayFromZero).ToString("0.000") + "\t";
            }
            lines += temp + Environment.NewLine;

            temp = "Min dose [Gy] \t";
            for (int p = 0; p < numbOfStructs; p++)
            {
                temp += Math.Round(minList[p],3, MidpointRounding.AwayFromZero).ToString("0.000") + "\t";
            }
            lines += temp + Environment.NewLine;

            temp = "Max dose [Gy] \t";
            for (int p = 0; p < numbOfStructs; p++)
            {
                temp += Math.Round(maxList[p], 3, MidpointRounding.AwayFromZero).ToString("0.000") + "\t";
            }
            lines += temp + Environment.NewLine;

            temp = "Mean dose [Gy] \t";
            for (int p = 0; p < numbOfStructs; p++)
            {
                temp += Math.Round(meanList[p], 3, MidpointRounding.AwayFromZero).ToString("0.000") + "\t";
            }
            lines += temp + Environment.NewLine;

            temp = "D0.05cc [Gy] \t";
            for (int p = 0; p < numbOfStructs; p++)
            {
                temp += Math.Round(DxxList[p], 3, MidpointRounding.AwayFromZero).ToString("0.000") + "\t";
            }
            lines += temp + Environment.NewLine;

            temp = "D0.5cc [Gy] \t";
            for (int p = 0; p < numbOfStructs; p++)
            {
                temp += Math.Round(DyyList[p], 3, MidpointRounding.AwayFromZero).ToString("0.000") + "\t";
            }
            lines += temp + Environment.NewLine;

            temp = "D1cc [Gy] \t";
            for (int p = 0; p < numbOfStructs; p++)
            {
                temp += Math.Round(DzzList[p], 3, MidpointRounding.AwayFromZero).ToString("0.000") + "\t";
            }
            lines += temp + Environment.NewLine;

            //Numbers are added
            for (int h = 0; h < largestDVH; h++)
            {
                temp = "";

                for (int p = 0; p < numbOfStructs + 1; p++)
                {
                    temp += Math.Round(dvhList[h, p],3, MidpointRounding.AwayFromZero).ToString("0.000") + "\t";
                }
                lines += temp + Environment.NewLine;
            }

            File.WriteAllText(v + "\\" + filename + ".txt", lines);
        }

        /// <summary>
        /// DVH values are filled into the correct format defined by the dvhList.
        /// </summary>
        /// <param name="dvhList">The list with DVH values for all structures</param>
        /// <param name="planSetup">The plan setup</param>
        /// <param name="largestDVH">Size of the largest DVH</param>
        /// <param name="dvhresolution">DVH resolution</param>
        private void FillValues(double[,] dvhList, PlanSetup planSetup, int largestDVH, double dvhresolution)
        {
            //First collumn is filled with dose values
            dvhList[0, 0] = 0.0;
            for (int j = 1; j < largestDVH; j++)
            {
                dvhList[j, 0] = dvhList[j - 1, 0] + dvhresolution;
            }


            //Here we start by 1 as the first column is the dose
            //We will find all struyctures to fill in.
            int countStruct = 1;
            //double resolutiondvh = 0.1;
            for (int j = 0; j < planSetup.StructureSet.Structures.Count(); j++)
            {
                if (planSetup.StructureSet.Structures.ElementAt(j) == null || planSetup.StructureSet.Structures.ElementAt(j).IsEmpty || planSetup.StructureSet.Structures.ElementAt(j).DicomType.ToLower() == "support")
                {
                    continue;
                }
                DVHData dvhdata = planSetup.GetDVHCumulativeData(planSetup.StructureSet.Structures.ElementAt(j), DoseValuePresentation.Absolute, VolumePresentation.Relative, dvhresolution);
                DVHPoint[] dvh = dvhdata.CurveData;
                if (dvh.Count() == 0) continue;

                for (int p = 0; p < dvh.Count(); p++)
                {
                    dvhList[p, countStruct] = dvh[p].Volume;
                }
                countStruct++;
            }
        }

        /// <summary>
        /// DVH values are filled into the correct format defined by the dvhList.
        /// </summary>
        /// <param name="dvhList">The list with DVH values for all structures</param>
        /// <param name="planSetup">The plan setup</param>
        /// <param name="largestDVH">Size of the largest DVH</param>
        /// <param name="uncert">Uncertainty scenario</param>
        /// <param name="dvhresolution">DVH resolution</param>
        private void FillValues(double[,] dvhList, PlanSetup planSetup, int largestDVH, PlanUncertainty uncert, double dvhresolution)
        {
            //First collumn is filled with dose values
            dvhList[0, 0] = 0.0;
            for (int j = 1; j < largestDVH; j++)
            {
                dvhList[j, 0] = dvhList[j - 1, 0] + dvhresolution;
            }

            //Here we start by 1 as the first column is the dose
            //We will find all struyctures to fill in.
            int countStruct = 1;
            //double resolutiondvh = 0.1;
            for (int j = 0; j < planSetup.StructureSet.Structures.Count(); j++)
            {
                if (planSetup.StructureSet.Structures.ElementAt(j) == null || planSetup.StructureSet.Structures.ElementAt(j).IsEmpty || planSetup.StructureSet.Structures.ElementAt(j).DicomType.ToLower() == "support")
                {
                    continue;
                }
                DVHData dvhdata = uncert.GetDVHCumulativeData(planSetup.StructureSet.Structures.ElementAt(j), DoseValuePresentation.Absolute, VolumePresentation.Relative, dvhresolution);
                DVHPoint[] dvh = dvhdata.CurveData;
                if (dvh.Count() == 0) continue;

                for (int p = 0; p < dvh.Count(); p++)
                {
                    dvhList[p, countStruct] = dvh[p].Volume;
                }
                countStruct++;
            }
        }

        /// <summary>
        /// Structure ids are filled into the correct format defined by the idList.
        /// </summary>
        /// <param name="idList">A list of structure ids</param>
        /// <param name="planSetup">The plansetup</param>
        private void FillIDs(string[] idList, PlanSetup planSetup)
        {
            idList[0] = "Dose (Gy)";

            int countStruct = 1;

            for (int j = 0; j < planSetup.StructureSet.Structures.Count(); j++)
            {
                if (planSetup.StructureSet.Structures.ElementAt(j) == null || planSetup.StructureSet.Structures.ElementAt(j).IsEmpty || planSetup.StructureSet.Structures.ElementAt(j).DicomType.ToLower() == "support")
                {
                    continue;
                }

                idList[countStruct] = planSetup.StructureSet.Structures.ElementAt(j).Id;
                countStruct++;
            }
        }

        /// <summary>
        /// Determining the number of structures with a non zero contour.
        /// </summary>
        /// <param name="planSetup">The plansetup</param>
        /// <returns></returns>
        private int FindNumberOfStructs(PlanSetup planSetup)
        {
            //Looper over all structers as we have to count them.
            int numb = 0;
            for (int j = 0; j < planSetup.StructureSet.Structures.Count(); j++)
            {
                if (planSetup.StructureSet.Structures.ElementAt(j) == null || planSetup.StructureSet.Structures.ElementAt(j).IsEmpty || planSetup.StructureSet.Structures.ElementAt(j).DicomType.ToLower() == "support")
                {
                    continue;
                }
                numb++;
            }
            return numb;
        }

        /// <summary>
        /// Determining the largest DVH curve for all relevant structures of a plan.
        /// </summary>
        /// <param name="planSetup">Plansetup</param>
        /// <param name="dvhresolution">DVH resolution</param>
        /// <returns></returns>
        private int FindLargestDVH(PlanSetup planSetup, double dvhresolution)
        {
            //Looper over all structers as we have to count them.
            int dvhsize = 0;
            for (int j = 0; j < planSetup.StructureSet.Structures.Count(); j++)
            {
                if (planSetup.StructureSet.Structures.ElementAt(j) == null || planSetup.StructureSet.Structures.ElementAt(j).IsEmpty || planSetup.StructureSet.Structures.ElementAt(j).DicomType.ToLower() == "support")
                {
                    continue;
                }

                DVHData dvhdata = planSetup.GetDVHCumulativeData(planSetup.StructureSet.Structures.ElementAt(j), DoseValuePresentation.Absolute, VolumePresentation.Relative, dvhresolution);
                DVHPoint[] dvh = dvhdata.CurveData;

                if (dvh.Count() >= dvhsize)
                    dvhsize = dvh.Count();
            }
            return dvhsize;
        }

        /// <summary>
        /// Determining the largest DVH curve for all relevant structures of a plan.
        /// </summary>
        /// <param name="planSetup">Plansetup</param>
        /// <param name="uncert">Uncertainty scenario</param>
        /// <param name="dvhresolution">DVH resolution</param>
        /// <returns></returns>
        private int FindLargestDVH(PlanSetup planSetup, PlanUncertainty uncert, double dvhresolution)
        {
            //Looper over all structers as we have to count them.
            int dvhsize = 0;
            for (int j = 0; j < planSetup.StructureSet.Structures.Count(); j++)
            {
                if (planSetup.StructureSet.Structures.ElementAt(j) == null || planSetup.StructureSet.Structures.ElementAt(j).IsEmpty || planSetup.StructureSet.Structures.ElementAt(j).DicomType.ToLower() == "support")
                {
                    continue;
                }

                DVHData dvhdata = uncert.GetDVHCumulativeData(planSetup.StructureSet.Structures.ElementAt(j), DoseValuePresentation.Absolute, VolumePresentation.Relative, dvhresolution);
                DVHPoint[] dvh = dvhdata.CurveData;

                if (dvh.Count() >= dvhsize)
                    dvhsize = dvh.Count();
            }
            return dvhsize;
        }

        /// <summary>
        /// Function for calculating the DVHpoints for the header
        /// </summary>
        /// <param name="DVH">The DVH data for a structure</param>
        /// <param name="XXcc">The volume to read off the dose in</param>
        /// <param name="prescribedDose">The prescribed dose for the plan</param>
        /// <returns></returns>
        private double CalculateDXXcc(DVHPoint[] DVH, double XXcc, double prescribedDose)
        {
            
            if (DVH == null || DVH.Count() == 0 || DVH.Max(d => d.Volume) < XXcc)
            {
                return -1000.0;
            }
            else
            {
                DVHPoint test = DVH.FirstOrDefault(d => d.Volume <= XXcc);

                if (test.DoseValue == null)
                {
                    return -1000.0;
                }


                if (test.DoseValue.IsAbsoluteDoseValue)
                {
                    return test.DoseValue.Dose;

                }
                else
                {
                    return test.DoseValue.Dose * prescribedDose;
                }
            }
        }
    }
}
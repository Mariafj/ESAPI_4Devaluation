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
    /// A class that created the files for DVH export
    /// </summary>
    internal class ExportableDVHs
    {
        public PlanSetup[] AllPlans { get; }
        public string[] AllPlanIds { get; }
        public string Folder { get; }
        public double DvhResolution { get; }
        
        /// <summary>
        /// Single plan dvh export
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
        /// DVhs are saved to files
        /// </summary>
        internal void SaveAll()
        {
            //Loopeing over planer
            for (int i = 0; i < AllPlanIds.Count(); i++)
            {
                string filename = AllPlanIds[i];
                string firstLine = "Nominal dose. Plan id: " + AllPlanIds[i];

                //Data is collected in a large matrix and we need to determine the size first.
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
                FillIminmaxmean(minList, maxList, meanList, AllPlans[i], DvhResolution);

                WriteDVHfile(Folder, filename, dvhList, idList, numbOfStructs, largestDVH, firstLine, volList, minList, maxList, meanList); //MULTI
            }

            //We are checking if the nominal plan has uncertainty scenarios. If yes we need to export them as well.
            if (AllPlans[0].PlanUncertainties.Count() != 0)
            {
                foreach (var uncert in AllPlans[0].PlanUncertainties)
                {
                    if (uncert.Dose == null) continue;

                    string filename = AllPlans[0].Id.Substring(0, 4) + "_" + uncert.Id;
                    string firstLine = "Uncertainty scenario: " + uncert.DisplayName + " to nominal plan: " + AllPlans[0].Id;

                    //Data is collected in a large matrix and we need to determine the size first.
                    int largestDVH = FindLargestDVH(AllPlans[0], uncert, DvhResolution);
                    int numbOfStructs = FindNumberOfStructs(AllPlans[0]);
                    double[,] dvhList = new double[largestDVH, numbOfStructs + 1];

                    FillValues(dvhList, AllPlans[0], largestDVH, uncert, DvhResolution); // Values for the uncertainty scenario

                    string[] idList = new string[numbOfStructs + 1];
                    FillIDs(idList, AllPlans[0]); // Same value as for the nominel

                    double[] volList = new double[numbOfStructs];
                    FillIVols(volList, AllPlans[0]); // Same value as for the nominel

                    double[] minList = new double[numbOfStructs];
                    double[] maxList = new double[numbOfStructs];
                    double[] meanList = new double[numbOfStructs];
                    FillIminmaxmean(minList, maxList, meanList, AllPlans[0], uncert, DvhResolution); // Values for the uncertainty scenario

                    WriteDVHfile(Folder, filename, dvhList, idList, numbOfStructs, largestDVH, firstLine, volList, minList, maxList, meanList);
                }
            }
        }

        /// <summary>
        /// Calculates the min, max and mean dose values for all structures in a plan and added to three lists with the correct format.
        /// </summary>
        /// <param name="minList">List of min dose for each structure</param>
        /// <param name="maxList">List of max dose for each structure</param>
        /// <param name="meanList">List of mean dose for each structure</param>
        /// <param name="planSetup">The single plan to extract from</param>
        /// <param name="dvhresolution">The dvh resolution</param>
        private void FillIminmaxmean(double[] minList, double[] maxList, double[] meanList, PlanSetup planSetup, double dvhresolution)
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

                countStruct++;
            }
        }

        /// <summary>
        /// Calculates the min, max and mean dose values for all structures in a plan and added to three lists with the correct format.
        /// </summary>
        /// <param name="minList">List of min dose for each structure</param>
        /// <param name="maxList">List of max dose for each structure</param>
        /// <param name="meanList">List of mean dose for each structure</param>
        /// <param name="planSetup">The plan to extract from</param>
        /// <param name="dvhresolution">The dvh resolution</param>
        private void FillIminmaxmean(double[] minList, double[] maxList, double[] meanList, PlanSetup planSetup, PlanUncertainty uncert, double dvhresolution)
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

                countStruct++;
            }
        }

        /// <summary>
        /// Calculates the volume of all structures in a plan and added to the volList with the correct format.
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
        private void WriteDVHfile(string v, string filename, double[,] dvhList, string[] idList, int numbOfStructs, int largestDVH, string firstLine, double[] volList, double[] minList, double[] maxList, double[] meanList)
        {
            string lines = firstLine + Environment.NewLine;

            //Structure IDs are added
            string temp = "";
            for (int p = 0; p < numbOfStructs + 1; p++)
            {
                temp += idList[p] + "\t";
            }
            lines += temp + Environment.NewLine;

            temp = "volume (cc) \t";
            for (int p = 0; p < numbOfStructs; p++)
            {
                temp += volList[p].ToString("0.00") + "\t";
            }
            lines += temp + Environment.NewLine;

            temp = "Min Dose (Gy) \t";
            for (int p = 0; p < numbOfStructs; p++)
            {
                temp += minList[p].ToString("0.00") + "\t";
            }
            lines += temp + Environment.NewLine;

            temp = "Max Dose (Gy) \t";
            for (int p = 0; p < numbOfStructs; p++)
            {
                temp += maxList[p].ToString("0.00") + "\t";
            }
            lines += temp + Environment.NewLine;

            temp = "Mean Dose (Gy) \t";
            for (int p = 0; p < numbOfStructs; p++)
            {
                temp += meanList[p].ToString("0.00") + "\t";
            }
            lines += temp + Environment.NewLine;

            //Numbers are added
            for (int h = 0; h < largestDVH; h++)
            {
                temp = "";

                for (int p = 0; p < numbOfStructs + 1; p++)
                {
                    temp += dvhList[h, p].ToString("0.00") + "\t";
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
                dvhList[j, 0] = dvhList[j - 1, 0] + 0.1;
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
    }
}
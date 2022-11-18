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
    internal class ExportableDVHs
    {
        public PlanSetup[] AllPlans { get; }
        public PlanSum[] AllPlanSums { get; }
        public string Folder { get; }
        public double DvhResolution { get; }
        public ExportableDVHs(PlanSetup[] allPlans, string folder, double dvhresolution)
        {
            AllPlans = allPlans;
            Folder = folder;
            DvhResolution = dvhresolution;
        }
        public ExportableDVHs(PlanSum[] allPlans, string folder, double dvhresolution)
        {
            AllPlanSums = allPlans;
            Folder = folder;
            DvhResolution = dvhresolution;
        }
        internal void SaveAll()
        {
            //Loopeing over planer
            for (int i = 0; i < AllPlans.Count(); i++)
            {
                string filename = AllPlans[i].Id;
                string firstLine = "Nominal dose. Plan id: " + AllPlans[i].Id;

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
        /// Calculates the min, max and mean dose values for all structures in a plan and aded to three lists with the correct format.
        /// </summary>
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
        /// Calculates the min, max and mean dose values for all structures in a plan and aded to three lists with the correct format.
        /// </summary>
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
        /// Determining the number of structures with a contour.
        /// </summary>
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
        /// Determining the largest DVH curve for all relevant structures.
        /// </summary>
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
        /// Determining the largest DVH curve for all relevant structures.
        /// </summary>
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
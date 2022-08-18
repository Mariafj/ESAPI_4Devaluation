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

        public PlanSetup[] AllPlans { get;}
        public string Folder { get;}
        public double DvhResolution { get; }


        public ExportableDVHs(PlanSetup[] allPlans, string folder, double dvhresolution)
        {
            AllPlans = allPlans;
            Folder = folder;
            DvhResolution = dvhresolution;
        }


        internal void SaveAll()
        {
            //Loopeing over planer
            for (int i = 0; i < AllPlans.Count(); i++)
            {
                string filename = AllPlans[i].Id;
                //MessageBox.Show(filename);

                string firstLine = "Nominal dose. Plan id: " + AllPlans[i].Id;
                //Data skal nu samles i en stor matrice
                int largestDVH = FindLargestDVH(AllPlans[i], DvhResolution);
                //MessageBox.Show("Længste DVH fundet til " + largestDVH.ToString());

                int numbOfStructs = FindNumberOfStructs(AllPlans[i]);
                //MessageBox.Show("Antal strukturer fundet til: " + numbOfStructs.ToString());

                //Matricen kan nu oprettes da vi ved hvor stor den skal være
                double[,] dvhList = new double[largestDVH, numbOfStructs + 1]; //MULTI
                //MessageBox.Show("Tom liste oprettet til strukturer");
                FillValues(dvhList, AllPlans[i], largestDVH, numbOfStructs, DvhResolution); //MULTI
                //MessageBox.Show("Struktrnavne fyldt i");

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

            //Nu tjekker vi om den nominelle plan har usikkerhedsscenarier. Hvis ja, så skal disse også udskrives på samme måde
            if (AllPlans[0].PlanUncertainties.Count() != 0)
            {
                foreach (var uncert in AllPlans[0].PlanUncertainties)
                {
                    if (uncert.Dose == null) continue;

                    string filename = AllPlans[0].Id.Substring(0, 4) + "_" + uncert.Id;
                    string firstLine = "Uncertainty scenario: " + uncert.DisplayName + " to nominal plan: " + AllPlans[0].Id;
                    //Data skal nu samles i en stor matrice
                    int largestDVH = FindLargestDVH(AllPlans[0], uncert, DvhResolution);
                    int numbOfStructs = FindNumberOfStructs(AllPlans[0]);

                    //Matricen kan nu oprettes da vi ved hvor stor den skal være
                    double[,] dvhList = new double[largestDVH, numbOfStructs + 1];
                    string[] idList = new string[numbOfStructs + 1];

                    double[] volList = new double[numbOfStructs];
                    FillIVols(volList, AllPlans[0]);

                    double[] minList = new double[numbOfStructs];
                    double[] maxList = new double[numbOfStructs];
                    double[] meanList = new double[numbOfStructs];

                    FillIminmaxmean(minList, maxList, meanList, AllPlans[0], DvhResolution);

                    FillValues(dvhList, AllPlans[0], largestDVH, numbOfStructs, uncert, DvhResolution);
                    FillIDs(idList, AllPlans[0]);

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

            //Struktur ider tilføjes
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

            //Tal tilføjes
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
        private void FillValues(double[,] dvhList, PlanSetup planSetup, int largestDVH, int numbOfStructs, double dvhresolution)
        {
            //Første kolonne fyldes med dosisværdier
            dvhList[0, 0] = 0.0;
            for (int j = 1; j < largestDVH; j++)
            {
                dvhList[j, 0] = dvhList[j - 1, 0] + dvhresolution;
            }


            //Her starter vi ved 1, da den første kolonne er dosis
            //Nu finder vi alle de strukturer der skal fyldes i.
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
        private void FillValues(double[,] dvhList, PlanSetup planSetup, int largestDVH, int numbOfStructs, PlanUncertainty uncert, double dvhresolution)
        {
            //Første kolonne fyldes med dosisværdier
            dvhList[0, 0] = 0.0;
            for (int j = 1; j < largestDVH; j++)
            {
                dvhList[j, 0] = dvhList[j - 1, 0] + 0.1;
            }

            //Resten fyldes med 0'er. Burde ikke være nødvendigt
            //for (int k = 0; k < largestDVH; k++)
            //{
            //    for (int m = 0; m < numbOfStructs; m++)
            //    {
            //        dvhList[k, m + 1] = 0.0;
            //    }
            //}

            //Her starter vi ved 1, da den første kolonne er dosis
            //Nu finder vi alle de strukturer der skal fyldes i.
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

            int countStruct = 1; //Her starter vi ved 1, da den første kolonne er dosis
                                 //Nu finder vi alle de strukturer der skal fyldes i.
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
            //Looper over strukturer da vi lige skal tælle.
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
            //Looper over strukturer da vi lige skal tælle.
            int dvhsize = 0;
            //double resolutiondvh = 0.1;
            for (int j = 0; j < planSetup.StructureSet.Structures.Count(); j++)
            {
                if (planSetup.StructureSet.Structures.ElementAt(j) == null || planSetup.StructureSet.Structures.ElementAt(j).IsEmpty || planSetup.StructureSet.Structures.ElementAt(j).DicomType.ToLower() == "support")
                {
                    continue;
                }

                //MessageBox.Show(planSetup.StructureSet.Structures.ElementAt(j).Id + " har dicomtypen : " + planSetup.StructureSet.Structures.ElementAt(j).DicomType.ToLower());

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
            //Looper over strukturer da vi lige skal tælle.
            int dvhsize = 0;
            //double resolutiondvh = 0.1;
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
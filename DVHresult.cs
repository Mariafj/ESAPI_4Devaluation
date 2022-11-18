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

// ABOUT
// ESAPI 4D evaluation script
// Developed at the Danish Centre for Particle Therapy by medical physicist Maria Fuglsang Jensen
// February 2022
// The script can be used to:
// Automatical recalculation of a proton, IMRT or VMAT plan on all phases of a 4D
// Perform a simple evaluation on plans calculated on all phases of a 4D
// Export of DVHs from the main plan and phase plans.
// The script is still under development.Each clinic must perform their own quality assurance of script.

using System.Linq;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
/// <summary>
/// A class for the DVH calculations.
/// </summary>
public class DVHresult
{
    public double V95CTV1 { get; set; }
    public double V95CTV2 { get; set; }
    public double V50_SC { get; set; }
    public double V50_SC2 { get; set; }
    public double V50_SC3 { get; set; }

    public double D_CTV1 { get; set; }
    public double D_CTV2 { get; set; }
    public double D_OAR { get; set; }
    public double D_OAR2 { get; set; }
    public double D_OAR3 { get; set; }

    /// <summary>
    /// A class for the DVH calculations for each plan.
    /// </summary> 
    public DVHresult(PlanSetup plan, string[] structnames, double D1, double D2, double D3, double D4, double D5)
    {
        Dose planDose = plan.Dose;

        V95CTV1 = -1000.0;
        V95CTV2 = -1000.0;
        V50_SC = -1000.0;
        V50_SC2 = -1000.0;
        V50_SC3 = -1000.0;

        D_CTV1 = D1;
        D_CTV2 = D2;
        D_OAR = D3;
        D_OAR2 = D4;
        D_OAR3 = D5;

        if (planDose == null)
        {
            return;
        }

        for (int i = 0; i < structnames.Count(); i++)
        {
            if (structnames[i] == null || structnames[i] == "Skip")
            {
                continue;
            }

            //Vi leder efter strukturen
            bool noStruct = false;
            foreach (var test in plan.StructureSet.Structures)
            {
                if (test.Id == structnames[i])
                {
                    noStruct = true;
                }
            }

            if (noStruct == false)
            {
                continue;
            }

            //CTV
            if (i == 0)
            {
                DVHData CTV1dvh = plan.GetDVHCumulativeData(plan.StructureSet.Structures.First(s => s.Id == structnames[i]), DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.01);
                DVHPoint[] dvh = CTV1dvh.CurveData;

                if (dvh.Count() == 0 || dvh.Max(d => d.DoseValue.Dose) < D_CTV1 * 0.95)
                {
                    V95CTV1 = 0;
                }
                else
                {
                    V95CTV1 = dvh.First(d => d.DoseValue.Dose >= D_CTV1 * 0.95).Volume;
                }

            }

            if (i == 1)
            {
                DVHData CTV2dvh = plan.GetDVHCumulativeData(plan.StructureSet.Structures.First(s => s.Id == structnames[i]), DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.01);
                DVHPoint[] dvh = CTV2dvh.CurveData;

                if (dvh.Count() == 0 || dvh.Max(d => d.DoseValue.Dose) < D_CTV2 * 0.95)
                {
                    V95CTV2 = 0;
                }
                else
                {
                    V95CTV2 = dvh.First(d => d.DoseValue.Dose >= D_CTV2 * 0.95).Volume;
                }

            }

            //Spinal Cord
            if (i == 2)
            {
                DVHData PSCdvh = plan.GetDVHCumulativeData(plan.StructureSet.Structures.First(s => s.Id == structnames[i]), DoseValuePresentation.Absolute, VolumePresentation.AbsoluteCm3, 0.01);
                DVHPoint[] dvh = PSCdvh.CurveData;

                if (dvh.Count() == 0 || dvh.Max(d => d.DoseValue.Dose) < D_OAR)
                {
                    V50_SC = 0;
                }
                else
                {
                    V50_SC = dvh.First(d => d.DoseValue.Dose >= D_OAR).Volume;
                }
            }
            if (i == 3)
            {
                DVHData PSC2dvh = plan.GetDVHCumulativeData(plan.StructureSet.Structures.First(s => s.Id == structnames[i]), DoseValuePresentation.Absolute, VolumePresentation.AbsoluteCm3, 0.01);
                DVHPoint[] dvh = PSC2dvh.CurveData;

                if (dvh.Count() == 0 || dvh.Max(d => d.DoseValue.Dose) < D_OAR2)
                {
                    V50_SC2 = 0;
                }
                else
                {
                    V50_SC2 = dvh.First(d => d.DoseValue.Dose >= D_OAR2).Volume;
                }
            }
            if (i == 4)
            {
                DVHData PSC3dvh = plan.GetDVHCumulativeData(plan.StructureSet.Structures.First(s => s.Id == structnames[i]), DoseValuePresentation.Absolute, VolumePresentation.AbsoluteCm3, 0.01);
                DVHPoint[] dvh = PSC3dvh.CurveData;

                if (dvh.Count() == 0 || dvh.Max(d => d.DoseValue.Dose) < D_OAR3)
                {
                    V50_SC3 = 0;
                }
                else
                {
                    V50_SC3 = dvh.First(d => d.DoseValue.Dose >= D_OAR3).Volume;
                }
            }
        }
    }


    public DVHresult(PlanSum plan, string[] structnames, double D1, double D2, double D3, double D4, double D5)
    {
        Dose planDose = plan.Dose;

        V95CTV1 = -1000.0;
        V95CTV2 = -1000.0;
        V50_SC = -1000.0;
        V50_SC2 = -1000.0;
        V50_SC3 = -1000.0;

        D_CTV1 = D1;
        D_CTV2 = D2;
        D_OAR = D3;
        D_OAR2 = D4;
        D_OAR3 = D5;

        if (planDose == null)
        {
            return;
        }

        for (int i = 0; i < structnames.Count(); i++)
        {
            if (structnames[i] == null || structnames[i] == "Skip")
            {
                continue;
            }

            //Vi leder efter strukturen
            bool noStruct = false;
            foreach (var test in plan.StructureSet.Structures)
            {
                if (test.Id == structnames[i])
                {
                    noStruct = true;
                }
            }

            if (noStruct == false)
            {
                continue;
            }

            //CTV
            if (i == 0)
            {
                DVHData CTV1dvh = plan.GetDVHCumulativeData(plan.StructureSet.Structures.First(s => s.Id == structnames[i]), DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.01);
                DVHPoint[] dvh = CTV1dvh.CurveData;

                if (dvh.Count() == 0 || dvh.Max(d => d.DoseValue.Dose) < D_CTV1 * 0.95)
                {
                    V95CTV1 = 0;
                }
                else
                {
                    V95CTV1 = dvh.First(d => d.DoseValue.Dose >= D_CTV1 * 0.95).Volume;
                }

            }

            if (i == 1)
            {
                DVHData CTV2dvh = plan.GetDVHCumulativeData(plan.StructureSet.Structures.First(s => s.Id == structnames[i]), DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.01);
                DVHPoint[] dvh = CTV2dvh.CurveData;

                if (dvh.Count() == 0 || dvh.Max(d => d.DoseValue.Dose) < D_CTV2 * 0.95)
                {
                    V95CTV2 = 0;
                }
                else
                {
                    V95CTV2 = dvh.First(d => d.DoseValue.Dose >= D_CTV2 * 0.95).Volume;
                }

            }

            //Spinal Cord
            if (i == 2)
            {
                DVHData PSCdvh = plan.GetDVHCumulativeData(plan.StructureSet.Structures.First(s => s.Id == structnames[i]), DoseValuePresentation.Absolute, VolumePresentation.AbsoluteCm3, 0.01);
                DVHPoint[] dvh = PSCdvh.CurveData;

                if (dvh.Count() == 0 || dvh.Max(d => d.DoseValue.Dose) < D_OAR)
                {
                    V50_SC = 0;
                }
                else
                {
                    V50_SC = dvh.First(d => d.DoseValue.Dose >= D_OAR).Volume;
                }
            }
            if (i == 3)
            {
                DVHData PSC2dvh = plan.GetDVHCumulativeData(plan.StructureSet.Structures.First(s => s.Id == structnames[i]), DoseValuePresentation.Absolute, VolumePresentation.AbsoluteCm3, 0.01);
                DVHPoint[] dvh = PSC2dvh.CurveData;

                if (dvh.Count() == 0 || dvh.Max(d => d.DoseValue.Dose) < D_OAR2)
                {
                    V50_SC2 = 0;
                }
                else
                {
                    V50_SC2 = dvh.First(d => d.DoseValue.Dose >= D_OAR2).Volume;
                }
            }
            if (i == 4)
            {
                DVHData PSC3dvh = plan.GetDVHCumulativeData(plan.StructureSet.Structures.First(s => s.Id == structnames[i]), DoseValuePresentation.Absolute, VolumePresentation.AbsoluteCm3, 0.01);
                DVHPoint[] dvh = PSC3dvh.CurveData;

                if (dvh.Count() == 0 || dvh.Max(d => d.DoseValue.Dose) < D_OAR3)
                {
                    V50_SC3 = 0;
                }
                else
                {
                    V50_SC3 = dvh.First(d => d.DoseValue.Dose >= D_OAR3).Volume;
                }
            }
        }
    }
}


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

using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Reflection;

[assembly: ESAPIScript(IsWriteable = true)]


[assembly: AssemblyVersion("3.0.0.9")] //Skal ændres løbende
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0")]

namespace VMS.TPS
{
    public class Script
    {
        public Script()
        {
        }

        public void Execute(ScriptContext context, System.Windows.Window window)
        {

            // Script window generation
            var mainControl = new Evaluering4D.UserControl1();
            window.Content = mainControl;
            window.Width = 1060;
            window.MaxWidth = 1060;
            window.MinWidth = 700;
            window.Height = 780;
            window.MaxHeight = 780;
            window.MinHeight = 400;
            //Patient information is saved here
            mainControl.ScriptInfo = context;

            //All open plans are selectable in the first combobox
            foreach (var plan in context.PlansInScope)
            {
                //Writing also the course name in case there are more plans with same name.
                mainControl.SelectPlan_cb.Items.Add(plan.Course.Id  + "/" + plan.Id);
            }

            foreach (var sumplan in context.PlanSumsInScope)
            {
                //Sumplans can also be evaluated
                mainControl.SelectPlan_cb.Items.Add(sumplan.Course.Id + "/" + sumplan.Id);

            }
        }
    }
}

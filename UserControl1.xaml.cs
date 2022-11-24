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

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Windows.Threading;

namespace Evaluering4D
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        //Several public parameters are used in methods and functions.
        public ScriptContext ScriptInfo; //The database content for the selected patient.

        public string errormessages = ""; //A string with all messages and errors for the user.

        public StructureSet MainStructureSet; //Independent on if a sumplan or a single plan is chosen

        // Single plans
        public PlanSetup SelectedPlan; //The main plan that is to be copied to all phases
        //As all the new plans that are copied cannot be saved before the user presses "save", therefore they are kept in these public variables.
        public PlanSetup newPlan00;
        public PlanSetup newPlan10;
        public PlanSetup newPlan20;
        public PlanSetup newPlan30;
        public PlanSetup newPlan40;
        public PlanSetup newPlan50;
        public PlanSetup newPlan60;
        public PlanSetup newPlan70;
        public PlanSetup newPlan80;
        public PlanSetup newPlan90;

        // Sum plans
        public PlanSum SelectedPlanSum; //The main sum plan that is to be copied to all phases
        //As all the new plans that are copied cannot be saved before the user presses "save", therefore they are kept in these public variables.
        public PlanSum newPlanSum00;
        public PlanSum newPlanSum10;
        public PlanSum newPlanSum20;
        public PlanSum newPlanSum30;
        public PlanSum newPlanSum40;
        public PlanSum newPlanSum50;
        public PlanSum newPlanSum60;
        public PlanSum newPlanSum70;
        public PlanSum newPlanSum80;
        public PlanSum newPlanSum90;

        //The 10 phases have a set of possible series UIDS. This is used later to find the correct image, as the names and image UIDs are not unique.
        public List<string> UID_00 = new List<string> { };
        public List<string> UID_10 = new List<string> { };
        public List<string> UID_20 = new List<string> { };
        public List<string> UID_30 = new List<string> { };
        public List<string> UID_40 = new List<string> { };
        public List<string> UID_50 = new List<string> { };
        public List<string> UID_60 = new List<string> { };
        public List<string> UID_70 = new List<string> { };
        public List<string> UID_80 = new List<string> { };
        public List<string> UID_90 = new List<string> { };

        //The 10 3D images for each phase will be saved in these variables.
        public VMS.TPS.Common.Model.API.Image img00;
        public VMS.TPS.Common.Model.API.Image img10;
        public VMS.TPS.Common.Model.API.Image img20;
        public VMS.TPS.Common.Model.API.Image img30;
        public VMS.TPS.Common.Model.API.Image img40;
        public VMS.TPS.Common.Model.API.Image img50;
        public VMS.TPS.Common.Model.API.Image img60;
        public VMS.TPS.Common.Model.API.Image img70;
        public VMS.TPS.Common.Model.API.Image img80;
        public VMS.TPS.Common.Model.API.Image img90;

        public bool[] skip_img;

        public UserControl1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// All public variables and comboboxes are cleared by this function.
        /// </summary>
        private void clearAllPublicsAndCombos()
        {
            //Publics
            SelectedPlan = null;
            SelectedPlanSum = null;
            MainStructureSet = null;

            UID_00.Clear();
            UID_10.Clear();
            UID_20.Clear();
            UID_30.Clear();
            UID_40.Clear();
            UID_50.Clear();
            UID_60.Clear();
            UID_70.Clear();
            UID_80.Clear();
            UID_90.Clear();

            img00 = null;
            img10 = null;
            img20 = null;
            img30 = null;
            img40 = null;
            img50 = null;
            img60 = null;
            img70 = null;
            img80 = null;
            img90 = null;

            newPlan00 = null;
            newPlan10 = null;
            newPlan20 = null;
            newPlan30 = null;
            newPlan40 = null;
            newPlan50 = null;
            newPlan60 = null;
            newPlan70 = null;
            newPlan80 = null;
            newPlan90 = null;

            // PlanSum extention
            newPlanSum00 = null;
            newPlanSum10 = null;
            newPlanSum20 = null;
            newPlanSum30 = null;
            newPlanSum40 = null;
            newPlanSum50 = null;
            newPlanSum60 = null;
            newPlanSum70 = null;
            newPlanSum80 = null;
            newPlanSum90 = null;

            errormessages = "";
            Errors_txt.Text = errormessages;

            //Combos
            CTV1_cb.Items.Clear();
            CTV2_cb.Items.Clear();
            Spinal_cb.Items.Clear();
            Spinal2_cb.Items.Clear();
            Spinal3_cb.Items.Clear();

            CT00_cb.Items.Clear();
            CT10_cb.Items.Clear();
            CT20_cb.Items.Clear();
            CT30_cb.Items.Clear();
            CT40_cb.Items.Clear();
            CT50_cb.Items.Clear();
            CT60_cb.Items.Clear();
            CT70_cb.Items.Clear();
            CT80_cb.Items.Clear();
            CT90_cb.Items.Clear();

            CT00_plan_cb.Items.Clear();
            CT10_plan_cb.Items.Clear();
            CT20_plan_cb.Items.Clear();
            CT30_plan_cb.Items.Clear();
            CT40_plan_cb.Items.Clear();
            CT50_plan_cb.Items.Clear();
            CT60_plan_cb.Items.Clear();
            CT70_plan_cb.Items.Clear();
            CT80_plan_cb.Items.Clear();
            CT90_plan_cb.Items.Clear();
        }

        void AllowUIToUpdate()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new DispatcherOperationCallback(delegate (object parameter)
            {
                frame.Continue = false;
                return null;
            }), null);

            Dispatcher.PushFrame(frame);
            //EDIT:
            System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                                          new Action(delegate { }));
        }

        /// <summary>
        /// Based in a string course id and a string plan id, we determine if the plan is a sumplan or not.
        /// </summary>
        /// <param name="courseid">The course id</param>
        /// <param name="planid">The plan  id</param>
        /// <returns></returns>
        private bool SumPlanDetected(string courseid, string planid)
        {
            foreach (var sumplan in ScriptInfo.PlanSumsInScope)
            {
                if (sumplan.Id == planid && sumplan.Course.Id == courseid)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The user selects the main plan or sumplan and comboboxes for structure selection and image selection are filled.
        /// </summary>
        private void SelectPlan_Click(object sender, RoutedEventArgs e)
        {
            progress_lb.Content = "... Select 4D phases below and define optional DVH points in 2. Continue hereafter the script by pressing 'Select images' in 3a. or 3b. ...";
            AllowUIToUpdate();

            //First everything is cleared.
            clearAllPublicsAndCombos();

            //Buttons that are not to be used are disabled.
            EvalDoseE_btn.IsEnabled = false;
            CopyPlan_btn.IsEnabled = false;
            ExportDVH_btn.IsEnabled = false;

            //The selected plan is found.
            string[] planname = SelectPlan_cb.SelectedItem.ToString().Split('/');
            string courseid = planname.First();
            string planid = planname.Last();

            //The selected plan is saved as a global variable and we determine if it is a sumplan or a single plan.
            if (SumPlanDetected(courseid, planid) == true)  //sum plan
            {
                PlanSum mainPlanSum = ScriptInfo.Patient.Courses.Where(c => c.Id == courseid).FirstOrDefault().PlanSums.Where(p => p.Id == planid).FirstOrDefault();
                SelectedPlanSum = mainPlanSum;
                MainStructureSet = mainPlanSum.StructureSet;
            }
            else //single plan
            {
                PlanSetup mainPlan = ScriptInfo.Patient.Courses.Where(c => c.Id == courseid).FirstOrDefault().PlanSetups.Where(p => p.Id == planid).FirstOrDefault();
                SelectedPlan = mainPlan;
                MainStructureSet = mainPlan.StructureSet;
            }

            // Combobox with structure names are filled and sorted alfabetically
            CTV1_cb.Items.Add("Skip");
            CTV2_cb.Items.Add("Skip");
            Spinal_cb.Items.Add("Skip");
            Spinal2_cb.Items.Add("Skip");
            Spinal3_cb.Items.Add("Skip");

            IEnumerable<Structure> sortedStructs = MainStructureSet.Structures.OrderBy(s => s.Id);
            foreach (var struc in sortedStructs)
            {
                CTV1_cb.Items.Add(struc.Id);
                CTV2_cb.Items.Add(struc.Id);
                Spinal_cb.Items.Add(struc.Id);
                Spinal2_cb.Items.Add(struc.Id);
                Spinal3_cb.Items.Add(struc.Id);
            }

            //"Skip" is the default structure choice
            CTV1_cb.SelectedItem = CTV1_cb.Items[0];
            CTV2_cb.SelectedItem = CTV1_cb.Items[0];
            Spinal_cb.SelectedItem = Spinal_cb.Items[0];
            Spinal2_cb.SelectedItem = Spinal2_cb.Items[0];
            Spinal3_cb.SelectedItem = Spinal2_cb.Items[0];

            //The comboboxes with CT images are filled. We will try to select only relevant images.
            //NB THIS SELECTION WILL DEPEND ON THE COMMENTS SENT FROM THE CT SCANNER!
            CT00_cb.Items.Add("skip");
            CT10_cb.Items.Add("skip");
            CT20_cb.Items.Add("skip");
            CT30_cb.Items.Add("skip");
            CT40_cb.Items.Add("skip");
            CT50_cb.Items.Add("skip");
            CT60_cb.Items.Add("skip");
            CT70_cb.Items.Add("skip");
            CT80_cb.Items.Add("skip");
            CT90_cb.Items.Add("skip");

            //All 3D images in the study belonging to the primary planning image are now looped through.
            //This is a 16.1 specific method...
            //When the images are a potential match, their series UID is saved for later use, as this is a unique number.
            foreach (var img in MainStructureSet.Image.Series.Study.Images3D)
            {
                //We do not want to calculate on a MIP
                if (img.Series.Comment.ToString().Contains("MIP"))
                {
                    continue;
                }

                if (img.Series.Comment.ToString().Contains(" 0.0%") || img.Series.Comment.ToString().Contains("TRIGGER_DELAY 0%"))
                {
                    CT00_cb.Items.Add(img.Series.Id + "/" + img.Id);
                    UID_00.Add(img.Series.UID);
                }

                if (img.Series.Comment.ToString().Contains("10.0%") || img.Series.Comment.ToString().Contains("TRIGGER_DELAY 10%"))
                {
                    CT10_cb.Items.Add(img.Series.Id + "/" + img.Id);
                    UID_10.Add(img.Series.UID);

                }

                if (img.Series.Comment.ToString().Contains("20.0%") || img.Series.Comment.ToString().Contains("TRIGGER_DELAY 20%"))
                {
                    CT20_cb.Items.Add(img.Series.Id + "/" + img.Id);
                    UID_20.Add(img.Series.UID);

                }

                if (img.Series.Comment.ToString().Contains("30.0%") || img.Series.Comment.ToString().Contains("TRIGGER_DELAY 30%"))
                {
                    CT30_cb.Items.Add(img.Series.Id + "/" + img.Id);
                    UID_30.Add(img.Series.UID);

                }

                if (img.Series.Comment.ToString().Contains("40.0%") || img.Series.Comment.ToString().Contains("TRIGGER_DELAY 40%"))
                {
                    CT40_cb.Items.Add(img.Series.Id + "/" + img.Id);
                    UID_40.Add(img.Series.UID);

                }

                if (img.Series.Comment.ToString().Contains("50.0%") || img.Series.Comment.ToString().Contains("TRIGGER_DELAY 50%"))
                {
                    CT50_cb.Items.Add(img.Series.Id + "/" + img.Id);
                    UID_50.Add(img.Series.UID);

                }

                if (img.Series.Comment.ToString().Contains("60.0%") || img.Series.Comment.ToString().Contains("TRIGGER_DELAY 60%"))
                {
                    CT60_cb.Items.Add(img.Series.Id + "/" + img.Id);
                    UID_60.Add(img.Series.UID);

                }

                if (img.Series.Comment.ToString().Contains("70.0%") || img.Series.Comment.ToString().Contains("TRIGGER_DELAY 70%"))
                {
                    CT70_cb.Items.Add(img.Series.Id + "/" + img.Id);
                    UID_70.Add(img.Series.UID);

                }

                if (img.Series.Comment.ToString().Contains("80.0%") || img.Series.Comment.ToString().Contains("TRIGGER_DELAY 80%"))
                {
                    CT80_cb.Items.Add(img.Series.Id + "/" + img.Id);
                    UID_80.Add(img.Series.UID);

                }

                if (img.Series.Comment.ToString().Contains("90.0%") || img.Series.Comment.ToString().Contains("TRIGGER_DELAY 90%"))
                {
                    CT90_cb.Items.Add(img.Series.Id + "/" + img.Id);
                    UID_90.Add(img.Series.UID);

                }
            }

            //The first image in each combobox is selected, it it exists.
            if (CT00_cb.Items.Count > 1)
            {
                CT00_cb.SelectedItem = CT00_cb.Items[1];
            }
            else
            {
                CT00_cb.SelectedItem = CT00_cb.Items[0];
            }

            if (CT10_cb.Items.Count > 1)
            {
                CT10_cb.SelectedItem = CT10_cb.Items[1];
            }
            else
            {
                CT10_cb.SelectedItem = CT10_cb.Items[0];
            }

            if (CT20_cb.Items.Count > 1)
            {
                CT20_cb.SelectedItem = CT20_cb.Items[1];
            }
            else
            {
                CT20_cb.SelectedItem = CT20_cb.Items[0];
            }

            if (CT30_cb.Items.Count > 1)
            {
                CT30_cb.SelectedItem = CT30_cb.Items[1];
            }
            else
            {
                CT30_cb.SelectedItem = CT30_cb.Items[0];
            }

            if (CT40_cb.Items.Count > 1)
            {
                CT40_cb.SelectedItem = CT40_cb.Items[1];
            }
            else
            {
                CT40_cb.SelectedItem = CT40_cb.Items[0];
            }

            if (CT50_cb.Items.Count > 1)
            {
                CT50_cb.SelectedItem = CT50_cb.Items[1];
            }
            else
            {
                CT50_cb.SelectedItem = CT50_cb.Items[0];
            }

            if (CT60_cb.Items.Count > 1)
            {
                CT60_cb.SelectedItem = CT60_cb.Items[1];
            }
            else
            {
                CT60_cb.SelectedItem = CT60_cb.Items[0];
            }

            if (CT70_cb.Items.Count > 1)
            {
                CT70_cb.SelectedItem = CT70_cb.Items[1];
            }
            else
            {
                CT70_cb.SelectedItem = CT70_cb.Items[0];
            }

            if (CT80_cb.Items.Count > 1)
            {
                CT80_cb.SelectedItem = CT80_cb.Items[1];
            }
            else
            {
                CT80_cb.SelectedItem = CT80_cb.Items[0];
            }

            if (CT90_cb.Items.Count > 1)
            {
                CT90_cb.SelectedItem = CT90_cb.Items[1];
            }
            else
            {
                CT90_cb.SelectedItem = CT90_cb.Items[0];
            }

            // Buttens for finalizing the image choice are activated
            SelectImages_btn.IsEnabled = true;
            SelectImagesE_btn.IsEnabled = true;

            //Errormessages are written in the UI.
            Errors_txt.Text = errormessages;
        }
        /// <summary>
        /// Finds the correct 3D image given a seried UID list and a string defining the selected phase
        /// </summary>
        /// <param name="uid_list">A list of UIDs</param>
        /// <param name="v">The image id</param>
        /// <param name="phase">The phase identification string</param>
        /// <returns></returns>
        private VMS.TPS.Common.Model.API.Image FindCorrectImage(List<string> uid_list, string v, string phase)
        {
            //All images with the correct name are selected but only one is needed.
            //(The tests of this function are probably not all needed anymore after I implemented the seriesUID check.)

            // The user chose to skip this phase
            if (v == "skip")
            {
                //If no image is choosen an error is written to the UI.
                errormessages += "Phase: " + phase + " is skipped \n";
                return null;
            }

            IEnumerable<VMS.TPS.Common.Model.API.Image> temp = MainStructureSet.Image.Series.Study.Images3D.Where(p => p.Series.Id + "/" + p.Id == v);

            if (uid_list.Count() > 1 && temp.Count() > 1) //Sevral UIDs and several CTs with the same name
            {
                //We are adding a message if there are more CTs with the same name AND several possible series IDs.
                errormessages += phase + ":NB several phases have the same id. Please verify that the plans are created correctly.\n";

                foreach (var item in temp)
                {
                    if ((item.Series.Id + "/" + item.Id) == v && uid_list.Contains(item.Series.UID))
                    {
                        return item;
                    }
                }
            }
            else if (temp.Count() > 1) //Several CTs but only one correct UID. No need for a message.
            {
                foreach (var item in temp)
                {
                    if ((item.Series.Id + "/" + item.Id) == v && uid_list.Contains(item.Series.UID))
                    {
                        return item;
                    }
                }
            }
            else //Only one CT og one series UID
            {
                return temp.First();
            }

            //If no image is choosen an error is written to the UI.
            errormessages += "No image was selected for " + phase + "\n";
            return null;
        }

        /// <summary>
        /// Selecting images for evaluation only.
        /// </summary>
        private void SelectImagesE_btn_Click(object sender, RoutedEventArgs e)
        {
            bool writeYN = false;
            SelectImages(writeYN);
        }

        /// <summary>
        /// Selecting images for copy and recalculaton of the main plan.
        /// </summary>
        private void SelectImages_btn_Click(object sender, RoutedEventArgs e)
        {
            bool writeYN = true;
            SelectImages(writeYN);
        }

        /// <summary>
        /// The images are selected and saved to the public variables
        /// If there are plans calculated on the images, they will be added to the plan-comboboxes.
        /// The function destinquished between the writable version and the non-writable by using the bookean writeYN
        /// </summary>
        /// <param name="writeYN">Boolean determining if the script will evaluate or create 4D plans</param>
        private void SelectImages(bool writeYN)
        {
            if (writeYN == true) //The script will create new plans on the phases
            {
                progress_lb.Content = "... Images are selected. Press 'Create plans' to continue the script ...";
                AllowUIToUpdate();
            }
            else //The script will evaluate already created plans on the phases
            {
                progress_lb.Content = "... Images are selected. Choose the plans to evaluate an press 'Evaluate plans' ...";
                AllowUIToUpdate();
            }

            //The selected images are found by using the function "findCorrectImage".
            img00 = FindCorrectImage(UID_00, CT00_cb.SelectedItem.ToString(), "phase 00");
            img10 = FindCorrectImage(UID_10, CT10_cb.SelectedItem.ToString(), "phase 10");
            img20 = FindCorrectImage(UID_20, CT20_cb.SelectedItem.ToString(), "phase 20");
            img30 = FindCorrectImage(UID_30, CT30_cb.SelectedItem.ToString(), "phase 30");
            img40 = FindCorrectImage(UID_40, CT40_cb.SelectedItem.ToString(), "phase 40");
            img50 = FindCorrectImage(UID_50, CT50_cb.SelectedItem.ToString(), "phase 50");
            img60 = FindCorrectImage(UID_60, CT60_cb.SelectedItem.ToString(), "phase 60");
            img70 = FindCorrectImage(UID_70, CT70_cb.SelectedItem.ToString(), "phase 70");
            img80 = FindCorrectImage(UID_80, CT80_cb.SelectedItem.ToString(), "phase 80");
            img90 = FindCorrectImage(UID_90, CT90_cb.SelectedItem.ToString(), "phase 90");

            //The plan comboboxes are cleared and ready to be filled after
            CT00_plan_cb.Items.Clear();
            CT10_plan_cb.Items.Clear();
            CT20_plan_cb.Items.Clear();
            CT30_plan_cb.Items.Clear();
            CT40_plan_cb.Items.Clear();
            CT50_plan_cb.Items.Clear();
            CT60_plan_cb.Items.Clear();
            CT70_plan_cb.Items.Clear();
            CT80_plan_cb.Items.Clear();
            CT90_plan_cb.Items.Clear();

            // It is possible to select a plan on the image if there is one.
            // Only plans in the same course as the selected plans are checked.
            // We used the series UID as this is unique.
            if (SelectedPlanSum == null) //Single plans
            {
                foreach (var plan in SelectedPlan.Course.PlanSetups)
                {
                    if (img00 != null && plan.StructureSet.Image.Series.UID == img00.Series.UID && plan.StructureSet.Image.Id == img00.Id) CT00_plan_cb.Items.Add(plan.Id);

                    if (img10 != null && plan.StructureSet.Image.Series.UID == img10.Series.UID && plan.StructureSet.Image.Id == img10.Id) CT10_plan_cb.Items.Add(plan.Id);

                    if (img20 != null && plan.StructureSet.Image.Series.UID == img20.Series.UID && plan.StructureSet.Image.Id == img20.Id) CT20_plan_cb.Items.Add(plan.Id);

                    if (img30 != null && plan.StructureSet.Image.Series.UID == img30.Series.UID && plan.StructureSet.Image.Id == img30.Id) CT30_plan_cb.Items.Add(plan.Id);

                    if (img40 != null && plan.StructureSet.Image.Series.UID == img40.Series.UID && plan.StructureSet.Image.Id == img40.Id) CT40_plan_cb.Items.Add(plan.Id);

                    if (img50 != null && plan.StructureSet.Image.Series.UID == img50.Series.UID && plan.StructureSet.Image.Id == img50.Id) CT50_plan_cb.Items.Add(plan.Id);

                    if (img60 != null && plan.StructureSet.Image.Series.UID == img60.Series.UID && plan.StructureSet.Image.Id == img60.Id) CT60_plan_cb.Items.Add(plan.Id);

                    if (img70 != null && plan.StructureSet.Image.Series.UID == img70.Series.UID && plan.StructureSet.Image.Id == img70.Id) CT70_plan_cb.Items.Add(plan.Id);

                    if (img80 != null && plan.StructureSet.Image.Series.UID == img80.Series.UID && plan.StructureSet.Image.Id == img80.Id) CT80_plan_cb.Items.Add(plan.Id);

                    if (img90 != null && plan.StructureSet.Image.Series.UID == img90.Series.UID && plan.StructureSet.Image.Id == img90.Id) CT90_plan_cb.Items.Add(plan.Id);

                }
            }
            else // Plan sums
            {
                foreach (var plan in SelectedPlanSum.Course.PlanSums)
                {
                    if (img00 != null && plan.StructureSet.Image.Series.UID == img00.Series.UID && plan.StructureSet.Image.Id == img00.Id) CT00_plan_cb.Items.Add(plan.Id);

                    if (img10 != null && plan.StructureSet.Image.Series.UID == img10.Series.UID && plan.StructureSet.Image.Id == img10.Id) CT10_plan_cb.Items.Add(plan.Id);

                    if (img20 != null && plan.StructureSet.Image.Series.UID == img20.Series.UID && plan.StructureSet.Image.Id == img20.Id) CT20_plan_cb.Items.Add(plan.Id);

                    if (img30 != null && plan.StructureSet.Image.Series.UID == img30.Series.UID && plan.StructureSet.Image.Id == img30.Id) CT30_plan_cb.Items.Add(plan.Id);

                    if (img40 != null && plan.StructureSet.Image.Series.UID == img40.Series.UID && plan.StructureSet.Image.Id == img40.Id) CT40_plan_cb.Items.Add(plan.Id);

                    if (img50 != null && plan.StructureSet.Image.Series.UID == img50.Series.UID && plan.StructureSet.Image.Id == img50.Id) CT50_plan_cb.Items.Add(plan.Id);

                    if (img60 != null && plan.StructureSet.Image.Series.UID == img60.Series.UID && plan.StructureSet.Image.Id == img60.Id) CT60_plan_cb.Items.Add(plan.Id);

                    if (img70 != null && plan.StructureSet.Image.Series.UID == img70.Series.UID && plan.StructureSet.Image.Id == img70.Id) CT70_plan_cb.Items.Add(plan.Id);

                    if (img80 != null && plan.StructureSet.Image.Series.UID == img80.Series.UID && plan.StructureSet.Image.Id == img80.Id) CT80_plan_cb.Items.Add(plan.Id);

                    if (img90 != null && plan.StructureSet.Image.Series.UID == img90.Series.UID && plan.StructureSet.Image.Id == img90.Id) CT90_plan_cb.Items.Add(plan.Id);

                }
            }

            // If the user wants to create new plans the correct buttons are enables
            if (writeYN)
            {
                //PrepareImages_btn.IsEnabled = true;
                CopyPlan_btn.IsEnabled = true;
            }

            // If the user wants to evaluate existing plans the correct buttons are enables
            // And if the plans are not to be copied, we try to select the plans that already exist
            if (!writeYN)
            {
                EvalDoseE_btn.IsEnabled = true;

                try
                {
                    CT00_plan_cb.SelectedItem = CT00_plan_cb.Items[0];
                }
                catch (Exception)
                {

                }

                try
                {
                    CT10_plan_cb.SelectedItem = CT10_plan_cb.Items[0];
                }
                catch (Exception)
                {

                }

                try
                {
                    CT20_plan_cb.SelectedItem = CT20_plan_cb.Items[0];
                }
                catch (Exception)
                {

                }
                try
                {
                    CT30_plan_cb.SelectedItem = CT30_plan_cb.Items[0];
                }
                catch (Exception)
                {

                }

                try
                {
                    CT40_plan_cb.SelectedItem = CT40_plan_cb.Items[0];
                }
                catch (Exception)
                {

                }

                try
                {
                    CT50_plan_cb.SelectedItem = CT50_plan_cb.Items[0];
                }
                catch (Exception)
                {

                }

                try
                {
                    CT60_plan_cb.SelectedItem = CT60_plan_cb.Items[0];
                }
                catch (Exception)
                {

                }

                try
                {
                    CT70_plan_cb.SelectedItem = CT70_plan_cb.Items[0];
                }
                catch (Exception)
                {

                }

                try
                {
                    CT80_plan_cb.SelectedItem = CT80_plan_cb.Items[0];
                }
                catch (Exception)
                {

                }
                try
                {
                    CT90_plan_cb.SelectedItem = CT90_plan_cb.Items[0];
                }
                catch (Exception)
                {

                }
            }
            Errors_txt.Text = errormessages;
        }

        /// <summary>
        /// The plans are copied to the phases and new buttons are enabled.
        /// The copy process depends on the plan type.
        /// </summary>
        private void CopyPlan_btn_Click(object sender, RoutedEventArgs e)
        {
            ScriptInfo.Patient.BeginModifications();

            string[] body = new string[10];
            string[] calib = new string[10];
            string[] overwrite = new string[10];

            VMS.TPS.Common.Model.API.Image[] imageList = new VMS.TPS.Common.Model.API.Image[10] { img00, img10, img20, img30, img40, img50, img60, img70, img80, img90 };

            AdjustPhaseImages adjustPhaseImages = new AdjustPhaseImages(imageList, ScriptInfo, MainStructureSet);
       
            if (body_chb.IsChecked == true)
            {
                progress_lb.Content = "... creating structure sets and BODY ...";
                AllowUIToUpdate();

                body = adjustPhaseImages.CreateBody();
            }

            if (copybody_chb.IsChecked == true)
            {
                progress_lb.Content = "... copying BODY ...";
                AllowUIToUpdate();

                body = adjustPhaseImages.CopyBody();
            }

            if (calib_chb.IsChecked == true)
            {
                progress_lb.Content = "... Setting calibration curves ...";
                AllowUIToUpdate();
                calib = adjustPhaseImages.CopyCalibration();
            }

            if (overw_chb.IsChecked == true)
            {
                progress_lb.Content = "... Copying and overwriting structures ...";
                AllowUIToUpdate();
                overwrite = adjustPhaseImages.OverwriteStructures();
            }

            for (int i = 0; i < 10; i++)
            {
                if (body[i] != "" || calib[i] != "" || overwrite[i] != "")
                {
                    errormessages += " PHASE " + i.ToString() + " : \n";
                    errormessages += body[i];
                    errormessages += calib[i];
                    errormessages += overwrite[i];
                }
            }

            Errors_txt.Text = errormessages;

            if (SelectedPlan != null && SelectedPlan.PlanType.ToString().Contains("Proton")) //single plan and proton plan
            {
                progress_lb.Content = "... Copying proton plans ...";
                AllowUIToUpdate();
                CopyProtons();
            }
            else if (SelectedPlan != null) // single plan and photon plan
            {
                progress_lb.Content = "... Copying photon plans ...";
                AllowUIToUpdate();
                CopyPhotons();
            }
            if (SelectedPlanSum != null && SelectedPlanSum.PlanSetups.First().PlanType.ToString().Contains("Proton")) //Sum plan and proton plan
            {
                progress_lb.Content = "... Copying proton sum plans ...";
                AllowUIToUpdate();
                CopyProtonsSum();
            }
            else if (SelectedPlanSum != null) //Sum plan and photon plan
            {
                progress_lb.Content = "... Copying photon sum plans ...";
                AllowUIToUpdate();
                CopyPhotonsSum();
            }

            // The evaluation button is pressed automatically in this case.
            EvalDoseE_btn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            EvalDoseE_btn.IsEnabled = true;
            Errors_txt.Text = errormessages;
        }

        /// <summary>
        /// The photon sumplan is copied to the phases
        /// </summary>
        private void CopyPhotonsSum()
        {
            //Name of the photonplan. TODO: this is a bit stupid, as if the name already exists the script will crash...
            string ph_prefix = FindPrefixForPlans();
            if (ph_prefix == null)
            {
                return;
            }

            //The photon sum plans are copied and calculated
            //The plans are added to the comboboxes.
            //The plans are saved in the public variables.
            //The UI is updated
            List<ExternalPlanSetup> list0 = new List<ExternalPlanSetup> ();
            List<ExternalPlanSetup> list1 = new List<ExternalPlanSetup> ();
            List<ExternalPlanSetup> list2 = new List<ExternalPlanSetup> ();
            List<ExternalPlanSetup> list3 = new List<ExternalPlanSetup> ();
            List<ExternalPlanSetup> list4 = new List<ExternalPlanSetup> ();
            List<ExternalPlanSetup> list5 = new List<ExternalPlanSetup> ();
            List<ExternalPlanSetup> list6 = new List<ExternalPlanSetup> ();
            List<ExternalPlanSetup> list7 = new List<ExternalPlanSetup> ();
            List<ExternalPlanSetup> list8 = new List<ExternalPlanSetup> ();
            List<ExternalPlanSetup> list9 = new List<ExternalPlanSetup> ();

            for (int i = 0; i < SelectedPlanSum.PlanSetups.Count(); i++)
            {
                var copyFrom = SelectedPlanSum.PlanSetups.ElementAt(i);


                progress_lb.Content = "... Copying to phase 00 ...";
                AllowUIToUpdate();
                list0.Add(CalcPhoton(img00, rec_00, ph_prefix + "00_" + i.ToString(), copyFrom));


                progress_lb.Content = "... Copying to phase 10 ...";
                AllowUIToUpdate();
                list1.Add(CalcPhoton(img10, rec_10, ph_prefix + "10_" + i.ToString(), copyFrom));
                

                progress_lb.Content = "... Copying to phase 20 ...";
                AllowUIToUpdate();
                list2.Add(CalcPhoton(img20, rec_20, ph_prefix + "20_" + i.ToString(), copyFrom));


                progress_lb.Content = "... Copying to phase 30 ...";
                AllowUIToUpdate();
                list3.Add(CalcPhoton(img30, rec_30, ph_prefix + "30_" + i.ToString(), copyFrom));


                progress_lb.Content = "... Copying to phase 40 ...";
                AllowUIToUpdate();
                list4.Add(CalcPhoton(img40, rec_40, ph_prefix + "40_" + i.ToString(), copyFrom));


                progress_lb.Content = "... Copying to phase 50 ...";
                AllowUIToUpdate();
                list5.Add(CalcPhoton(img50, rec_50, ph_prefix + "50_" + i.ToString(), copyFrom));


                progress_lb.Content = "... Copying to phase 60 ...";
                AllowUIToUpdate();
                list6.Add(CalcPhoton(img60, rec_60, ph_prefix + "60_" + i.ToString(), copyFrom));


                progress_lb.Content = "... Copying to phase 70 ...";
                AllowUIToUpdate();
                list7.Add(CalcPhoton(img70, rec_70, ph_prefix + "70_" + i.ToString(), copyFrom));


                progress_lb.Content = "... Copying to phase 80 ...";
                AllowUIToUpdate();
                list8.Add(CalcPhoton(img80, rec_80, ph_prefix + "80_" + i.ToString(), copyFrom));


                progress_lb.Content = "... Copying to phase 90 ...";
                AllowUIToUpdate();
                list9.Add(CalcPhoton(img90, rec_90, ph_prefix + "90_" + i.ToString(), copyFrom));


                progress_lb.Content = "...All plans are copied ...";
                AllowUIToUpdate();
            }

            if (img00 != null)
            {
                newPlanSum00 = SelectedPlanSum.Course.CreatePlanSum(list0, img00);
                newPlanSum00.Id = "Sum_" + ph_prefix + "00";
                AddPhasePlanSum(CT00_plan_cb, newPlanSum00);

            }
            if (img10 != null)
            {
                newPlanSum10 = SelectedPlanSum.Course.CreatePlanSum(list1, img10);
                newPlanSum10.Id = "Sum_" + ph_prefix + "10";
                AddPhasePlanSum(CT10_plan_cb, newPlanSum10);

            }
            if (img20 != null)
            {
                newPlanSum20 = SelectedPlanSum.Course.CreatePlanSum(list2, img20);
                newPlanSum20.Id = "Sum_" + ph_prefix + "20";
                AddPhasePlanSum(CT20_plan_cb, newPlanSum20);

            }
            if (img30 != null)
            {
                newPlanSum30 = SelectedPlanSum.Course.CreatePlanSum(list3, img30);
                newPlanSum30.Id = "Sum_" + ph_prefix + "30";
                AddPhasePlanSum(CT30_plan_cb, newPlanSum30);

            }
            if (img40 != null)
            {
                newPlanSum40 = SelectedPlanSum.Course.CreatePlanSum(list4, img40);
                newPlanSum40.Id = "Sum_" + ph_prefix + "40";
                AddPhasePlanSum(CT40_plan_cb, newPlanSum40);

            }
            if (img50 != null)
            {
                newPlanSum50 = SelectedPlanSum.Course.CreatePlanSum(list5, img50);
                newPlanSum50.Id = "Sum_" + ph_prefix + "50";
                AddPhasePlanSum(CT50_plan_cb, newPlanSum50);

            }
            if (img60 != null)
            {
                newPlanSum60 = SelectedPlanSum.Course.CreatePlanSum(list6, img60);
                newPlanSum60.Id = "Sum_" + ph_prefix + "60";
                AddPhasePlanSum(CT60_plan_cb, newPlanSum60);

            }
            if (img70 != null)
            {
                newPlanSum70 = SelectedPlanSum.Course.CreatePlanSum(list7, img70);
                newPlanSum70.Id = "Sum_" + ph_prefix + "70";
                AddPhasePlanSum(CT70_plan_cb, newPlanSum70);

            }
            if (img80 != null)
            {
                newPlanSum80 = SelectedPlanSum.Course.CreatePlanSum(list8, img80);
                newPlanSum80.Id = "Sum_" + ph_prefix + "80";
                AddPhasePlanSum(CT80_plan_cb, newPlanSum80);

            }
            if (img90 != null)
            {
                newPlanSum90 = SelectedPlanSum.Course.CreatePlanSum(list9, img90);
                newPlanSum90.Id = "Sum_" + ph_prefix + "90";
                AddPhasePlanSum(CT90_plan_cb, newPlanSum90);
            }
        }

        /// <summary>
        /// Sumplans on the phase images are added to the plan comboboxes
        /// </summary>
        /// <param name="plan_cb">The plan combobox in the UI</param>
        /// <param name="plan">The plan sum</param>
        private void AddPhasePlanSum(ComboBox plan_cb, PlanSum plan)
        {
            if (plan_cb.SelectedItem == null && plan != null)
            {
                plan_cb.Items.Add(plan.Id);
                int itemno = plan_cb.Items.Count;
                plan_cb.SelectedItem = plan_cb.Items[itemno - 1];
            }
        }

        /// <summary>
        /// The proton sumplan is copied to the phases
        /// </summary>
        private void CopyProtonsSum()
        {
            string pro_prefix = FindPrefixForPlans();
            if (pro_prefix == null)
            {
                return;
            }

            //The proton plans are copied and calculated
            //The plans are added to the comboboxes.
            //The plans are saved in the public variables.
            //The UI is updated

            List<IonPlanSetup> list0 = new List<IonPlanSetup>();
            List<IonPlanSetup> list1 = new List<IonPlanSetup>();
            List<IonPlanSetup> list2 = new List<IonPlanSetup>();
            List<IonPlanSetup> list3 = new List<IonPlanSetup>();
            List<IonPlanSetup> list4 = new List<IonPlanSetup>();
            List<IonPlanSetup> list5 = new List<IonPlanSetup>();
            List<IonPlanSetup> list6 = new List<IonPlanSetup>();
            List<IonPlanSetup> list7 = new List<IonPlanSetup>();
            List<IonPlanSetup> list8 = new List<IonPlanSetup>();
            List<IonPlanSetup> list9 = new List<IonPlanSetup>();

            for (int i = 0; i < SelectedPlanSum.PlanSetups.Count(); i++)
            {
                var copyFrom = SelectedPlanSum.PlanSetups.ElementAt(i);

                progress_lb.Content = "... Copying to phase 00 ...";
                AllowUIToUpdate();
                list0.Add(CalcProton(img00, rec_00, pro_prefix + "00_" + i.ToString(), copyFrom));


                progress_lb.Content = "... Copying to phase 10 ...";
                AllowUIToUpdate();
                list0.Add(CalcProton(img10, rec_10, pro_prefix + "10_" + i.ToString(), copyFrom));


                progress_lb.Content = "... Copying to phase 20 ...";
                AllowUIToUpdate();
                list0.Add(CalcProton(img20, rec_20, pro_prefix + "20_" + i.ToString(), copyFrom));


                progress_lb.Content = "... Copying to phase 30 ...";
                AllowUIToUpdate();
                list0.Add(CalcProton(img30, rec_30, pro_prefix + "30_" + i.ToString(), copyFrom));


                progress_lb.Content = "... Copying to phase 40 ...";
                AllowUIToUpdate();
                list0.Add(CalcProton(img40, rec_40, pro_prefix + "40_" + i.ToString(), copyFrom));


                progress_lb.Content = "... Copying to phase 50 ...";
                AllowUIToUpdate();
                list0.Add(CalcProton(img50, rec_50, pro_prefix + "50_" + i.ToString(), copyFrom));


                progress_lb.Content = "... Copying to phase 60 ...";
                AllowUIToUpdate();
                list0.Add(CalcProton(img60, rec_60, pro_prefix + "60_" + i.ToString(), copyFrom));


                progress_lb.Content = "... Copying to phase 70 ...";
                AllowUIToUpdate();
                list0.Add(CalcProton(img70, rec_70, pro_prefix + "70_" + i.ToString(), copyFrom));


                progress_lb.Content = "... Copying to phase 80 ...";
                AllowUIToUpdate();
                list0.Add(CalcProton(img80, rec_80, pro_prefix + "80_" + i.ToString(), copyFrom));


                progress_lb.Content = "... Copying to phase 90 ...";
                AllowUIToUpdate();
                list0.Add(CalcProton(img90, rec_90, pro_prefix + "90_" + i.ToString(), copyFrom));


                progress_lb.Content = "...All plans are copied ...";
                AllowUIToUpdate();
            }

            if (img00 != null)
            {
                newPlanSum00 = SelectedPlanSum.Course.CreatePlanSum(list0, img00);
                newPlanSum00.Id = "Sum_" + pro_prefix + "00";
                AddPhasePlanSum(CT00_plan_cb, newPlanSum00);

            }
            if (img10 != null)
            {
                newPlanSum10 = SelectedPlanSum.Course.CreatePlanSum(list1, img10);
                newPlanSum10.Id = "Sum_" + pro_prefix + "10";
                AddPhasePlanSum(CT10_plan_cb, newPlanSum10);

            }
            if (img20 != null)
            {
                newPlanSum20 = SelectedPlanSum.Course.CreatePlanSum(list2, img20);
                newPlanSum20.Id = "Sum_" + pro_prefix + "20";
                AddPhasePlanSum(CT20_plan_cb, newPlanSum20);

            }
            if (img30 != null)
            {
                newPlanSum30 = SelectedPlanSum.Course.CreatePlanSum(list3, img30);
                newPlanSum30.Id = "Sum_" + pro_prefix + "30";
                AddPhasePlanSum(CT30_plan_cb, newPlanSum30);

            }
            if (img40 != null)
            {
                newPlanSum40 = SelectedPlanSum.Course.CreatePlanSum(list4, img40);
                newPlanSum40.Id = "Sum_" + pro_prefix + "40";
                AddPhasePlanSum(CT40_plan_cb, newPlanSum40);

            }
            if (img50 != null)
            {
                newPlanSum50 = SelectedPlanSum.Course.CreatePlanSum(list5, img50);
                newPlanSum50.Id = "Sum_" + pro_prefix + "50";
                AddPhasePlanSum(CT50_plan_cb, newPlanSum50);

            }
            if (img60 != null)
            {
                newPlanSum60 = SelectedPlanSum.Course.CreatePlanSum(list6, img60);
                newPlanSum60.Id = "Sum_" + pro_prefix + "60";
                AddPhasePlanSum(CT60_plan_cb, newPlanSum60);

            }
            if (img70 != null)
            {
                newPlanSum70 = SelectedPlanSum.Course.CreatePlanSum(list7, img70);
                newPlanSum70.Id = "Sum_" + pro_prefix + "70";
                AddPhasePlanSum(CT70_plan_cb, newPlanSum70);

            }
            if (img80 != null)
            {
                newPlanSum80 = SelectedPlanSum.Course.CreatePlanSum(list8, img80);
                newPlanSum80.Id = "Sum_" + pro_prefix + "80";
                AddPhasePlanSum(CT80_plan_cb, newPlanSum80);

            }
            if (img90 != null)
            {
                newPlanSum90 = SelectedPlanSum.Course.CreatePlanSum(list9, img90);
                newPlanSum90.Id = "Sum_" + pro_prefix + "90";
                AddPhasePlanSum(CT90_plan_cb, newPlanSum90);
            }
        }

        /// <summary>
        /// Copying a selected photon plan to all phases and calculating the dose.
        /// </summary>
        private void CopyPhotons()
        {
            //Name of the photonplan. TODO: this is a bit stupid, as if the name already exists the script will crash...
            string ph_prefix = FindPrefixForPlans();
            if (ph_prefix == null)
            {
                return;
            }

            //The photon plans are copied and calculated
            //The plans are added to the comboboxes.
            //The plans are saved in the public variables.
            //The UI is updated

            progress_lb.Content = "... Copying to phase 00 ...";
            AllowUIToUpdate();

            ExternalPlanSetup Plan00 = CalcPhoton(img00, rec_00, ph_prefix + "00", SelectedPlan);
            AddPhasePlan(CT00_plan_cb, Plan00);
            newPlan00 = Plan00;
            progress_lb.Content = "... Copying to phase 10 ...";
            AllowUIToUpdate();

            ExternalPlanSetup Plan10 = CalcPhoton(img10, rec_10, ph_prefix + "10", SelectedPlan);
            AddPhasePlan(CT10_plan_cb, Plan10);
            newPlan10 = Plan10;
            progress_lb.Content = "... Copying to phase 20 ...";
            AllowUIToUpdate();

            ExternalPlanSetup Plan20 = CalcPhoton(img20, rec_20, ph_prefix + "20", SelectedPlan);
            AddPhasePlan(CT20_plan_cb, Plan20);
            newPlan20 = Plan20;
            progress_lb.Content = "... Copying to phase 30 ...";
            AllowUIToUpdate();

            ExternalPlanSetup Plan30 = CalcPhoton(img30, rec_30, ph_prefix + "30", SelectedPlan);
            AddPhasePlan(CT30_plan_cb, Plan30);
            newPlan30 = Plan30;
            progress_lb.Content = "... Copying to phase 40 ...";
            AllowUIToUpdate();

            ExternalPlanSetup Plan40 = CalcPhoton(img40, rec_40, ph_prefix + "40", SelectedPlan);
            AddPhasePlan(CT40_plan_cb, Plan40);
            newPlan40 = Plan40;
            progress_lb.Content = "... Copying to phase 50 ...";
            AllowUIToUpdate();

            ExternalPlanSetup Plan50 = CalcPhoton(img50, rec_50, ph_prefix + "50", SelectedPlan);
            AddPhasePlan(CT50_plan_cb, Plan50);
            newPlan50 = Plan50;
            progress_lb.Content = "... Copying to phase 60 ...";
            AllowUIToUpdate();

            ExternalPlanSetup Plan60 = CalcPhoton(img60, rec_60, ph_prefix + "60", SelectedPlan);
            AddPhasePlan(CT60_plan_cb, Plan60);
            newPlan60 = Plan60;
            progress_lb.Content = "... Copying to phase 70 ...";
            AllowUIToUpdate();


            ExternalPlanSetup Plan70 = CalcPhoton(img70, rec_70, ph_prefix + "70", SelectedPlan);
            AddPhasePlan(CT70_plan_cb, Plan70);
            newPlan70 = Plan70;
            progress_lb.Content = "... Copying to phase 80 ...";

            AllowUIToUpdate();

            ExternalPlanSetup Plan80 = CalcPhoton(img80, rec_80, ph_prefix + "80", SelectedPlan);
            AddPhasePlan(CT80_plan_cb, Plan80);
            newPlan80 = Plan80;
            progress_lb.Content = "... Copying to phase 90 ...";

            AllowUIToUpdate();

            ExternalPlanSetup Plan90 = CalcPhoton(img90, rec_90, ph_prefix + "90", SelectedPlan);
            AddPhasePlan(CT90_plan_cb, Plan90);
            newPlan90 = Plan90;
            progress_lb.Content = "...All plans are copied ...";
            AllowUIToUpdate();
        }

        /// <summary>
        /// Copying a selected proton plan to all phases and calculating the dose.
        /// </summary>
        private void CopyProtons()
        {
            string pro_prefix = FindPrefixForPlans();
            if (pro_prefix == null)
            {
                return;
            }

            //The photon plans are copied and calculated
            //The plans are added to the comboboxes.
            //The plans are saved in the public variables.
            //The UI is updated


            progress_lb.Content = "... Copying to phase 00 ...";
            AllowUIToUpdate();

            IonPlanSetup Plan00 = CalcProton(img00, rec_00, pro_prefix + "00", SelectedPlan);
            AddPhasePlan(CT00_plan_cb, Plan00);
            newPlan00 = Plan00;
            progress_lb.Content = "... Copying to phase 10 ...";

            AllowUIToUpdate();

            IonPlanSetup Plan10 = CalcProton(img10, rec_10, pro_prefix + "10", SelectedPlan);
            AddPhasePlan(CT10_plan_cb, Plan10);
            newPlan10 = Plan10;
            progress_lb.Content = "... Copying to phase 20 ...";

            AllowUIToUpdate();

            IonPlanSetup Plan20 = CalcProton(img20, rec_20, pro_prefix + "20", SelectedPlan);
            AddPhasePlan(CT20_plan_cb, Plan20);
            newPlan20 = Plan20;
            progress_lb.Content = "... Copying to phase 30 ...";

            AllowUIToUpdate();

            IonPlanSetup Plan30 = CalcProton(img30, rec_30, pro_prefix + "30", SelectedPlan);
            AddPhasePlan(CT30_plan_cb, Plan30);
            newPlan30 = Plan30;
            progress_lb.Content = "... Copying to phase 40 ...";

            AllowUIToUpdate();

            IonPlanSetup Plan40 = CalcProton(img40, rec_40, pro_prefix + "40", SelectedPlan);
            AddPhasePlan(CT40_plan_cb, Plan40);
            newPlan40 = Plan40;
            progress_lb.Content = "... Copying to phase 50 ...";

            AllowUIToUpdate();

            IonPlanSetup Plan50 = CalcProton(img50, rec_50, pro_prefix + "50", SelectedPlan);
            AddPhasePlan(CT50_plan_cb, Plan50);
            newPlan50 = Plan50;
            progress_lb.Content = "... Copying to phase 60 ...";

            AllowUIToUpdate();

            IonPlanSetup Plan60 = CalcProton(img60, rec_60, pro_prefix + "60", SelectedPlan);
            AddPhasePlan(CT60_plan_cb, Plan60);
            newPlan60 = Plan60;
            progress_lb.Content = "... Copying to phase 70 ...";

            AllowUIToUpdate();

            IonPlanSetup Plan70 = CalcProton(img70, rec_70, pro_prefix + "70", SelectedPlan);
            AddPhasePlan(CT70_plan_cb, Plan70);
            newPlan70 = Plan70;
            progress_lb.Content = "... Copying to phase 80 ...";


            AllowUIToUpdate();

            IonPlanSetup Plan80 = CalcProton(img80, rec_80, pro_prefix + "80", SelectedPlan);
            AddPhasePlan(CT80_plan_cb, Plan80);
            newPlan80 = Plan80;
            progress_lb.Content = "... Copying to phase 90 ...";

            AllowUIToUpdate();

            IonPlanSetup Plan90 = CalcProton(img90, rec_90, pro_prefix + "90", SelectedPlan);
            AddPhasePlan(CT90_plan_cb, Plan90);
            newPlan90 = Plan90;
            progress_lb.Content = "... All plans are copied ...";

            AllowUIToUpdate();
        }

        /// <summary>
        /// A free prefix is found for all the new 4D plans
        /// </summary>
        /// <returns></returns>
        private string FindPrefixForPlans()
        {
            string planID = "";
            
            if (SelectedPlan != null)
            {
                planID = SelectedPlan.Id;
            }
            else
            {
                planID = SelectedPlanSum.Id;
            }
            
            string pro_prefix = planID.Substring(0, 2) + "_";
            if (NameIsNotUnique(pro_prefix))
            {
                return pro_prefix;
            }
            pro_prefix = planID.Substring(0, 2) + "1_";
            if (NameIsNotUnique(pro_prefix))
            {
                return pro_prefix;
            }
            pro_prefix = planID.Substring(0, 2) + "2_";
            if (NameIsNotUnique(pro_prefix))
            {
                return pro_prefix;
            }
            pro_prefix = planID.Substring(0, 2) + "3_";
            if (NameIsNotUnique(pro_prefix))
            {
                return pro_prefix;
            }
            pro_prefix = planID.Substring(0, 2) + "4_";
            if (NameIsNotUnique(pro_prefix))
            {
                return pro_prefix;
            }
            return null;
        }

        /// <summary>
        /// Determining if a selected prefix is unique in the course
        /// </summary>
        /// <param name="prefix">The plan prefix</param>
        /// <returns></returns>
        private bool NameIsNotUnique(string prefix)
        {

            Course courseID = null;

            if (SelectedPlan != null)
            {
                courseID = SelectedPlan.Course;
            }
            else
            {
                courseID = SelectedPlanSum.Course;
            }

            foreach (var pla in courseID.PlanSetups)
            {
                if (pla.Id == prefix)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// The plans are added to the combobox and selected.
        /// </summary>
        /// <param name="plan_cb">Combobox in the UI</param>
        /// <param name="plan">The plan to add to the combobox</param>
        private void AddPhasePlan(ComboBox plan_cb, PlanSetup plan)
        {
            if (plan_cb.SelectedItem == null && plan != null)
            {
                plan_cb.Items.Add(plan.Id);
                int itemno = plan_cb.Items.Count;
                plan_cb.SelectedItem = plan_cb.Items[itemno - 1];
            }
        }

        /// <summary>
        /// Copying a selected photon plan to a single phase and calculating the dose.
        /// The method is different for VMAT and IMRT/static.
        /// A rectangle is colored green if the calculation is a sucess.
        /// </summary>
        /// <param name="img">The source phase image</param>
        /// <param name="rec">The box on the UI that shows if the calculations went correct</param>
        /// <param name="name">The new ID for the 4D plan</param>
        /// <param name="copyFrom">The original plan that will be copied</param>
        /// <returns></returns>
        private ExternalPlanSetup CalcPhoton(VMS.TPS.Common.Model.API.Image img, Rectangle rec, string name,PlanSetup copyFrom)
        {
            StringBuilder outputDia = new StringBuilder("");
            ExternalPlanSetup plan = null;

            if (img == null)
            {
                return plan;
            }

            // If the plan is copied to the same image we do not need to calculate.
            // If the plan is copied to a new image, we will have to calculate later.
            // The copy-method is the same for IMRT og VMAT
            if (copyFrom.StructureSet.Image.Id == img.Id && copyFrom.StructureSet.Image.Series.UID == img.Series.UID)
            {
                plan = copyFrom.Course.CopyPlanSetup(copyFrom) as ExternalPlanSetup;
                rec.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                plan.Id = name;
                return plan;
            }
            else
            {
                plan = copyFrom.Course.CopyPlanSetup(copyFrom, img, outputDia) as ExternalPlanSetup;
            }

            plan.Id = name;


            // IMRT plans are copied now
            if (plan.Beams.First().Technique.ToString().ToUpper() == "STATIC")
            {
                errormessages += plan.Id + ": IMRT or static photon plan" + "\n";

                //The MU settings are copied from the original plan. This is the initialisation of the list of values for each beam in the plan.
                List<KeyValuePair<string, MetersetValue>> calculateIMRT = new List<KeyValuePair<string, MetersetValue>>();

                foreach (var item in copyFrom.Beams)
                {
                    KeyValuePair<string, MetersetValue> temp = new KeyValuePair<string, MetersetValue>(item.Id, item.Meterset);
                    calculateIMRT.Add(temp);
                }

                // If the LMC can be calculated now we will do it.
                // I dont understand why I sometimes have to do it and otherwise not. 
                // Maybe this part can be completely skipped. However it does not hurt as the exception is caught and ignored when it is impossible...
                try
                {
                    var res2 = plan.CalculateLeafMotions();
                    errormessages += "Leafmotion calculated for: " + plan.Id + "\n";

                    if (!res2.Success)
                    {
                        errormessages += "Leafmotion calculation error for plan: " + plan.Id + "\n";
                        return plan;
                    }
                }
                catch (Exception)
                {
                    errormessages += "Leafmotion not calculated for: " + plan.Id + "\n";

                }

                //The dose is calculated
                var res = plan.CalculateDoseWithPresetValues(calculateIMRT);

                //The normalization is set.
                if (plan.PlanNormalizationValue != copyFrom.PlanNormalizationValue)
                {
                    plan.PlanNormalizationValue = copyFrom.PlanNormalizationValue;
                }

                // If the calculation fails or not...
                if (!res.Success)
                {
                    errormessages += "Dose calculation error for plan: " + plan.Id + "\n";
                    return plan;
                }
                else
                {
                    rec.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                    return plan;
                }

            }
            else //VMAT plans are handeled differently as there is no need for preset MU
            {
                errormessages += plan.Id + ": VMAT plan" + "\n";

                var res = plan.CalculateDose();

                if (!res.Success)
                {
                    errormessages += "Dose calculation error for plan: " + plan.Id + "\n";
                    return plan;
                }
                else
                {
                    rec.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                    plan.PlanNormalizationValue = copyFrom.PlanNormalizationValue;
                    return plan;
                }
            }
        }

        /// <summary>
        /// Copying a selected proton plan to a single phase and calculating the dose
        /// A rectangle is colored green if the calculation is a sucess.
        /// </summary>
        /// <param name="img">The source phase image</param>
        /// <param name="rec">The box on the UI that shows if the calculations went correct</param>
        /// <param name="name">The new ID for the 4D plan</param>
        /// <param name="copyFrom">The original plan that will be copied</param>
        /// <returns></returns>
        private IonPlanSetup CalcProton(VMS.TPS.Common.Model.API.Image img, Rectangle rec, string name, PlanSetup copyFrom)
        {

            StringBuilder outputDia = new StringBuilder("");
            IonPlanSetup plan = null;

            if (img == null)
            {
                return plan;
            }

            // If the plan is copied to the same image we do not need to calculate.
            // If the plan is copied to a new image, we will have to calculate later.
            if (copyFrom.StructureSet.Image.Series.UID == img.Series.UID && copyFrom.StructureSet.Image.Id == img.Id)
            {
                plan = copyFrom.Course.CopyPlanSetup(copyFrom) as IonPlanSetup;
            }
            else
            {
                plan = copyFrom.Course.CopyPlanSetup(copyFrom, img, outputDia) as IonPlanSetup;
            }

            plan.Id = name;


            var res = plan.CalculateDoseWithoutPostProcessing();
            errormessages += plan.Id + ": Proton plan" + "\n";

            if (!res.Success)
            {
                errormessages += "Dose calculation error for plan: " + plan.Id + "\n";
                return plan;
            }
            else
            {
                rec.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                return plan;
            }
        }

        /// <summary>
        /// The dose is evaluated and the MU are compared to the original plan.
        /// The dose to the selected structures are calculated and printed in the UI. 
        /// </summary>
        private void EvalDose_btn_Click(object sender, RoutedEventArgs e)
        {
            progress_lb.Content = "... DVH points are evaluated ...";
            AllowUIToUpdate();

            //The structures are imported
            string[] structurenames = new string[5] { CTV1_cb.SelectedItem.ToString(), CTV2_cb.SelectedItem.ToString(), Spinal_cb.SelectedItem.ToString(), Spinal2_cb.SelectedItem.ToString(), Spinal3_cb.SelectedItem.ToString() };

            if (SelectedPlan != null) //single plans
            {
                //The plans are found.
                PlanSetup CT00plan = FindPlan(0, CT00_plan_cb);
                PlanSetup CT10plan = FindPlan(10, CT10_plan_cb);
                PlanSetup CT20plan = FindPlan(20, CT20_plan_cb);
                PlanSetup CT30plan = FindPlan(30, CT30_plan_cb);
                PlanSetup CT40plan = FindPlan(40, CT40_plan_cb);
                PlanSetup CT50plan = FindPlan(50, CT50_plan_cb);
                PlanSetup CT60plan = FindPlan(60, CT60_plan_cb);
                PlanSetup CT70plan = FindPlan(70, CT70_plan_cb);
                PlanSetup CT80plan = FindPlan(80, CT80_plan_cb);
                PlanSetup CT90plan = FindPlan(90, CT90_plan_cb);

                //If the plans were not added to the public variables (as in the case of evaluation only) they will be added.
                if (newPlan00 == null) 
                {
                    newPlan00 = CT00plan;
                    newPlan10 = CT10plan;
                    newPlan20 = CT20plan;
                    newPlan30 = CT30plan;
                    newPlan40 = CT40plan;
                    newPlan50 = CT50plan;
                    newPlan60 = CT60plan;
                    newPlan70 = CT70plan;
                    newPlan80 = CT80plan;
                    newPlan90 = CT90plan;
                }
            }
            else // Sum plan
            {
                //The sumplans are found.
                PlanSum CT00plan = FindPlanSum(0, CT00_plan_cb);
                PlanSum CT10plan = FindPlanSum(10, CT10_plan_cb);
                PlanSum CT20plan = FindPlanSum(20, CT20_plan_cb);
                PlanSum CT30plan = FindPlanSum(30, CT30_plan_cb);
                PlanSum CT40plan = FindPlanSum(40, CT40_plan_cb);
                PlanSum CT50plan = FindPlanSum(50, CT50_plan_cb);
                PlanSum CT60plan = FindPlanSum(60, CT60_plan_cb);
                PlanSum CT70plan = FindPlanSum(70, CT70_plan_cb);
                PlanSum CT80plan = FindPlanSum(80, CT80_plan_cb);
                PlanSum CT90plan = FindPlanSum(90, CT90_plan_cb);

                //If the plans were not added to the public variables (as in the case of evaluation only) they will be added.
                if (newPlanSum00 == null)
                {
                    newPlanSum00 = CT00plan;
                    newPlanSum10 = CT10plan;
                    newPlanSum20 = CT20plan;
                    newPlanSum30 = CT30plan;
                    newPlanSum40 = CT40plan;
                    newPlanSum50 = CT50plan;
                    newPlanSum60 = CT60plan;
                    newPlanSum70 = CT70plan;
                    newPlanSum80 = CT80plan;
                    newPlanSum90 = CT90plan;
                }
            }

            //The evaluation doses are imported
            double D1 = 50.0;
            double D2 = 50.0;
            double D3 = 50.0;
            double D4 = 50.0;
            double D5 = 50.0;

            //If they user has defined another dose it will be imported
            if (!Double.TryParse(CTV1_tb.Text, out D1))
            {
                D1 = 50.0;
            }
            if (!Double.TryParse(CTV2_tb.Text, out D2))
            {
                D2 = 50.0;
            }
            if (!Double.TryParse(OAR_tb.Text, out D3))
            {
                D3 = 50.0;
            }
            if (!Double.TryParse(OAR2_tb.Text, out D4))
            {
                D4 = 50.0;
            }
            if (!Double.TryParse(OAR3_tb.Text, out D5))
            {
                D5 = 50.0;
            }
            //The final doses are written as a message
            errormessages += "TAR1 prescribed dose: " + D1.ToString("0.00") + "\n";
            errormessages += "TAR2 prescribed dose: " + D2.ToString("0.00") + "\n";
            errormessages += "OAR1 dose for evaluation: " + D3.ToString("0.00") + "\n";
            errormessages += "OAR2 dose for evaluation: " + D4.ToString("0.00") + "\n";
            errormessages += "OAR3 dose for evaluation: " + D5.ToString("0.00") + "\n";

            if (SelectedPlan != null) //Single plans
            {
                // DVH results are read and saved in a variable if MU is approved. We allow a difference of less than 0.2 MU??.
                // If the MU difference is too big, the rectangle will be set to red.
                if (CorrectMU(newPlan00, rec_00))
                {
                    DVHresult CT00 = new DVHresult(newPlan00, structurenames, D1, D2, D3, D4, D5);
                    SetValues(CT00, CT00_CTV1_lb, CT00_CTV2_lb, CT00_SC_lb, CT00_SC2_lb, CT00_SC3_lb);
                }

                if (CorrectMU(newPlan10, rec_10))
                {
                    DVHresult CT10 = new DVHresult(newPlan10, structurenames, D1, D2, D3, D4, D5);
                    SetValues(CT10, CT10_CTV1_lb, CT10_CTV2_lb, CT10_SC_lb, CT10_SC2_lb, CT10_SC3_lb);
                }

                if (CorrectMU(newPlan20, rec_20))
                {
                    DVHresult CT20 = new DVHresult(newPlan20, structurenames, D1, D2, D3, D4, D5);
                    SetValues(CT20, CT20_CTV1_lb, CT20_CTV2_lb, CT20_SC_lb, CT20_SC2_lb, CT20_SC3_lb);
                }

                if (CorrectMU(newPlan30, rec_30))
                {
                    DVHresult CT30 = new DVHresult(newPlan30, structurenames, D1, D2, D3, D4, D5);
                    SetValues(CT30, CT30_CTV1_lb, CT30_CTV2_lb, CT30_SC_lb, CT30_SC2_lb, CT30_SC3_lb);
                }

                if (CorrectMU(newPlan40, rec_40))
                {
                    DVHresult CT40 = new DVHresult(newPlan40, structurenames, D1, D2, D3, D4, D5);
                    SetValues(CT40, CT40_CTV1_lb, CT40_CTV2_lb, CT40_SC_lb, CT40_SC2_lb, CT40_SC3_lb);
                }

                if (CorrectMU(newPlan50, rec_50))
                {
                    DVHresult CT50 = new DVHresult(newPlan50, structurenames, D1, D2, D3, D4, D5);
                    SetValues(CT50, CT50_CTV1_lb, CT50_CTV2_lb, CT50_SC_lb, CT50_SC2_lb, CT50_SC3_lb);
                }

                if (CorrectMU(newPlan60, rec_60))
                {
                    DVHresult CT60 = new DVHresult(newPlan60, structurenames, D1, D2, D3, D4, D5);
                    SetValues(CT60, CT60_CTV1_lb, CT60_CTV2_lb, CT60_SC_lb, CT60_SC2_lb, CT60_SC3_lb);
                }

                if (CorrectMU(newPlan70, rec_70))
                {
                    DVHresult CT70 = new DVHresult(newPlan70, structurenames, D1, D2, D3, D4, D5);
                    SetValues(CT70, CT70_CTV1_lb, CT70_CTV2_lb, CT70_SC_lb, CT70_SC2_lb, CT70_SC3_lb);
                }

                if (CorrectMU(newPlan80, rec_80))
                {
                    DVHresult CT80 = new DVHresult(newPlan80, structurenames, D1, D2, D3, D4, D5);
                    SetValues(CT80, CT80_CTV1_lb, CT80_CTV2_lb, CT80_SC_lb, CT80_SC2_lb, CT80_SC3_lb);
                }

                if (CorrectMU(newPlan90, rec_90))
                {
                    DVHresult CT90 = new DVHresult(newPlan90, structurenames, D1, D2, D3, D4, D5);
                    SetValues(CT90, CT90_CTV1_lb, CT90_CTV2_lb, CT90_SC_lb, CT90_SC2_lb, CT90_SC3_lb);
                }
            }
            else //Sumplans
            {
                // DVH results are read and saved in a variable if MU is approved. We allow a difference of less than 0.2 MU??.
                // If the MU difference is too big, the rectangle will be set to red.
                if (CorrectMUSum(newPlanSum00, rec_00))
                {
                    DVHresult CT00 = new DVHresult(newPlanSum00, structurenames, D1, D2, D3, D4, D5);
                    SetValues(CT00, CT00_CTV1_lb, CT00_CTV2_lb, CT00_SC_lb, CT00_SC2_lb, CT00_SC3_lb);
                }

                if (CorrectMUSum(newPlanSum10, rec_10))
                {
                    DVHresult CT10 = new DVHresult(newPlanSum10, structurenames, D1, D2, D3, D4, D5);
                    SetValues(CT10, CT10_CTV1_lb, CT10_CTV2_lb, CT10_SC_lb, CT10_SC2_lb, CT10_SC3_lb);
                }

                if (CorrectMUSum(newPlanSum20, rec_20))
                {
                    DVHresult CT20 = new DVHresult(newPlanSum20, structurenames, D1, D2, D3, D4, D5);
                    SetValues(CT20, CT20_CTV1_lb, CT20_CTV2_lb, CT20_SC_lb, CT20_SC2_lb, CT20_SC3_lb);
                }

                if (CorrectMUSum(newPlanSum30, rec_30))
                {
                    DVHresult CT30 = new DVHresult(newPlanSum30, structurenames, D1, D2, D3, D4, D5);
                    SetValues(CT30, CT30_CTV1_lb, CT30_CTV2_lb, CT30_SC_lb, CT30_SC2_lb, CT30_SC3_lb);
                }

                if (CorrectMUSum(newPlanSum40, rec_40))
                {
                    DVHresult CT40 = new DVHresult(newPlanSum40, structurenames, D1, D2, D3, D4, D5);
                    SetValues(CT40, CT40_CTV1_lb, CT40_CTV2_lb, CT40_SC_lb, CT40_SC2_lb, CT40_SC3_lb);
                }

                if (CorrectMUSum(newPlanSum50, rec_50))
                {
                    DVHresult CT50 = new DVHresult(newPlanSum50, structurenames, D1, D2, D3, D4, D5);
                    SetValues(CT50, CT50_CTV1_lb, CT50_CTV2_lb, CT50_SC_lb, CT50_SC2_lb, CT50_SC3_lb);
                }

                if (CorrectMUSum(newPlanSum60, rec_60))
                {
                    DVHresult CT60 = new DVHresult(newPlanSum60, structurenames, D1, D2, D3, D4, D5);
                    SetValues(CT60, CT60_CTV1_lb, CT60_CTV2_lb, CT60_SC_lb, CT60_SC2_lb, CT60_SC3_lb);
                }

                if (CorrectMUSum(newPlanSum70, rec_70))
                {
                    DVHresult CT70 = new DVHresult(newPlanSum70, structurenames, D1, D2, D3, D4, D5);
                    SetValues(CT70, CT70_CTV1_lb, CT70_CTV2_lb, CT70_SC_lb, CT70_SC2_lb, CT70_SC3_lb);
                }

                if (CorrectMUSum(newPlanSum80, rec_80))
                {
                    DVHresult CT80 = new DVHresult(newPlanSum80, structurenames, D1, D2, D3, D4, D5);
                    SetValues(CT80, CT80_CTV1_lb, CT80_CTV2_lb, CT80_SC_lb, CT80_SC2_lb, CT80_SC3_lb);
                }

                if (CorrectMUSum(newPlanSum90, rec_90))
                {
                    DVHresult CT90 = new DVHresult(newPlanSum90, structurenames, D1, D2, D3, D4, D5);
                    SetValues(CT90, CT90_CTV1_lb, CT90_CTV2_lb, CT90_SC_lb, CT90_SC2_lb, CT90_SC3_lb);
                }
            }

            //The DVH collection can now be exported
            ExportDVH_btn.IsEnabled = true;
            Errors_txt.Text = errormessages;

            progress_lb.Content = "... Done! ...";
        }

        /// <summary>
        /// Checking if the MU in the sum plans are correct
        /// The calibration curve is checked
        /// </summary>
        /// <param name="plan">The plan sum</param>
        /// <param name="rec">the rectangle that is colord green or red depending on the result</param>
        /// <returns></returns>
        private bool CorrectMUSum(PlanSum plan, Rectangle rec)
        {
            bool MUisOK = true;

            if (plan == null)
            {
                MUisOK = false;
            }
            else
            {
                if (SelectedPlanSum.StructureSet.Image.Series.ImagingDeviceId != plan.StructureSet.Image.Series.ImagingDeviceId)
                {
                    errormessages += plan.Id + " is not on the same calibration curve as the baseplan \n";
                    MUisOK = false;
                }

                for(int j = 0; j < plan.PlanSetups.Count(); j++)
                {
                    PlanSetup ps = plan.PlanSetups.ElementAt(j); 
                    
                    for (int i = 0; i < ps.Beams.Count(); i++)
                    {
                        double test1 = ps.Beams.ElementAt(i).Meterset.Value;
                        double test2 = SelectedPlanSum.PlanSetups.ElementAt(j).Beams.ElementAt(i).Meterset.Value;

                        if (Math.Abs(test1 - test2) / test2 > 0.01)
                        {
                            MUisOK = false;
                            errormessages += ps.Id + " and the main plan differs in MU by: " + Math.Abs(test1 - test2).ToString("0.00") + "for field: " + ps.Beams.ElementAt(i).Id + "\n";
                        }
                        else if (Math.Abs(test1 - test2) / test2 > 0.0001)
                        {
                            errormessages += ps.Id + " and the main plan differs slightly in MU by: " + Math.Abs(test1 - test2).ToString("0.00") + "for field: " + ps.Beams.ElementAt(i).Id + "\n";
                        }
                    }
                }
            }

            if (MUisOK == false)
            {
                rec.Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0)); //red
                AllowUIToUpdate();
            }
            else
            {
                rec.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));//green
                AllowUIToUpdate();
            }

            return MUisOK;
        }

        /// <summary>
        /// Checking if the MU in the single plans are correct
        /// The calibration curve is checked
        /// </summary>
        /// <param name="plan">The plan sum</param>
        /// <param name="rec">the rectangle that is colord green or red depending on the result</param>
        /// <returns></returns>
        private bool CorrectMU(PlanSetup plan, Rectangle rec)
        {
            bool MUisOK = true;

            if (plan == null)
            {
                MUisOK = false;
            }
            else
            {
                if (SelectedPlan.StructureSet.Image.Series.ImagingDeviceId != plan.StructureSet.Image.Series.ImagingDeviceId) 
                {
                    errormessages += plan.Id + " is not on the same calibration curve as the baseplan \n";
                    MUisOK = false;
                }

                for (int i = 0; i < plan.Beams.Count(); i++)
                {
                    double test1 = plan.Beams.ElementAt(i).Meterset.Value;
                    double test2 = SelectedPlan.Beams.ElementAt(i).Meterset.Value;

                    if (Math.Abs(test1 - test2) / test2 > 0.01)
                    {
                        MUisOK = false;
                        errormessages += plan.Id + " and the main plan differs in MU by: " + Math.Abs(test1 - test2).ToString("0.00") + "for field: " + plan.Beams.ElementAt(i).Id + "\n";

                    }
                    else if (Math.Abs(test1 - test2) / test2 > 0.0001)
                    {
                        errormessages += plan.Id + " and the main plan differs slightly in MU by: " + Math.Abs(test1 - test2).ToString("0.00") + "for field: " + plan.Beams.ElementAt(i).Id + "\n";
                    }
                }
            }

            if (MUisOK == false)
            {
                rec.Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0)); //red
                AllowUIToUpdate();
            }
            else
            {
                rec.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));//green
                AllowUIToUpdate();
            }

            return MUisOK;
        }

        /// <summary>
        /// An int indicating the phase and the corresponding combobox is used to fetch the treatment plan selected in the box
        /// It will either catch a plan selected by the user or use the public variables with the newly calculated plans.
        /// </summary>
        /// <param name="v">The phase identification integer</param>
        /// <param name="CT_cb">The plan combobox where the planID can be extracted from</param>
        /// <returns></returns>
        private PlanSum FindPlanSum(int v, ComboBox CT_cb)
        {
            PlanSum CTplan = null;

            try
            {
                CTplan = ScriptInfo.Course.PlanSums.First(p => p.Id == CT_cb.SelectedItem.ToString());
            }
            catch (Exception)
            {

                if (v == 0)
                {
                    CTplan = newPlanSum00;
                }
                if (v == 10)
                {
                    CTplan = newPlanSum10;
                }
                if (v == 20)
                {
                    CTplan = newPlanSum20;
                }
                if (v == 30)
                {
                    CTplan = newPlanSum30;
                }
                if (v == 40)
                {
                    CTplan = newPlanSum40;
                }
                if (v == 50)
                {
                    CTplan = newPlanSum50;
                }
                if (v == 60)
                {
                    CTplan = newPlanSum60;
                }
                if (v == 70)
                {
                    CTplan = newPlanSum70;
                }
                if (v == 80)
                {
                    CTplan = newPlanSum80;
                }
                if (v == 90)
                {
                    CTplan = newPlanSum90;
                }
            }
            return CTplan;
        }

        /// <summary>
        /// An int indicating the phase and the corresponding combobox is used to fetch the treatment plan selected in the box
        /// It will either catch a plan selected by the user or use the public variables with the newly calculated plans.
        /// </summary>
        /// <param name="v">The phase identification integer</param>
        /// <param name="CT_cb">The plan combobox where the planID can be extracted from</param>
        /// <returns></returns>
        private PlanSetup FindPlan(int v, ComboBox CT_cb)
        {

            PlanSetup CTplan = null;

            try
            {
                CTplan = ScriptInfo.Course.PlanSetups.First(p => p.Id == CT_cb.SelectedItem.ToString());
            }
            catch (Exception)
            {

                if (v == 0)
                {
                    CTplan = newPlan00;
                }
                if (v == 10)
                {
                    CTplan = newPlan10;
                }
                if (v == 20)
                {
                    CTplan = newPlan20;
                }
                if (v == 30)
                {
                    CTplan = newPlan30;
                }
                if (v == 40)
                {
                    CTplan = newPlan40;
                }
                if (v == 50)
                {
                    CTplan = newPlan50;
                }
                if (v == 60)
                {
                    CTplan = newPlan60;
                }
                if (v == 70)
                {
                    CTplan = newPlan70;
                }
                if (v == 80)
                {
                    CTplan = newPlan80;
                }
                if (v == 90)
                {
                    CTplan = newPlan90;
                }
            }
            return CTplan;
        }

        /// <summary>
        /// Given the DVH results for a phase plan, the results are set in the UI.
        /// </summary>
        /// <param name="res">The DVH results class object</param>
        /// <param name="CTV1_lb">UI label to hold the Tar1 result</param>
        /// <param name="CTV2_lb">UI label to hold the Tar2 result</param>
        /// <param name="SC_lb">UI label to hold the OAR1 result</param>
        /// <param name="SC2_lb">UI label to hold the OAR2 result</param>
        /// <param name="SC3_lb">UI label to hold the OAR3 result</param>
        private void SetValues(DVHresult res, Label CTV1_lb, Label CTV2_lb, Label SC_lb, Label SC2_lb, Label SC3_lb)
        {

            if (res.V95CTV1 == -1000)
            {
                CTV1_lb.Content = res.V95CTV1.ToString("N/A");
            }
            else
            {
                CTV1_lb.Content = res.V95CTV1.ToString("0.00");
            }

            if (res.V95CTV2 == -1000)
            {
                CTV2_lb.Content = res.V95CTV2.ToString("N/A");
            }
            else
            {
                CTV2_lb.Content = res.V95CTV2.ToString("0.00");
            }

            if (res.V50_SC == -1000)
            {
                SC_lb.Content = "N/A";
            }
            else
            {
                SC_lb.Content = res.V50_SC.ToString("0.00");
            }

            if (res.V50_SC2 == -1000)
            {
                SC2_lb.Content = "N/A";
            }
            else
            {
                SC2_lb.Content = res.V50_SC2.ToString("0.00");
            }

            if (res.V50_SC3 == -1000)
            {
                SC3_lb.Content = "N/A";
            }
            else
            {
                SC3_lb.Content = res.V50_SC3.ToString("0.00");
            }
        }

        /// <summary>
        /// All DVHes are exported for the main plan and the new phases.
        /// If uncertainty doses exist for the main plan, they will be exported as well.
        /// The user defines the resolution. Default is 0.1.
        /// </summary>
        private void ExportDVH_btn_Click(object sender, RoutedEventArgs e)
        {

            double dvhresolution = 0.1;
            if (!Double.TryParse(dvhresolution_tb.Text, out dvhresolution))
            {
                dvhresolution = 0.1;
            }

            if (dvhresolution < 0.001)
            {
                MessageBox.Show("The DVH resolution is too low. Use 0.001 Gy or higher.");
                return;
            }

            if (SelectedPlan != null) //single plan
            {
                PlanSetup[] allPlans = CreateList(SelectedPlan, newPlan00, newPlan10, newPlan20, newPlan30, newPlan40, newPlan50, newPlan60, newPlan70, newPlan80, newPlan90);
                Errors_txt.Text = errormessages;

                //Select a folder
                string folderToSave = null;
                string dummyFileName = "Save Here";
                SaveFileDialog sf = new SaveFileDialog()
                {
                    Title = "Select the folder to save the files in",
                    FileName = dummyFileName
                };
                sf.ShowDialog();

                folderToSave = System.IO.Path.GetDirectoryName(sf.FileName);

                Directory.CreateDirectory(folderToSave + "\\" + "dvh_exports");
                ExportableDVHs dvhs = new ExportableDVHs(allPlans, folderToSave + "\\" + "dvh_exports", dvhresolution);
                dvhs.SaveAll();

            }
            else //If the plan is a plan sum. Each element is exported separatly = MANY FILES!
            {
                for (int i = 0; i < SelectedPlanSum.PlanSetups.Count(); i++)
                {
                    PlanSetup[] allPlans = CreateList(i, SelectedPlanSum, newPlanSum00, newPlanSum10, newPlanSum20, newPlanSum30, newPlanSum40, newPlanSum50, newPlanSum60, newPlanSum70, newPlanSum80, newPlanSum90);
                    Errors_txt.Text = errormessages;

                    //Select a folder
                    string folderToSave = null;
                    string dummyFileName = "Save Here";
                    SaveFileDialog sf = new SaveFileDialog()
                    {
                        Title = "Select the folder to save the files in",
                        FileName = dummyFileName
                    };
                    sf.ShowDialog();

                    folderToSave = System.IO.Path.GetDirectoryName(sf.FileName);

                    Directory.CreateDirectory(folderToSave + "\\" + "dvh_exports");
                    ExportableDVHs dvhs = new ExportableDVHs(allPlans, folderToSave + "\\" + "dvh_exports", dvhresolution);
                    dvhs.SaveAll();
                }
            }
        }

        /// <summary>
        /// Creates a list of the plans to export DVHs for
        /// </summary>
        /// <param name="selected">Nominel plan</param>
        /// <param name="new00">Phase 0</param>
        /// <param name="new10">Phase 1</param>
        /// <param name="new20">Phase 2</param>
        /// <param name="new30">Phase 3</param>
        /// <param name="new40">Phase 4</param>
        /// <param name="new50">Phase 5</param>
        /// <param name="new60">Phase 6</param>
        /// <param name="new70">Phase 7</param>
        /// <param name="new80">Phase 8</param>
        /// <param name="new90">Phase 9</param>
        /// <returns></returns>
        private PlanSetup[] CreateList(PlanSetup selected, PlanSetup new00, PlanSetup new10, PlanSetup new20, PlanSetup new30, PlanSetup new40, PlanSetup new50, PlanSetup new60, PlanSetup new70, PlanSetup new80, PlanSetup new90)
        {
            PlanSetup[] allPlans = new PlanSetup[11];

            if (selected != null) allPlans[0] = selected;
            if (new00 != null) allPlans[1] = new00;
            if (new10 != null) allPlans[2] = new10;
            if (new20 != null) allPlans[3] = new20;
            if (new30 != null) allPlans[4] = new30;
            if (new40 != null) allPlans[5] = new40;
            if (new50 != null) allPlans[6] = new50;
            if (new60 != null) allPlans[7] = new60;
            if (new70 != null) allPlans[8] = new70;
            if (new80 != null) allPlans[9] = new80;
            if (new90 != null) allPlans[10] = new90;
            return allPlans;
        }

        /// <summary>
        /// Creates a list of the single plans to export DVHs for within the sumplans
        /// </summary>
        /// <param name="selected">Nominel plan</param>
        /// <param name="new00">Phase 0</param>
        /// <param name="new10">Phase 1</param>
        /// <param name="new20">Phase 2</param>
        /// <param name="new30">Phase 3</param>
        /// <param name="new40">Phase 4</param>
        /// <param name="new50">Phase 5</param>
        /// <param name="new60">Phase 6</param>
        /// <param name="new70">Phase 7</param>
        /// <param name="new80">Phase 8</param>
        /// <param name="new90">Phase 9</param>
        /// <returns></returns>
        private PlanSetup[] CreateList(int i, PlanSum selected, PlanSum new00, PlanSum new10, PlanSum new20, PlanSum new30, PlanSum new40, PlanSum new50, PlanSum new60, PlanSum new70, PlanSum new80, PlanSum new90)
        {
            PlanSetup[] allPlans = new PlanSetup[11];

            if (selected != null) allPlans[0] = selected.PlanSetups.ElementAt(i);
            if (new00 != null) allPlans[1] = new00.PlanSetups.ElementAt(i);
            if (new10 != null) allPlans[2] = new10.PlanSetups.ElementAt(i);
            if (new20 != null) allPlans[3] = new20.PlanSetups.ElementAt(i);
            if (new30 != null) allPlans[4] = new30.PlanSetups.ElementAt(i);
            if (new40 != null) allPlans[5] = new40.PlanSetups.ElementAt(i);
            if (new50 != null) allPlans[6] = new50.PlanSetups.ElementAt(i);
            if (new60 != null) allPlans[7] = new60.PlanSetups.ElementAt(i);
            if (new70 != null) allPlans[8] = new70.PlanSetups.ElementAt(i);
            if (new80 != null) allPlans[9] = new80.PlanSetups.ElementAt(i);
            if (new90 != null) allPlans[10] = new90.PlanSetups.ElementAt(i);
            return allPlans;
        }
    }
}
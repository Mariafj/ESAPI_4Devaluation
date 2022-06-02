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
        public PlanSetup SelectedPlan; //The main plan that is to be copied to all phases
        public string errormessages = ""; //A string with all messages and errors for the user.

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

            errormessages = "";
            Errors_txt.Text = errormessages;

            //Combos
            CTV1_cb.Items.Clear();
            CTV2_cb.Items.Clear();
            Spinal_cb.Items.Clear();
            Spinal2_cb.Items.Clear();

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
        /// The user selects the main plan and comboboxes for structure selection and image selection are filled.
        /// </summary>
        private void SelectPlan_Click(object sender, RoutedEventArgs e)
        {
            //First everything is cleared.
            clearAllPublicsAndCombos();

            //Buttons that are not to be used are disabled.
            EvalDoseE_btn.IsEnabled = false;
            CopyPlan_btn.IsEnabled = false;
            //PrepareImages_btn.IsEnabled = false;
            ExportDVH_btn.IsEnabled = false;

            //The treatment plan is defined and saved in the public variable.
            string[] planname = SelectPlan_cb.SelectedItem.ToString().Split('/');
            string courseid = planname.First();
            string planid = planname.Last();
            PlanSetup mainPlan = ScriptInfo.Patient.Courses.Where(c => c.Id == courseid).FirstOrDefault().PlanSetups.Where(p => p.Id == planid).FirstOrDefault();
            SelectedPlan = mainPlan;

            // Combobox with structure names are filled and sorted alfabetically
            CTV1_cb.Items.Add("Skip");
            CTV2_cb.Items.Add("Skip");
            Spinal_cb.Items.Add("Skip");
            Spinal2_cb.Items.Add("Skip");

            IEnumerable<Structure> sortedStructs = mainPlan.StructureSet.Structures.OrderBy(s => s.Id);
            foreach (var struc in sortedStructs)
            {
                CTV1_cb.Items.Add(struc.Id);
                CTV2_cb.Items.Add(struc.Id);
                Spinal_cb.Items.Add(struc.Id);
                Spinal2_cb.Items.Add(struc.Id);
            }
            //"Skip" is the default structure choice
            CTV1_cb.SelectedItem = CTV1_cb.Items[0];
            CTV2_cb.SelectedItem = CTV1_cb.Items[0];
            Spinal_cb.SelectedItem = Spinal_cb.Items[0];
            Spinal2_cb.SelectedItem = Spinal2_cb.Items[0];

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
            foreach (var img in mainPlan.StructureSet.Image.Series.Study.Images3D)
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


            // Buttens for finalizing the image choice is activated
            SelectImages_btn.IsEnabled = true;
            SelectImagesE_btn.IsEnabled = true;

            //Errormessages are written in the UI.
            Errors_txt.Text = errormessages;
        }

        /// <summary>
        /// Finds the correct 3D image given a seried UID list and a string defining the selected phase.
        /// </summary>
        private VMS.TPS.Common.Model.API.Image findCorrectImage(List<string> uid_list, string v, string phase)
        {
            //All images with the correct name are selected but only one is needed.
            //(The tests of this function are probably not all needed anymore after I implemented the seriesUID check.)
            IEnumerable<VMS.TPS.Common.Model.API.Image> temp = SelectedPlan.Series.Study.Images3D.Where(p => p.Series.Id + "/" + p.Id == v);

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
        /// The images are selected and saved to the public variables.
        /// If there are plans calculated on the images, they will be added to the plan-comboboxes.
        /// The function destinquished between the writable version and the non-writable by using the bookean writeYN
        /// </summary>
        private void SelectImages(bool writeYN)
        {
            
            //writeYN == true => The script will create new plans on the phases
            //writeYN == false => The script will evaluate already created plans on the phases
            
            //The selected images are found by using the function "findCorrectImage".
            img00 = findCorrectImage(UID_00, CT00_cb.SelectedItem.ToString(), "phase 00");
            img10 = findCorrectImage(UID_10, CT10_cb.SelectedItem.ToString(), "phase 10");
            img20 = findCorrectImage(UID_20, CT20_cb.SelectedItem.ToString(), "phase 20");
            img30 = findCorrectImage(UID_30, CT30_cb.SelectedItem.ToString(), "phase 30");
            img40 = findCorrectImage(UID_40, CT40_cb.SelectedItem.ToString(), "phase 40");
            img50 = findCorrectImage(UID_50, CT50_cb.SelectedItem.ToString(), "phase 50");
            img60 = findCorrectImage(UID_60, CT60_cb.SelectedItem.ToString(), "phase 60");
            img70 = findCorrectImage(UID_70, CT70_cb.SelectedItem.ToString(), "phase 70");
            img80 = findCorrectImage(UID_80, CT80_cb.SelectedItem.ToString(), "phase 80");
            img90 = findCorrectImage(UID_90, CT90_cb.SelectedItem.ToString(), "phase 90");

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
            foreach (var plan in SelectedPlan.Course.PlanSetups)
            {
                if (plan.StructureSet.Image.Series.UID == img00.Series.UID) CT00_plan_cb.Items.Add(plan.Id);

                if (plan.StructureSet.Image.Series.UID == img10.Series.UID) CT10_plan_cb.Items.Add(plan.Id);

                if (plan.StructureSet.Image.Series.UID == img20.Series.UID) CT20_plan_cb.Items.Add(plan.Id);

                if (plan.StructureSet.Image.Series.UID == img30.Series.UID) CT30_plan_cb.Items.Add(plan.Id);

                if (plan.StructureSet.Image.Series.UID == img40.Series.UID) CT40_plan_cb.Items.Add(plan.Id);

                if (plan.StructureSet.Image.Series.UID == img50.Series.UID) CT50_plan_cb.Items.Add(plan.Id);

                if (plan.StructureSet.Image.Series.UID == img60.Series.UID) CT60_plan_cb.Items.Add(plan.Id);

                if (plan.StructureSet.Image.Series.UID == img70.Series.UID) CT70_plan_cb.Items.Add(plan.Id);

                if (plan.StructureSet.Image.Series.UID == img80.Series.UID) CT80_plan_cb.Items.Add(plan.Id);

                if (plan.StructureSet.Image.Series.UID == img90.Series.UID) CT90_plan_cb.Items.Add(plan.Id);
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
        /// For all images with structuresets the overwrittes structures are copied to the phases from the nominal plan scan. 
        /// It the scans have the same imaging device the HU values are also transfered.
        /// </summary>
        private void OverwriteStructures()
        {

            VMS.TPS.Common.Model.API.Image[] imageList = new VMS.TPS.Common.Model.API.Image[10] { img00, img10, img20, img30, img40, img50, img60, img70, img80, img90 };
            StructureSet[] structureSetList = new StructureSet[10];

            int[] checklist = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //MAY 2022
            //Are we missing structuresets on the images?
            for (int i = 0; i < imageList.Length; i++)
            {
                var img = imageList.ElementAt(i);

                //Finding the structure set for the image phase
                foreach (var struSet in ScriptInfo.Patient.StructureSets)
                {
                    if (struSet.Image.Id == img.Id && struSet.Image.Series.UID == img.Series.UID)
                    {
                        //We have a structureset!
                        checklist[i] = 1;
                        structureSetList[i] = struSet;
                    }
                }
            }

            //Finding overwritten structures and looking for the same structures on the phases
            List<Structure> overwrittenStructs = new List<Structure>();
            foreach (var stru in SelectedPlan.StructureSet.Structures)
            {
                double HU;
                if (stru.GetAssignedHU(out HU))
                {
                    overwrittenStructs.Add(stru);
                }
            }
            errormessages += SelectedPlan.Id + " has " + overwrittenStructs.Count() + " overwritten structures." + "\n";


            for (int i = 0; i < imageList.Count(); i++)
            {
                var img = imageList[i];

                // We will only transfer HU if the images are on the same calibration curve
                if (SelectedPlan.StructureSet.Image.Series.ImagingDeviceId == img.Series.ImagingDeviceId)
                {
                    var struSet = structureSetList[i];
                    // System.Windows.MessageBox.Show("Image: " + struSet.Image.Id + " Series: " + struSet.Image.Series.Id + " Har et strukturset: " + struSet.Id);
                }
            }

            ////We will loop over all the images in the list and overwrite structures where it is possible
            for (int i = 0; i < imageList.Count(); i++)
            {
                var img = imageList[i];

                // We will only transfer HU if the images are on the same calibration curve
                if (SelectedPlan.StructureSet.Image.Series.ImagingDeviceId == img.Series.ImagingDeviceId)
                {

                    var struSet = structureSetList[i];

                    //System.Windows.MessageBox.Show("Image: " + struSet.Image.Id + " Series: " + struSet.Image.Series.Id + " Har et strukturset: " + struSet.Id);

                    if (struSet.Image.Id == img.Id && struSet.Image.Series.UID == img.Series.UID)
                    {
                        // It is not possible to set the materialstable                            
                        // Looking through all structures to finde the correct structure names.
                        foreach (var strSelected in overwrittenStructs)
                        {

                            bool structureExist = false;
                            foreach (var str in struSet.Structures)
                            {
                                if (str.Id == strSelected.Id)
                                {
                                    structureExist = true;
                                }
                            }

                            //The structure has been copied. We can now overwrite it.
                            if (structureExist)
                            {

                                var str = struSet.Structures.Where(s => s.Id == strSelected.Id).First();

                                try
                                {
                                    double HU;
                                    strSelected.GetAssignedHU(out HU);
                                    str.SetAssignedHU(HU);
                                    errormessages += "HU-value: " + HU.ToString() + " assigned toe: " + str.Id + " on image: " + img.Series.Id + "/" + img.Id + "\n";

                                }
                                catch (Exception)
                                {
                                    errormessages += "Unable to set HU-value to: " + str.Id + " on image: " + img.Series.Id + "/" + img.Id + "\n";
                                }
                            }
                            else if (struSet.CanAddStructure(strSelected.DicomType, strSelected.Id))
                            {                          
                                //We will now try to copy the structure!

                                double HU;
                                Structure newstru = struSet.AddStructure(strSelected.DicomType, strSelected.Id);
                                newstru.SegmentVolume = strSelected.SegmentVolume;
                                strSelected.GetAssignedHU(out HU);
                                newstru.SetAssignedHU(HU);
                                errormessages += "HU-value: " + HU.ToString() + " assigned to: " + newstru.Id + " on image: " + img.Series.Id + "/" + img.Id + "\n";
                            }
                        }
                    }
                }
            }

            Errors_txt.Text = errormessages;
        }

        /// <summary>
        /// The calibration curve is set to the same device as for the nominal plan if it is possible.
        /// </summary>
        private void CopyCalibration()
        {
            VMS.TPS.Common.Model.API.Image[] imageList = new VMS.TPS.Common.Model.API.Image[10] { img00, img10, img20, img30, img40, img50, img60, img70, img80, img90 };


            // The imagingDevice (aka the calibration curve) is compared. 
            // If it is different we will try to change it. 
            // If it cannot be changed the user will recieve a message about it.
            foreach (var img in imageList)
            {
                if (SelectedPlan.StructureSet.Image.Series.ImagingDeviceId != img.Series.ImagingDeviceId)
                {
                    try
                    {
                        img.Series.SetImagingDevice(SelectedPlan.StructureSet.Image.Series.ImagingDeviceId);
                        errormessages += img.Series.Id + "/" + img.Id + ": Imaging device: " + SelectedPlan.StructureSet.Image.Series.ImagingDeviceId + "\n";

                    }
                    catch (Exception)
                    {
                        if (SelectedPlan.StructureSet.Image.Series.ImagingDeviceId != img.Series.ImagingDeviceId)
                        {
                            errormessages += img.Series.Id + "/" + img.Id + ": Unable to set imaging device" + "\n";
                        }
                    }
                }
            }
            Errors_txt.Text = errormessages;
        }

        /// <summary>
        /// For each phase in the 4D we search for a structure set. 
        /// If the set does not exist it is created and the body structure is copied from the nominal plan. 
        /// If it exists the body structure is overwritten with the structure from the nominal plan if possible.
        /// </summary>
        private void CreateBody()
        {
            //A list with all the images.
            VMS.TPS.Common.Model.API.Image[] imageList = new VMS.TPS.Common.Model.API.Image[10] { img00, img10, img20, img30, img40, img50, img60, img70, img80, img90 };
            StructureSet[] structureSetList = new StructureSet[10];

            int[] checklist = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //MAY 2022
            //Are we missing structuresets on the images?
            for (int i = 0; i < imageList.Length; i++)
            {
                var img = imageList.ElementAt(i);

                //Finding the structure set for the image phase
                foreach (var struSet in ScriptInfo.Patient.StructureSets)
                {
                    if (struSet.Image.Id == img.Id && struSet.Image.Series.UID == img.Series.UID)
                    {
                        //We have a structureset!
                        checklist[i] = 1;
                        structureSetList[i] = struSet;
                    }
                }
            }


            // Missing structure sets will be created and a body is copied from the original scan.
            Structure bodystructure = SelectedPlan.StructureSet.Structures.First(s => s.DicomType.ToUpper() == "EXTERNAL");
            for (int i = 0; i < checklist.Count(); i++)
            {
                if (checklist[i] != 1)
                {
                    var img = imageList.ElementAt(i);
                    StructureSet test = img.CreateNewStructureSet();
                    test.Id = img.Id;

                    var parameters = SelectedPlan.StructureSet.GetDefaultSearchBodyParameters();
                    Structure newBody = test.CreateAndSearchBody(parameters);
                    newBody.Id = "BODY";
                    structureSetList[i] = test;
                    newBody.SegmentVolume = bodystructure.SegmentVolume;

                    errormessages += img.Series.Id + "/" + img.Id + ": BODY is copied. \n";
                }
                else
                {
                    var img = imageList.ElementAt(i);
                    Structure bodystructurePhase = structureSetList[i].Structures.First(s => s.DicomType.ToUpper() == "EXTERNAL");

                    try
                    {
                        bodystructurePhase.SegmentVolume = bodystructure.SegmentVolume;
                        errormessages += img.Series.Id + "/" + img.Id + ": BODY is copied. \n";
                    }
                    catch (Exception)
                    {
                        errormessages += img.Series.Id + "/" + img.Id + ": BODY is NOT copied. \n";
                    }

                }
            }
            Errors_txt.Text = errormessages;
        }

        /// <summary>
        /// The 4DCT phases are modified acording to the planning CT:
        /// The same calibration curve is set, if possible
        /// Overwritten structures are also overwritten on the phases if they structures have been transfered before hand by the user.
        /// </summary>
        //private void PrepareImages_btn_Click(object sender, RoutedEventArgs e)
        //{
        //    //The script writes in the database
        //    ScriptInfo.Patient.BeginModifications();


        //    //A list with all the images.
        //    VMS.TPS.Common.Model.API.Image[] imageList = new VMS.TPS.Common.Model.API.Image[10] { img00, img10, img20, img30, img40, img50, img60, img70, img80, img90 };
        //    StructureSet[] structureSetList = new StructureSet[10];

        //    int[] checklist = new int[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0};

        //    //MAY 2022
        //    //Are we missing structuresets on the images?
        //    for (int i = 0; i < imageList.Length; i++)
        //    {

        //        var img = imageList.ElementAt(i);

        //        //Finding the structure set for the image phase
        //        foreach (var struSet in ScriptInfo.Patient.StructureSets)
        //        {
        //            if (struSet.Image.Id == img.Id && struSet.Image.Series.UID == img.Series.UID)
        //            {
        //                //We have a structureset!
        //                checklist[i] = 1;
        //                structureSetList[i] = struSet;
        //            }
        //        }
        //    }


        //    //We are now looking for structure set and if it does not exist it will be copied from the main plan.
        //    Structure bodystructure = SelectedPlan.StructureSet.Structures.First(s => s.DicomType.ToUpper() == "EXTERNAL");
        //    for (int i = 0; i < checklist.Count(); i++)
        //    {
        //        if (checklist[i]!= 1)
        //        {
        //            var img = imageList.ElementAt(i);
        //            StructureSet test = img.CreateNewStructureSet();
        //            test.Id = img.Id;

        //            var parameters = SelectedPlan.StructureSet.GetDefaultSearchBodyParameters();
        //            Structure newBody = test.CreateAndSearchBody(parameters);
        //            newBody.Id = "BODY";
        //            structureSetList[i] = test;
        //            newBody.SegmentVolume = bodystructure.SegmentVolume;

        //            errormessages += img.Series.Id + "/" + img.Id + ": The body structure has been copied from the main structureset. \n";
        //        }
        //    }


        //    System.Windows.MessageBox.Show("Nu sættes kalibreringskurven");
        //    // The imagingDevice (aka the calibration curve) is compared. 
        //    // If it is different we will try to change it. 
        //    // If it cannot be changed the user will recieve a message about it.
        //    foreach (var img in imageList)
        //    {
        //        if (SelectedPlan.StructureSet.Image.Series.ImagingDeviceId != img.Series.ImagingDeviceId)
        //        {
        //            try
        //            {
        //                img.Series.SetImagingDevice(SelectedPlan.StructureSet.Image.Series.ImagingDeviceId);
        //                errormessages += img.Series.Id + "/" + img.Id + ": Imaging device is set to " + SelectedPlan.StructureSet.Image.Series.ImagingDeviceId + "\n";

        //            }
        //            catch (Exception)
        //            {
        //                if (SelectedPlan.StructureSet.Image.Series.ImagingDeviceId != img.Series.ImagingDeviceId)
        //                {
        //                    errormessages += img.Series.Id + "/" + img.Id + ": Unable to set imaging device" + "\n";
        //                }
        //            }
        //        }
        //    }

        //    System.Windows.MessageBox.Show("Nu overskrives strukturer med HUværdier");
        //    //Finding overwritten structures and looking for the same structures on the phases
        //    List<Structure> overwrittenStructs = new List<Structure>();
        //    foreach (var stru in SelectedPlan.StructureSet.Structures)
        //    {
        //        double HU;
        //        if (stru.GetAssignedHU(out HU))
        //        {
        //            overwrittenStructs.Add(stru);
        //        }
        //    }
        //    //errormessages += SelectedPlan.Id + " has " + overwrittenStructs.Count() + " overwritten structures." + "\n";


        //    for (int i = 0; i < imageList.Count(); i++)
        //    {
        //        var img = imageList[i];

        //        // We will only transfer HU if the images are on the same calibration curve
        //        if (SelectedPlan.StructureSet.Image.Series.ImagingDeviceId == img.Series.ImagingDeviceId)
        //        {

        //            var struSet = structureSetList[i];

        //            //System.Windows.MessageBox.Show("Image: " + struSet.Image.Id + " Series: " + struSet.Image.Series.Id + " Har et strukturset: " + struSet.Id);
        //        }
        //    }



        //    ////We will loop over all the images in the list and overwrite structures where it is possible
        //    for (int i = 0; i < imageList.Count(); i++)
        //    {
        //        var img = imageList[i];

        //        // We will only transfer HU if the images are on the same calibration curve
        //        if (SelectedPlan.StructureSet.Image.Series.ImagingDeviceId == img.Series.ImagingDeviceId)
        //        {

        //            var struSet = structureSetList[i];

        //            System.Windows.MessageBox.Show("Image: " + struSet.Image.Id + " Series: " + struSet.Image.Series.Id + " Har et strukturset: " + struSet.Id);


        //            if (struSet.Image.Id == img.Id && struSet.Image.Series.UID == img.Series.UID)
        //            {
        //                // It is not possible to set the materialstable                            
        //                // Looking through all structures to finde the correct structure names.
        //                foreach (var strSelected in overwrittenStructs)
        //                {

        //                    bool structureExist = false;
        //                    foreach (var str in struSet.Structures)
        //                    {
        //                        if (str.Id == strSelected.Id)
        //                        {
        //                            structureExist = true;
        //                        }
        //                    }

        //                    //The structure has been copied. We can now overwrite it.
        //                    if (structureExist)
        //                    {

        //                        var str = struSet.Structures.Where(s => s.Id == strSelected.Id).First();

        //                        try
        //                        {
        //                            double HU;
        //                            strSelected.GetAssignedHU(out HU);
        //                            str.SetAssignedHU(HU);
        //                            errormessages += "HU-value: " + HU.ToString() + " assigned for structure: " + str.Id + " on image: " + img.Series.Id + "/" + img.Id + "\n";

        //                        }
        //                        catch (Exception)
        //                        {
        //                            errormessages += "Unable to set HU-value for structure: " + str.Id + " on image: " + img.Series.Id + "/" + img.Id + "\n";
        //                        }
        //                    }
        //                    else
        //                    {//We will now try to copy the structure!

        //                        double HU;
        //                        Structure newstru = struSet.AddStructure(strSelected.DicomType, strSelected.Id);
        //                        newstru.SegmentVolume = strSelected.SegmentVolume; //DOES THIS WORK?
        //                        strSelected.GetAssignedHU(out HU);
        //                        newstru.SetAssignedHU(HU);
        //                        errormessages += "HU-value: " + HU.ToString() + " assigned for new structure: " + newstru.Id + " on image: " + img.Series.Id + "/" + img.Id + "\n";
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    Errors_txt.Text = errormessages;
        //}

        /// <summary>
        /// The plans are copied to the phases and new buttons are enabled.
        /// The copy process depends on the plan type.
        /// </summary>
        private void CopyPlan_btn_Click(object sender, RoutedEventArgs e)
        {
            ScriptInfo.Patient.BeginModifications();

            if (body_chb.IsChecked == true)
            {
                CreateBody();
            }

            if (calib_chb.IsChecked == true)
            {
                CopyCalibration();
            }

            if (overw_chb.IsChecked == true)
            {
                OverwriteStructures();
            }

            if (SelectedPlan.PlanType.ToString().Contains("Proton")) //proton
            {
                CopyProtons();
            }
            else // foton
            {
                CopyPhotons();
            }

            // The evaluation button is pressed automatically in this case.
            EvalDoseE_btn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            EvalDoseE_btn.IsEnabled = true;
            Errors_txt.Text = errormessages;
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
            ExternalPlanSetup Plan00 = CalcPhoton(img00, rec_00, ph_prefix + "00");
            AddPhasePlan(CT00_plan_cb, Plan00);
            newPlan00 = Plan00;
            AllowUIToUpdate();

            ExternalPlanSetup Plan10 = CalcPhoton(img10, rec_10, ph_prefix + "10");
            AddPhasePlan(CT10_plan_cb, Plan10);
            newPlan10 = Plan10;
            AllowUIToUpdate();

            ExternalPlanSetup Plan20 = CalcPhoton(img20, rec_20, ph_prefix + "20");
            AddPhasePlan(CT20_plan_cb, Plan20);
            newPlan20 = Plan20;
            AllowUIToUpdate();

            ExternalPlanSetup Plan30 = CalcPhoton(img30, rec_30, ph_prefix + "30");
            AddPhasePlan(CT30_plan_cb, Plan30);
            newPlan30 = Plan30;
            AllowUIToUpdate();

            ExternalPlanSetup Plan40 = CalcPhoton(img40, rec_40, ph_prefix + "40");
            AddPhasePlan(CT40_plan_cb, Plan40);
            newPlan40 = Plan40;
            AllowUIToUpdate();

            ExternalPlanSetup Plan50 = CalcPhoton(img50, rec_50, ph_prefix + "50");
            AddPhasePlan(CT50_plan_cb, Plan50);
            newPlan50 = Plan50;
            AllowUIToUpdate();

            ExternalPlanSetup Plan60 = CalcPhoton(img60, rec_60, ph_prefix + "60");
            AddPhasePlan(CT60_plan_cb, Plan60);
            newPlan60 = Plan60;
            AllowUIToUpdate();

            ExternalPlanSetup Plan70 = CalcPhoton(img70, rec_70, ph_prefix + "70");
            AddPhasePlan(CT70_plan_cb, Plan70);
            newPlan70 = Plan70;
            AllowUIToUpdate();

            ExternalPlanSetup Plan80 = CalcPhoton(img80, rec_80, ph_prefix + "80");
            AddPhasePlan(CT80_plan_cb, Plan80);
            newPlan80 = Plan80;
            AllowUIToUpdate();

            ExternalPlanSetup Plan90 = CalcPhoton(img90, rec_90, ph_prefix + "90");
            AddPhasePlan(CT90_plan_cb, Plan90);
            newPlan90 = Plan90;
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
            IonPlanSetup Plan00 = CalcProton(img00, rec_00, pro_prefix + "00");
            AddPhasePlan(CT00_plan_cb, Plan00);
            newPlan00 = Plan00;
            AllowUIToUpdate();

            IonPlanSetup Plan10 = CalcProton(img10, rec_10, pro_prefix + "10");
            AddPhasePlan(CT10_plan_cb, Plan10);
            newPlan10 = Plan10;
            AllowUIToUpdate();

            IonPlanSetup Plan20 = CalcProton(img20, rec_20, pro_prefix + "20");
            AddPhasePlan(CT20_plan_cb, Plan20);
            newPlan20 = Plan20;
            AllowUIToUpdate();

            IonPlanSetup Plan30 = CalcProton(img30, rec_30, pro_prefix + "30");
            AddPhasePlan(CT30_plan_cb, Plan30);
            newPlan30 = Plan30;
            AllowUIToUpdate();

            IonPlanSetup Plan40 = CalcProton(img40, rec_40, pro_prefix + "40");
            AddPhasePlan(CT40_plan_cb, Plan40);
            newPlan40 = Plan40;
            AllowUIToUpdate();

            IonPlanSetup Plan50 = CalcProton(img50, rec_50, pro_prefix + "50");
            AddPhasePlan(CT50_plan_cb, Plan50);
            newPlan50 = Plan50;
            AllowUIToUpdate();

            IonPlanSetup Plan60 = CalcProton(img60, rec_60, pro_prefix + "60");
            AddPhasePlan(CT60_plan_cb, Plan60);
            newPlan60 = Plan60;
            AllowUIToUpdate();

            IonPlanSetup Plan70 = CalcProton(img70, rec_70, pro_prefix + "70");
            AddPhasePlan(CT70_plan_cb, Plan70);
            newPlan70 = Plan70;
            AllowUIToUpdate();

            IonPlanSetup Plan80 = CalcProton(img80, rec_80, pro_prefix + "80");
            AddPhasePlan(CT80_plan_cb, Plan80);
            newPlan80 = Plan80;
            AllowUIToUpdate();

            IonPlanSetup Plan90 = CalcProton(img90, rec_90, pro_prefix + "90");
            AddPhasePlan(CT90_plan_cb, Plan90);
            newPlan90 = Plan90;
            AllowUIToUpdate();
        }

        private string FindPrefixForPlans()
        {
            string pro_prefix = "4D_" + SelectedPlan.Id.Substring(0, 2) + "_";
            if(NameIsNotUnique(pro_prefix))
            {
                return pro_prefix;
            }
            pro_prefix = "4D_1_" + SelectedPlan.Id.Substring(0, 2) + "_";
            if (NameIsNotUnique(pro_prefix))
            {
                return pro_prefix;
            }
            pro_prefix = "4D_2_" + SelectedPlan.Id.Substring(0, 2) + "_";
            if (NameIsNotUnique(pro_prefix))
            {
                return pro_prefix;
            }
            pro_prefix = "4D_3_" + SelectedPlan.Id.Substring(0, 2) + "_";
            if (NameIsNotUnique(pro_prefix))
            {
                return pro_prefix;
            }
            pro_prefix = "4D_4_" + SelectedPlan.Id.Substring(0, 2) + "_";
            if (NameIsNotUnique(pro_prefix))
            {
                return pro_prefix;
            }
            return null;
        }

        private bool NameIsNotUnique(string pro_prefix)
        {
            foreach (var pla in SelectedPlan.Course.PlanSetups)
            {
                if (pla.Id == pro_prefix)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// The plans are added to the combobox and selected.
        /// </summary>
        private void AddPhasePlan(ComboBox plan_cb, PlanSetup plan)
        {
            if (plan_cb.SelectedItem == null)
            {
                plan_cb.Items.Add(plan.Id);
                int itemno = plan_cb.Items.Count;
                plan_cb.SelectedItem = plan_cb.Items[itemno - 1];
            }
        }

        /// <summary>
        /// Copying a selected photon plan to a single phase and calculating the dose.
        /// A rectangle is colored green if the calculation is a sucess.
        /// </summary>
        private ExternalPlanSetup CalcPhoton(VMS.TPS.Common.Model.API.Image img, Rectangle rec, string name)
        {

            StringBuilder outputDia = new StringBuilder("");
            ExternalPlanSetup plan;

            // If the plan is copied to the same image we do not need to calculate.
            // If the plan is copied to a new image, we will have to calculate later.
            // The copy-method is the same for IMRT og VMAT
            if (SelectedPlan.StructureSet.Image.Id == img.Id && SelectedPlan.StructureSet.Image.Series.UID == img.Series.UID)
            {
                plan = SelectedPlan.Course.CopyPlanSetup(SelectedPlan) as ExternalPlanSetup;
                rec.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                plan.Id = name;
                return plan;
            }
            else
            {
                plan = SelectedPlan.Course.CopyPlanSetup(SelectedPlan, img, outputDia) as ExternalPlanSetup;
            }

            plan.Id = name;


            // IMRT plans are copied now
            if (plan.Beams.First().Technique.ToString().ToUpper() == "STATIC")
            {
                errormessages += plan.Id + ": IMRT or static photon plan" + "\n";

                //The MU settings are copied from the original plan. This is the initialisation of the list of values for each beam in the plan.
                List<KeyValuePair<string, MetersetValue>> calculateIMRT = new List<KeyValuePair<string, MetersetValue>>();

                foreach (var item in SelectedPlan.Beams)
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
                    if (!res2.Success)
                    {
                        errormessages += "Leafmotion calculation error for plan: " + plan.Id + "\n";
                        return plan;
                    }
                }
                catch (Exception)
                {

                }

                //The dose is calculated
                var res = plan.CalculateDoseWithPresetValues(calculateIMRT);

                //The normalization is set. But this part is taken out.... Not nessesary
                //if (plan.PlanNormalizationValue != SelectedPlan.PlanNormalizationValue)
                //{
                //    plan.PlanNormalizationValue = SelectedPlan.PlanNormalizationValue;
                //}

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
                //MessageBox.Show("VMAT plan");
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
                    plan.PlanNormalizationValue = SelectedPlan.PlanNormalizationValue;
                    return plan;
                }
            }
        }

        /// <summary>
        /// Copying a selected proton plan to a single phase and calculating the dose.
        /// A rectangle is colored green if the calculation is a success.
        /// </summary>
        private IonPlanSetup CalcProton(VMS.TPS.Common.Model.API.Image img, Rectangle rec, string name)
        {

            StringBuilder outputDia = new StringBuilder("");
            IonPlanSetup plan;

            // If the plan is copied to the same image we do not need to calculate.
            // If the plan is copied to a new image, we will have to calculate later.
            if (SelectedPlan.StructureSet.Image.Series.UID == img.Series.UID && SelectedPlan.StructureSet.Image.Id == img.Id)
            {
                plan = SelectedPlan.Course.CopyPlanSetup(SelectedPlan) as IonPlanSetup;
            }
            else
            {
                plan = SelectedPlan.Course.CopyPlanSetup(SelectedPlan, img, outputDia) as IonPlanSetup;
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
            //The structures are imported
            string[] structurenames = new string[4] { CTV1_cb.SelectedItem.ToString(), CTV2_cb.SelectedItem.ToString(), Spinal_cb.SelectedItem.ToString(), Spinal2_cb.SelectedItem.ToString() };

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

            //The evaluation doses are imported
            double D1 = 50.0;
            double D2 = 50.0;
            double D3 = 50.0;
            double D4 = 50.0;

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
                D3 = 50.0;
            }

            //The final doses are written as a message
            errormessages += "CTV1 prescribed dose: " + D1.ToString("0.00") + "\n";
            errormessages += "CTV2 prescribed dose: " + D2.ToString("0.00") + "\n";
            errormessages += "OAR dose for evaluation: " + D3.ToString("0.00") + "\n";
            errormessages += "OAR dose for evaluation: " + D4.ToString("0.00") + "\n";


            // DVH results are read and saved in a variable if MU is approved. We allow a difference of less than 0.2 MU.
            // If the MU difference is too big, the rectangle will be set to red.
            if (CorrectMU(CT00plan, rec_00))
            {
                DVHresult CT00 = new DVHresult(CT00plan, structurenames, D1, D2, D3, D4);
                setValues(CT00, CT00_CTV1_lb, CT00_CTV2_lb, CT00_SC_lb, CT00_SC2_lb);
            }

            if (CorrectMU(CT10plan, rec_10))
            {
                DVHresult CT10 = new DVHresult(CT10plan, structurenames, D1, D2, D3, D4);
                setValues(CT10, CT10_CTV1_lb, CT10_CTV2_lb, CT10_SC_lb, CT10_SC2_lb);
            }

            if (CorrectMU(CT20plan, rec_20))
            {
                DVHresult CT20 = new DVHresult(CT20plan, structurenames, D1, D2, D3, D4);
                setValues(CT20, CT20_CTV1_lb, CT20_CTV2_lb, CT20_SC_lb, CT20_SC2_lb);
            }

            if (CorrectMU(CT30plan, rec_30))
            {
                DVHresult CT30 = new DVHresult(CT30plan, structurenames, D1, D2, D3, D4);
                setValues(CT30, CT30_CTV1_lb, CT30_CTV2_lb, CT30_SC_lb, CT30_SC2_lb);
            }

            if (CorrectMU(CT40plan, rec_40))
            {
                DVHresult CT40 = new DVHresult(CT40plan, structurenames, D1, D2, D3, D4);
                setValues(CT40, CT40_CTV1_lb, CT40_CTV2_lb, CT40_SC_lb, CT40_SC2_lb);
            }

            if (CorrectMU(CT50plan, rec_50))
            {
                DVHresult CT50 = new DVHresult(CT50plan, structurenames, D1, D2, D3, D4);
                setValues(CT50, CT50_CTV1_lb, CT50_CTV2_lb, CT50_SC_lb, CT50_SC2_lb);
            }

            if (CorrectMU(CT60plan, rec_60))
            {
                DVHresult CT60 = new DVHresult(CT60plan, structurenames, D1, D2, D3, D4);
                setValues(CT60, CT60_CTV1_lb, CT60_CTV2_lb, CT60_SC_lb, CT60_SC2_lb);
            }

            if (CorrectMU(CT70plan, rec_70))
            {
                DVHresult CT70 = new DVHresult(CT70plan, structurenames, D1, D2, D3, D4);
                setValues(CT70, CT70_CTV1_lb, CT70_CTV2_lb, CT70_SC_lb, CT70_SC2_lb);
            }

            if (CorrectMU(CT80plan, rec_80))
            {
                DVHresult CT80 = new DVHresult(CT80plan, structurenames, D1, D2, D3, D4);
                setValues(CT80, CT80_CTV1_lb, CT80_CTV2_lb, CT80_SC_lb, CT80_SC2_lb);
            }

            if (CorrectMU(CT90plan, rec_90))
            {
                DVHresult CT90 = new DVHresult(CT90plan, structurenames, D1, D2, D3, D4);
                setValues(CT90, CT90_CTV1_lb, CT90_CTV2_lb, CT90_SC_lb, CT90_SC2_lb);
            }

            //Nu kan der eksporteres DVH'er
            ExportDVH_btn.IsEnabled = true;
            Errors_txt.Text = errormessages;
        }

        /// <summary>
        /// The MU of the plan is compared to the original plan, and the rectangle is colored red if the difference is larger than 1 promil.
        /// The calibration curve is checked.
        /// </summary>
        private bool CorrectMU(PlanSetup plan, Rectangle rec)
        {
            bool MUisOK = true;

            if (plan == null)
            {
                errormessages += "Select a plan to analyse \n";
                MUisOK = false;
            }
            else
            {
                if (SelectedPlan.StructureSet.Image.Series.ImagingDeviceId != plan.StructureSet.Image.Series.ImagingDeviceId)
                    errormessages += plan.Id + " is not on the same calibrationsurve as the baseplan \n";

                for (int i = 0; i < plan.Beams.Count(); i++)
                {
                    double test1 = plan.Beams.ElementAt(i).Meterset.Value;
                    double test2 = SelectedPlan.Beams.ElementAt(i).Meterset.Value;

                    if (Math.Abs(test1 - test2) / test2 > 0.001)
                    {
                        MUisOK = false;
                        errormessages += plan.Id + " and the main plan differs in MU by: " + Math.Abs(test1 - test2).ToString("0.00") + "\n";
                        rec.Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0)); //red

                    }
                    else if (Math.Abs(test1 - test2) / test2 > 0.00001)
                    {
                        errormessages += plan.Id + " and the main plan differs slightly in MU by: " + Math.Abs(test1 - test2).ToString("0.00") + "\n";
                        rec.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 0));//yellow
                    }
                    else
                    {
                        rec.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));//green
                    }
                }
            }
            return MUisOK;
        }
        
        /// <summary>
        /// An int indicating the phase and the corresponding combobox is used to fetch the treatment plan selected in the box.
        /// It will either catch a plan selected by the user or use the public variables with the newly calculated plans.
        /// </summary>
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
        private void setValues(DVHresult res, Label CTV1_lb, Label CTV2_lb, Label SC_lb, Label SC2_lb)
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

        }
        
        #region DVHexport
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

            PlanSetup[] allPlans = new PlanSetup[11] { SelectedPlan, newPlan00, newPlan10, newPlan20, newPlan30, newPlan40, newPlan50, newPlan60, newPlan70, newPlan80, newPlan90 };
            Errors_txt.Text = errormessages;

            //MessageBox.Show("Planer fundet og lagt i en array");

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

            //MessageBox.Show("Vi gemmer her: " + folderToSave + "\\" + "dvh_exports");

            //Loopeing over planer
            for (int i = 0; i < allPlans.Count(); i++)
            {
                string filename = allPlans[i].Id;
                //MessageBox.Show(filename);

                string firstLine = "Nominal dose. Plan id: " + allPlans[i].Id;
                //Data skal nu samles i en stor matrice
                int largestDVH = FindLargestDVH(allPlans[i], dvhresolution);
                //MessageBox.Show("Længste DVH fundet til " + largestDVH.ToString());

                int numbOfStructs = FindNumberOfStructs(allPlans[i]);
                //MessageBox.Show("Antal strukturer fundet til: " + numbOfStructs.ToString());

                //Matricen kan nu oprettes da vi ved hvor stor den skal være
                double[,] dvhList = new double[largestDVH, numbOfStructs + 1]; //MULTI
                //MessageBox.Show("Tom liste oprettet til strukturer");
                FillValues(dvhList, allPlans[i], largestDVH, numbOfStructs, dvhresolution); //MULTI
                //MessageBox.Show("Struktrnavne fyldt i");

                string[] idList = new string[numbOfStructs + 1];
                FillIDs(idList, allPlans[i]);

                double[] volList = new double[numbOfStructs];
                FillIVols(volList, allPlans[i]);

                double[] minList = new double[numbOfStructs];
                double[] maxList = new double[numbOfStructs];
                double[] meanList = new double[numbOfStructs];

                FillIminmaxmean(minList, maxList, meanList, allPlans[i],dvhresolution);

                WriteDVHfile(folderToSave + "\\" + "dvh_exports", filename, dvhList, idList, numbOfStructs, largestDVH, firstLine, volList, minList,maxList,meanList); //MULTI
            }

            //Nu tjekker vi om den nominelle plan har usikkerhedsscenarier. Hvis ja, så skal disse også udskrives på samme måde
            if (allPlans[0].PlanUncertainties.Count() != 0)
            {
                foreach (var uncert in allPlans[0].PlanUncertainties)
                {
                    if (uncert.Dose == null) continue;

                    string filename = allPlans[0].Id.Substring(0, 4) + "_" + uncert.Id;
                    string firstLine = "Uncertainty scenario: " + uncert.DisplayName + " to nominal plan: " + allPlans[0].Id;
                    //Data skal nu samles i en stor matrice
                    int largestDVH = FindLargestDVH(allPlans[0], uncert, dvhresolution);
                    int numbOfStructs = FindNumberOfStructs(allPlans[0]);

                    //Matricen kan nu oprettes da vi ved hvor stor den skal være
                    double[,] dvhList = new double[largestDVH, numbOfStructs + 1];
                    string[] idList = new string[numbOfStructs + 1];

                    double[] volList = new double[numbOfStructs];
                    FillIVols(volList, allPlans[0]);

                    double[] minList = new double[numbOfStructs];
                    double[] maxList = new double[numbOfStructs];
                    double[] meanList = new double[numbOfStructs];

                    FillIminmaxmean(minList, maxList, meanList, allPlans[0], dvhresolution);

                    FillValues(dvhList, allPlans[0], largestDVH, numbOfStructs, uncert, dvhresolution);
                    FillIDs(idList, allPlans[0]);

                    WriteDVHfile(folderToSave + "\\" + "dvh_exports", filename, dvhList, idList, numbOfStructs, largestDVH, firstLine, volList, minList, maxList, meanList);
                }
            }
        }

        /// <summary>
        /// Calculates the min, max and mean dose values for all structures in a plan and aded to three lists with the correct format.
        /// </summary>
        private void FillIminmaxmean(double[] minList, double[] maxList, double[] meanList, PlanSetup planSetup,double dvhresolution)
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
        #endregion DVHexport

    }
}

/// <summary>
/// A class for the DVH calculations.
/// </summary>
public class DVHresult
{
    public double V95CTV1 { get; set; }
    public double V95CTV2 { get; set; }
    public double V50_SC { get; set; }
    public double V50_SC2 { get; set; }

    public double D_CTV1 { get; set; }
    public double D_CTV2 { get; set; }
    public double D_OAR { get; set; }
    public double D_OAR2 { get; set; }


    /// <summary>
    /// A class for the DVH calculations for each plan.
    /// </summary> 
    public DVHresult(PlanSetup plan, string[] structnames, double D1, double D2, double D3, double D4)
    {
        Dose planDose = plan.Dose;

        V95CTV1 = -1000.0;
        V95CTV2 = -1000.0;
        V50_SC = -1000.0;
        V50_SC2 = -1000.0;

        D_CTV1 = D1;
        D_CTV2 = D2;
        D_OAR = D3;
        D_OAR2 = D3;

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
        }
    }
}


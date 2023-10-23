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
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace Evaluering4D
{
    /// <summary>
    /// A class that can adjust the calibration curves for an image, overwrite and copy structures and create a body.
    /// </summary>
    internal class AdjustPhaseImages
    {
        public Image[] ImageList { get; set; }
        public ScriptContext ScriptInfo { get; set; }
        public StructureSet SelectedStructureSet { get; set; } //The main structureset with the main plan.

        /// <summary>
        /// Constructs an object with the list of images to adjust, the script info on the patient and the structureset belonging to the image
        /// </summary>
        /// <param name="imageList"></param>
        /// <param name="scriptInfo"></param>
        /// <param name="selectedSet"></param>
        public AdjustPhaseImages(Image[] imageList,ScriptContext scriptInfo, StructureSet selectedSet )
        {
            ImageList = imageList;
            ScriptInfo = scriptInfo;
            SelectedStructureSet = selectedSet;
        }

        /// <summary>
        /// This method can transfer HU-overwritten structures to the 4D phases and overwrite them.
        /// </summary>
        public string[] OverwriteStructures()
        {
            string[] report_string = new string[10];

            // If something goes wrong we will flag it and write a message
            bool errorCopy = false;

            // A checklist is used to check if an image has a structure set or not, or if the image is skipped and therefor is null.
            // The structure sets are collected in a list as we cannot get the structure from the image.
            int[] checklist = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            StructureSet[] structureSetList = new StructureSet[10];

            for (int i = 0; i < ImageList.Length; i++)
            {
                var img = ImageList.ElementAt(i);

                // Searching through all structure sets in the patient to find the correct sets.
                foreach (var struSet in ScriptInfo.Patient.StructureSets)
                {
                    if (img != null && struSet.Image.Id == img.Id && struSet.Image.Series.UID == img.Series.UID && img.Series.Study.Id == SelectedStructureSet.Image.Series.Study.Id)
                    {
                        //We have a structureset!
                        checklist[i] = 1;
                        structureSetList[i] = struSet;
                    }
                }
            }

            //Finding HU-overwritten structures in the original plan
            List<Structure> overwrittenStructs = new List<Structure>();
            foreach (var stru in SelectedStructureSet.Structures)
            {
                double HU;
                if (stru.GetAssignedHU(out HU))
                {
                    overwrittenStructs.Add(stru);
                }
            }

            // Looping over all images and copying structures if needed and hereafter HU-overwriting if the calibration curve is the same as the original plan.
            for (int i = 0; i < ImageList.Count(); i++)
            {
                bool dicomTypesChanged = false;
                var img = ImageList[i];

                string message = "";

                // Checking if the image is skipped and checking if the calibration curves are the same
                if (img != null && SelectedStructureSet.Image.Series.ImagingDeviceId == img.Series.ImagingDeviceId)
                {
                    var struSet = structureSetList[i];

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

                        //If the stucture already exist, it will be HU-overwritten.
                        if (structureExist)
                        {
                            var str = struSet.Structures.Where(s => s.Id == strSelected.Id).First();

                            double HU;
                            strSelected.GetAssignedHU(out HU);

                            try
                            {
                                str.SetAssignedHU(HU);
                                message += "- " + HU.ToString() + " HU assigned: " + str.Id + "\n";
                            }
                            catch (Exception)
                            {
                                if (str.GetAssignedHU(out double HUs) && HUs == HU)
                                {
                                    message += "- HU is correct: " + str.Id + "\n";

                                }
                                else
                                {
                                    message += "- Unable to assign HU: " + str.Id + "\n";
                                    errorCopy = true;
                                }
                            }
                        }
                        // The structure does not exist and we must create it. Due to an issue with ESAPI and dicom types this is done by two try/catch methods.
                        else
                        {
                            double HU;
                            strSelected.GetAssignedHU(out HU);

                            try
                            {
                                Structure newstru = struSet.AddStructure(strSelected.DicomType, strSelected.Id);
                                newstru.SegmentVolume = strSelected.SegmentVolume;
                                newstru.SetAssignedHU(HU);
                                message += "- " + HU.ToString() + " HU assigned: " + newstru.Id + "\n";
                            }
                            catch (Exception)
                            {
                                // There is an issue in ESAPI that transfered structures can loose there Dicom Type and for some reason not be transfered copied.
                                // This is handled by creating a new structure for the couch and setting a dicom type.
                                // This can however cause problems if overwritten structures are overlapping. As the priority is then changed.
                                try
                                {
                                    Structure newstru = struSet.AddStructure("CONTROL", strSelected.Id + "_copy");
                                    newstru.SegmentVolume = strSelected.SegmentVolume;
                                    newstru.SetAssignedHU(HU);
                                    message += "- " + HU.ToString() + " HU assigned: " + newstru.Id + "\n";
                                    dicomTypesChanged = true;
                                }
                                catch (Exception)
                                {
                                    errorCopy = true;
                                }
                            }
                        }
                    }
                }
                else if (img != null)
                {
                    errorCopy = true;
                }

                if (dicomTypesChanged)
                {
                    message += "- Some DICOM types have been changed. Check if the WET is correct if overwritten structures are overlapping. \n";
                }


                if (errorCopy)
                {
                    message += "- ERROR: structure(s) not copied/assigned \n";
                }


                report_string[i] = message;

            }
            return report_string;

        }

        /// <summary>
        /// The calibration curve is set to the same device as for the nominal plan if it is possible.
        /// </summary>
        public string[] CopyCalibration()
        {
            string[] report_string = new string[10];
            int phasecount = 0;

            // The imagingDevice (aka the calibration curve) is compared. 
            // If it is different we will try to change it. 
            // If it cannot be changed the user will recieve a message about it.
            foreach (var img in ImageList)
            {

                string message = "";

                if (img != null && SelectedStructureSet.Image.Series.ImagingDeviceId != img.Series.ImagingDeviceId)
                {
                    try
                    {
                        img.Series.SetImagingDevice(SelectedStructureSet.Image.Series.ImagingDeviceId);


                        message += "- Imaging device is set" + "\n";

                    }
                    catch (Exception)
                    {
                        if (SelectedStructureSet.Image.Series.ImagingDeviceId != img.Series.ImagingDeviceId)
                        {

                            message += "- Unable to set imaging device" + "\n";
                        }
                    }
                }
                else if (img != null)
                {

                    message += "- Imaging device is correct" + "\n";
                }

                report_string[phasecount] = message;
                phasecount++;

            }
            return report_string;

        }

        /// <summary>
        /// For each phase in the 4D we search for a structure set. 
        /// If the set does not exist it is created.
        /// We will also create the body structure.
        /// </summary>
        public string[] CreateBody()
        {
            string[] report_string = new string[10];

            StructureSet[] structureSetList = new StructureSet[10];

            int[] checklist = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //Are we missing structuresets on the images?
            for (int i = 0; i < ImageList.Length; i++)
            {
                var img = ImageList.ElementAt(i);
                //Finding the structure set for the image phase
                foreach (var struSet in ScriptInfo.Patient.StructureSets)
                {
                    if (img != null && struSet.Image.Id == img.Id && struSet.Image.Series.UID == img.Series.UID)
                    {
                        //We have a structureset!
                        checklist[i] = 1;
                        structureSetList[i] = struSet;
                    }
                }
            }

            // Missing structure sets will be created and a default body is created.
            Structure bodystructure = SelectedStructureSet.Structures.First(s => s.DicomType.ToUpper() == "EXTERNAL");
            for (int i = 0; i < checklist.Count(); i++)
            {
                string message = "";

                if (ImageList.ElementAt(i) != null)
                {
                    if (checklist[i] != 1)
                    {
                        var img = ImageList.ElementAt(i);
                        StructureSet test = img.CreateNewStructureSet();
                        test.Id = img.Id;

                        var parameters = SelectedStructureSet.GetDefaultSearchBodyParameters();
                        Structure newBody = test.CreateAndSearchBody(parameters);
                        newBody.Id = "BODY";
                        structureSetList[i] = test;

                        message += "- BODY is created. \n";
                    }
                    else
                    {
                        var img = ImageList.ElementAt(i);

                        // We need to check if there is a body structure in the structure set.
                        bool findBody = false;

                        foreach (var stru in structureSetList[i].Structures)
                        {
                            if (stru.DicomType.ToUpper() == "EXTERNAL")
                            {
                                findBody = true;
                            }
                        }


                        if (findBody == false) // There was no body, we will create it before we copy the setment.
                        {
                            var parameters = SelectedStructureSet.GetDefaultSearchBodyParameters();
                            Structure newBody = structureSetList[i].CreateAndSearchBody(parameters);
                            newBody.Id = "BODY";

                            message += "- BODY is created. \n";
                        }
                    }
                }
                report_string[i] = message;

            }
            //Errors_txt.Text = errormessages;
            return report_string;
        }

        /// <summary>
        /// For some centres the total body from the nominal plan is needed on all the phases. This function copies the body, if the user has selected this option.
        /// </summary>
        /// <returns></returns>
        public string[] CopyBody()
        {
            string[] report_string = new string[10];

            //Creating a list of all structuresets
            StructureSet[] structureSetList = new StructureSet[10];
            for (int i = 0; i < ImageList.Length; i++)
            {
                var img = ImageList.ElementAt(i);
                //Finding the structure set for the image phase
                foreach (var struSet in ScriptInfo.Patient.StructureSets)
                {
                    if (img != null && struSet.Image.Id == img.Id && struSet.Image.Series.UID == img.Series.UID)
                    {
                        //We have a structureset!
                        structureSetList[i] = struSet;
                    }
                }
            }

            Structure bodystructure = SelectedStructureSet.Structures.First(s => s.DicomType.ToUpper() == "EXTERNAL");
            for (int i = 0; i < 10; i++)
            {
                string message = "";

                if (ImageList.ElementAt(i) != null)
                {
                    foreach (var stru in structureSetList[i].Structures)
                    {
                        if (stru.DicomType.ToUpper() == "EXTERNAL")
                        {
                            Structure bodystructurePhase = structureSetList[i].Structures.First(s => s.DicomType.ToUpper() == "EXTERNAL");

                            try
                            {
                                bodystructurePhase.SegmentVolume = bodystructure.SegmentVolume;
                                message += "- BODY is copied. \n";
                            }
                            catch (Exception)
                            {
                                message += "- BODY is NOT copied. \n";
                            }
                        }
                    }
                }
                report_string[i] = message;

            }
            return report_string;
        }
    }
}
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace COFRSFrameworkInstaller
{
    public class RestServiceWizard : IWizard
    {
        private UserInputProject inputForm;
        private bool Proceed = false;

        // This method is called before opening any item that
        // has the OpenInEditor attribute.
        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public void ProjectFinishedGenerating(Project project)
        {
        }

        // This method is only called for item templates,
        // not for project templates.
        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }

        // This method is called after the project is created.
        public void RunFinished()
        {
        }

        public void RunStarted(object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind, object[] customParams)
        {
            try
            {
                Random randomNumberGenerator = new Random(Convert.ToInt32(0x0000ffffL & DateTime.Now.ToFileTimeUtc()));
                // Display a form to the user. The form collects
                // input for the custom message.
                inputForm = new UserInputProject();
                if (inputForm.ShowDialog() == DialogResult.OK)
                {

                    Version version = Version.Parse(replacementsDictionary["$targetframeworkversion$"]);
                    string idModelVersion = "net461";
                    string tupleVersion = "net461";
                    string packagesFolder = "..\\packages";
                    string securityModel = "";

                    string solutionFolder = replacementsDictionary["$solutiondirectory$"].ToLower();
                    string projectFolder = replacementsDictionary["$destinationdirectory$"].ToLower();

                    if (projectFolder.Contains(solutionFolder) && !string.Equals(solutionFolder, projectFolder))
                        packagesFolder = "..\\packages";
                    else
                        packagesFolder = ".\\packages";

                    if (version >= new Version(4, 7, 2))
                        idModelVersion = "net472";

                    if (version >= new Version(4, 7, 0))
                        tupleVersion = "net47";

                    var compactVersion = replacementsDictionary["$targetframeworkversion$"];
                    compactVersion = "net" + compactVersion.Replace(".", "");

                    var logPath = Path.Combine(replacementsDictionary["$destinationdirectory$"], "App_Data\\log-{Date}.json").Replace("\\", "\\\\");

                    var portNumber = randomNumberGenerator.Next(1024, 65534);

                    if (inputForm.databaseTechnology.SelectedIndex == 0)
                        replacementsDictionary.Add("$databasetech$", "mysql");
                    else if (inputForm.databaseTechnology.SelectedIndex == 1)
                        replacementsDictionary.Add("$databasetech$", "postgresql");
                    else if (inputForm.databaseTechnology.SelectedIndex == 2)
                        replacementsDictionary.Add("$databasetech$", "sqlserver");

                    if (inputForm.SecuritySelector.SelectedIndex == 0)
                        securityModel = "none";
                    else
                        securityModel = "OAuth";

                    if (compactVersion == "net461")
                        replacementsDictionary.Add("$automapperversion$", compactVersion);
                    else
                        replacementsDictionary.Add("$automapperversion$", "net47");

                    if (compactVersion == "net461")
                        replacementsDictionary.Add("$mysqlversion$", "net452");

                    // Add custom parameters.
                    replacementsDictionary.Add("$companymoniker$", inputForm.CompanyMoniker.Text);
                    replacementsDictionary.Add("$securitymodel$", securityModel);
                    replacementsDictionary.Add("$compactversion$", compactVersion);
                    replacementsDictionary.Add("$idmodelversion$", idModelVersion);
                    replacementsDictionary.Add("$tupleVersion$", tupleVersion);
                    replacementsDictionary.Add("$logPath$", logPath);
                    replacementsDictionary.Add("$portNumber$", portNumber.ToString());
                    replacementsDictionary.Add("$packagesfolder$", packagesFolder);

                    Proceed = true;


                }
                else
                {
                    Proceed = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        // This method is only called for item templates,
        // not for project templates.
        public bool ShouldAddProjectItem(string filePath)
        {
            return Proceed;
        }
    }
}





using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint;
using System.IO;
using Nextlabs.Entitlement.Wizard.Resources;
using System.Security;
using Microsoft.Win32;
using System.Diagnostics;


namespace Nextlabs.Entitlement.Wizard
{

	public partial class InstallProcessControl : InstallerControl
	{
		//add list to save the install message,then display the details message
		private static List<string> messageList = new List<string>();
		private static readonly TimeSpan JobTimeout = TimeSpan.FromMinutes(15);

		private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
		private CommandList executeCommands;
		private CommandList rollbackCommands;
		private int nextCommand;
		private bool completed;
		private bool requestCancel;
		private int errors;
		private int rollbackErrors;

		public InstallProcessControl()
		{
			InitializeComponent();

			errorPictureBox.Visible = false;
			errorDetailsTextBox.Visible = false;

			this.Load += new EventHandler(InstallProcessControl_Load);
		}

		#region Event Handlers

		private void InstallProcessControl_Load(object sender, EventArgs e)
		{
			switch (Form.Operation)
			{
			case InstallOperation.Install:
				Form.SetTitle(CommonUIStrings.installTitle);
				Form.SetSubTitle(InstallConfiguration.FormatString(CommonUIStrings.installSubTitle));
				break;

			case InstallOperation.Upgrade:
				Form.SetTitle(CommonUIStrings.upgradeTitle);
				Form.SetSubTitle(InstallConfiguration.FormatString(CommonUIStrings.upgradeSubTitle));
				break;

			case InstallOperation.UpgradeRestore:
				Form.SetTitle(CommonUIStrings.upgradeRestoreTitle);
				Form.SetSubTitle(InstallConfiguration.FormatString(CommonUIStrings.upgradeRestoreSubTitile));
				break;

			case InstallOperation.Repair:
				Form.SetTitle(CommonUIStrings.repairTitle);
				Form.SetSubTitle(InstallConfiguration.FormatString(CommonUIStrings.repairSubTitle));
				break;

			case InstallOperation.Uninstall:
				Form.SetTitle(CommonUIStrings.uninstallTitle);
				Form.SetSubTitle(InstallConfiguration.FormatString(CommonUIStrings.uninstallSubTitle));
				break;
			}

			Form.PrevButton.Enabled = false;
			Form.NextButton.Enabled = false;
		}

		private void TimerEventInstall(Object myObject, EventArgs myEventArgs)
		{
			timer.Stop();

			if (requestCancel)
			{
				descriptionLabel.Text = Resources.CommonUIStrings.descriptionLabelTextOperationCanceled;
				InitiateRollback();
			}

			else if (nextCommand < executeCommands.Count)
			{

				Command command = executeCommands[nextCommand];
				bool ret = true;
				try
				{
					ret = command.Execute();
				}
				catch (Exception ex)
				{
					if (ex.Message.Contains("Access to the path") && ex.Message.Contains("The removal of this file"))
					{
						ret = true;
					}
					else
					{
						messageList.Add(CommonUIStrings.logError);
						messageList.Add(ex.Message);
						messageList.Add(ex.ToString());

						errors++;
						errorPictureBox.Visible = true;
						errorDetailsTextBox.Visible = true;
						errorDetailsTextBox.Text = ex.Message;

						descriptionLabel.Text = Resources.CommonUIStrings.descriptionLabelTextErrorsDetected;
						InitiateRollback();
						return;
					}
				}
				if (ret)
				{
					nextCommand++;
					progressBar.PerformStep();

					if (nextCommand < executeCommands.Count)
					{
						descriptionLabel.Text = executeCommands[nextCommand].Description;
					}
				}
				timer.Start();
			}

			else
			{
				descriptionLabel.Text = Resources.CommonUIStrings.descriptionLabelTextSuccess;
				HandleCompletion();
			}
		}

		private void TimerEventRollback(Object myObject, EventArgs myEventArgs)
		{
			timer.Stop();

			if (nextCommand < rollbackCommands.Count)
			{
				try
				{
					Command command = rollbackCommands[nextCommand];
					if (command.Rollback())
					{
						nextCommand++;
						progressBar.PerformStep();
					}
				}

				catch (Exception ex)
				{
					messageList.Add(CommonUIStrings.logError);
					messageList.Add(ex.Message);
					messageList.Add(ex.ToString());

					rollbackErrors++;
					nextCommand++;
					progressBar.PerformStep();
				}

				timer.Start();
			}

			else
			{
				if (rollbackErrors == 0)
				{
					progressBar.Step = 1;
					progressBar.Maximum = 1;
					progressBar.Value = 1;
					progressBar.PerformStep();
					descriptionLabel.Text = Resources.CommonUIStrings.descriptionLabelTextRollbackSuccess;
				}
				else
				{
					descriptionLabel.Text = string.Format(Resources.CommonUIStrings.descriptionLabelTextRollbackError, rollbackErrors);
				}
				HandleRollBackCompletion();
			}
		}

		#endregion

		#region Protected Methods

		protected internal override void RequestCancel()
		{
			if (completed)
			{
				base.RequestCancel();
			}
			else
			{
				requestCancel = true;
				Form.AbortButton.Enabled = false;
				Form.NextButton.Enabled = false;
			}
		}

		protected internal override void Open(InstallOptions options)
		{
			executeCommands = new CommandList();
			rollbackCommands = new CommandList();
			nextCommand = 0;
			SPFeatureScope featureScope = InstallConfiguration.FeatureScope;
			DeactivateSiteCollectionFeatureCommand deactivateSiteCollectionFeatureCommand = null;

			switch (Form.Operation)
			{
			case InstallOperation.Install:
				executeCommands.Add(new AddSolutionCommand(this));
				executeCommands.Add(new CreateDeploymentJobCommand(this, options.WebApplicationTargets));
#if SP2016 || SP2019
				executeCommands.Add(new WaitForJobCompletionCommand(this, CommonUIStrings.waitForSolutionDeployment, Form.Operation, options));
#else
                    executeCommands.Add(new WaitForJobCompletionCommand(this, CommonUIStrings.waitForSolutionDeployment));
#endif
				if (featureScope == SPFeatureScope.Farm)
				{
					executeCommands.Add(new ActivateFarmFeatureCommand(this));
				}
				else if (featureScope == SPFeatureScope.Site)
				{
					executeCommands.Add(new ActivateSiteCollectionFeatureCommand(this, options.SiteCollectionTargets));
				}
				executeCommands.Add(new RegisterVersionNumberCommand(this));

				for (int i = executeCommands.Count - 1; i <= 0; i--)
				{
					rollbackCommands.Add(executeCommands[i]);
				}
				break;

			case InstallOperation.Upgrade:
				if (featureScope == SPFeatureScope.Farm)
				{
					executeCommands.Add(new DeactivateFarmFeatureCommand(this));
				}
				else if (featureScope == SPFeatureScope.Site)
				{
					deactivateSiteCollectionFeatureCommand = new DeactivateSiteCollectionFeatureCommand(this);
					executeCommands.Add(deactivateSiteCollectionFeatureCommand);
				}
				if (!IsSolutionRenamed())
				{
					executeCommands.Add(new CreateUpgradeJobCommand(this));
					executeCommands.Add(new WaitForJobCompletionCommand(this, CommonUIStrings.waitForSolutionUpgrade));
				}
				else
				{
					executeCommands.Add(new CreateRetractionJobCommand(this));
					executeCommands.Add(new WaitForJobCompletionCommand(this, CommonUIStrings.waitForSolutionRetraction));
					executeCommands.Add(new RemoveSolutionCommand(this));
					executeCommands.Add(new AddSolutionCommand(this));
					executeCommands.Add(new CreateDeploymentJobCommand(this, GetDeployedApplications()));
					executeCommands.Add(new WaitForJobCompletionCommand(this, CommonUIStrings.waitForSolutionDeployment));
				}
				if (featureScope == SPFeatureScope.Farm)
				{
					executeCommands.Add(new ActivateFarmFeatureCommand(this));
				}
				if (featureScope == SPFeatureScope.Site)
				{
					executeCommands.Add(new ActivateSiteCollectionFeatureCommand(this, deactivateSiteCollectionFeatureCommand.DeactivatedSiteCollections));
				}
				executeCommands.Add(new RegisterVersionNumberCommand(this));
				break;

			case InstallOperation.Repair:
				if (featureScope == SPFeatureScope.Farm)
				{
					executeCommands.Add(new DeactivateFarmFeatureCommand(this));
				}
				if (featureScope == SPFeatureScope.Site)
				{
					deactivateSiteCollectionFeatureCommand = new DeactivateSiteCollectionFeatureCommand(this);
					executeCommands.Add(deactivateSiteCollectionFeatureCommand);
				}
				executeCommands.Add(new CreateRetractionJobCommand(this));
				executeCommands.Add(new WaitForJobCompletionCommand(this, CommonUIStrings.waitForSolutionRetraction));
				executeCommands.Add(new RemoveSolutionCommand(this));
				executeCommands.Add(new AddSolutionCommand(this));
				executeCommands.Add(new CreateDeploymentJobCommand(this, GetDeployedApplications()));
				executeCommands.Add(new WaitForJobCompletionCommand(this, CommonUIStrings.waitForSolutionDeployment));
				if (featureScope == SPFeatureScope.Farm)
				{
					executeCommands.Add(new ActivateFarmFeatureCommand(this));
				}
				if (featureScope == SPFeatureScope.Site)
				{
					executeCommands.Add(new ActivateSiteCollectionFeatureCommand(this, deactivateSiteCollectionFeatureCommand.DeactivatedSiteCollections));
				}
				executeCommands.Add(new RegisterVersionNumberCommand(this));
				break;

			case InstallOperation.Uninstall:
				if (featureScope == SPFeatureScope.Farm)
				{
					executeCommands.Add(new DeactivateFarmFeatureCommand(this));
				}
				if (featureScope == SPFeatureScope.Site)
				{
					executeCommands.Add(new DeactivateSiteCollectionFeatureCommand(this));
				}
				executeCommands.Add(new CreateRetractionJobCommand(this));
				executeCommands.Add(new WaitForJobCompletionCommand(this, CommonUIStrings.waitForSolutionRetraction));
				executeCommands.Add(new RemoveSolutionCommand(this));
				executeCommands.Add(new UnregisterVersionNumberCommand(this));
				break;

			case InstallOperation.UpgradeRestore:
				executeCommands.Add(new DeactivateFarmFeatureCommand(this));
				executeCommands.Add(new CreateRetractionJobCommand(this));
				executeCommands.Add(new WaitForJobCompletionCommand(this, CommonUIStrings.waitForSolutionRetraction));
				executeCommands.Add(new RemoveSolutionCommand(this));
				executeCommands.Add(new AddSolutionCommand(this));
				executeCommands.Add(new CreateDeploymentJobCommand(this, GetDeployedApplications()));
				executeCommands.Add(new WaitForJobCompletionCommand(this, CommonUIStrings.waitForSolutionDeployment));
				executeCommands.Add(new ActivateFarmFeatureCommand(this));
				executeCommands.Add(new RegisterVersionNumberCommand(this));
				executeCommands.Add(new UpgradeRestoreCommand(this, GetDeployedApplications()));
				break;
			}

			progressBar.Maximum = executeCommands.Count;

			descriptionLabel.Text = executeCommands[0].Description;

			timer.Interval = 1000;
			timer.Tick += new EventHandler(TimerEventInstall);
			timer.Start();
		}

		#endregion

		#region Private Methods

		private void HandleCompletion()
		{
			completed = true;

			Form.NextButton.Enabled = true;
			Form.AbortButton.Text = CommonUIStrings.abortButtonText;
			Form.AbortButton.Enabled = true;

			CompletionControl nextControl = new CompletionControl();

			foreach (string message in messageList)
			{
				nextControl.Details += message + "\r\n";
			}

			switch (Form.Operation)
			{
			case InstallOperation.Install:
				nextControl.Title = errors == 0 ? CommonUIStrings.installSuccess : CommonUIStrings.installError;
				break;

			case InstallOperation.Upgrade:
				nextControl.Title = errors == 0 ? CommonUIStrings.upgradeSuccess : CommonUIStrings.upgradeError;
				break;

			case InstallOperation.UpgradeRestore:
				nextControl.Title = errors == 0 ? CommonUIStrings.upgradeRestoreSuccess : CommonUIStrings.upgradeRestoreError;
				break;

			case InstallOperation.Repair:
				nextControl.Title = errors == 0 ? CommonUIStrings.repairSuccess : CommonUIStrings.repairError;
				break;

			case InstallOperation.Uninstall:
				nextControl.Title = errors == 0 ? CommonUIStrings.uninstallSuccess : CommonUIStrings.uninstallError;
				break;
			}

			Form.ContentControls.Add(nextControl);
		}

		private void HandleRollBackCompletion()
		{
			completed = true;

			Form.NextButton.Enabled = false;
			Form.AbortButton.Text = CommonUIStrings.abortButtonText;
			Form.AbortButton.Enabled = true;
		}

		private void InitiateRollback()
		{
			Form.AbortButton.Enabled = false;

			progressBar.Maximum = rollbackCommands.Count;
			progressBar.Value = rollbackCommands.Count;
			nextCommand = 0;
			rollbackErrors = 0;
			progressBar.Step = -1;

			//
			// Create and start new timer.
			//
			timer = new System.Windows.Forms.Timer();
			timer.Interval = 1000;
			timer.Tick += new EventHandler(TimerEventRollback);
			timer.Start();
		}

		private bool IsSolutionRenamed()
		{
			SPFarm farm = SPFarm.Local;
			SPSolution solution = farm.Solutions[InstallConfiguration.SolutionId];
			if (solution == null) return false;
			string filename = @"C:\Program Files\NextLabs\SharePoint Enforcer\solution\NextLabs.Entitlement.wsp";
			FileInfo solutionFileInfo = new FileInfo(filename);
			return !solution.Name.Equals(solutionFileInfo.Name, StringComparison.OrdinalIgnoreCase);
		}

		private Collection<SPWebApplication> GetDeployedApplications()
		{
			SPFarm farm = SPFarm.Local;
			SPSolution solution = farm.Solutions[InstallConfiguration.SolutionId];
			if (solution.ContainsWebApplicationResource)
			{
				return solution.DeployedWebApplications;
			}
			return null;
		}

		#endregion

		#region Command Classes

		/// <summary>
		/// The base class of all installation commands.
		/// </summary>
		private abstract class Command
		{
			private readonly InstallProcessControl parent;

			protected Command(InstallProcessControl parent)
			{
				this.parent = parent;
			}

			internal InstallProcessControl Parent
			{
				get { return parent; }
			}

			internal abstract string Description { get; }

			protected internal virtual bool Execute() { return true; }

			protected internal virtual bool Rollback() { return true; }
		}

		private class CommandList : List<Command>
		{
		}

		/// <summary>
		/// The base class of all SharePoint solution related commands.
		/// </summary>
		private abstract class SolutionCommand : Command
		{
			protected SolutionCommand(InstallProcessControl parent) : base(parent) { }

			protected void RemoveSolution()
			{
				try
				{
					SPFarm farm = SPFarm.Local;
					SPSolution solution = farm.Solutions[InstallConfiguration.SolutionId];
					if (solution != null)
					{
						if (!solution.Deployed)
						{
							solution.Delete();
						}
					}
				}

				catch (SqlException ex)
				{
					throw new InstallException(ex.Message, ex);
				}
			}
		}

		/// <summary>
		/// Command for adding the SharePoint solution.
		/// </summary>
		private class AddSolutionCommand : SolutionCommand
		{
			internal AddSolutionCommand(InstallProcessControl parent) : base(parent)
			{
			}

			internal override string Description
			{
				get
				{
					return CommonUIStrings.addSolutionCommand;
				}
			}

			protected internal override bool Execute()
			{
				string filename = InstallConfiguration.SolutionFile;
				filename = @"C:\Program Files\NextLabs\SharePoint Enforcer\solution\NextLabs.Entitlement.wsp";
				if (String.IsNullOrEmpty(filename))
				{
					throw new InstallException(CommonUIStrings.installExceptionConfigurationNoWsp);
				}

				try
				{
					SPFarm farm = SPFarm.Local;
					SPSolution solution = farm.Solutions.Add(filename);
					return true;
				}

				catch (SecurityException ex)
				{
					string message = CommonUIStrings.addSolutionAccessError;
					if (Environment.OSVersion.Version >= new Version("6.0"))
						message += " " + CommonUIStrings.addSolutionAccessErrorWinServer2008Solution;
					else
						message += " " + CommonUIStrings.addSolutionAccessErrorWinServer2003Solution;
					throw new InstallException(message, ex);
				}

				catch (IOException ex)
				{
					throw new InstallException(ex.Message, ex);
				}

				catch (ArgumentException ex)
				{
					throw new InstallException(ex.Message, ex);
				}

				catch (SqlException ex)
				{
					throw new InstallException(ex.Message, ex);
				}
			}

			protected internal override bool Rollback()
			{
				RemoveSolution();
				return true;
			}
		}

		/// <summary>
		/// Command for removing the SharePoint solution.
		/// </summary>
		private class RemoveSolutionCommand : SolutionCommand
		{
			internal RemoveSolutionCommand(InstallProcessControl parent) : base(parent) { }

			internal override string Description
			{
				get
				{
					return CommonUIStrings.removeSolutionCommand;
				}
			}

			protected internal override bool Execute()
			{
				RemoveSolution();
				return true;
			}
		}

		private abstract class JobCommand : Command
		{
			protected JobCommand(InstallProcessControl parent) : base(parent) { }

			protected static void RemoveExistingJob(SPSolution solution)
			{
				if (solution.JobStatus == SPRunningJobStatus.Initialized)
				{
					throw new InstallException(CommonUIStrings.installExceptionDuplicateJob);
				}

				SPJobDefinition jobDefinition = GetSolutionJob(solution);
				if (jobDefinition != null)
				{
					jobDefinition.Delete();
					Thread.Sleep(500);
				}
			}

			private static SPJobDefinition GetSolutionJob(SPSolution solution)
			{
				SPFarm localFarm = SPFarm.Local;
				SPTimerService service = localFarm.TimerService;
				foreach (SPJobDefinition definition in service.JobDefinitions)
				{
					if (definition.Title != null && definition.Title.Contains(solution.Name))
					{
						return definition;
					}
				}
				return null;
			}

			protected static DateTime GetImmediateJobTime()
			{
				// Min time is start from 1970-01-01
				return DateTime.Now - TimeSpan.FromDays(1);
				// return new DateTime(1970, 1, 1);
			}
		}

		/// <summary>
		/// Command for creating a deployment job.
		/// </summary>
		private class CreateDeploymentJobCommand : JobCommand
		{
			private readonly Collection<SPWebApplication> applications;

			internal CreateDeploymentJobCommand(InstallProcessControl parent, IList<SPWebApplication> applications) : base(parent)
			{
				if (applications != null)
				{
					this.applications = new Collection<SPWebApplication>();
					foreach (SPWebApplication application in applications)
					{
						this.applications.Add(application);
					}
				}
				else
				{
					this.applications = null;
				}
			}

			internal override string Description
			{
				get
				{
					return CommonUIStrings.createDeploymentJobCommand;
				}
			}

			protected internal override bool Execute()
			{
				try
				{
					SPSolution installedSolution = SPFarm.Local.Solutions[InstallConfiguration.SolutionId];

					//
					// Remove existing job, if any. 
					//
					if (installedSolution.JobExists)
					{
						RemoveExistingJob(installedSolution);
					}

					messageList.Add("***** SOLUTION DEPLOYMENT *****");
					if (installedSolution.ContainsWebApplicationResource && applications != null && applications.Count > 0)
					{
						installedSolution.Deploy(GetImmediateJobTime(), true, applications, true);
					}
					else
					{
						installedSolution.Deploy(GetImmediateJobTime(), true, true);
					}
#if SP2013
          string POWERSHELLPath = @"C:\Windows\System32\WindowsPowerShell\v1.0\PowerShell.exe";
          //Running commandline to install NextLabs.Entitlement.Basic (14 hive)feature to support when website is upgrade from sp2010. 
		  CommonLib.Prepare14FeatureFiles();
          string args = "";
          string SPEinstallPath = CommonLib.GetSPEIntalledPath();
          args = "-ExecutionPolicy ByPass & '" + SPEinstallPath + "bin\\InstallBasicFeatureFor2013Mixed.ps1' ";
          CommonLib.ExecuteCommand(POWERSHELLPath, args);
          args = "-ExecutionPolicy ByPass & '" + SPEinstallPath + "bin\\InstallEventReceiverFeatureFor2013Mixed.ps1' ";
          CommonLib.ExecuteCommand(POWERSHELLPath, args);
          
#endif
					return true;
				}

				catch (SPException ex)
				{
					throw new InstallException(ex.Message, ex);
				}

				catch (SqlException ex)
				{
					throw new InstallException(ex.Message, ex);
				}
			}

			protected internal override bool Rollback()
			{
				SPSolution installedSolution = SPFarm.Local.Solutions[InstallConfiguration.SolutionId];

				if (installedSolution != null)
				{
					//
					// Remove existing job, if any. 
					//
					if (installedSolution.JobExists)
					{
						RemoveExistingJob(installedSolution);
					}

					messageList.Add("***** SOLUTION RETRACTION *****");
					if (installedSolution.ContainsWebApplicationResource)
					{
						installedSolution.Retract(GetImmediateJobTime(), applications);
					}
					else
					{
						installedSolution.Retract(GetImmediateJobTime());
					}
				}

				return true;
			}
		}

		/// <summary>
		/// Command for creating an upgrade job.
		/// </summary>
		private class CreateUpgradeJobCommand : JobCommand
		{
			internal CreateUpgradeJobCommand(InstallProcessControl parent)
			  : base(parent)
			{
			}

			internal override string Description
			{
				get
				{
					return CommonUIStrings.createUpgradeJobCommand;
				}
			}

			protected internal override bool Execute()
			{
				try
				{
					string filename = InstallConfiguration.SolutionFile;
					filename = @"C:\Program Files\NextLabs\SharePoint Enforcer\solution\NextLabs.Entitlement.wsp";
					if (String.IsNullOrEmpty(filename))
					{
						throw new InstallException(CommonUIStrings.installExceptionConfigurationNoWsp);
					}

					SPSolution installedSolution = SPFarm.Local.Solutions[InstallConfiguration.SolutionId];

					//
					// Remove existing job, if any. 
					//
					if (installedSolution.JobExists)
					{
						RemoveExistingJob(installedSolution);
					}

					messageList.Add(CommonUIStrings.logUpgrade);
					installedSolution.Upgrade(filename, GetImmediateJobTime());
					return true;
				}

				catch (SqlException ex)
				{
					throw new InstallException(ex.Message, ex);
				}
			}
		}

		/// <summary>
		/// Command for creating a retraction job.
		/// </summary>
		private class CreateRetractionJobCommand : JobCommand
		{
			internal CreateRetractionJobCommand(InstallProcessControl parent) : base(parent)
			{
			}

			internal override string Description
			{
				get
				{
					return CommonUIStrings.createRetractionJobCommand;
				}
			}

			protected internal override bool Execute()
			{
				try
				{
					SPSolution installedSolution = SPFarm.Local.Solutions[InstallConfiguration.SolutionId];

					//
					// Remove existing job, if any. 
					//
					if (installedSolution.JobExists)
					{
						RemoveExistingJob(installedSolution);
					}

					if (installedSolution.Deployed)
					{
						messageList.Add(CommonUIStrings.logRetract);
						if (installedSolution.ContainsWebApplicationResource)
						{
							Collection<SPWebApplication> applications = installedSolution.DeployedWebApplications;
							installedSolution.Retract(GetImmediateJobTime(), applications);
						}
						else
						{
							installedSolution.Retract(GetImmediateJobTime());
						}
					}
					return true;
				}

				catch (SqlException ex)
				{
					throw new InstallException(ex.Message, ex);
				}
			}
		}


		private class WaitForJobCompletionCommand : Command
		{
			private bool m_bIsInstallCase = false;
			private int m_nWaitTimes = 0;
			private readonly string[] m_RuningFlags = new string[] { " --", " \\", " |", " /" };
			private readonly string m_descriptionBaseInfo = "";

			private string description;
			private DateTime startTime;
			private bool first = true;

			private InstallOperation m_op = InstallOperation.Uninstall;
			private InstallOptions m_opt = null;

			internal WaitForJobCompletionCommand(InstallProcessControl parent, string description, InstallOperation op, InstallOptions opt)
				: this(parent, description)
			{
				m_opt = opt;
				m_op = op;
			}
			internal WaitForJobCompletionCommand(InstallProcessControl parent, string description) : base(parent)
			{
				this.description = description;
				m_descriptionBaseInfo = description;

				if (m_descriptionBaseInfo == CommonUIStrings.waitForSolutionDeployment)
				{
					m_bIsInstallCase = true;
				}
			}

			internal override string Description
			{
				get
				{
					return description;
				}
			}

			protected internal override bool Execute()
			{
				try
				{
					SPSolution installedSolution = SPFarm.Local.Solutions[InstallConfiguration.SolutionId];

					if (first)
					{
						if (!installedSolution.JobExists) return true;
						startTime = DateTime.Now;
						first = false;
					}

					//
					// Wait for job to end
					//
					if (installedSolution.JobExists)
					{
						//if (DateTime.Now > startTime.Add(JobTimeout))
						//{
						//	throw new InstallException(CommonUIStrings.installExceptionTimeout);
						//}

						description = m_descriptionBaseInfo + m_RuningFlags[m_nWaitTimes % m_RuningFlags.Length];
						return false;
					}
					else
					{
						bool bPartlyDeployed = false;
#if SP2016 || SP2019
						if (m_op == InstallOperation.Install && installedSolution.DeployedWebApplications.Count != m_opt.WebApplicationTargets.Count)
						{
							bPartlyDeployed = true;
						}
#endif

						SPSolutionOperationResult result = installedSolution.LastOperationResult;
						if ((result != SPSolutionOperationResult.DeploymentSucceeded && result != SPSolutionOperationResult.RetractionSucceeded) || bPartlyDeployed)
						{
#if SP2016 || SP2019
							if (m_op == InstallOperation.Install && (result != SPSolutionOperationResult.DeploymentSucceeded || bPartlyDeployed))
							{
								Collection<SPWebApplication> app = new Collection<SPWebApplication>();
								foreach (SPWebApplication application in m_opt.WebApplicationTargets)
								{
									app.Add(application);
								}

								SPSolution solution = SPFarm.Local.Solutions[InstallConfiguration.SolutionId];
								solution.Deploy(DateTime.Now, true, app, true);

								bool jobexists = solution.JobExists;
								while (jobexists)
								{
									Thread.Sleep(1000);
									jobexists = solution.JobExists;
								}

								if (solution.Deployed)
								{
									messageList.Add(installedSolution.LastOperationDetails);
									return true;
								}
								else
								{
									throw new InstallException(installedSolution.LastOperationDetails);
								}
							}
							else
							{
#endif
								throw new InstallException(installedSolution.LastOperationDetails);
#if SP2016 || SP2019
							}
#endif
						}
						messageList.Add(installedSolution.LastOperationDetails);
						return true;
					}
				}
				catch (Exception ex)
				{
					throw new InstallException(ex.Message, ex);
				}
			}

			protected internal override bool Rollback()
			{
				SPSolution installedSolution = SPFarm.Local.Solutions[InstallConfiguration.SolutionId];

				//
				// Wait for job to end
				//
				if (installedSolution != null)
				{
					if (installedSolution.JobExists)
					{
						if (DateTime.Now > startTime.Add(JobTimeout))
						{
							throw new InstallException(CommonUIStrings.installExceptionTimeout);
						}
						return false;
					}
					else
					{
						messageList.Add(installedSolution.LastOperationDetails);
					}
				}

				return true;
			}
		}

		private abstract class FeatureCommand : Command
		{
			protected FeatureCommand(InstallProcessControl parent) : base(parent) { }

			// Modif JPI - 
			protected static void DeactivateFeature(List<Guid?> featureIds)
			{
				try
				{
					if (featureIds != null && featureIds.Count > 0)
					{
						foreach (Guid? featureId in featureIds)
						{
							if (featureId != null)
							{
								SPFeature feature = SPWebService.AdministrationService.Features[featureId.Value];
								if (feature != null)
								{
									SPWebService.AdministrationService.Features.Remove(featureId.Value);
								}
							}
						}
					}
				}

				catch (ArgumentException ex)  // Missing assembly in GAC
				{
				}

				catch (InvalidOperationException ex)  // Missing receiver class
				{
				}

				catch (SqlException ex)
				{
					throw new InstallException(ex.Message, ex);
				}
			}
			// Modif JPI - Fin
		}

		private class ActivateFarmFeatureCommand : FeatureCommand
		{
			internal ActivateFarmFeatureCommand(InstallProcessControl parent) : base(parent) { }

			internal override string Description
			{
				get
				{
					// Modif JPI
					return CommonUIStrings.activateFarmFeatureCommand;
					// Modif JPI - Fin
				}
			}

			protected internal override bool Execute()
			{
				try
				{
					// Modif JPI 
					List<Guid?> featureIds = InstallConfiguration.FeatureId;
					if (featureIds != null && featureIds.Count > 0)
					{
						foreach (Guid? featureId in featureIds)
						{
							if (featureId != null)
							{
								SPFeature feature = SPWebService.AdministrationService.Features.Add(featureId.Value, true);
							}
						}
					}
					return true;
					// Modif JPI - Fin
				}

				catch (Exception ex)
				{
					throw new InstallException(ex.Message, ex);
				}
			}

			protected internal override bool Rollback()
			{
				DeactivateFeature(InstallConfiguration.FeatureId);
				return true;
			}
		}

		private class DeactivateFarmFeatureCommand : FeatureCommand
		{
			internal DeactivateFarmFeatureCommand(InstallProcessControl parent) : base(parent) { }

			internal override string Description
			{
				get
				{
					// Modif JPI - 
					return CommonUIStrings.deactivateFarmFeatureCommand;
					// Modif JPI - Fin
				}
			}

			protected internal override bool Execute()
			{
				try
				{
					// Modif JPI - 
					List<Guid?> featureIds = InstallConfiguration.FeatureId;
					if (featureIds != null && featureIds.Count > 0)
					{
						foreach (Guid? featureId in featureIds)
						{
							if (featureId != null && SPWebService.AdministrationService.Features[featureId.Value] != null)
							{
								SPWebService.AdministrationService.Features.Remove(featureId.Value);
							}
						}
					}

					return true;
					// Modif JPI - Fin
				}

				catch (Exception ex)
				{
					messageList.Add(ex.Message);
					messageList.Add(ex.ToString());
				}

				return true;
			}
		}

		private abstract class SiteCollectionFeatureCommand : Command
		{
			internal SiteCollectionFeatureCommand(InstallProcessControl parent) : base(parent) { }

			// Modif JPI - 
			protected static void DeactivateFeature(IList<SPSite> siteCollections, List<Guid?> featureIds)
			{
				try
				{
					if (siteCollections != null && featureIds != null && featureIds.Count > 0)
					{
						messageList.Add(CommonUIStrings.logFeatureDeactivate);
						foreach (SPSite siteCollection in siteCollections)
						{
							foreach (Guid? featureId in featureIds)
							{
								if (featureId == null) continue;

								SPFeature feature = siteCollection.Features[featureId.Value];
								if (feature == null) continue;

								siteCollection.Features.Remove(featureId.Value);
							}
						}
					}
				}

				catch (ArgumentException ex)  // Missing assembly in GAC
				{
				}

				catch (InvalidOperationException ex)  // Missing receiver class
				{
				}

				catch (SqlException ex)
				{
					throw new InstallException(ex.Message, ex);
				}
			}
			// Modif JPI - Fin
		}

		private class ActivateSiteCollectionFeatureCommand : SiteCollectionFeatureCommand
		{
			private readonly IList<SPSite> siteCollections;

			internal ActivateSiteCollectionFeatureCommand(InstallProcessControl parent, IList<SPSite> siteCollections) : base(parent)
			{
				this.siteCollections = siteCollections;
			}

			internal override string Description
			{
				// Modif JPI - 
				get
				{
					return String.Format(CommonUIStrings.activateSiteCollectionFeatureCommand, siteCollections.Count, siteCollections.Count == 1 ? String.Empty : "s");
				}
				// Modif JPI - Fin
			}

			protected internal override bool Execute()
			{
				try
				{
					// Modif JPI - 
					List<Guid?> featureIds = InstallConfiguration.FeatureId;
					if (siteCollections != null && featureIds != null && featureIds.Count > 0)
					{
						messageList.Add(CommonUIStrings.logFeatureActivate);
						foreach (SPSite siteCollection in siteCollections)
						{
							foreach (Guid? featureId in featureIds)
							{
								if (featureId == null) continue;

								SPFeature feature = siteCollection.Features.Add(featureId.Value, true);
							}
						}
					}

					return true;
					// Modif JPI - Fin
				}

				catch (Exception ex)
				{
					throw new InstallException(ex.Message, ex);
				}
			}

			protected internal override bool Rollback()
			{
				DeactivateFeature(siteCollections, InstallConfiguration.FeatureId);
				return true;
			}
		}

		private class DeactivateSiteCollectionFeatureCommand : SiteCollectionFeatureCommand
		{
			private List<SPSite> deactivatedSiteCollections;

			internal DeactivateSiteCollectionFeatureCommand(InstallProcessControl parent)
			  : base(parent)
			{
				deactivatedSiteCollections = new List<SPSite>();
			}

			public List<SPSite> DeactivatedSiteCollections
			{
				get { return deactivatedSiteCollections; }
			}

			internal override string Description
			{
				get { return CommonUIStrings.deactivateSiteCollectionFeatureCommand; }
			}

			protected internal override bool Execute()
			{
				try
				{
					List<Guid?> featureIds = InstallConfiguration.FeatureId;

					SPFarm farm = SPFarm.Local;
					SPSolution solution = farm.Solutions[InstallConfiguration.SolutionId];
					if (solution != null && solution.Deployed && featureIds != null && featureIds.Count > 0)
					{
						messageList.Add(CommonUIStrings.logFeatureDeactivate);

						//
						// LFN - Stopped using solution.DeployedWebApplications as it seems to produced a FormatException 
						// when created a new Guid value. Looks like a bug in SharePoint that we cannot do anything about. 
						// I have therefore adopted a new strategy by looping through all Web applications.
						//

						foreach (SPWebApplication webApp in SPWebService.AdministrationService.WebApplications)
						{
							DeactivateFeatures(webApp);
						}

						foreach (SPWebApplication webApp in SPWebService.ContentService.WebApplications)
						{
							DeactivateFeatures(webApp);
						}
					}
				}

				catch (Exception ex)
				{
					messageList.Add(ex.Message);
					messageList.Add(ex.ToString());
				}

				return true;
			}

			private void DeactivateFeatures(SPWebApplication webApp)
			{
				List<Guid?> featureIds = InstallConfiguration.FeatureId;

				foreach (SPSite siteCollection in webApp.Sites)
				{
					foreach (Guid? featureId in featureIds)
					{
						if (featureId == null) continue;
						if (siteCollection.Features[featureId.Value] == null) continue;
						// LFN - Just deactivate the feature right away. No need to use intermidate step with local dictionary.
						siteCollection.Features.Remove(featureId.Value);

						// KML - not sure why JPI used this local dictionary
						//       instead of doing "deactivatedSiteCollections.Add(siteCollection)" here
						// LFN - Agree no need to do this. Works just fine by deactivating directly.
						//_dicDeactivatedSiteCollections[siteCollection.Url] = siteCollection;
					}

					// LFN - It is a memory and resource leak to forget this! See http://msdn.microsoft.com/en-us/library/aa973248.aspx
					// LFN - Well, they might be disposed by the system when the installer process dies. But I think it is good coding
					// practice never to forget this. 
					siteCollection.Dispose();
				}
			}
		}

		/// <summary>
		/// Command that registers the version number of a solution.
		/// </summary>
		private class RegisterVersionNumberCommand : Command
		{
			private Version oldVersion;

			internal RegisterVersionNumberCommand(InstallProcessControl parent) : base(parent) { }

			internal override string Description
			{
				get
				{
					return CommonUIStrings.registerVersionNumberCommand;
				}
			}

			protected internal override bool Execute()
			{
				oldVersion = InstallConfiguration.InstalledVersion;
				InstallConfiguration.InstalledVersion = InstallConfiguration.SolutionVersion;
				return true;
			}

			protected internal override bool Rollback()
			{
				InstallConfiguration.InstalledVersion = oldVersion;
				return true;
			}
		}

		/// <summary>
		/// Command that unregisters the version number of a solution.
		/// </summary>
		private class UnregisterVersionNumberCommand : Command
		{
			internal UnregisterVersionNumberCommand(InstallProcessControl parent) : base(parent) { }

			internal override string Description
			{
				get
				{
					return CommonUIStrings.unregisterVersionNumberCommand;
				}
			}

			protected internal override bool Execute()
			{
				InstallConfiguration.InstalledVersion = null;
				return true;
			}
		}

		/// <summary>
		/// Command that reinstall wsp package and restore features.
		/// </summary>
		private class UpgradeRestoreCommand : Command
		{
			private List<SPWebApplication> selectWebApps;
			List<string> selectedSites;
			internal UpgradeRestoreCommand(InstallProcessControl parent, Collection<SPWebApplication> webApps) : base(parent)
			{
				selectWebApps = CommonLib.RemoveBasicFeature(webApps);
			}
			internal override string Description
			{
				get
				{
					return CommonUIStrings.upgradeRestoreSubTitile;
				}
			}

			protected internal override bool Execute()
			{
				CommonLib.UpgradeRestore(selectWebApps);
				return true;
			}
		}

		#endregion
	}
}

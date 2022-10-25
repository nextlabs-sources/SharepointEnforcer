using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SharePoint.Administration;
using System.Threading;
using System.IO;

namespace NextLabs.Diagnostic.RegCategory
{
    /// <summary>
    /// Represents diagnostics manager exposing two sample levels through SharePoint diagnostic logging service
    /// </summary>
    public class NextlabsDiagnosticsManager : BaseDiagnosticsManager
    {
        public const string ServiceId = "CE DiagnosticsManager";   // Identifier of the service

        public const string CategoryId = "Compliant Enterprise";     // Identifier of the first sample category

        /// <summary>
        /// Retrieve the instance of NextlabsDiagnosticsManager persisted to the specified farm configuration database
        /// </summary>
        /// <param name="farm">A SharePoint farm</param>
        /// <returns>The instance of NextlabsDiagnosticsManager persisted to the specified farm configuration database</returns>
        public static NextlabsDiagnosticsManager GetLocal(SPFarm farm)
        {
            return farm.Services.GetValue<NextlabsDiagnosticsManager>(ServiceId);
        }

        /// <summary>
        /// Creates a new instance of the NextlabsDiagnosticsManager object
        /// </summary>
        public NextlabsDiagnosticsManager()
            : base(ServiceId)
        {
            RegisterCategory(CategoryId);
        }

        protected override BaseDiagnosticsManager GetCurrentOnFarm(SPFarm farm)
        {
            return NextlabsDiagnosticsManager.GetLocal(farm);
        }
    }
}

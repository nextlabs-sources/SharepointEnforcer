using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SharePoint.Administration;
using System.Reflection;

// using obsolete object "IDiagnosticsManager" and "IDiagnosticsLevel".
#pragma warning disable 618

namespace NextLabs.Diagnostic.RegCategory
{

    /// <summary>
    /// Base class for diagnostics levels manager persisted to SharePoint configuration database
    /// </summary>
    public abstract class BaseDiagnosticsManager : SPService, IDiagnosticsManager
    {

        /// <summary>
        /// Persisted diagnostics level
        /// </summary>
        protected class DiagnosticsLevel : SPAutoSerializingObject, IDiagnosticsLevel
        {
            private string _title = null;

            /// <summary>
            /// Parent diagnostics manager
            /// </summary>
            public BaseDiagnosticsManager Manager
            {
                get { return _manager; }
                set { _manager = value; }
            } private BaseDiagnosticsManager _manager = null;

            /// <summary>
            /// Diagnostics level is hidden
            /// </summary>
            public bool Hidden
            {
                get { return false; }
            }

            /// <summary>
            /// Identifier of the diagnostics level
            /// </summary>
            public string Id
            {
                get { return _title; }
                set { _title = value; }
            }

            /// <summary>
            /// Name of the diagnostics level
            /// </summary>
            public string Name
            {
                get { return _title; }
            }

            [Persisted]
            private TraceSeverity _traceSeverity;

            /// <summary>
            /// Trace severity
            /// </summary>
            public TraceSeverity TraceSeverity
            {
                get { return _traceSeverity; }
                set
                {
                    _traceSeverity = value;
                    _manager.NeedsUpdate = true;
                }
            }

            [Persisted]
            private EventSeverity _eventSeverity;

            /// <summary>
            /// Event severity
            /// </summary>
            public EventSeverity EventSeverity
            {
                get { return _eventSeverity; }
                set
                {
                    _eventSeverity = value;
                    _manager.NeedsUpdate = true;
                }
            }
        }

        // Collection of references for sub-categories
        private Dictionary<string, DiagnosticsLevel> _categories = null;

        /// <summary>
        /// Determines if diagnostics manager has to be updated
        /// </summary>
        public bool NeedsUpdate
        {
            get { return needsUpdate; }
            set { needsUpdate = value; }
        } private bool needsUpdate = false;

        /// <summary>
        /// Retrieve the instance of BaseDiagnosticsManager stored in the config DB of the given farm
        /// </summary>
        /// <param name="farm"></param>
        /// <returns></returns>
        protected abstract BaseDiagnosticsManager GetCurrentOnFarm(SPFarm farm);

        /// <summary>
        /// Creates a new instance of the class BaseDiagnosticsManager
        /// </summary>
        /// <param name="id">Identifier of the object in the configuration database</param>
        public BaseDiagnosticsManager(string id)
            : base(id, SPFarm.Local)
        {
            _categories = new Dictionary<string, DiagnosticsLevel>();
        }

        /// <summary>
        /// Registers a diagnostics category to persist to the configuration database
        /// </summary>
        /// <param name="name"></param>
        public void RegisterCategory(string categoryName)
        {
            DiagnosticsLevel category = new DiagnosticsLevel();

            category.Id = categoryName;
            category.Manager = this;

            _categories.Add(categoryName, category);
        }

        public void UnRegisterCategory(string categoryName)
        {
            _categories.Remove(categoryName);
        }

        /// <summary>
        /// Called immediately after the base class deserializes itself to do additional work
        /// </summary>
        protected override void OnDeserialization()
        {

            // Apply persisted state to each registered categories
            foreach (DiagnosticsLevel category in _categories.Values)
            {
                DiagnosticsLevel persitedCategory = this.Properties[category.Id] as DiagnosticsLevel;

                if (persitedCategory != null)
                {
                    category.EventSeverity = persitedCategory.EventSeverity;
                    category.TraceSeverity = persitedCategory.TraceSeverity;
                }
            }

            base.OnDeserialization();
        }

        /// <summary>
        /// Updates SharePoint config with any changes made to the object
        /// </summary>
        public override void Update()
        {

            // Update base object only if needed or if the manager is being registered
            if (needsUpdate || GetCurrentOnFarm(SPFarm.Local) == null)
            {

                // Exit if an existing diagnostics time job is running (required for web farm mode compliance)
                SPJobDefinition diagnosticsServiceJob = SPFarm.Local.TimerService.JobDefinitions["DiagnosticsServiceTimerJobDefinition"];
                if (diagnosticsServiceJob != null && diagnosticsServiceJob.Status == SPObjectStatus.Online)
                {
                    return;
                }

                // Update each category
                foreach (DiagnosticsLevel category in _categories.Values)
                {
                    this.Properties[category.Id] = category;
                }
								//To fix bug 585,catch multiply update call exception, added by William
								try
								{
									base.Update();
								}
								catch
								{
									
								}                
                needsUpdate = false;
            }
        }

        /// <summary>
        /// Gets the specified trace log category
        /// </summary>
        /// <param name="name">The name of the trace log category to return</param>
        /// <returns>Returns a IDiagnosticsLevel class that represents the trace log category</returns>
        public IDiagnosticsLevel GetItem(string name)
        {
            return _categories[name];
        }

        /// <summary>
        /// Gets a collection of all trace log categories for the application
        /// </summary>
        /// <returns>Returns an enumerable collection of IDiagnosticsLevel objects</returns>
        public IEnumerable<IDiagnosticsLevel> GetItems()
        {
            List<IDiagnosticsLevel> items = new List<IDiagnosticsLevel>();

            foreach (DiagnosticsLevel category in _categories.Values)
            {
                items.Add(category);
            }

            return items;
        }

        /// <summary>
        /// Resets all trace log categories defined for the application to their default event and trace severities
        /// </summary>
        public void ResetAll()
        {
            foreach (IDiagnosticsLevel item in this.GetItems())
            {
                item.EventSeverity = EventSeverity.None;
                item.TraceSeverity = TraceSeverity.None;
            }
        }

        /// <summary>
        /// Resets the specified trace log category to its default event and trace severities
        /// </summary>
        /// <param name="item">The trace log category to reset</param>
        public void ResetItem(IDiagnosticsLevel item)
        {
            item.EventSeverity = EventSeverity.None;
            item.TraceSeverity = TraceSeverity.None;
        }

        /// <summary>
        /// Sets all trace log categories defined for the application to the specified trace severity
        /// </summary>
        /// <param name="traceSeverity">The trace severity to assign to all the trace log categories defined for the application</param>
        public void SetAll(TraceSeverity traceSeverity)
        {
            foreach (IDiagnosticsLevel item in this.GetItems())
            {
                item.TraceSeverity = traceSeverity;
            }
        }

        /// <summary>
        /// Sets all trace log categories defined for the application to the specified event and trace severities
        /// </summary>
        /// <param name="traceSeverity">The trace severity to assign to all the trace log categories defined for the application</param>
        /// <param name="eventSeverity">The event severity to assign to all the trace log categories defined for the application</param>
        public void SetAll(TraceSeverity traceSeverity, EventSeverity eventSeverity)
        {
            foreach (IDiagnosticsLevel item in this.GetItems())
            {
                item.EventSeverity = eventSeverity;
                item.TraceSeverity = traceSeverity;
            }
        }
    }
}

#pragma warning restore 618
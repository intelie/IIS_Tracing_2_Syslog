﻿namespace iisTracing2syslog
{
	partial class IISTracing2Syslog
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.eventLog = new System.Diagnostics.EventLog();
            ((System.ComponentModel.ISupportInitialize)(this.eventLog)).BeginInit();
            // 
            // IISTracing2Syslog
            // 
            this.ServiceName = "IISTracing2Syslog";
            ((System.ComponentModel.ISupportInitialize)(this.eventLog)).EndInit();

		}

        #endregion

        protected System.Diagnostics.EventLog eventLog;
    }
}

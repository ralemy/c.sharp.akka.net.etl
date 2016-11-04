namespace mv_impinj
{
    partial class ConnectorService
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
            this.connectorInstaller1 = new mv_impinj.ConnectorInstaller();
            // 
            // connectorInstaller1
            // 
            this.connectorInstaller1.Description = "Connector Service to send RAIN RFID data from Impinj platform to MobileView Gener" +
    "ic Gateway";
            this.connectorInstaller1.DisplayName = "MobileView Impinj Connector";
            this.connectorInstaller1.ServiceName = "mv_impinj_connector";
            // 
            // ConnectorService
            // 
            this.ServiceName = "ConnectorService";

        }

        #endregion

        private ConnectorInstaller connectorInstaller1;
    }
}
